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

            // 2. Инициализируем DSU
            var parent = new Dictionary<string, string>();
            var rank = new Dictionary<string, int>();

            foreach (var vertexId in graph.Vertices.Keys)
            {
                parent[vertexId] = vertexId;
                rank[vertexId] = 0;
            }

            // 3. Строим MST
            var mstEdges = new List<IEdge>();

            foreach (var edge in edges)
            {
                string root1 = Find(parent, edge.Source);
                string root2 = Find(parent, edge.Target);

                if (root1 != root2)
                {
                    mstEdges.Add(edge);
                    Union(parent, rank, root1, root2);

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
                parent[vertex] = Find(parent, parent[vertex]);
            }
            return parent[vertex];
        }

        private void Union(Dictionary<string, string> parent, Dictionary<string, int> rank, string x, string y)
        {
            string rootX = Find(parent, x);
            string rootY = Find(parent, y);

            if (rootX == rootY)
                return;

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

        public void VisualizeResult(GraphVisualModel visualModel, AlgorithmResult result)
        {
            if (visualModel == null || result == null || !result.Success)
                return;

            // Сбрасываем цвета
            visualModel.ResetColors();

            // Выделяем рёбра MST
            if (result.Data.TryGetValue("mstEdges", out object edgesObj) && edgesObj is List<string> mstEdges)
            {
                foreach (var edgeId in mstEdges)
                {
                    visualModel.SetEdgeColor(edgeId, Colors.Green);
                }

                // Также выделяем вершины MST
                var vertices = new HashSet<string>();
                foreach (var edgeId in mstEdges)
                {
                    if (visualModel is IGraphModelAccessor accessor)
                    {
                        var edge = accessor.GetEdgeById(edgeId);
                        if (edge != null)
                        {
                            vertices.Add(edge.Source);
                            vertices.Add(edge.Target);
                        }
                    }
                }

                foreach (var vertexId in vertices)
                {
                    visualModel.SetVertexColor(vertexId, Colors.Green);
                }
            }
        }
    }

    // Вспомогательный интерфейс для доступа к данным
    public interface IGraphModelAccessor
    {
        IEdge GetEdgeById(string edgeId);
    }
}