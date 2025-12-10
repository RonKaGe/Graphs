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
                // 1. Валидация
                if (!graph.Vertices.ContainsKey(startVertexId))
                {
                    result.Success = false;
                    result.Message = $"Start vertex '{startVertexId}' not found";
                    return result;
                }

                if (endVertexId != null && !graph.Vertices.ContainsKey(endVertexId))
                {
                    result.Success = false;
                    result.Message = $"End vertex '{endVertexId}' not found";
                    return result;
                }

                // 2. Проверка на отрицательные веса
                foreach (var edge in graph.Edges.Values)
                {
                    if (edge.Weight.HasValue && edge.Weight.Value < 0)
                    {
                        result.Success = false;
                        result.Message = "Graph contains negative edge weights. Dijkstra cannot handle them.";
                        return result;
                    }
                }

                // 3. Выполнение алгоритма с сохранением предков
                var (distances, previous) = CalculateShortestPathsWithPrevious(graph, startVertexId);

                // 4. Сохраняем данные в результат
                result.Success = true;
                result.Data["distances"] = distances;
                result.Data["previous"] = previous;
                result.Data["startVertex"] = startVertexId;  // ДОБАВЛЕНО!

                if (endVertexId != null)
                {
                    result.Data["endVertex"] = endVertexId;  // ДОБАВЛЕНО!

                    if (distances.ContainsKey(endVertexId) && distances[endVertexId] < double.PositiveInfinity)
                    {
                        // Находим путь используя информацию о предках
                        var path = ReconstructPath(previous, startVertexId, endVertexId);
                        if (path != null)
                        {
                            result.Data["path"] = path;
                            result.Data["edges"] = GetPathEdges(graph, path);
                            result.Message = $"Shortest path from '{startVertexId}' to '{endVertexId}': " +
                                           $"{string.Join(" → ", path)} (distance: {distances[endVertexId]})";
                        }
                        else
                        {
                            result.Message = $"No path found from '{startVertexId}' to '{endVertexId}'";
                        }
                    }
                    else
                    {
                        result.Message = $"No path found from '{startVertexId}' to '{endVertexId}'";
                    }
                }
                else
                {
                    result.Message = $"Calculated distances from '{startVertexId}' to all vertices";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error executing Dijkstra: {ex.Message}";
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
                foreach (var neighborId in graph.GetNeighbors(currentVertex))
                {
                    if (visited.Contains(neighborId))
                        continue;

                    // Находим вес ребра
                    double edgeWeight = 1.0;
                    var edge = FindEdgeBetween(graph, currentVertex, neighborId);

                    if (edge != null && edge.Weight.HasValue)
                        edgeWeight = edge.Weight.Value;

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
                return;

            // Сбрасываем цвета
            visualModel.ResetColors();

            // Выделяем начальную вершину
            if (result.Data.TryGetValue("startVertex", out object startObj) && startObj is string startVertex)
            {
                visualModel.SetVertexColor(startVertex, Colors.Gold);

                // Выделяем конечную вершину и путь
                if (result.Data.TryGetValue("endVertex", out object endObj) && endObj is string endVertex)
                {
                    visualModel.SetVertexColor(endVertex, Colors.DarkGreen);

                    if (result.Data.TryGetValue("path", out object pathObj) && pathObj is List<string> path)
                    {
                        // Выделяем вершины пути
                        foreach (var vertexId in path)
                        {
                            if (vertexId != startVertex && vertexId != endVertex)
                            {
                                visualModel.SetVertexColor(vertexId, Colors.Orange);
                            }
                        }

                        // Выделяем рёбра пути
                        if (result.Data.TryGetValue("edges", out object edgesObj) && edgesObj is List<string> edges)
                        {
                            foreach (var edgeId in edges)
                            {
                                visualModel.SetEdgeColor(edgeId, Colors.Red);
                            }
                        }
                    }
                }
                else
                {
                    // Если конечная вершина не указана, просто показываем все достижимые вершины
                    if (result.Data.TryGetValue("distances", out object distObj) && distObj is Dictionary<string, double> distances)
                    {
                        foreach (var kvp in distances)
                        {
                            if (kvp.Key != startVertex && kvp.Value < double.PositiveInfinity)
                            {
                                visualModel.SetVertexColor(kvp.Key, Colors.LightGreen);
                            }
                        }
                    }
                }
            }
        }
    }
}