using GraphEditor.Core;
using GraphEditor.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GraphEditor.Core.Services
{
    public class GraphSerializer
    {
        public string SerializeToJson(IGraphModel graph)
        {
            var data = new GraphData
            {
                IsDirected = graph.IsDirected,
                AllowParallelEdges = graph.AllowParallelEdges,
                AllowSelfLoops = graph.AllowSelfLoops,
                Vertices = graph.Vertices.Values.Select(v => new VertexData
                {
                    Id = v.Id,
                    Label = v.Label
                }).ToList(),
                Edges = graph.Edges.Values.Select(e => new EdgeData
                {
                    Id = e.Id,
                    Source = e.Source,
                    Target = e.Target,
                    Weight = e.Weight,
                    Capacity = e.Capacity
                }).ToList()
            };

            return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        }

        public IGraphModel DeserializeFromJson(string json)
        {
            var data = JsonSerializer.Deserialize<GraphData>(json);
            if (data == null)
                throw new InvalidOperationException("Failed to deserialize graph");

            var graph = new GraphModel(data.IsDirected, data.AllowParallelEdges, data.AllowSelfLoops);

            foreach (var vertex in data.Vertices)
            {
                graph.AddVertex(vertex.Id, vertex.Label);
            }

            foreach (var edge in data.Edges)
            {
                graph.AddEdge(edge.Id, edge.Source, edge.Target, edge.Weight, edge.Capacity);
            }

            return graph;
        }

        public void SaveToFile(IGraphModel graph, string filePath)
        {
            var json = SerializeToJson(graph);
            File.WriteAllText(filePath, json);
        }

        public IGraphModel LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var json = File.ReadAllText(filePath);
            return DeserializeFromJson(json);
        }

        private class GraphData
        {
            public bool IsDirected { get; set; }
            public bool AllowParallelEdges { get; set; }
            public bool AllowSelfLoops { get; set; }
            public List<VertexData> Vertices { get; set; } = new();
            public List<EdgeData> Edges { get; set; } = new();
        }

        private class VertexData
        {
            public string Id { get; set; } = string.Empty;
            public string? Label { get; set; }
        }

        private class EdgeData
        {
            public string Id { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
            public string Target { get; set; } = string.Empty;
            public double? Weight { get; set; }
            public double? Capacity { get; set; }
        }
    }
}