using GraphEditor.Core;
using GraphEditor.Core.Models;
using GraphEditor.Visual;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace GraphEditor.Services
{
    public class CommandService
    {
        private readonly IGraphModel _graphModel;
        private readonly GraphVisualModel _visualModel;

        public event Action<string> OperationCompleted;
        public event Action<string> ErrorOccurred;
        public event Action VisualChanged;

        public CommandService(IGraphModel graphModel, GraphVisualModel visualModel)
        {
            _graphModel = graphModel ?? throw new ArgumentNullException(nameof(graphModel));
            _visualModel = visualModel ?? throw new ArgumentNullException(nameof(visualModel));
        }

        // === Операции с вершинами ===

        public bool AddVertex(string id, string label = null, Point? position = null)
        {
            try
            {
                if (_graphModel.AddVertex(id, label))
                {
                    if (position.HasValue)
                    {
                        _visualModel.AddVertex(id, position.Value);
                    }

                    OperationCompleted?.Invoke($"Vertex '{id}' added");
                    VisualChanged?.Invoke();
                    return true;
                }

                ErrorOccurred?.Invoke($"Vertex '{id}' already exists");
                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to add vertex: {ex.Message}");
                return false;
            }
        }

        public bool AddVertexAtPosition(Point position)
        {
            string id = GenerateVertexId();
            return AddVertex(id, $"Vertex {id}", position);
        }

        public bool RemoveVertex(string id)
        {
            try
            {
                if (_graphModel.RemoveVertex(id))
                {
                    _visualModel.RemoveVertex(id);
                    OperationCompleted?.Invoke($"Vertex '{id}' removed");
                    VisualChanged?.Invoke();
                    return true;
                }

                ErrorOccurred?.Invoke($"Vertex '{id}' not found");
                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to remove vertex: {ex.Message}");
                return false;
            }
        }

        public bool SetVertexLabel(string id, string label)
        {
            try
            {
                if (_graphModel.SetVertexLabel(id, label))
                {
                    OperationCompleted?.Invoke($"Vertex '{id}' label changed to '{label}'");
                    return true;
                }

                ErrorOccurred?.Invoke($"Vertex '{id}' not found");
                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to change label: {ex.Message}");
                return false;
            }
        }

        // === Операции с рёбрами ===

        public bool AddEdge(string id, string source, string target, double? weight = null)
        {
            try
            {
                if (_graphModel.AddEdge(id, source, target, weight))
                {
                    _visualModel.AddEdge(id);
                    OperationCompleted?.Invoke($"Edge '{source}-{target}' added");
                    VisualChanged?.Invoke();
                    return true;
                }

                ErrorOccurred?.Invoke($"Failed to add edge '{source}-{target}'");
                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to add edge: {ex.Message}");
                return false;
            }
        }

        public bool RemoveEdge(string id)
        {
            try
            {
                if (_graphModel.RemoveEdge(id))
                {
                    _visualModel.RemoveEdge(id);
                    OperationCompleted?.Invoke($"Edge '{id}' removed");
                    VisualChanged?.Invoke();
                    return true;
                }

                ErrorOccurred?.Invoke($"Edge '{id}' not found");
                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to remove edge: {ex.Message}");
                return false;
            }
        }

        public bool SetEdgeWeight(string id, double? weight)
        {
            try
            {
                if (_graphModel.SetEdgeWeight(id, weight))
                {
                    OperationCompleted?.Invoke($"Edge '{id}' weight changed to {weight}");
                    VisualChanged?.Invoke();
                    return true;
                }

                ErrorOccurred?.Invoke($"Edge '{id}' not found");
                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to change edge weight: {ex.Message}");
                return false;
            }
        }

        // === Визуальные операции ===

        public void SetVertexColor(string vertexId, Color color)
        {
            try
            {
                _visualModel.SetVertexColor(vertexId, color);
                OperationCompleted?.Invoke($"Vertex '{vertexId}' color changed");
                VisualChanged?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to change vertex color: {ex.Message}");
            }
        }

        public void SetEdgeColor(string edgeId, Color color)
        {
            try
            {
                _visualModel.SetEdgeColor(edgeId, color);
                OperationCompleted?.Invoke($"Edge '{edgeId}' color changed");
                VisualChanged?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to change edge color: {ex.Message}");
            }
        }

        public void ResetColors()
        {
            try
            {
                _visualModel.ResetStyles(Colors.LightBlue, Colors.Black);
                OperationCompleted?.Invoke("Colors reset to default");
                VisualChanged?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to reset colors: {ex.Message}");
            }
        }

        // === Файловые операции ===

        public void SaveGraph(string filePath)
        {
            try
            {
                // Простая сериализация
                using var writer = new StreamWriter(filePath);
                writer.WriteLine("Graph Data:");
                writer.WriteLine($"Vertices: {_graphModel.Vertices.Count}");
                writer.WriteLine($"Edges: {_graphModel.Edges.Count}");

                foreach (var vertex in _graphModel.Vertices.Values)
                {
                    writer.WriteLine($"Vertex: {vertex.Id}, Label: {vertex.Label}");
                }

                foreach (var edge in _graphModel.Edges.Values)
                {
                    writer.WriteLine($"Edge: {edge.Id}, {edge.Source}->{edge.Target}, Weight: {edge.Weight}");
                }

                OperationCompleted?.Invoke($"Graph saved to {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to save graph: {ex.Message}");
            }
        }

        public void LoadGraph(string filePath)
        {
            try
            {
                // Простая заглушка для загрузки
                if (File.Exists(filePath))
                {
                    OperationCompleted?.Invoke($"Graph loaded from {Path.GetFileName(filePath)}");
                }
                else
                {
                    ErrorOccurred?.Invoke($"File not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to load graph: {ex.Message}");
            }
        }

        // === Вспомогательные методы ===

        private string GenerateVertexId()
        {
            char nextChar = 'F';
            var existingIds = _graphModel.Vertices.Keys
                .Where(id => id.Length == 1 && char.IsLetter(id[0]))
                .Select(id => char.ToUpper(id[0]))
                .ToHashSet();

            for (char c = 'F'; c <= 'Z'; c++)
            {
                if (!existingIds.Contains(c))
                    return c.ToString();
            }

            int num = 1;
            while (_graphModel.Vertices.ContainsKey($"V{num}"))
                num++;
            return $"V{num}";
        }

        // === Алгоритмы ===

        public void ApplyCircleLayout()
        {
            try
            {
                var layout = new CircleLayout();
                _visualModel.ApplyLayout(_graphModel, layout);
                OperationCompleted?.Invoke("Circle layout applied");
                VisualChanged?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to apply circle layout: {ex.Message}");
            }
        }

        public void ApplyRandomLayout()
        {
            try
            {
                var layout = new RandomLayout();
                _visualModel.ApplyLayout(_graphModel, layout);
                OperationCompleted?.Invoke("Random layout applied");
                VisualChanged?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to apply random layout: {ex.Message}");
            }
        }
    }
}