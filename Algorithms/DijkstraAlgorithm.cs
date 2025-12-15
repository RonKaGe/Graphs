using GraphEditor.Core;
using GraphEditor.Visual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace GraphEditor.Algorithms
{
    public class DijkstraAlgorithm
    {
        public AlgorithmResult Execute(IGraphModel graph, string startVertexId, string endVertexId = null)
        {
            var result = new AlgorithmResult();

            try
            {
                Console.WriteLine($"=== Dijkstra.Execute ===");
                Console.WriteLine($"Start: {startVertexId}, End: {endVertexId ?? "null"}");
                Console.WriteLine($"Graph vertices: {string.Join(", ", graph.Vertices.Keys)}");

                // 1. Валидация
                if (!graph.Vertices.ContainsKey(startVertexId))
                {
                    result.Success = false;
                    result.Message = $"Start vertex '{startVertexId}' not found. Available vertices: {string.Join(", ", graph.Vertices.Keys)}";
                    return result;
                }

                if (endVertexId != null && !graph.Vertices.ContainsKey(endVertexId))
                {
                    result.Success = false;
                    result.Message = $"End vertex '{endVertexId}' not found. Available vertices: {string.Join(", ", graph.Vertices.Keys)}";
                    return result;
                }

                // 2. Проверка на отрицательные веса
                bool hasNegativeWeight = false;
                foreach (var edge in graph.Edges.Values)
                {
                    if (edge.Weight.HasValue && edge.Weight.Value < 0)
                    {
                        hasNegativeWeight = true;
                        break;
                    }
                }

                if (hasNegativeWeight)
                {
                    result.Success = false;
                    result.Message = "Graph contains negative edge weights. Dijkstra cannot handle them.";
                    return result;
                }

                // 3. Выполнение алгоритма с сохранением предков
                var (distances, previous) = CalculateShortestPathsWithPrevious(graph, startVertexId);

                // 4. Сохраняем данные в результат
                result.Success = true;
                result.Data["distances"] = distances;
                result.Data["previous"] = previous;
                result.Data["startVertex"] = startVertexId;
                result.Data["hasPathToAll"] = distances.All(d => d.Value < double.PositiveInfinity);

                if (endVertexId != null)
                {
                    result.Data["endVertex"] = endVertexId;

                    if (distances.ContainsKey(endVertexId) && distances[endVertexId] < double.PositiveInfinity)
                    {
                        // Находим путь используя информацию о предках
                        var path = ReconstructPath(previous, startVertexId, endVertexId);
                        if (path != null && path.Count > 0)
                        {
                            result.Data["path"] = path;

                            // Получаем рёбра пути
                            var pathEdges = GetPathEdges(graph, path);
                            result.Data["edges"] = pathEdges;

                            result.Message = $"Shortest path from '{startVertexId}' to '{endVertexId}': " +
                                           $"{string.Join(" → ", path)} (distance: {distances[endVertexId]:F2})";
                        }
                        else
                        {
                            result.Message = $"No path found from '{startVertexId}' to '{endVertexId}'";
                        }
                    }
                    else
                    {
                        result.Message = $"No path found from '{startVertexId}' to '{endVertexId}' (distance: ∞)";
                    }
                }
                else
                {
                    // Вычисляем расстояния до всех вершин
                    var reachableVertices = distances
                        .Where(d => d.Value < double.PositiveInfinity && d.Key != startVertexId)
                        .ToList();

                    if (reachableVertices.Count > 0)
                    {
                        result.Message = $"Distances from '{startVertexId}':\n" +
                                       string.Join("\n", reachableVertices.Select(v => $"  to '{v.Key}': {v.Value:F2}"));
                    }
                    else
                    {
                        result.Message = $"No reachable vertices from '{startVertexId}'";
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error executing Dijkstra: {ex.Message}\n{ex.StackTrace}";
            }

            return result;
        }

        private (Dictionary<string, double> distances, Dictionary<string, string> previous)
            CalculateShortestPathsWithPrevious(IGraphModel graph, string start)
        {
            var distances = new Dictionary<string, double>();
            var previous = new Dictionary<string, string>();
            var visited = new HashSet<string>();

            // Используем SortedSet как приоритетную очередь
            var priorityQueue = new SortedSet<(double distance, string vertexId)>(
                Comparer<(double distance, string vertexId)>.Create((a, b) =>
                {
                    int distCompare = a.distance.CompareTo(b.distance);
                    return distCompare != 0 ? distCompare : a.vertexId.CompareTo(b.vertexId);
                }));

            // Инициализация
            foreach (var vertexId in graph.Vertices.Keys)
            {
                distances[vertexId] = double.PositiveInfinity;
                previous[vertexId] = null;
            }

            distances[start] = 0;
            priorityQueue.Add((0, start));

            while (priorityQueue.Count > 0)
            {
                var (currentDist, currentVertex) = priorityQueue.Min;
                priorityQueue.Remove(priorityQueue.Min);

                if (visited.Contains(currentVertex))
                    continue;

                visited.Add(currentVertex);

                // Обходим соседей
                var neighbors = graph.GetNeighbors(currentVertex);
                foreach (var neighborId in neighbors)
                {
                    if (visited.Contains(neighborId))
                        continue;

                    // Находим вес ребра
                    double edgeWeight = 1.0;
                    var edge = FindEdgeBetween(graph, currentVertex, neighborId);

                    if (edge != null && edge.Weight.HasValue)
                    {
                        edgeWeight = edge.Weight.Value;
                    }
                    else if (edge == null)
                    {
                        // Ребро не найдено, пропускаем
                        continue;
                    }

                    double newDist = distances[currentVertex] + edgeWeight;

                    if (newDist < distances[neighborId])
                    {
                        // Удаляем старую запись из очереди, если она существует
                        priorityQueue.Remove((distances[neighborId], neighborId));

                        distances[neighborId] = newDist;
                        previous[neighborId] = currentVertex;
                        priorityQueue.Add((newDist, neighborId));
                    }
                }
            }

            return (distances, previous);
        }

        private List<string> ReconstructPath(Dictionary<string, string> previous, string start, string end)
        {
            var path = new List<string>();

            if (!previous.ContainsKey(end) || previous[end] == null)
                return null;

            string current = end;

            while (current != null && current != start)
            {
                path.Insert(0, current);
                if (!previous.ContainsKey(current))
                    return null;
                current = previous[current];
            }

            if (current == null)
                return null;

            path.Insert(0, start);
            return path;
        }

        private List<string> GetPathEdges(IGraphModel graph, List<string> path)
        {
            var edges = new List<string>();

            for (int i = 0; i < path.Count - 1; i++)
            {
                var edge = FindEdgeBetween(graph, path[i], path[i + 1]);
                if (edge != null)
                {
                    edges.Add(edge.Id);
                }
            }

            return edges;
        }

        private IEdge FindEdgeBetween(IGraphModel graph, string source, string target)
        {
            foreach (var edge in graph.Edges.Values)
            {
                if ((edge.Source == source && edge.Target == target) ||
                    (!graph.IsDirected && edge.Source == target && edge.Target == source))
                {
                    return edge;
                }
            }
            return null;
        }

        public void VisualizeResult(GraphVisualModel visualModel, AlgorithmResult result)
        {
            if (visualModel == null || result == null || !result.Success)
            {
                Console.WriteLine("VisualizeResult: invalid input");
                return;
            }

            Console.WriteLine("Visualizing Dijkstra result...");

            // Выделяем начальную вершину
            if (result.Data.TryGetValue("startVertex", out object startObj) && startObj is string startVertex)
            {
                visualModel.SetVertexColor(startVertex, Colors.Gold);
                Console.WriteLine($"  Start vertex {startVertex} colored Gold");

                // Выделяем конечную вершину и путь
                if (result.Data.TryGetValue("endVertex", out object endObj) && endObj is string endVertex)
                {
                    visualModel.SetVertexColor(endVertex, Colors.DarkGreen);
                    Console.WriteLine($"  End vertex {endVertex} colored DarkGreen");

                    if (result.Data.TryGetValue("path", out object pathObj) && pathObj is List<string> path)
                    {
                        Console.WriteLine($"  Path: {string.Join(" → ", path)}");

                        // Выделяем вершины пути
                        foreach (var vertexId in path)
                        {
                            if (vertexId != startVertex && vertexId != endVertex)
                            {
                                visualModel.SetVertexColor(vertexId, Colors.Orange);
                                Console.WriteLine($"  Path vertex {vertexId} colored Orange");
                            }
                        }

                        // Выделяем рёбра пути
                        if (result.Data.TryGetValue("edges", out object edgesObj) && edgesObj is List<string> edges)
                        {
                            foreach (var edgeId in edges)
                            {
                                visualModel.SetEdgeColor(edgeId, Colors.Red);
                                Console.WriteLine($"  Path edge {edgeId} colored Red");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("  No specific path found");
                    }
                }
                else
                {
                    // Если конечная вершина не указана, показываем все достижимые вершины
                    Console.WriteLine("  No end vertex specified, showing all reachable vertices");
                    if (result.Data.TryGetValue("distances", out object distObj) && distObj is Dictionary<string, double> distances)
                    {
                        foreach (var kvp in distances)
                        {
                            if (kvp.Key != startVertex && kvp.Value < double.PositiveInfinity)
                            {
                                // Градиент цвета в зависимости от расстояния
                                double maxDist = distances.Values.Where(v => v < double.PositiveInfinity).Max();
                                double ratio = kvp.Value / maxDist;

                                byte greenValue = (byte)(255 * (1 - ratio * 0.7));
                                byte redValue = (byte)(255 * ratio * 0.7);

                                var color = Color.FromRgb(redValue, greenValue, 100);
                                visualModel.SetVertexColor(kvp.Key, color);

                                Console.WriteLine($"  Vertex {kvp.Key} (distance {kvp.Value:F2}) colored");
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Visualization complete");
        }
    }
}