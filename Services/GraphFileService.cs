using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using GraphEditor.Core;
using GraphEditor.Core.Models;
using GraphEditor.Serialization;
using GraphEditor.Visual;

namespace GraphEditor.Services
{
    public class GraphFileService
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public GraphFileService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public void SaveGraph(string filePath, GraphModel graphModel, IGraphVisualModel visualModel)
        {
            try
            {
                var graphData = ConvertToGraphData(graphModel, visualModel);
                string json = JsonSerializer.Serialize(graphData, _jsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save graph: {ex.Message}", ex);
            }
        }

        public (GraphModel GraphModel, IGraphVisualModel VisualModel) LoadGraph(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var graphData = JsonSerializer.Deserialize<GraphData>(json, _jsonOptions);

                if (graphData == null)
                    throw new InvalidOperationException("Failed to parse graph file");

                return ConvertFromGraphData(graphData);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load graph: {ex.Message}", ex);
            }
        }

        private GraphData ConvertToGraphData(GraphModel graphModel, IGraphVisualModel visualModel)
        {
            var data = new GraphData
            {
                GraphModel = new GraphModelData
                {
                    IsDirected = graphModel.IsDirected,
                    AllowParallelEdges = graphModel.AllowParallelEdges,
                    AllowSelfLoops = graphModel.AllowSelfLoops
                },
                VisualModel = new VisualModelData(),
                Settings = new SettingsData()
            };

            // Сохраняем вершины
            foreach (var vertex in graphModel.Vertices.Values)
            {
                data.GraphModel.Vertices.Add(new VertexData
                {
                    Id = vertex.Id,
                    Label = vertex.Label,
                    Data = vertex.Data != null ?
                        new Dictionary<string, object>(vertex.Data) : null
                });
            }

            // Сохраняем рёбра
            foreach (var edge in graphModel.Edges.Values)
            {
                data.GraphModel.Edges.Add(new EdgeData
                {
                    Id = edge.Id,
                    Source = edge.Source,
                    Target = edge.Target,
                    Weight = edge.Weight,
                    Capacity = edge.Capacity,
                    Data = edge.Data != null ?
                        new Dictionary<string, object>(edge.Data) : null
                });
            }

            // Сохраняем визуальную информацию
            if (visualModel is GraphVisualModel concreteVisual)
            {
                // Позиции вершин
                foreach (var position in concreteVisual.VertexPositions)
                {
                    data.VisualModel.VertexPositions[position.Key] =
                        PointData.FromPoint(position.Value);
                }

                // Цвета вершин
                foreach (var color in concreteVisual.VertexColors)
                {
                    data.VisualModel.VertexColors[color.Key] =
                        ColorData.FromColor(color.Value);
                }

                // Цвета рёбер
                foreach (var color in concreteVisual.EdgeColors)
                {
                    data.VisualModel.EdgeColors[color.Key] =
                        ColorData.FromColor(color.Value);
                }
            }

            return data;
        }

        private (GraphModel, IGraphVisualModel) ConvertFromGraphData(GraphData graphData)
        {
            // Создаём модель графа
            var graphModel = new GraphModel(
                isDirected: graphData.GraphModel.IsDirected,
                allowParallelEdges: graphData.GraphModel.AllowParallelEdges,
                allowSelfLoops: graphData.GraphModel.AllowSelfLoops
            );

            // Добавляем вершины
            foreach (var vertexData in graphData.GraphModel.Vertices)
            {
                graphModel.AddVertex(vertexData.Id, vertexData.Label);
            }

            // Добавляем рёбра
            foreach (var edgeData in graphData.GraphModel.Edges)
            {
                graphModel.AddEdge(
                    edgeData.Id,
                    edgeData.Source,
                    edgeData.Target,
                    edgeData.Weight,
                    edgeData.Capacity
                );
            }

            // Создаём визуальную модель
            var visualModel = new GraphVisualModel();

            // Загружаем позиции вершин
            foreach (var position in graphData.VisualModel.VertexPositions)
            {
                visualModel.SetVertexPosition(position.Key, position.Value.ToPoint());
            }

            // Загружаем цвета вершин
            foreach (var color in graphData.VisualModel.VertexColors)
            {
                visualModel.SetVertexColor(color.Key, color.Value.ToColor());
            }

            // Загружаем цвета рёбер
            foreach (var color in graphData.VisualModel.EdgeColors)
            {
                visualModel.SetEdgeColor(color.Key, color.Value.ToColor());
            }

            return (graphModel, visualModel);
        }

        public bool IsValidGraphFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<GraphData>(json, _jsonOptions);
                return data != null;
            }
            catch
            {
                return false;
            }
        }
    }
}