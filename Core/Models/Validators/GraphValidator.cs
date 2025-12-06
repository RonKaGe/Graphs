using GraphEditor.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphEditor.Core.Models.Validators
{
    public static class GraphValidator
    {
        public static bool ValidateVertexId(string id, out string error)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                error = "Vertex ID cannot be null or empty";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static bool ValidateEdgeId(string id, out string error)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                error = "Edge ID cannot be null or empty";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static bool ValidateVertexExists(IGraphModel graph, string vertexId, out string error)
        {
            if (!graph.Vertices.ContainsKey(vertexId))
            {
                error = $"Vertex '{vertexId}' does not exist in the graph";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static bool ValidateNoParallelEdges(IGraphModel graph, string source, string target, out string error)
        {
            if (!graph.AllowParallelEdges)
            {
                var existingEdge = graph.Edges.Values.FirstOrDefault(e =>
                    (e.Source == source && e.Target == target) ||
                    (!graph.IsDirected && e.Source == target && e.Target == source));

                if (existingEdge != null)
                {
                    error = $"Parallel edge already exists between '{source}' and '{target}'";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public static bool ValidateSelfLoop(IGraphModel graph, string source, string target, out string error)
        {
            if (source == target && !graph.AllowSelfLoops)
            {
                error = "Self-loops are not allowed in this graph";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static bool ValidateEdgeWeight(double? weight, out string error)
        {
            if (weight.HasValue && weight.Value < 0)
            {
                error = "Edge weight cannot be negative";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static bool ValidateEdgeCapacity(double? capacity, out string error)
        {
            if (capacity.HasValue && capacity.Value <= 0)
            {
                error = "Edge capacity must be positive";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}