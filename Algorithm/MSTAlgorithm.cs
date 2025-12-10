using GraphEditor.Core;
using GraphEditor.Visual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace GraphEditor.Algorithms
{
    public class MSTAlgorithm
    {
        public string Name => "Kruskal's Algorithm";
        public string Description => "Finds Minimum Spanning Tree in an undirected graph";

        public AlgorithmResult Execute(IGraphModel graph)
        {
            var result = new AlgorithmResult();

            try
            {
                // 1. Проверка, что граф неориентированный
                if (graph.IsDirected)
                {
                    result.Success = false;
                    result.Message = "MST algorithms require undirected graphs";
                    return result;
                }

                // 2. Проверка связности
                if (!IsGraphConnected(graph))
                {
                    result.Success = false;
                    result.Message = "Graph is not connected. MST requires connected graph.";
                    return result;
                }

                // 3. Выполнение алгоритма Крускала
                var mstEdges = Kruskal(graph);
                var totalWeight = mstEdges.Sum(e => e.Weight ?? 1.0);

                // 4. Формирование результата
                result.Success = true;
                result.Data["mstEdges"] = mstEdges.Select(e => e.Id).ToList();
                result.Data["totalWeight"] = totalWeight;
                result.Data["edgeCount"] = mstEdges.Count;
                result.Data["vertexCount"] = graph.Vertices.Count;
                result.Message = $"MST found with {mstEdges.Count} edges and total weight {totalWeight:F2}";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error executing Kruskal: {ex.Message}";
            }

            return result;
        }

        private bool IsGraphConnected(IGraphModel graph)
        {
            if (graph.Vertices.Count == 0)
                return false;

            var visited = new HashSet<string>();
            var stack = new Stack<string>();

            // Начинаем с первой вершины
            string startVertex = graph.Vertices.Keys.First();
            stack.Push(startVertex);

            while (stack.Count > 0)
            {
                string current = stack.Pop();
                if (visited.Contains(current))
                    continue;

                visited.Add(current);

                foreach (var neighbor in graph.GetNeighbors(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        stack.Push(neighbor);
                    }
                }
            }

            return visited.Count == graph.Vertices.Count;
        }

        private List<IEdge> Kruskal(IGraphModel graph)
        {
            // 1. Сортируем рёбра по весу
            var edges = graph.Edges.Values
                .Where(e => e.Source != e.Target) // Игнорируем петли
                .OrderBy(e => e.Weight ?? 1.0)
                .ToList();

            // 2. Инициализируем DSU (Disjoint Set Union)
            var parent = new Dictionary<string, string>();
            var rank = new Dictionary<string, int>();

            foreach (var vertexId in graph.Vertices.Keys)
            {
                parent[vertexId] = vertexId;
                rank[vertexId] = 0;
            }

            // 3. Проходим по рёбрам и строим MST
            var mstEdges = new List<IEdge>();

            foreach (var edge in edges)
            {
                string root1 = Find(parent, edge.Source);
                string root2 = Find(parent, edge.Target);

                if (root1 != root2)
                {
                    mstEdges.Add(edge);
                    Union(parent, rank, root1, root2);

                    // Если собрали достаточно рёбер, выходим
                    if (mstEdges.Count == graph.Vertices.Count - 1)
                        break;
                }
            }

            return mstEdges;
        }

        private string Find(Dictionary<string, string> parent, string vertex)
        {
            if (parent[vertex] != vertex)
            {
                parent[vertex] = Find(parent, parent[vertex]); // Path compression
            }
            return parent[vertex];
        }

        private void Union(Dictionary<string, string> parent, Dictionary<string, int> rank, string x, string y)
        {
            string rootX = Find(parent, x);
            string rootY = Find(parent, y);

            if (rootX == rootY)
                return;

            // Union by rank
            if (rank[rootX] < rank[rootY])
            {
                parent[rootX] = rootY;
            }
            else if (rank[rootX] > rank[rootY])
            {
                parent[rootY] = rootX;
            }
            else
            {
                parent[rootY] = rootX;
                rank[rootX]++;
            }
        }

        // Метод для алгоритма Прима (альтернатива)
        public AlgorithmResult ExecutePrim(IGraphModel graph, string startVertex = null)
        {
            var result = new AlgorithmResult();

            try
            {
                if (graph.IsDirected)
                {
                    result.Success = false;
                    result.Message = "Prim's algorithm requires undirected graphs";
                    return result;
                }

                if (startVertex == null)
                {
                    startVertex = graph.Vertices.Keys.First();
                }

                if (!graph.Vertices.ContainsKey(startVertex))
                {
                    result.Success = false;
                    result.Message = $"Start vertex '{startVertex}' not found";
                    return result;
                }

                var mstEdges = Prim(graph, startVertex);
                var totalWeight = mstEdges.Sum(e => e.Weight ?? 1.0);

                result.Success = true;
                result.Data["mstEdges"] = mstEdges.Select(e => e.Id).ToList();
                result.Data["totalWeight"] = totalWeight;
                result.Data["startVertex"] = startVertex;
                result.Message = $"MST (Prim) found with {mstEdges.Count} edges and total weight {totalWeight:F2}";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error executing Prim: {ex.Message}";
            }

            return result;
        }

        private List<IEdge> Prim(IGraphModel graph, string startVertex)
        {
            var mstEdges = new List<IEdge>();
            var visited = new HashSet<string>();
            var edgeQueue = new SortedSet<(double weight, IEdge edge)>();

            visited.Add(startVertex);

            // Добавляем все рёбра из начальной вершины
            AddEdgesToQueue(graph, startVertex, visited, edgeQueue);

            while (edgeQueue.Count > 0 && visited.Count < graph.Vertices.Count)
            {
                var (weight, edge) = edgeQueue.Min;
                edgeQueue.Remove(edgeQueue.Min);

                string unvisitedVertex = null;

                if (!visited.Contains(edge.Source))
                    unvisitedVertex = edge.Source;
                else if (!visited.Contains(edge.Target))
                    unvisitedVertex = edge.Target;

                if (unvisitedVertex != null)
                {
                    mstEdges.Add(edge);
                    visited.Add(unvisitedVertex);
                    AddEdgesToQueue(graph, unvisitedVertex, visited, edgeQueue);
                }
            }

            return mstEdges;
        }

        private void AddEdgesToQueue(
            IGraphModel graph,
            string vertex,
            HashSet<string> visited,
            SortedSet<(double weight, IEdge edge)> queue)
        {
            foreach (var edge in graph.Edges.Values)
            {
                if (edge.Source == vertex && !visited.Contains(edge.Target))
                {
                    queue.Add((edge.Weight ?? 1.0, edge));
                }
                else if (edge.Target == vertex && !visited.Contains(edge.Source))
                {
                    queue.Add((edge.Weight ?? 1.0, edge));
                }
            }
        }

        // Визуализация MST
        public void VisualizeResult(
            IGraphVisualModel visualModel,
            AlgorithmResult result,
            Color mstColor = default)
        {
            if (visualModel == null || result == null || !result.Success)
                return;

            if (mstColor == default) mstColor = Colors.Green;

            if (result.Data.ContainsKey("mstEdges"))
            {
                var mstEdgeIds = result.Data["mstEdges"] as List<string>;
                if (mstEdgeIds != null)
                {
                    foreach (var edgeId in mstEdgeIds)
                    {
                        visualModel.SetEdgeColor(edgeId, mstColor);
                    }

                    // Также можно выделить вершины
                    var vertexIds = new HashSet<string>();
                    foreach (var edgeId in mstEdgeIds)
                    {
                        if (graph.TryGetEdge(edgeId, out var edge))
                        {
                            vertexIds.Add(edge.Source);
                            vertexIds.Add(edge.Target);
                        }
                    }

                    foreach (var vertexId in vertexIds)
                    {
                        visualModel.SetVertexColor(vertexId, mstColor);
                    }
                }
            }
        }
    }
}