using GraphEditor.ViewModels;
using GraphEditor.Visual;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GraphEditor.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private bool _isDragging = false;
        private string _draggedVertexId = string.Empty;
        private Point _dragStartPoint;

        public MainWindow()
        {
            InitializeComponent();

            // Создаём ViewModel
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Подписываемся на изменение визуальной модели
            _viewModel.VisualModel.VisualChanged += OnVisualChanged;

            // Первоначальная отрисовка
            DrawGraph();

            // Подписываемся на события мыши
            GraphCanvas.MouseLeftButtonDown += GraphCanvas_MouseLeftButtonDown;
            GraphCanvas.MouseMove += GraphCanvas_MouseMove;
            GraphCanvas.MouseLeftButtonUp += GraphCanvas_MouseLeftButtonUp;
            GraphCanvas.MouseRightButtonDown += GraphCanvas_MouseRightButtonDown;
        }

        private void OnVisualChanged(object? sender, EventArgs e)
        {
            // Перерисовываем граф при изменении визуальной модели
            DrawGraph();
        }

        private void DrawGraph()
        {
            // Очищаем canvas
            GraphCanvas.Children.Clear();

            // Проверяем, есть ли граф для отрисовки
            if (_viewModel?.GraphModel == null || _viewModel.VisualModel == null)
                return;

            // 1. Рисуем рёбра
            foreach (var edge in _viewModel.GraphModel.Edges.Values)
            {
                DrawEdge(edge);
            }

            // 2. Рисуем вершины (поверх рёбер)
            foreach (var vertex in _viewModel.GraphModel.Vertices.Values)
            {
                DrawVertex(vertex);
            }
        }

        private void DrawVertex(Core.IVertex vertex)
        {
            var position = _viewModel.VisualModel.GetVertexPosition(vertex.Id) ?? new Point(100, 100);
            var color = _viewModel.VisualModel.VertexColors.ContainsKey(vertex.Id)
                ? _viewModel.VisualModel.VertexColors[vertex.Id]
                : Colors.LightBlue;

            // Эллипс вершины
            var ellipse = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Tag = vertex.Id,
                Cursor = Cursors.Hand
            };

            Canvas.SetLeft(ellipse, position.X - 20);
            Canvas.SetTop(ellipse, position.Y - 20);

            // Добавляем ToolTip
            ellipse.ToolTip = $"ID: {vertex.Id}\nLabel: {vertex.Label ?? "(no label)"}";

            GraphCanvas.Children.Add(ellipse);

            // Текст с ID вершины
            var text = new TextBlock
            {
                Text = vertex.Label ?? vertex.Id,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Tag = vertex.Id
            };

            Canvas.SetLeft(text, position.X - 15);
            Canvas.SetTop(text, position.Y - 10);
            GraphCanvas.Children.Add(text);
        }

        private void DrawEdge(Core.IEdge edge)
        {
            var sourcePos = _viewModel.VisualModel.GetVertexPosition(edge.Source) ?? new Point(0, 0);
            var targetPos = _viewModel.VisualModel.GetVertexPosition(edge.Target) ?? new Point(100, 100);
            var color = _viewModel.VisualModel.EdgeColors.ContainsKey(edge.Id)
                ? _viewModel.VisualModel.EdgeColors[edge.Id]
                : Colors.Black;

            // Линия ребра
            var line = new Line
            {
                X1 = sourcePos.X,
                Y1 = sourcePos.Y,
                X2 = targetPos.X,
                Y2 = targetPos.Y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
                Tag = edge.Id
            };

            // Добавляем ToolTip
            string tooltipText = $"ID: {edge.Id}\nFrom: {edge.Source}\nTo: {edge.Target}";
            if (edge.Weight.HasValue)
                tooltipText += $"\nWeight: {edge.Weight.Value:F2}";
            if (edge.Capacity.HasValue)
                tooltipText += $"\nCapacity: {edge.Capacity.Value:F2}";

            line.ToolTip = tooltipText;

            GraphCanvas.Children.Add(line);

            // Текст с весом (если есть)
            if (edge.Weight.HasValue)
            {
                var midX = (sourcePos.X + targetPos.X) / 2;
                var midY = (sourcePos.Y + targetPos.Y) / 2;

                var text = new TextBlock
                {
                    Text = edge.Weight.Value.ToString("F1"),
                    Foreground = Brushes.Red,
                    Background = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 10,
                    Padding = new Thickness(2)
                };

                Canvas.SetLeft(text, midX);
                Canvas.SetTop(text, midY);
                GraphCanvas.Children.Add(text);
            }
        }

        // === Обработчики мыши ===
        private void GraphCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(GraphCanvas);
            _dragStartPoint = position;

            // Ищем вершину под курсором
            foreach (var child in GraphCanvas.Children)
            {
                if (child is Ellipse ellipse && ellipse.Tag is string vertexId)
                {
                    var left = Canvas.GetLeft(ellipse);
                    var top = Canvas.GetTop(ellipse);
                    var rect = new Rect(left, top, ellipse.Width, ellipse.Height);

                    if (rect.Contains(position))
                    {
                        _isDragging = true;
                        _draggedVertexId = vertexId;
                        GraphCanvas.CaptureMouse();
                        e.Handled = true;

                        // Обновляем статус
                        _viewModel.StatusText = $"Dragging vertex: {vertexId}";
                        return;
                    }
                }
            }
        }

        private void GraphCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && !string.IsNullOrEmpty(_draggedVertexId))
            {
                var currentPosition = e.GetPosition(GraphCanvas);
                var delta = currentPosition - _dragStartPoint;

                // Обновляем позицию в визуальной модели
                var currentPos = _viewModel.VisualModel.GetVertexPosition(_draggedVertexId) ?? new Point(0, 0);
                var newPosition = new Point(currentPos.X + delta.X, currentPos.Y + delta.Y);
                _viewModel.VisualModel.SetVertexPosition(_draggedVertexId, newPosition);

                // Перерисовываем граф
                DrawGraph();

                _dragStartPoint = currentPosition;
            }
        }

        private void GraphCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _draggedVertexId = string.Empty;
                GraphCanvas.ReleaseMouseCapture();
                _viewModel.StatusText = "Ready";
            }
        }

        private void GraphCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(GraphCanvas);

            // Показываем информацию об элементе под курсором
            foreach (var child in GraphCanvas.Children)
            {
                if (child is Ellipse ellipse && ellipse.Tag is string vertexId)
                {
                    var left = Canvas.GetLeft(ellipse);
                    var top = Canvas.GetTop(ellipse);
                    var rect = new Rect(left, top, ellipse.Width, ellipse.Height);

                    if (rect.Contains(position))
                    {
                        _viewModel.StatusText = $"Right-click on vertex: {vertexId}";
                        e.Handled = true;
                        return;
                    }
                }
                else if (child is Line line && line.Tag is string edgeId)
                {
                    // Простая проверка для линии (можно улучшить)
                    var start = new Point(line.X1, line.Y1);
                    var end = new Point(line.X2, line.Y2);

                    // Приблизительная проверка близости к линии
                    if (IsPointNearLine(position, start, end, 5))
                    {
                        _viewModel.StatusText = $"Right-click on edge: {edgeId}";
                        e.Handled = true;
                        return;
                    }
                }
            }

            _viewModel.StatusText = $"Right-click at ({position.X:F0}, {position.Y:F0})";
        }

        private bool IsPointNearLine(Point point, Point lineStart, Point lineEnd, double tolerance)
        {
            // Упрощённая проверка близости точки к линии
            var lineVector = lineEnd - lineStart;
            var pointVector = point - lineStart;

            var lineLength = lineVector.Length;
            if (lineLength < 0.001) return false;

            var normalizedLineVector = lineVector / lineLength;
            var projectionLength = pointVector.X * normalizedLineVector.X + pointVector.Y * normalizedLineVector.Y;

            if (projectionLength < 0 || projectionLength > lineLength)
                return false;

            var projection = lineStart + normalizedLineVector * projectionLength;
            var distance = (point - projection).Length;

            return distance <= tolerance;
        }

        // Закрытие окна
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Отписываемся от событий
            if (_viewModel?.VisualModel != null)
            {
                _viewModel.VisualModel.VisualChanged -= OnVisualChanged;
            }

            GraphCanvas.MouseLeftButtonDown -= GraphCanvas_MouseLeftButtonDown;
            GraphCanvas.MouseMove -= GraphCanvas_MouseMove;
            GraphCanvas.MouseLeftButtonUp -= GraphCanvas_MouseLeftButtonUp;
            GraphCanvas.MouseRightButtonDown -= GraphCanvas_MouseRightButtonDown;
        }
    }
}