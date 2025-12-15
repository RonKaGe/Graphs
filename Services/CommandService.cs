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
        private readonly GraphFileService _fileService;

        public event Action<string> OperationCompleted;
        public event Action<string> ErrorOccurred;
        public event Action VisualChanged;

        public CommandService(IGraphModel graphModel, GraphVisualModel visualModel)
        {
            _graphModel = graphModel ?? throw new ArgumentNullException(nameof(graphModel));
            _visualModel = visualModel ?? throw new ArgumentNullException(nameof(visualModel));

            // Создаем сервис для работы с файлами
            _fileService = new GraphFileService();
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

        // === Файловые операции (ОБНОВЛЕНЫ!) ===

        public void SaveGraph(string filePath)
        {
            try
            {
                // Используем GraphFileService для сохранения в JSON формате
                if (_graphModel is GraphModel concreteGraphModel)
                {
                    _fileService.SaveGraph(filePath, concreteGraphModel, _visualModel);
                    OperationCompleted?.Invoke($"Graph saved to {Path.GetFileName(filePath)}");
                }
                else
                {
                    throw new InvalidOperationException("Cannot save: graph model type not supported");
                }
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
                if (!File.Exists(filePath))
                {
                    ErrorOccurred?.Invoke($"File not found: {filePath}");
                    return;
                }

                if (!_fileService.IsValidGraphFile(filePath))
                {
                    ErrorOccurred?.Invoke($"File '{Path.GetFileName(filePath)}' is not a valid graph file");
                    return;
                }

                // Загружаем граф через GraphFileService
                var (loadedGraphModel, loadedVisualModel) = _fileService.LoadGraph(filePath);

                // Копируем данные из загруженного графа в текущий
                CopyGraphData(loadedGraphModel, loadedVisualModel);

                OperationCompleted?.Invoke($"Graph loaded from {Path.GetFileName(filePath)}");
                VisualChanged?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to load graph: {ex.Message}");
            }
        }

        private void CopyGraphData(GraphModel sourceGraphModel, IGraphVisualModel sourceVisualModel)
        {
            // Очищаем текущий граф
            ClearCurrentGraph();

            // Копируем свойства графа
            // Note: В текущей реализации GraphModel не имеет сеттеров для IsDirected и других свойств
            // Может потребоваться создать новый экземпляр GraphModel или добавить методы для изменения этих свойств

            // Добавляем вершины
            foreach (var vertex in sourceGraphModel.Vertices.Values)
            {
                _graphModel.AddVertex(vertex.Id, vertex.Label);
            }

            // Добавляем рёбра
            foreach (var edge in sourceGraphModel.Edges.Values)
            {
                _graphModel.AddEdge(edge.Id, edge.Source, edge.Target, edge.Weight, edge.Capacity);
            }

            // Копируем визуальные данные
            if (sourceVisualModel is GraphVisualModel concreteVisualModel)
            {
                // Позиции вершин
                foreach (var position in concreteVisualModel.VertexPositions)
                {
                    _visualModel.SetVertexPosition(position.Key, position.Value);
                }

                // Цвета вершин
                foreach (var color in concreteVisualModel.VertexColors)
                {
                    _visualModel.SetVertexColor(color.Key, color.Value);
                }

                // Цвета рёбер
                foreach (var color in concreteVisualModel.EdgeColors)
                {
                    _visualModel.SetEdgeColor(color.Key, color.Value);
                }
            }
        }

        private void ClearCurrentGraph()
        {
            // Удаляем все рёбра
            var edgesToRemove = _graphModel.Edges.Keys.ToList();
            foreach (var edgeId in edgesToRemove)
            {
                _graphModel.RemoveEdge(edgeId);
            }

            // Удаляем все вершины
            var verticesToRemove = _graphModel.Vertices.Keys.ToList();
            foreach (var vertexId in verticesToRemove)
            {
                _graphModel.RemoveVertex(vertexId);
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