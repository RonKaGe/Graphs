using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

// Используем наши контракты
using GraphEditor.Core;

namespace GraphEditor.Views
{
    public partial class MainWindow : Window
    {
        // Граф модель (пока тестовая, потом Артём подключит свою)
        private readonly IGraphModel _graphModel;

        // Визуальная модель (твоя)
        private readonly GraphEditor.Visual.GraphVisualModel _visualModel;

        // Алгоритмы расположения (твои)
        private readonly GraphEditor.Visual.CircleLayout _circleLayout;
        private readonly GraphEditor.Visual.RandomLayout _randomLayout;
        private readonly GraphEditor.Visual.ForceLayout _forceLayout;

        // Для перетаскивания
        private bool _isDragging = false;
        private string _draggedVertexId;
        private Point _dragStartPoint;

        public MainWindow()
        {
            InitializeComponent();

            // Создаём тестовую реализацию графа (потом заменим на Артёмову)
            _graphModel = CreateTestGraphModel();

            // Инициализируем визуальную модель
            _visualModel = new GraphEditor.Visual.GraphVisualModel();
            _visualModel.InitializeFromGraph(_graphModel);

            // Создаём алгоритмы расположения
            _circleLayout = new GraphEditor.Visual.CircleLayout();
            _randomLayout = new GraphEditor.Visual.RandomLayout();
            _forceLayout = new GraphEditor.Visual.ForceLayout();

            // Настраиваем обработчики
            SetupEventHandlers();

            // Первая отрисовка
            DrawGraph();
            UpdateStatus();
        }

        private void SetupEventHandlers()
        {
            // === Обработчики кнопок ===
            BtnAddVertex.Click += (s, e) => AddVertexMode();
            BtnAddEdge.Click += (s, e) => AddEdgeMode();
            BtnCircleLayout.Click += (s, e) => ApplyCircleLayout();
            BtnRandomLayout.Click += (s, e) => ApplyRandomLayout();
            BtnForceLayout.Click += (s, e) => ApplyForceLayout();
            BtnSelectMode.Click += (s, e) => SelectMode();
            BtnDragMode.Click += (s, e) => DragMode();

            // === Обработчики меню ===
            MenuItemLayoutCircle.Click += (s, e) => ApplyCircleLayout();
            MenuItemLayoutRandom.Click += (s, e) => ApplyRandomLayout();
            MenuItemLayoutForce.Click += (s, e) => ApplyForceLayout();
            MenuItemResetColors.Click += (s, e) => ResetColors();
            MenuItemFileExit.Click += (s, e) => Close();

            // === Обработчики Canvas ===
            GraphCanvas.MouseLeftButtonDown += OnCanvasMouseDown;
            GraphCanvas.MouseMove += OnCanvasMouseMove;
            GraphCanvas.MouseLeftButtonUp += OnCanvasMouseUp;
            GraphCanvas.MouseRightButtonDown += OnCanvasRightButtonDown;

        }

        // === Режимы работы ===
        private void SelectMode()
        {
            StatusText.Text = "Mode: Select";
            HighlightButton(BtnSelectMode);
        }

        private void DragMode()
        {
            StatusText.Text = "Mode: Drag";
            HighlightButton(BtnDragMode);
        }

        private void AddVertexMode()
        {
            StatusText.Text = "Mode: Add Vertex";
            HighlightButton(BtnAddVertex);
        }

        private void AddEdgeMode()
        {
            StatusText.Text = "Mode: Add Edge";
            HighlightButton(BtnAddEdge);
        }

        private void HighlightButton(Button activeButton)
        {
            // Сбрасываем все кнопки
            var buttons = new[] { BtnSelectMode, BtnDragMode, BtnAddVertex, BtnAddEdge };
            foreach (var button in buttons)
            {
                button.Background = Brushes.Transparent;
            }

            // Подсвечиваем активную
            if (activeButton != null)
            {
                activeButton.Background = new SolidColorBrush(Color.FromArgb(100, 100, 150, 255));
            }
        }

        // === Алгоритмы расположения ===
        private void ApplyCircleLayout()
        {
            try
            {
                _visualModel.ApplyLayout(_graphModel, _circleLayout);
                DrawGraph();
                LayoutStatusText.Text = "Layout: Circle";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Circle Layout Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyRandomLayout()
        {
            try
            {
                _visualModel.ApplyLayout(_graphModel, _randomLayout);
                DrawGraph();
                LayoutStatusText.Text = "Layout: Random";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Random Layout Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyForceLayout()
        {
            try
            {
                _visualModel.ApplyLayout(_graphModel, _forceLayout);
                DrawGraph();
                LayoutStatusText.Text = "Layout: Force";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Force Layout Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetColors()
        {
            _visualModel.ResetColors(Colors.LightBlue, Colors.Black);
            DrawGraph();
        }

        // === Отрисовка графа ===
        private void DrawGraph()
        {
            GraphCanvas.Children.Clear();

            // 1. Рисуем рёбра
            foreach (var edge in _graphModel.Edges.Values)
            {
                DrawEdge(edge);
            }

            // 2. Рисуем вершины
            foreach (var vertex in _graphModel.Vertices.Values)
            {
                DrawVertex(vertex);
            }
        }

        private void DrawVertex(IVertex vertex)
        {
            var position = _visualModel.GetVertexPosition(vertex.Id) ?? new Point(100, 100);
            var color = _visualModel.VertexColors.ContainsKey(vertex.Id)
                ? _visualModel.VertexColors[vertex.Id]
                : Colors.LightBlue;

            // Эллипс вершины
            var ellipse = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Tag = vertex.Id
            };

            Canvas.SetLeft(ellipse, position.X - 20);
            Canvas.SetTop(ellipse, position.Y - 20);

            GraphCanvas.Children.Add(ellipse);

            // Текст с ID вершины
            var text = new TextBlock
            {
                Text = vertex.Id,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };

            Canvas.SetLeft(text, position.X - 10);
            Canvas.SetTop(text, position.Y - 12);
            GraphCanvas.Children.Add(text);
        }

        private void DrawEdge(IEdge edge)
        {
            var sourcePos = _visualModel.GetVertexPosition(edge.Source) ?? new Point(0, 0);
            var targetPos = _visualModel.GetVertexPosition(edge.Target) ?? new Point(100, 100);
            var color = _visualModel.EdgeColors.ContainsKey(edge.Id)
                ? _visualModel.EdgeColors[edge.Id]
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
                    Padding = new Thickness(2)
                };

                Canvas.SetLeft(text, midX);
                Canvas.SetTop(text, midY);
                GraphCanvas.Children.Add(text);
            }
        }

        // === Обработка мыши ===
        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(GraphCanvas);

            // Проверяем, кликнули ли по вершине
            foreach (var child in GraphCanvas.Children)
            {
                if (child is Ellipse ellipse && ellipse.Tag is string vertexId)
                {
                    var left = Canvas.GetLeft(ellipse);
                    var top = Canvas.GetTop(ellipse);
                    var rect = new Rect(left, top, ellipse.Width, ellipse.Height);

                    if (rect.Contains(position))
                    {
                        // Начинаем перетаскивание
                        _isDragging = true;
                        _draggedVertexId = vertexId;
                        _dragStartPoint = position;
                        GraphCanvas.CaptureMouse();
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && !string.IsNullOrEmpty(_draggedVertexId))
            {
                var currentPosition = e.GetPosition(GraphCanvas);
                var delta = currentPosition - _dragStartPoint;

                // Обновляем позицию в визуальной модели
                var currentPos = _visualModel.GetVertexPosition(_draggedVertexId) ?? new Point(0, 0);
                var newPosition = new Point(currentPos.X + delta.X, currentPos.Y + delta.Y);
                _visualModel.SetVertexPosition(_draggedVertexId, newPosition);

                // Перерисовываем
                DrawGraph();

                _dragStartPoint = currentPosition;
            }
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _draggedVertexId = null;
                GraphCanvas.ReleaseMouseCapture();
            }
        }

        private void OnCanvasRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Правая кнопка - сброс к режиму выбора
            SelectMode();
        }

        // === Обновление статуса ===
        private void UpdateStatus()
        {
            VertexCountText.Text = $"Vertices: {_graphModel.Vertices.Count}";
            EdgeCountText.Text = $"Edges: {_graphModel.Edges.Count}";
        }

        // === Тестовая реализация IGraphModel (временно) ===
        private IGraphModel CreateTestGraphModel()
        {
            return new TestGraphModel();
        }

        // Временный класс для тестирования
        private class TestGraphModel : IGraphModel
        {
            public bool IsDirected => false;
            public bool AllowParallelEdges => true;
            public bool AllowSelfLoops => true;

            public IReadOnlyDictionary<string, IVertex> Vertices { get; }
            public IReadOnlyDictionary<string, IEdge> Edges { get; }
            public event EventHandler? Changed;

            public TestGraphModel()
            {
                // Создаём тестовые вершины
                var vertices = new Dictionary<string, IVertex>();
                for (int i = 0; i < 6; i++)
                {
                    string id = ((char)('A' + i)).ToString();
                    vertices[id] = new TestVertex { Id = id, Label = $"Vertex {id}" };
                }

                // Создаём тестовые рёбра
                var edges = new Dictionary<string, IEdge>();
                for (int i = 0; i < 6; i++)
                {
                    string from = ((char)('A' + i)).ToString();
                    string to = ((char)('A' + (i + 1) % 6)).ToString();
                    string id = from + to;
                    edges[id] = new TestEdge { Id = id, Source = from, Target = to, Weight = i + 1 };
                }

                Vertices = vertices;
                Edges = edges;
            }

            // Реализация методов интерфейса (заглушки)
            public bool AddVertex(string id, string? label = null) => true;
            public bool RemoveVertex(string id) => true;
            public bool AddEdge(string id, string source, string target, double? weight = null, double? capacity = null) => true;
            public bool RemoveEdge(string id) => true;
            public bool SetVertexLabel(string id, string? label) => true;
            public bool SetEdgeWeight(string id, double? weight) => true;
            public bool SetEdgeCapacity(string id, double? capacity) => true;
            public bool TryGetVertex(string id, out IVertex? v) { v = null; return false; }
            public bool TryGetEdge(string id, out IEdge? e) { e = null; return false; }
            public IReadOnlyCollection<string> GetNeighbors(string vertexId) => new List<string>();
            public double[,] BuildAdjacencyMatrix(bool useWeights = true) => new double[0, 0];
            public int[,] BuildIncidenceMatrix() => new int[0, 0];
        }

        private class TestVertex : IVertex
        {
            public string Id { get; set; } = "";
            public string? Label { get; set; }
            public IReadOnlyDictionary<string, object>? Data => null;
        }

        private class TestEdge : IEdge
        {
            public string Id { get; set; } = "";
            public string Source { get; set; } = "";
            public string Target { get; set; } = "";
            public double? Weight { get; set; }
            public double? Capacity => null;
            public IReadOnlyDictionary<string, object>? Data => null;
        }
    }
}