using GraphEditor.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphEditor.Core.Models
{
    public class GraphModel : GraphModelBase
    {
        private readonly Dictionary<string, IVertex> _verticesStorage = new();
        private readonly Dictionary<string, IEdge> _edgesStorage = new();
        private readonly Dictionary<string, HashSet<string>> _adjacencyList = new();
        private readonly Dictionary<string, HashSet<string>> _edgeConnections = new();
        private int _edgeCounter = 0;

        private Dictionary<string, HashSet<string>>? _outgoingEdgesCache = null;
        private bool _cacheInvalid = true;

        private class MutableVertex : IVertex
        {
            public string Id { get; }
            public string? Label { get; set; }
            public IReadOnlyDictionary<string, object>? Data { get; set; }

            public MutableVertex(string id, string? label)
            {
                Id = id ?? throw new ArgumentNullException(nameof(id));
                Label = label;
            }
        }

        private class MutableEdge : IEdge
        {
            public string Id { get; }
            public string Source { get; }
            public string Target { get; }
            public double? Weight { get; set; }
            public double? Capacity { get; set; }
            public IReadOnlyDictionary<string, object>? Data { get; set; }

            public MutableEdge(string id, string source, string target, double? weight, double? capacity)
            {
                Id = id ?? throw new ArgumentNullException(nameof(id));
                Source = source ?? throw new ArgumentNullException(nameof(source));
                Target = target ?? throw new ArgumentNullException(nameof(target));
                Weight = weight;
                Capacity = capacity;
            }
        }

        public GraphModel(bool isDirected = false, bool allowParallelEdges = true, bool allowSelfLoops = true)
        {
            IsDirected = isDirected;
            AllowParallelEdges = allowParallelEdges;
            AllowSelfLoops = allowSelfLoops;

            Vertices = new Dictionary<string, IVertex>(_verticesStorage);
            Edges = new Dictionary<string, IEdge>(_edgesStorage);
        }

        public override bool AddVertex(string id, string? label = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Vertex ID cannot be null or whitespace", nameof(id));

            if (_verticesStorage.ContainsKey(id))
                return false;

            var vertex = new MutableVertex(id, label);

            _verticesStorage.Add(id, vertex);
            _adjacencyList.Add(id, new HashSet<string>());

            if (!IsDirected)
                _edgeConnections.Add(id, new HashSet<string>());

            Vertices = new Dictionary<string, IVertex>(_verticesStorage);
            InvalidateCache();
            RaiseChanged();

            return true;
        }

        public override bool RemoveVertex(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Vertex ID cannot be null or whitespace", nameof(id));

            if (!_verticesStorage.ContainsKey(id))
                return false;

            var edgesToRemove = _edgesStorage
                .Where(e => e.Value.Source == id || e.Value.Target == id)
                .Select(e => e.Key)
                .ToList();

            foreach (var edgeId in edgesToRemove)
                RemoveEdgeInternal(edgeId);

            _verticesStorage.Remove(id);
            _adjacencyList.Remove(id);

            if (!IsDirected)
            {
                _edgeConnections.Remove(id);
                foreach (var neighborId in _edgeConnections.Keys.ToList())
                    _edgeConnections[neighborId].Remove(id);
            }

            Vertices = new Dictionary<string, IVertex>(_verticesStorage);
            Edges = new Dictionary<string, IEdge>(_edgesStorage);
            InvalidateCache();
            RaiseChanged();

            return true;
        }

        public override bool AddEdge(string id, string source, string target, double? weight = null, double? capacity = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Edge ID cannot be null or whitespace", nameof(id));

            if (!_verticesStorage.ContainsKey(source) || !_verticesStorage.ContainsKey(target))
                return false;

            if (source == target && !AllowSelfLoops)
                return false;

            if (!AllowParallelEdges && !CheckParallelEdges(source, target))
                return false;

            string finalId = string.IsNullOrEmpty(id) ? $"edge_{_edgeCounter++}" : id;

            if (_edgesStorage.ContainsKey(finalId))
                return false;

            var edge = new MutableEdge(finalId, source, target, weight, capacity);

            _edgesStorage.Add(finalId, edge);
            _adjacencyList[source].Add(target);

            if (!IsDirected)
            {
                _adjacencyList[target].Add(source);
                _edgeConnections[source].Add(target);
                _edgeConnections[target].Add(source);
            }

            Edges = new Dictionary<string, IEdge>(_edgesStorage);
            InvalidateCache();
            RaiseChanged();

            return true;
        }

        private bool CheckParallelEdges(string source, string target)
        {
            if (IsDirected)
                return !_adjacencyList[source].Contains(target);
            else
                return !(_edgeConnections.TryGetValue(source, out var sourceConns) && sourceConns.Contains(target)) &&
                       !(_edgeConnections.TryGetValue(target, out var targetConns) && targetConns.Contains(source));
        }

        public override bool RemoveEdge(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Edge ID cannot be null or whitespace", nameof(id));

            if (!RemoveEdgeInternal(id))
                return false;

            Edges = new Dictionary<string, IEdge>(_edgesStorage);
            InvalidateCache();
            RaiseChanged();

            return true;
        }

        private bool RemoveEdgeInternal(string edgeId)
        {
            if (!_edgesStorage.TryGetValue(edgeId, out var edge))
                return false;

            var source = edge.Source;
            var target = edge.Target;

            _edgesStorage.Remove(edgeId);
            _adjacencyList[source].Remove(target);

            if (!IsDirected)
            {
                _adjacencyList[target].Remove(source);
                _edgeConnections[source].Remove(target);
                _edgeConnections[target].Remove(source);
            }

            return true;
        }

        public override bool SetVertexLabel(string id, string? label)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Vertex ID cannot be null or whitespace", nameof(id));

            if (!_verticesStorage.TryGetValue(id, out var vertex))
                return false;

            if (vertex is not MutableVertex mutableVertex)
            {
                mutableVertex = new MutableVertex(id, label) { Data = vertex.Data };
                _verticesStorage[id] = mutableVertex;
            }
            else
            {
                mutableVertex.Label = label;
            }

            Vertices = new Dictionary<string, IVertex>(_verticesStorage);
            RaiseChanged();

            return true;
        }

        public override bool SetEdgeWeight(string id, double? weight)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Edge ID cannot be null or whitespace", nameof(id));

            if (!_edgesStorage.TryGetValue(id, out var edge))
                return false;

            if (edge is not MutableEdge mutableEdge)
            {
                mutableEdge = new MutableEdge(id, edge.Source, edge.Target, weight, edge.Capacity)
                { Data = edge.Data };
                _edgesStorage[id] = mutableEdge;
            }
            else
            {
                mutableEdge.Weight = weight;
            }

            Edges = new Dictionary<string, IEdge>(_edgesStorage);
            RaiseChanged();

            return true;
        }

        public override bool SetEdgeCapacity(string id, double? capacity)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Edge ID cannot be null or whitespace", nameof(id));

            if (!_edgesStorage.TryGetValue(id, out var edge))
                return false;

            if (edge is not MutableEdge mutableEdge)
            {
                mutableEdge = new MutableEdge(id, edge.Source, edge.Target, edge.Weight, capacity)
                { Data = edge.Data };
                _edgesStorage[id] = mutableEdge;
            }
            else
            {
                mutableEdge.Capacity = capacity;
            }

            Edges = new Dictionary<string, IEdge>(_edgesStorage);
            RaiseChanged();

            return true;
        }

        public override bool TryGetVertex(string id, out IVertex? v)
        {
            var success = _verticesStorage.TryGetValue(id, out var vertex);
            v = vertex;
            return success;
        }

        public override bool TryGetEdge(string id, out IEdge? e)
        {
            var success = _edgesStorage.TryGetValue(id, out var edge);
            e = edge;
            return success;
        }

        public override IReadOnlyCollection<string> GetNeighbors(string vertexId)
        {
            if (string.IsNullOrWhiteSpace(vertexId))
                throw new ArgumentException("Vertex ID cannot be null or whitespace", nameof(vertexId));

            if (!_verticesStorage.ContainsKey(vertexId))
                return Array.Empty<string>();

            if (!IsDirected)
            {
                return _adjacencyList.TryGetValue(vertexId, out var neighbors)
                    ? neighbors.ToList().AsReadOnly()
                    : Array.Empty<string>();
            }

            if (_cacheInvalid || _outgoingEdgesCache == null)
                UpdateOutgoingEdgesCache();

            return _outgoingEdgesCache!.TryGetValue(vertexId, out var cachedNeighbors)
                ? cachedNeighbors.ToList().AsReadOnly()
                : Array.Empty<string>();
        }

        public override double[,] BuildAdjacencyMatrix(bool useWeights = true)
        {
            int vertexCount = _verticesStorage.Count;

            if (vertexCount == 0)
                return new double[0, 0];

            var sortedVertices = _verticesStorage.Keys.OrderBy(id => id).ToList();
            var vertexIndexMap = new Dictionary<string, int>();

            for (int i = 0; i < sortedVertices.Count; i++)
                vertexIndexMap[sortedVertices[i]] = i;

            double[,] matrix = new double[vertexCount, vertexCount];

            for (int i = 0; i < vertexCount; i++)
                for (int j = 0; j < vertexCount; j++)
                    matrix[i, j] = useWeights ? double.PositiveInfinity : 0.0;

            foreach (var edge in _edgesStorage.Values)
            {
                int sourceIndex = vertexIndexMap[edge.Source];
                int targetIndex = vertexIndexMap[edge.Target];

                double value = useWeights ? (edge.Weight ?? 1.0) : 1.0;
                matrix[sourceIndex, targetIndex] = value;

                if (!IsDirected && sourceIndex != targetIndex)
                    matrix[targetIndex, sourceIndex] = value;
            }

            if (AllowSelfLoops)
            {
                foreach (var edge in _edgesStorage.Values.Where(e => e.Source == e.Target))
                {
                    int vertexIndex = vertexIndexMap[edge.Source];
                    double value = useWeights ? (edge.Weight ?? 1.0) : 1.0;
                    matrix[vertexIndex, vertexIndex] = value;
                }
            }

            return matrix;
        }

        public override int[,] BuildIncidenceMatrix()
        {
            int vertexCount = _verticesStorage.Count;
            int edgeCount = _edgesStorage.Count;

            if (vertexCount == 0 || edgeCount == 0)
                return new int[0, 0];

            var sortedVertices = _verticesStorage.Keys.OrderBy(id => id).ToList();
            var sortedEdges = _edgesStorage.Keys.OrderBy(id => id).ToList();

            var vertexIndexMap = new Dictionary<string, int>();
            var edgeIndexMap = new Dictionary<string, int>();

            for (int i = 0; i < sortedVertices.Count; i++)
                vertexIndexMap[sortedVertices[i]] = i;

            for (int i = 0; i < sortedEdges.Count; i++)
                edgeIndexMap[sortedEdges[i]] = i;

            int[,] matrix = new int[vertexCount, edgeCount];

            foreach (var edge in _edgesStorage.Values)
            {
                int edgeIndex = edgeIndexMap[edge.Id];
                int sourceIndex = vertexIndexMap[edge.Source];
                int targetIndex = vertexIndexMap[edge.Target];

                if (IsDirected)
                {
                    matrix[sourceIndex, edgeIndex] = -1;
                    matrix[targetIndex, edgeIndex] = 1;
                }
                else
                {
                    matrix[sourceIndex, edgeIndex] = 1;
                    matrix[targetIndex, edgeIndex] = 1;

                    if (edge.Source == edge.Target)
                        matrix[sourceIndex, edgeIndex] = 2;
                }
            }

            return matrix;
        }

        private void UpdateOutgoingEdgesCache()
        {
            if (!IsDirected)
                return;

            _outgoingEdgesCache = new Dictionary<string, HashSet<string>>();

            foreach (var vertexId in _verticesStorage.Keys)
                _outgoingEdgesCache[vertexId] = new HashSet<string>();

            foreach (var edge in _edgesStorage.Values)
            {
                if (_outgoingEdgesCache.TryGetValue(edge.Source, out var neighbors))
                    neighbors.Add(edge.Target);
            }

            _cacheInvalid = false;
        }

        private void InvalidateCache()
        {
            _cacheInvalid = true;
        }
    }
}