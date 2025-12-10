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
        public string Name => "Dijkstra's Algorithm";
        public string Description => "Finds shortest paths from a start vertex to all other vertices";

        public AlgorithmResult Execute(
            IGraphModel graph,
            string startVertexId,
            string endVertexId = null)
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

                // 3. Выполнение алгоритма
                var (distances, previous) = CalculateShortestPaths(graph, startVertexId);

                // 4. Формирование результата
                result.Success = true;
                result.Data["distances"] = distances;
                result.Data["previous"] = previous;
                result.Data["startVertex"] = startVertexId;

                if (endVertexId != null)
                {
                    if (distances.ContainsKey(endVertexId) &&
                        distances[endVertexId] < double.PositiveInfinity)
                    {
                        // Находим ВСЕ кратчайшие пути
                        var allPaths = FindAllShortestPaths(graph, startVertexId, endVertexId, distances);
                        result.Data["paths"] = allPaths;
                        result.Data["endVertex"] = endVertexId;

                        if (allPaths.Count == 1)
                        {
                            result.Message = $"Shortest path from '{startVertexId}' to '{endVertexId}': " +
                                           $"{string.Join(" → ", allPaths[0])} (distance: {distances[endVertexId]})";
                        }
                        else
                        {
                            result.Message = $"Found {allPaths.Count} shortest paths from '{startVertexId}' " +
                                           $"to '{endVertexId}' with distance {distances[endVertexId]}";
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

        private (Dictionary<string, double> distances, Dictionary<string, List<string>> previous)
            CalculateShortestPaths(IGraphModel graph, string start)
        {
            var distances = new Dictionary<string, double>();
            var previous = new Dictionary<string, List<string>>();
            var unvisited = new HashSet<string>();
            var priorityQueue = new SortedSet<(double distance, string vertexId)>();

            // Инициализация
            foreach (var vertexId in graph.Vertices.Keys)
            {
                distances[vertexId] = double.PositiveInfinity;
                previous[vertexId] = new List<string>();
                unvisited.Add(vertexId);
            }

            distances[start] = 0;
            priorityQueue.Add((0, start));
            previous[start] = new List<string> { start };

            while (priorityQueue.Count > 0)
            {
                var (currentDist, currentVertex) = priorityQueue.Min;
                priorityQueue.Remove(priorityQueue.Min);

                if (!unvisited.Contains(currentVertex))
                    continue;

                unvisited.Remove(currentVertex);

                // Обходим соседей
                foreach (var edge in GetOutgoingEdges(graph, currentVertex))
                {
                    string neighbor = edge.Target;

                    if (!unvisited.Contains(neighbor))
                        continue;

                    double edgeWeight = edge.Weight ?? 1.0;
                    double altDistance = distances[currentVertex] + edgeWeight;

                    if (altDistance < distances[neighbor] - 0.0001) // Учитываем погрешность
                    {
                        distances[neighbor] = altDistance;
                        previous[neighbor] = new List<string> { currentVertex };
                        priorityQueue.Add((altDistance, neighbor));
                    }
                    else if (Math.Abs(altDistance - distances[neighbor]) < 0.0001)
                    {
                        // Найден альтернативный путь с той же длиной
                        previous[neighbor].Add(currentVertex);
                    }
                }
            }

            return (distances, previous);
        }

        private List<IEdge> GetOutgoingEdges(IGraphModel graph, string vertexId)
        {
            var edges = new List<IEdge>();

            foreach (var edge in graph.Edges.Values)
            {
                if (edge.Source == vertexId)
                {
                    edges.Add(edge);
                }
                else if (!graph.IsDirected && edge.Target == vertexId)
                {
                    // Для неориентированного графа добавляем ребро в обратном направлении
                    edges.Add(edge);
                }
            }

            return edges;
        }

        private List<List<string>> FindAllShortestPaths(
            IGraphModel graph,
            string start,
            string end,
            Dictionary<string, double> distances)
        {
            var allPaths = new List<List<string>>();

            if (distances[end] >= double.PositiveInfinity)
                return allPaths;

            FindPathsDFS(graph, start, end, distances,
                new List<string>(),
                new HashSet<string>(),
                allPaths);

            return allPaths;
        }

        private void FindPathsDFS(
            IGraphModel graph,
            string current,
            string target,
            Dictionary<string, double> distances,
            List<string> currentPath,
            HashSet<string> visited,
            List<List<string>> allPaths)
        {
            currentPath.Add(current);
            visited.Add(current);

            if (current == target)
            {
                allPaths.Add(new List<string>(currentPath));
            }
            else
            {
                // Ищем соседей, которые лежат на кратчайшем пути
                foreach (var edge in GetOutgoingEdges(graph, current))
                {
                    string neighbor = edge.Target;

                    if (visited.Contains(neighbor))
                        continue;

                    double edgeWeight = edge.Weight ?? 1.0;
                    double expectedDist = distances[current] + edgeWeight;

                    if (Math.Abs(expectedDist - distances[neighbor]) < 0.0001)
                    {
                        FindPathsDFS(graph, neighbor, target, distances, currentPath, visited, allPaths);
                    }
                }
            }

            currentPath.RemoveAt(currentPath.Count - 1);
            visited.Remove(current);
        }

        // Метод для визуализации путей
        public void VisualizeResult(
            IGraphVisualModel visualModel,
            AlgorithmResult result,
            Color startColor = default,
            Color endColor = default,
            Color pathColor = default)
        {
            if (visualModel == null || result == null || !result.Success)
                return;

            // Установка цветов по умолчанию
            if (startColor == default) startColor = Colors.Gold;
            if (endColor == default) endColor = Colors.DarkGreen;
            if (pathColor == default) pathColor = Colors.Red;

            string startVertex = result.Data["startVertex"] as string;
            visualModel.SetVertexColor(startVertex, startColor);

            if (result.Data.ContainsKey("endVertex"))
            {
                string endVertex = result.Data["endVertex"] as string;
                visualModel.SetVertexColor(endVertex, endColor);

                if (result.Data.ContainsKey("paths"))
                {
                    var paths = result.Data["paths"] as List<List<string>>;
                    if (paths != null && paths.Count > 0)
                    {
                        // Используем разные цвета для разных путей, если их несколько
                        Color[] availableColors = new[]
                        {
                            Colors.Red,
                            Colors.Blue,
                            Colors.Green,
                            Colors.Orange,
                            Colors.Purple,
                            Colors.Teal,
                            Colors.DeepPink
                        };

                        for (int i = 0; i < paths.Count; i++)
                        {
                            var path = paths[i];
                            var color = availableColors[i % availableColors.Length];

                            // Выделяем вершины пути (кроме начальной и конечной)
                            foreach (var vertexId in path)
                            {
                                if (vertexId != startVertex && vertexId != endVertex)
                                {
                                    visualModel.SetVertexColor(vertexId, color);
                                }
                            }

                            // Выделяем рёбра пути
                            for (int j = 0; j < path.Count - 1; j++)
                            {
                                string source = path[j];
                                string target = path[j + 1];

                                // Находим ребро между вершинами
                                string edgeId = FindEdgeIdBetween(visualModel, source, target);
                                if (edgeId != null)
                                {
                                    visualModel.SetEdgeColor(edgeId, color);
                                }
                            }
                        }
                    }
                }
            }
        }

        private string FindEdgeIdBetween(IGraphVisualModel visualModel, string source, string target)
        {
            // Этот метод нужно будет адаптировать под вашу архитектуру
            // Здесь ищем ребро между двумя вершинами
            // В реальной реализации нужно обратиться к модели графа

            // Временная заглушка
            return $"{source}-{target}";
        }
    }
}