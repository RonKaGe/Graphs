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
        public string Name => "Ford-Fulkerson Algorithm";
        public string Description => "Finds maximum flow in a flow network";

        public AlgorithmResult Execute(
            IGraphModel graph,
            string sourceId,
            string sinkId)
        {
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
                bool hasCapacity = graph.Edges.Values.Any(e => e.Capacity.HasValue);
                if (!hasCapacity)
                {
                    result.Success = false;
                    result.Message = "No edge capacities defined. Please set capacities for edges.";
                    return result;
                }

                // 3. Создаём остаточную сеть
                var residualGraph = CreateResidualGraph(graph);

                // 4. Выполняем алгоритм Форда-Фалкерсона
                double maxFlow = 0;
                var flow = new Dictionary<string, double>();

                // Инициализируем поток нулями
                foreach (var edge in graph.Edges.Values)
                {
                    flow[edge.Id] = 0;
                }

                // Ищем увеличивающие пути
                while (true)
                {
                    var (augmentingPath, bottleneck) = FindAugmentingPath(residualGraph, sourceId, sinkId);

                    if (augmentingPath == null || bottleneck <= 0)
                        break;

                    // Увеличиваем поток вдоль пути
                    UpdateFlowAlongPath(augmentingPath, bottleneck, flow, residualGraph);
                    maxFlow += bottleneck;
                }

                // 5. Формируем результат
                result.Success = true;
                result.Data["maxFlow"] = maxFlow;
                result.Data["flow"] = flow;
                result.Data["source"] = sourceId;
                result.Data["sink"] = sinkId;
                result.Message = $"Maximum flow from '{sourceId}' to '{sinkId}' is {maxFlow}";
            }
            catch (Exception ex)
            {
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
            public double Capacity { get; set; }  // Остаточная пропускная способность
            public string OriginalEdgeId { get; set; }
            public bool IsReverse { get; set; }
        }

        private Dictionary<string, List<ResidualEdge>> CreateResidualGraph(IGraphModel graph)
        {
            var residualGraph = new Dictionary<string, List<ResidualEdge>>();

            // Инициализируем списки смежности
            foreach (var vertexId in graph.Vertices.Keys)
            {
                residualGraph[vertexId] = new List<ResidualEdge>();
            }

            // Добавляем прямые и обратные рёбра
            foreach (var edge in graph.Edges.Values)
            {
                double capacity = edge.Capacity ?? double.PositiveInfinity;

                // Прямое ребро (остаточная способность = capacity)
                residualGraph[edge.Source].Add(new ResidualEdge
                {
                    Id = $"forward_{edge.Id}",
                    Source = edge.Source,
                    Target = edge.Target,
                    Capacity = capacity,
                    OriginalEdgeId = edge.Id,
                    IsReverse = false
                });

                // Обратное ребро (остаточная способность = 0)
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
            Dictionary<string, List<ResidualEdge>> residualGraph,
            string source,
            string sink)
        {
            // Поиск в ширину (BFS)
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

            // Восстанавливаем путь
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

            path.Reverse(); // Чтобы путь был от source к sink
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
                // Обновляем остаточные пропускные способности
                edge.Capacity -= flowAmount;

                // Находим обратное ребро и увеличиваем его пропускную способность
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

        // Визуализация потока
        public void VisualizeResult(
            IGraphVisualModel visualModel,
            AlgorithmResult result,
            Color sourceColor = default,
            Color sinkColor = default,
            Color flowColor = default)
        {
            if (visualModel == null || result == null || !result.Success)
                return;

            if (sourceColor == default) sourceColor = Colors.Blue;
            if (sinkColor == default) sinkColor = Colors.Red;
            if (flowColor == default) flowColor = Colors.Green;

            string source = result.Data["source"] as string;
            string sink = result.Data["sink"] as string;
            var flow = result.Data["flow"] as Dictionary<string, double>;

            // Выделяем source и sink
            visualModel.SetVertexColor(source, sourceColor);
            visualModel.SetVertexColor(sink, sinkColor);

            // Выделяем рёбра с потоком > 0
            if (flow != null)
            {
                foreach (var kvp in flow)
                {
                    if (kvp.Value > 0)
                    {
                        visualModel.SetEdgeColor(kvp.Key, flowColor);
                    }
                }
            }
        }
    }
}