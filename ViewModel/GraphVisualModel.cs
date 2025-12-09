using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using GraphEditor.Core;

namespace GraphEditor.Visual
{
    public class GraphVisualModel : IGraphVisualModel, INotifyPropertyChanged
    {
        private readonly Dictionary<string, Point> _vertexPositions = new();
        private readonly Dictionary<string, Color> _vertexColors = new();
        private readonly Dictionary<string, Color> _edgeColors = new();

        // Выделенные элементы
        private readonly HashSet<string> _selectedVertices = new();
        private readonly HashSet<string> _selectedEdges = new();

        // Цвета по умолчанию
        private Color _defaultVertexColor = Colors.LightBlue;
        private Color _defaultEdgeColor = Colors.Black;
        private readonly Color _selectedColor = Colors.Red;

        // === РЕАЛИЗАЦИЯ СВОЙСТВ IGraphVisualModel ===
        public IReadOnlyDictionary<string, Point> VertexPositions => _vertexPositions;
        public IReadOnlyDictionary<string, Color> VertexColors => _vertexColors;
        public IReadOnlyDictionary<string, Color> EdgeColors => _edgeColors;

        // === ДОПОЛНИТЕЛЬНЫЕ СВОЙСТВА (для удобства) ===
        public IEnumerable<string> SelectedVertices => _selectedVertices;
        public IEnumerable<string> SelectedEdges => _selectedEdges;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? VisualChanged;

        // === РЕАЛИЗАЦИЯ МЕТОДОВ IGraphVisualModel ===

        public void SetVertexPosition(string vertexId, Point position)
        {
            if (_vertexPositions.ContainsKey(vertexId))
            {
                _vertexPositions[vertexId] = position;
                OnVisualChanged();
                OnPropertyChanged(nameof(VertexPositions));
            }
        }

        public Point? GetVertexPosition(string vertexId)
        {
            return _vertexPositions.TryGetValue(vertexId, out var pos) ? pos : null;
        }

        public void SetVertexColor(string vertexId, Color color)
        {
            _vertexColors[vertexId] = color;
            OnVisualChanged();
            OnPropertyChanged(nameof(VertexColors));
        }

        public void SetEdgeColor(string edgeId, Color color)
        {
            _edgeColors[edgeId] = color;
            OnVisualChanged();
            OnPropertyChanged(nameof(EdgeColors));
        }

        public void ApplyLayout(IGraphModel graph, ILayoutAlgorithm algorithm)
        {
            if (algorithm == null) throw new ArgumentNullException(nameof(algorithm));

            var newPositions = algorithm.ComputePositions(graph);
            UpdatePositions(newPositions);
        }

        public void ResetStyles(Color defaultVertexColor, Color defaultEdgeColor)
        {
            _defaultVertexColor = defaultVertexColor;
            _defaultEdgeColor = defaultEdgeColor;
            ResetColors();
        }

        // === ОСТАЛЬНЫЕ МЕТОДЫ (остаются как есть) ===

        public void SelectVertex(string vertexId, bool selected)
        {
            if (selected)
            {
                _selectedVertices.Add(vertexId);
                SetVertexColor(vertexId, _selectedColor);
            }
            else
            {
                _selectedVertices.Remove(vertexId);
                SetVertexColor(vertexId, _defaultVertexColor);
            }
        }

        public void SelectEdge(string edgeId, bool selected)
        {
            if (selected)
            {
                _selectedEdges.Add(edgeId);
                SetEdgeColor(edgeId, _selectedColor);
            }
            else
            {
                _selectedEdges.Remove(edgeId);
                SetEdgeColor(edgeId, _defaultEdgeColor);
            }
        }

        public void ClearSelection()
        {
            foreach (var vertexId in _selectedVertices.ToList())
            {
                SelectVertex(vertexId, false);
            }

            foreach (var edgeId in _selectedEdges.ToList())
            {
                SelectEdge(edgeId, false);
            }

            _selectedVertices.Clear();
            _selectedEdges.Clear();
        }

        public void UpdatePositions(IReadOnlyDictionary<string, Point> newPositions)
        {
            foreach (var pos in newPositions)
            {
                _vertexPositions[pos.Key] = pos.Value;
            }
            OnVisualChanged();
            OnPropertyChanged(nameof(VertexPositions));
        }

        public void ResetColors()
        {
            foreach (var vertexId in _vertexColors.Keys.ToList())
            {
                _vertexColors[vertexId] = _defaultVertexColor;
            }

            foreach (var edgeId in _edgeColors.Keys.ToList())
            {
                _edgeColors[edgeId] = _defaultEdgeColor;
            }

            OnVisualChanged();
            OnPropertyChanged(nameof(VertexColors));
            OnPropertyChanged(nameof(EdgeColors));
        }

        public void InitializeFromGraph(IGraphModel graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));

            _vertexPositions.Clear();
            _vertexColors.Clear();
            _edgeColors.Clear();
            _selectedVertices.Clear();
            _selectedEdges.Clear();

            var random = new Random();

            // Инициализация вершин
            foreach (var vertexId in graph.Vertices.Keys)
            {
                _vertexPositions[vertexId] = new Point(
                    random.Next(100, 700),
                    random.Next(100, 500)
                );
                _vertexColors[vertexId] = _defaultVertexColor;
            }

            // Инициализация рёбер
            foreach (var edgeId in graph.Edges.Keys)
            {
                _edgeColors[edgeId] = _defaultEdgeColor;
            }

            OnVisualChanged();
        }

        public void AddVertex(string vertexId, Point position)
        {
            if (string.IsNullOrEmpty(vertexId))
                throw new ArgumentException("Vertex ID cannot be null or empty", nameof(vertexId));

            _vertexPositions[vertexId] = position;
            _vertexColors[vertexId] = _defaultVertexColor;
            OnVisualChanged();
            OnPropertyChanged(nameof(VertexPositions));
            OnPropertyChanged(nameof(VertexColors));
        }

        public void AddEdge(string edgeId)
        {
            if (string.IsNullOrEmpty(edgeId))
                throw new ArgumentException("Edge ID cannot be null or empty", nameof(edgeId));

            _edgeColors[edgeId] = _defaultEdgeColor;
            OnVisualChanged();
            OnPropertyChanged(nameof(EdgeColors));
        }

        public void RemoveVertex(string vertexId)
        {
            _vertexPositions.Remove(vertexId);
            _vertexColors.Remove(vertexId);
            _selectedVertices.Remove(vertexId);
            OnVisualChanged();
            OnPropertyChanged(nameof(VertexPositions));
            OnPropertyChanged(nameof(VertexColors));
        }

        public void RemoveEdge(string edgeId)
        {
            _edgeColors.Remove(edgeId);
            _selectedEdges.Remove(edgeId);
            OnVisualChanged();
            OnPropertyChanged(nameof(EdgeColors));
        }

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ для задания ===

        // Для алгоритма Дейкстры - выделить путь цветом
        public void HighlightPath(IEnumerable<string> vertexIds, IEnumerable<string> edgeIds, Color pathColor)
        {
            // Сбросим предыдущие цвета пути
            ResetColors();

            // Выделяем вершины пути
            foreach (var vertexId in vertexIds)
            {
                SetVertexColor(vertexId, pathColor);
            }

            // Выделяем рёбра пути
            foreach (var edgeId in edgeIds)
            {
                SetEdgeColor(edgeId, pathColor);
            }

            OnVisualChanged();
        }

        // Для максимального потока - показать поток на рёбрах
        public void ShowFlowOnEdges(Dictionary<string, double> flows, Color flowColor)
        {
            foreach (var flow in flows)
            {
                SetEdgeColor(flow.Key, flowColor);
            }
            OnVisualChanged();
        }

        // Для МСТ - выделить дерево
        public void HighlightTree(IEnumerable<string> edgeIds, Color treeColor)
        {
            ResetColors();

            foreach (var edgeId in edgeIds)
            {
                SetEdgeColor(edgeId, treeColor);
            }

            OnVisualChanged();
        }

        // Получить позицию для алгоритмов компоновки
        public Point GetVertexCenter(string vertexId)
        {
            return _vertexPositions.TryGetValue(vertexId, out var pos)
                ? pos
                : new Point(0, 0);
        }

        // Обновить позиции вершин из внешнего источника
        public void UpdateVertexPositions(Dictionary<string, Point> newPositions)
        {
            foreach (var kvp in newPositions)
            {
                _vertexPositions[kvp.Key] = kvp.Value;
            }
            OnVisualChanged();
            OnPropertyChanged(nameof(VertexPositions));
        }

        private void OnVisualChanged()
        {
            VisualChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}