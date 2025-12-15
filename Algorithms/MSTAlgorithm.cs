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
            Console.WriteLine($"=== MST.Execute ===");
            Console.WriteLine($"Graph vertices: {graph.Vertices.Count}, edges: {graph.Edges.Count}");
            Console.WriteLine($"Directed: {graph.IsDirected}");

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

                if (mstEdges.Count == 0)
                {
                    result.Success = false;
                    result.Message = "No MST found (empty graph or disconnected)";
                    return result;
                }

                var totalWeight = mstEdges.Sum(e => e.Weight ?? 1.0);

                // 4. Формирование результата
                result.Success = true;
                result.Data["mstEdges"] = mstEdges.Select(e => e.Id).ToList();
                result.Data["mstVertices"] = GetMSTVertices(mstEdges);
                result.Data["totalWeight"] = totalWeight;
                result.Data["edgeCount"] = mstEdges.Count;
                result.Data["vertexCount"] = graph.Vertices.Count;

                var edgeDetails = mstEdges.Select(e =>
                    $"  {e.Source}-{e.Target}: weight={e.Weight ?? 1.0:F2}");

                result.Message = $"MST found with {mstEdges.Count} edges and total weight {totalWeight:F2}\n" +
                               $"Edges:\n{string.Join("\n", edgeDetails)}";

                Console.WriteLine($"MST calculated: {mstEdges.Count} edges, total weight {totalWeight:F2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MST: {ex}");
                result.Success = false;
                result.Message = $"Error executing Kruskal: {ex.Message}";
            }

            return result;
        }

        private List<string> GetMSTVertices(List<IEdge> mstEdges)
        {
            var vertices = new HashSet<string>();
            foreach (var edge in mstEdges)
            {
                vertices.Add(edge.Source);
                vertices.Add(edge.Target);
            }
            return vertices.ToList();
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

            bool isConnected = visited.Count == graph.Vertices.Count;
            Console.WriteLine($"Graph connectivity: visited {visited.Count} of {graph.Vertices.Count} vertices - {(isConnected ? "connected" : "disconnected")}");
            return isConnected;
        }

        private List<IEdge> Kruskal(IGraphModel graph)
        {
            Console.WriteLine("Running Kruskal algorithm...");

            // 1. Сортируем рёбра по весу
            var edges = graph.Edges.Values
                .Where(e => e.Source != e.Target) // Игнорируем петли
                .OrderBy(e => e.Weight ?? 1.0)
                .ToList();

            Console.WriteLine($"Sorted edges: {edges.Count}");
            foreach (var edge in edges)
            {
                Console.WriteLine($"  {edge.Source}-{edge.Target}: weight={edge.Weight ?? 1.0:F2}");
            }

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

                    Console.WriteLine($"  Added edge {edge.Source}-{edge.Target} to MST");

                    if (mstEdges.Count == graph.Vertices.Count - 1)
                    {
                        Console.WriteLine($"  MST complete: {mstEdges.Count} edges (V-1 = {graph.Vertices.Count - 1})");
                        break;
                    }
                }
                else
                {
                    Console.WriteLine($"  Skipped edge {edge.Source}-{edge.Target} (would create cycle)");
                }
            }

            Console.WriteLine($"Final MST: {mstEdges.Count} edges");
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
            {
                Console.WriteLine("MST visualization: invalid input");
                return;
            }

            Console.WriteLine("Visualizing MST result...");

            // Выделяем рёбра MST
            if (result.Data.TryGetValue("mstEdges", out object edgesObj) && edgesObj is List<string> mstEdges)
            {
                Console.WriteLine($"  MST has {mstEdges.Count} edges");

                // Получаем вершины MST
                HashSet<string> mstVertices = new HashSet<string>();

                foreach (var edgeId in mstEdges)
                {
                    visualModel.SetEdgeColor(edgeId, Colors.Green);
                    Console.WriteLine($"  Edge {edgeId} colored Green");

                    // Собираем вершины
                    if (visualModel is IGraphModelAccessor accessor)
                    {
                        var edge = accessor.GetEdgeById(edgeId);
                        if (edge != null)
                        {
                            mstVertices.Add(edge.Source);
                            mstVertices.Add(edge.Target);
                        }
                    }
                }

                // Выделяем вершины MST
                foreach (var vertexId in mstVertices)
                {
                    visualModel.SetVertexColor(vertexId, Colors.Green);
                    Console.WriteLine($"  Vertex {vertexId} colored Green");
                }

                // Если есть вес MST, показываем его
                if (result.Data.TryGetValue("totalWeight", out object weightObj))
                {
                    double totalWeight = Convert.ToDouble(weightObj);
                    Console.WriteLine($"  Total MST weight: {totalWeight:F2}");
                }
            }
            else
            {
                Console.WriteLine("  No MST edges found in result");
            }

            Console.WriteLine("MST visualization complete");
        }
    }

    // Вспомогательный интерфейс для доступа к данным
    public interface IGraphModelAccessor
    {
        IEdge GetEdgeById(string edgeId);
    }
}