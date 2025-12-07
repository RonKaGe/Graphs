using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using GraphEditor.Core;

namespace GraphEditor.Visual
{
    public class GraphVisualModel : INotifyPropertyChanged
    {
        private readonly Dictionary<string, Point> _vertexPositions = new();
        private readonly Dictionary<string, Color> _vertexColors = new();
        private readonly Dictionary<string, Color> _edgeColors = new();

        public IReadOnlyDictionary<string, Point> VertexPositions => _vertexPositions;
        public IReadOnlyDictionary<string, Color> VertexColors => _vertexColors;
        public IReadOnlyDictionary<string, Color> EdgeColors => _edgeColors;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? VisualChanged;

        // === Основные методы ===

        public void SetVertexPosition(string vertexId, Point position)
        {
            if (_vertexPositions.ContainsKey(vertexId))
            {
                _vertexPositions[vertexId] = position;
                OnVisualChanged();
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
        }

        public void SetEdgeColor(string edgeId, Color color)
        {
            _edgeColors[edgeId] = color;
            OnVisualChanged();
        }

        // Метод для применения алгоритма расположения
        public void ApplyLayout(IGraphModel graph, ILayoutAlgorithm algorithm)
        {
            var newPositions = algorithm.ComputePositions(graph);
            UpdatePositions(newPositions);
        }

        public void UpdatePositions(IReadOnlyDictionary<string, Point> newPositions)
        {
            foreach (var pos in newPositions)
            {
                _vertexPositions[pos.Key] = pos.Value;
            }
            OnVisualChanged();
        }

        public void ResetColors(Color defaultVertexColor, Color defaultEdgeColor)
        {
            foreach (var vertexId in _vertexColors.Keys.ToList())
            {
                _vertexColors[vertexId] = defaultVertexColor;
            }

            foreach (var edgeId in _edgeColors.Keys.ToList())
            {
                _edgeColors[edgeId] = defaultEdgeColor;
            }

            OnVisualChanged();
        }

        public void InitializeFromGraph(IGraphModel graph)
        {
            _vertexPositions.Clear();
            _vertexColors.Clear();
            _edgeColors.Clear();

            var random = new Random();

            // Инициализация вершин
            foreach (var vertexId in graph.Vertices.Keys)
            {
                _vertexPositions[vertexId] = new Point(
                    random.Next(100, 700),
                    random.Next(100, 500)
                );
                _vertexColors[vertexId] = Colors.LightBlue;
            }

            // Инициализация рёбер
            foreach (var edgeId in graph.Edges.Keys)
            {
                _edgeColors[edgeId] = Colors.Black;
            }

            OnVisualChanged();
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