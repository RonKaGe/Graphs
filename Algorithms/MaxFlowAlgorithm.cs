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
            Console.WriteLine($"MaxFlow.Execute вызван: source={sourceId}, sink={sinkId}");
            Console.WriteLine($"Всего рёбер: {graph.Edges.Count}");

            var result = new AlgorithmResult();

            try
            {
                // 1. Валидация
                if (!graph.Vertices.ContainsKey(sourceId))
                {
                    result.Success = false;
                    result.Message = $"Source vertex '{sourceId}' not found";
                    return result;
                }

                if (!graph.Vertices.ContainsKey(sinkId))
                {
                    result.Success = false;
                    result.Message = $"Sink vertex '{sinkId}' not found";
                    return result;
                }

                if (sourceId == sinkId)
                {
                    result.Success = false;
                    result.Message = "Source and sink must be different vertices";
                    return result;
                }

                // 2. Проверка capacity
                bool hasCapacity = false;
                foreach (var edge in graph.Edges.Values)
                {
                    if (edge.Capacity.HasValue)
                    {
                        hasCapacity = true;
                        break;
                    }
                }

                if (!hasCapacity)
                {
                    result.Success = false;
                    result.Message = "Пропускная способность ребер не определена. Пожалуйста, сначала установите пропускную способность для ребер.";
                    return result;
                }

                // 3. Выполняем алгоритм Форда-Фалкерсона
                double maxFlow = 0;
                var flow = new Dictionary<string, double>();

                // Инициализируем поток нулями
                foreach (var edge in graph.Edges.Values)
                {
                    flow[edge.Id] = 0;
                }

                // Создаём остаточную сеть
                var residualGraph = CreateResidualGraph(graph);

                // Ищем увеличивающие пути
                while (true)
                {
                    var augmentingPath = FindAugmentingPath(residualGraph, sourceId, sinkId);

                    if (augmentingPath.path == null || augmentingPath.bottleneck <= 0)
                        break;

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
                result.Message = $"Maximum flow from '{sourceId}' to '{sinkId}' is {maxFlow:F2}";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error executing Ford-Fulkerson: {ex.Message}";
            }

            return result;
        }

        // ... (остальные методы как в предыдущей версии, но адаптированные)
        // Включу ключевые методы:

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
                return;

            // Сбрасываем цвета
            visualModel.ResetColors();

            // Выделяем source и sink
            if (result.Data.TryGetValue("source", out object sourceObj) && sourceObj is string source)
            {
                visualModel.SetVertexColor(source, Colors.Blue);
            }

            if (result.Data.TryGetValue("sink", out object sinkObj) && sinkObj is string sink)
            {
                visualModel.SetVertexColor(sink, Colors.Red);
            }

            // Выделяем рёбра с потоком > 0
            if (result.Data.TryGetValue("flow", out object flowObj) && flowObj is Dictionary<string, double> flow)
            {
                foreach (var kvp in flow)
                {
                    if (kvp.Value > 0)
                    {
                        visualModel.SetEdgeColor(kvp.Key, Colors.Green);
                    }
                }
            }
        }
    }
}