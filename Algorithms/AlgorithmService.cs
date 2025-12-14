using GraphEditor.Core;
using GraphEditor.Visual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace GraphEditor.Algorithms
{
    public class AlgorithmService
    {
        private readonly IGraphModel _graphModel;
        private readonly GraphVisualModel _visualModel;

        private readonly DijkstraAlgorithm _dijkstra;
        private readonly MaxFlowAlgorithm _maxFlow;
        private readonly MSTAlgorithm _mst;

        public AlgorithmService(IGraphModel graphModel, GraphVisualModel visualModel)
        {
            _graphModel = graphModel ?? throw new ArgumentNullException(nameof(graphModel));
            _visualModel = visualModel ?? throw new ArgumentNullException(nameof(visualModel));

            _dijkstra = new DijkstraAlgorithm();
            _maxFlow = new MaxFlowAlgorithm();
            _mst = new MSTAlgorithm();
        }

        public List<AlgorithmInfo> GetAvailableAlgorithms()
        {
            return new List<AlgorithmInfo>
            {
                new AlgorithmInfo
                {
                    Id = "Dijkstra",
                    Name = "Dijkstra (Shortest Path)",
                    Description = "Finds shortest paths between vertices",
                    RequiresParameters = true,
                    Parameters = new List<AlgorithmParameter>
                    {
                        new AlgorithmParameter { Name = "Start Vertex", Type = "vertex", IsRequired = true },
                        new AlgorithmParameter { Name = "Target Vertex", Type = "vertex", IsRequired = false }
                    }
                },
                new AlgorithmInfo
                {
                    Id = "MaxFlow",
                    Name = "Max Flow (Ford-Fulkerson)",
                    Description = "Finds maximum flow in network",
                    RequiresParameters = true,
                    Parameters = new List<AlgorithmParameter>
                    {
                        new AlgorithmParameter { Name = "Source", Type = "vertex", IsRequired = true },
                        new AlgorithmParameter { Name = "Sink", Type = "vertex", IsRequired = true }
                    }
                },
                new AlgorithmInfo
                {
                    Id = "MST",
                    Name = "Minimum Spanning Tree (Kruskal)",
                    Description = "Finds minimum spanning tree",
                    RequiresParameters = false
                }
            };
        }

        public AlgorithmResult RunAlgorithm(string algorithmId, Dictionary<string, string> parameters)
        {

            try
            {
                return algorithmId switch
                {
                    "Dijkstra" => RunDijkstra(parameters),
                    "MaxFlow" => RunMaxFlow(parameters),
                    "MST" => RunMST(),
                    _ => new AlgorithmResult
                    {
                        Success = false,
                        Message = $"Unknown algorithm: {algorithmId}"
                    }
                };
            }
            catch (Exception ex)
            {
                return new AlgorithmResult
                {
                    Success = false,
                    Message = $"Error running algorithm: {ex.Message}"
                };
            }
        }

        private AlgorithmResult RunDijkstra(Dictionary<string, string> parameters)
        {
            if (!parameters.TryGetValue("Start Vertex", out string startVertex) || string.IsNullOrEmpty(startVertex))
                return new AlgorithmResult { Success = false, Message = "Start vertex is required" };

            parameters.TryGetValue("Target Vertex", out string targetVertex);

            var result = _dijkstra.Execute(_graphModel, startVertex, targetVertex);

            if (result.Success)
            {
                _dijkstra.VisualizeResult(_visualModel, result);
            }

            return result;
        }

        private AlgorithmResult RunMaxFlow(Dictionary<string, string> parameters)
        {
            if (!parameters.TryGetValue("Source", out string source) || string.IsNullOrEmpty(source))
                return new AlgorithmResult { Success = false, Message = "Source vertex is required" };

            if (!parameters.TryGetValue("Sink", out string sink) || string.IsNullOrEmpty(sink))
                return new AlgorithmResult { Success = false, Message = "Sink vertex is required" };

            var result = _maxFlow.Execute(_graphModel, source, sink);

            if (result.Success)
            {
                _maxFlow.VisualizeResult(_visualModel, result);
            }

            return result;
        }

        private AlgorithmResult RunMST()
        {
            var result = _mst.Execute(_graphModel);

            if (result.Success)
            {
                _mst.VisualizeResult(_visualModel, result);
            }

            return result;
        }

        public void ResetVisualization()
        {
            _visualModel.ResetColors();
        }
    }

    public class AlgorithmInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool RequiresParameters { get; set; }
        public List<AlgorithmParameter> Parameters { get; set; } = new List<AlgorithmParameter>();
    }

    public class AlgorithmParameter
    {
        public string Name { get; set; }
        public string Type { get; set; } // "vertex", "number", "text"
        public bool IsRequired { get; set; }
        public string DefaultValue { get; set; }
    }
}