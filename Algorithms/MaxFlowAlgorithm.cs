using GraphEditor.Core;
using GraphEditor.Visual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace GraphEditor.Algorithms
{
    public class MaxFlowAlgorithm
    {
        public AlgorithmResult Execute(IGraphModel graph, string sourceId, string sinkId)
        {
            Console.WriteLine($"=== MaxFlow.Execute ===");
            Console.WriteLine($"Source: {sourceId}, Sink: {sinkId}");
            Console.WriteLine($"Total edges: {graph.Edges.Count}");

            var result = new AlgorithmResult();

            try
            {
                // 1. Валидация
                if (!graph.Vertices.ContainsKey(sourceId))
                {
                    result.Success = false;
                    result.Message = $"Source vertex '{sourceId}' not found. Available: {string.Join(", ", graph.Vertices.Keys)}";
                    return result;
                }

                if (!graph.Vertices.ContainsKey(sinkId))
                {
                    result.Success = false;
                    result.Message = $"Sink vertex '{sinkId}' not found. Available: {string.Join(", ", graph.Vertices.Keys)}";
                    return result;
                }

                if (sourceId == sinkId)
                {
                    result.Success = false;
                    result.Message = "Source and sink must be different vertices";
                    return result;
                }

                // 2. Проверка capacity - используем вес как capacity если capacity не задано
                Console.WriteLine("Edge capacities:");
                foreach (var edge in graph.Edges.Values)
                {
                    double capacity = edge.Capacity ?? (edge.Weight ?? 1.0);
                    Console.WriteLine($"  Edge {edge.Id} ({edge.Source}->{edge.Target}): capacity={capacity}");
                }

                // 3. Выполняем алгоритм Форда-Фалкерсона
                double maxFlow = 0;
                var flow = new Dictionary<string, double>();
                var residualGraph = CreateResidualGraph(graph);

                // Инициализируем поток нулями
                foreach (var edge in graph.Edges.Values)
                {
                    flow[edge.Id] = 0;
                }

                Console.WriteLine("Starting Ford-Fulkerson algorithm...");
                int iteration = 0;

                while (true)
                {
                    iteration++;
                    var augmentingPath = FindAugmentingPath(residualGraph, sourceId, sinkId);

                    if (augmentingPath.path == null || augmentingPath.bottleneck <= 0)
                    {
                        Console.WriteLine($"No more augmenting paths found after {iteration - 1} iterations");
                        break;
                    }

                    Console.WriteLine($"Iteration {iteration}: bottleneck={augmentingPath.bottleneck:F2}");
                    Console.WriteLine($"  Path: {string.Join(" -> ", augmentingPath.path.Select(e => $"{e.Source}-{e.Target}"))}");

                    // Увеличиваем поток вдоль пути
                    UpdateFlowAlongPath(augmentingPath.path, augmentingPath.bottleneck, flow, residualGraph);
                    maxFlow += augmentingPath.bottleneck;
                }

                // 4. Формируем результат
                result.Success = true;
                result.Data["maxFlow"] = maxFlow;
                result.Data["flow"] = flow;
                result.Data["source"] = sourceId;
                result.Data["sink"] = sinkId;

                // Добавляем информацию о потоках на рёбрах
                var flowDetails = new List<string>();
                foreach (var kvp in flow)
                {
                    if (kvp.Value > 0)
                    {
                        var edge = graph.Edges[kvp.Key];
                        flowDetails.Add($"  {edge.Source}->{edge.Target}: {kvp.Value:F2}");
                    }
                }

                result.Message = $"Maximum flow from '{sourceId}' to '{sinkId}' is {maxFlow:F2}\n" +
                               $"Flow details:\n{string.Join("\n", flowDetails)}";

                Console.WriteLine($"Max flow calculated: {maxFlow:F2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MaxFlow: {ex}");
                result.Success = false;
                result.Message = $"Error executing Ford-Fulkerson: {ex.Message}";
            }

            return result;
        }

        private class ResidualEdge
        {
            public string Id { get; set; }
            public string Source { get; set; }
            public string Target { get; set; }
            public double Capacity { get; set; }
            public string OriginalEdgeId { get; set; }
            public bool IsReverse { get; set; }
        }

        private Dictionary<string, List<ResidualEdge>> CreateResidualGraph(IGraphModel graph)
        {
            var residualGraph = new Dictionary<string, List<ResidualEdge>>();

            foreach (var vertexId in graph.Vertices.Keys)
            {
                residualGraph[vertexId] = new List<ResidualEdge>();
            }

            foreach (var edge in graph.Edges.Values)
            {
                double capacity = edge.Capacity ?? (edge.Weight ?? 1.0);

                if (capacity <= 0) capacity = 1.0; // Минимальная capacity

                // Прямое ребро
                residualGraph[edge.Source].Add(new ResidualEdge
                {
                    Id = $"forward_{edge.Id}",
                    Source = edge.Source,
                    Target = edge.Target,
                    Capacity = capacity,
                    OriginalEdgeId = edge.Id,
                    IsReverse = false
                });

                // Обратное ребро
                residualGraph[edge.Target].Add(new ResidualEdge
                {
                    Id = $"reverse_{edge.Id}",
                    Source = edge.Target,
                    Target = edge.Source,
                    Capacity = 0,
                    OriginalEdgeId = edge.Id,
                    IsReverse = true
                });

                Console.WriteLine($"  Added edge {edge.Source}->{edge.Target}: capacity={capacity}");
            }

            return residualGraph;
        }

        private (List<ResidualEdge> path, double bottleneck) FindAugmentingPath(
            Dictionary<string, List<ResidualEdge>> residualGraph, string source, string sink)
        {
            // BFS поиск пути
            var visited = new HashSet<string>();
            var queue = new Queue<string>();
            var parent = new Dictionary<string, (ResidualEdge edge, string fromVertex)>();

            queue.Enqueue(source);
            visited.Add(source);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();

                if (current == sink)
                    break;

                foreach (var edge in residualGraph[current])
                {
                    if (edge.Capacity > 0 && !visited.Contains(edge.Target))
                    {
                        visited.Add(edge.Target);
                        parent[edge.Target] = (edge, current);
                        queue.Enqueue(edge.Target);
                    }
                }
            }

            if (!visited.Contains(sink))
                return (null, 0);

            var path = new List<ResidualEdge>();
            double bottleneck = double.PositiveInfinity;
            string currentVertex = sink;

            while (currentVertex != source)
            {
                var (edge, fromVertex) = parent[currentVertex];
                path.Add(edge);
                bottleneck = Math.Min(bottleneck, edge.Capacity);
                currentVertex = fromVertex;
            }

            path.Reverse();
            return (path, bottleneck);
        }

        private void UpdateFlowAlongPath(
            List<ResidualEdge> path,
            double flowAmount,
            Dictionary<string, double> flow,
            Dictionary<string, List<ResidualEdge>> residualGraph)
        {
            foreach (var edge in path)
            {
                edge.Capacity -= flowAmount;

                // Обновляем обратное ребро
                var reverseEdge = residualGraph[edge.Target]
                    .FirstOrDefault(e => e.Target == edge.Source &&
                                       e.OriginalEdgeId == edge.OriginalEdgeId);

                if (reverseEdge != null)
                {
                    reverseEdge.Capacity += flowAmount;
                }

                // Обновляем фактический поток
                if (!edge.IsReverse)
                {
                    flow[edge.OriginalEdgeId] += flowAmount;
                }
                else
                {
                    flow[edge.OriginalEdgeId] -= flowAmount;
                }
            }
        }

        public void VisualizeResult(GraphVisualModel visualModel, AlgorithmResult result)
        {
            if (visualModel == null || result == null || !result.Success)
            {
                Console.WriteLine("MaxFlow visualization: invalid input");
                return;
            }

            Console.WriteLine("Visualizing MaxFlow result...");

            // Выделяем source и sink
            if (result.Data.TryGetValue("source", out object sourceObj) && sourceObj is string source)
            {
                visualModel.SetVertexColor(source, Colors.Blue);
                Console.WriteLine($"  Source vertex {source} colored Blue");
            }

            if (result.Data.TryGetValue("sink", out object sinkObj) && sinkObj is string sink)
            {
                visualModel.SetVertexColor(sink, Colors.Red);
                Console.WriteLine($"  Sink vertex {sink} colored Red");
            }

            // Выделяем рёбра с потоком > 0
            if (result.Data.TryGetValue("flow", out object flowObj) && flowObj is Dictionary<string, double> flow)
            {
                double maxFlow = 0;
                if (result.Data.TryGetValue("maxFlow", out object maxFlowObj))
                    maxFlow = Convert.ToDouble(maxFlowObj);

                foreach (var kvp in flow)
                {
                    if (kvp.Value > 0)
                    {
                        // Цвет зависит от величины потока (от зелёного к жёлтому)
                        double ratio = maxFlow > 0 ? kvp.Value / maxFlow : 0;
                        byte redValue = (byte)(255 * ratio);
                        byte greenValue = (byte)(255 * (1 - ratio));

                        var color = Color.FromRgb(redValue, greenValue, 0);
                        visualModel.SetEdgeColor(kvp.Key, color);

                        Console.WriteLine($"  Edge {kvp.Key}: flow={kvp.Value:F2}, colored (R:{redValue}, G:{greenValue})");
                    }
                }
            }

            Console.WriteLine("MaxFlow visualization complete");
        }
    }
}