using GraphEditor.Core;
using GraphEditor.Core.Models;
using GraphEditor.Serialization;
using GraphEditor.Visual;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;

namespace GraphEditor.Views
{
    public partial class MainWindow : Window
    {
        // Граф модель
        private GraphModel _graphModel;
        private IGraphVisualModel _visualModel;

        // Режимы работы
        private enum WorkMode { Select, Drag, AddVertex, AddEdge }
        private WorkMode _currentMode = WorkMode.Select;

        // Для перетаскивания
        private bool _isDragging = false;
        private string _draggedVertexId;
        private Point _dragStartPoint;

        // Для создания ребра
        private string _edgeStartVertexId;
        private Line _tempEdgeLine;

        // Для выделения
        private readonly HashSet<string> _selectedElements = new();

        // Цвета
        private readonly Color _defaultVertexColor = Colors.LightBlue;
        private readonly Color _defaultEdgeColor = Colors.Black;
        private readonly Color _selectedColor = Colors.Red;

        public MainWindow()
        {
            InitializeComponent();

            // Создаём граф модель с твоей реализацией
            _graphModel = new GraphModel(isDirected: false, allowParallelEdges: true, allowSelfLoops: true);

            // Добавляем тестовые вершины для демонстрации
            InitializeTestGraph();

            // Создаём визуальную модель
            _visualModel = new GraphVisualModel();

            // Инициализируем визуальную модель
            if (_visualModel is GraphVisualModel concreteVisual)
            {
                concreteVisual.InitializeFromGraph(_graphModel);
                concreteVisual.VisualChanged += OnVisualChanged;
            }

            // Настраиваем обработчики
            SetupEventHandlers();

            // Первая отрисовка
            DrawGraph();
            UpdateStatus();
        }

        private void InitializeTestGraph()
        {
            // Добавляем несколько тестовых вершин
            _graphModel.AddVertex("A", "Vertex A");
            _graphModel.AddVertex("B", "Vertex B");
            _graphModel.AddVertex("C", "Vertex C");
            _graphModel.AddVertex("D", "Vertex D");
            _graphModel.AddVertex("E", "Vertex E");

            // Добавляем тестовые рёбра
            _graphModel.AddEdge("AB", "A", "B", 5.0);
            _graphModel.AddEdge("BC", "B", "C", 3.0);
            _graphModel.AddEdge("CD", "C", "D", 7.0);
            _graphModel.AddEdge("DE", "D", "E", 2.0);
            _graphModel.AddEdge("EA", "E", "A", 4.0);
        }

        private void SetupEventHandlers()
        {
            // === Кнопки панели инструментов ===
            BtnAddVertex.Click += (s, e) => SetMode(WorkMode.AddVertex);
            BtnAddEdge.Click += (s, e) => SetMode(WorkMode.AddEdge);
            BtnCircleLayout.Click += (s, e) => ApplyCircleLayout();
            BtnRandomLayout.Click += (s, e) => ApplyRandomLayout();
            BtnSelectMode.Click += (s, e) => SetMode(WorkMode.Select);
            BtnDragMode.Click += (s, e) => SetMode(WorkMode.Drag);

            // === Пункты меню ===
            MenuItemLayoutCircle.Click += (s, e) => ApplyCircleLayout();
            MenuItemLayoutRandom.Click += (s, e) => ApplyRandomLayout();
            MenuItemResetColors.Click += (s, e) => ResetColors();
            MenuItemClearSelection.Click += (s, e) => ClearSelection();
            MenuItemFileExit.Click += (s, e) => Close();
            MenuItemFileOpen.Click += (s, e) => OpenGraph();
            MenuItemFileSave.Click += (s, e) => SaveGraph();

            // === Canvas события ===
            GraphCanvas.MouseLeftButtonDown += OnCanvasMouseDown;
            GraphCanvas.MouseMove += OnCanvasMouseMove;
            GraphCanvas.MouseLeftButtonUp += OnCanvasMouseUp;
            GraphCanvas.MouseRightButtonDown += OnCanvasRightButtonDown;
            GraphCanvas.PreviewKeyDown += OnCanvasKeyDown;

            // Событие изменения графа
            _graphModel.Changed += (sender, e) =>
            {
                DrawGraph();
                UpdateStatus();
            };
        }

        private void OnVisualChanged(object sender, EventArgs e)
        {
            DrawGraph();
            UpdateStatus();
        }

        private void SetMode(WorkMode mode)
        {
            _currentMode = mode;

            // Визуальная обратная связь
            HighlightActiveButton(mode);

            // Сбрасываем состояние создания ребра
            if (mode != WorkMode.AddEdge)
            {
                _edgeStartVertexId = null;
                ClearTempEdge();
            }
        }

        private void HighlightActiveButton(WorkMode mode)
        {
            // Сбрасываем все кнопки
            var buttons = new[] { BtnSelectMode, BtnDragMode, BtnAddVertex, BtnAddEdge };
            foreach (var button in buttons)
            {
                button.Background = Brushes.Transparent;
            }

            // Подсвечиваем активную
            switch (mode)
            {
                case WorkMode.Select:
                    BtnSelectMode.Background = new SolidColorBrush(Color.FromArgb(100, 100, 150, 255));
                    StatusText.Text = "Mode: Select (click to select, right-click for menu)";
                    break;
                case WorkMode.Drag:
                    BtnDragMode.Background = new SolidColorBrush(Color.FromArgb(100, 100, 150, 255));
                    StatusText.Text = "Mode: Drag (click and drag vertices)";
                    break;
                case WorkMode.AddVertex:
                    BtnAddVertex.Background = new SolidColorBrush(Color.FromArgb(100, 100, 150, 255));
                    StatusText.Text = "Mode: Add Vertex (click on canvas to add vertex)";
                    break;
                case WorkMode.AddEdge:
                    BtnAddEdge.Background = new SolidColorBrush(Color.FromArgb(100, 100, 150, 255));
                    StatusText.Text = "Mode: Add Edge (click two vertices to create edge)";
                    break;
            }
        }

        // === Алгоритмы расположения ===

        private void ApplyCircleLayout()
        {
            try
            {
                var layout = new CircleLayout();
                _visualModel.ApplyLayout(_graphModel, layout);
                LayoutStatusText.Text = "Layout: Circle";
                StatusText.Text = "Applied circle layout";
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
                var layout = new RandomLayout();
                _visualModel.ApplyLayout(_graphModel, layout);
                LayoutStatusText.Text = "Layout: Random";
                StatusText.Text = "Applied random layout";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Random Layout Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetColors()
        {
            if (_visualModel is GraphVisualModel concreteVisual)
            {
                concreteVisual.ResetColors();
            }
            StatusText.Text = "Colors reset to default";
        }

        private void ClearSelection()
        {
            if (_visualModel is GraphVisualModel concreteVisual)
            {
                concreteVisual.ClearSelection();
            }
            _selectedElements.Clear();
            StatusText.Text = "Selection cleared";
        }

        // === Обработка мыши ===

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(GraphCanvas);
            Debug.WriteLine($"MouseDown at {position}, Mode: {_currentMode}");

            if (e.ChangedButton == MouseButton.Left)
            {
                switch (_currentMode)
                {
                    case WorkMode.Select:
                        HandleSelectClick(position);
                        break;

                    case WorkMode.Drag:
                        StartDragging(position);
                        break;

                    case WorkMode.AddVertex:
                        AddVertexAtPosition(position);
                        break;

                    case WorkMode.AddEdge:
                        HandleAddEdgeClick(position);
                        break;
                }
            }

            e.Handled = true;
        }

        private void HandleSelectClick(Point position)
        {
            var element = FindElementAtPoint(position);
            Debug.WriteLine($"HandleSelectClick: element found = {element != null}");

            if (element != null && element.Tag is string id)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    // Ctrl+click - добавляем к выделению
                    ToggleElementSelection(element);
                }
                else
                {
                    // Обычный клик - выделяем только этот элемент
                    ClearSelection();
                    SelectElement(element);
                }
            }
            else
            {
                // Клик на пустом месте - снимаем выделение
                ClearSelection();
            }
        }

        private void HandleAddEdgeClick(Point position)
        {
            Debug.WriteLine($"HandleAddEdgeClick at {position}");

            var element = FindElementAtPoint(position);
            if (element != null && element.Tag is string vertexId)
            {
                Debug.WriteLine($"Clicked on vertex: {vertexId}");

                if (_edgeStartVertexId == null)
                {
                    // Первый клик - выбираем начальную вершину
                    _edgeStartVertexId = vertexId;
                    StatusText.Text = $"Edge from {vertexId} - select target vertex";

                    // Подсвечиваем начальную вершину
                    _visualModel.SetVertexColor(vertexId, Colors.Orange);

                    // Показываем временную линию
                    StartTempEdge(vertexId, position);
                }
                else if (_edgeStartVertexId != vertexId)
                {
                    // Второй клик на другой вершине - создаём ребро
                    Debug.WriteLine($"Creating edge from {_edgeStartVertexId} to {vertexId}");
                    CreateEdge(_edgeStartVertexId, vertexId);

                    // Снимаем подсветку с начальной вершины
                    ResetVertexColor(_edgeStartVertexId);

                    _edgeStartVertexId = null;
                    ClearTempEdge();
                    SetMode(WorkMode.AddEdge); // Остаёмся в режиме добавления рёбер
                }
                else
                {
                    // Клик на той же вершине - отмена
                    Debug.WriteLine($"Cancelling edge creation");
                    ResetVertexColor(_edgeStartVertexId);
                    _edgeStartVertexId = null;
                    ClearTempEdge();
                    StatusText.Text = "Edge creation cancelled";
                }
            }
            else
            {
                Debug.WriteLine($"Clicked on empty space");
                // Клик не на вершине
                if (_edgeStartVertexId != null)
                {
                    // Сбрасываем создание ребра
                    ResetVertexColor(_edgeStartVertexId);
                    _edgeStartVertexId = null;
                    ClearTempEdge();
                }
            }
        }

        private void ResetVertexColor(string vertexId)
        {
            if (_visualModel is GraphVisualModel concreteVisual)
            {
                if (concreteVisual.SelectedVertices.Contains(vertexId))
                {
                    _visualModel.SetVertexColor(vertexId, _selectedColor);
                }
                else
                {
                    _visualModel.SetVertexColor(vertexId, _defaultVertexColor);
                }
            }
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            // Обновляем временную линию при создании ребра
            if (_currentMode == WorkMode.AddEdge && _edgeStartVertexId != null && _tempEdgeLine != null)
            {
                var position = e.GetPosition(GraphCanvas);
                var startPos = _visualModel.GetVertexPosition(_edgeStartVertexId) ?? new Point(0, 0);
                _tempEdgeLine.X2 = position.X;
                _tempEdgeLine.Y2 = position.Y;
            }

            // Обработка перетаскивания
            if (_currentMode == WorkMode.Drag && _isDragging && !string.IsNullOrEmpty(_draggedVertexId))
            {
                var currentPosition = e.GetPosition(GraphCanvas);
                var delta = currentPosition - _dragStartPoint;

                // Обновляем позицию в визуальной модели
                var currentPos = _visualModel.GetVertexPosition(_draggedVertexId) ?? new Point(0, 0);
                var newPosition = new Point(currentPos.X + delta.X, currentPos.Y + delta.Y);
                _visualModel.SetVertexPosition(_draggedVertexId, newPosition);

                _dragStartPoint = currentPosition;
            }
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                StopDragging();
            }
        }

        private void OnCanvasRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(GraphCanvas);
            var element = FindElementAtPoint(position);

            if (element != null && element.Tag is string id)
            {
                // Показываем контекстное меню для элемента
                ShowElementContextMenu(position, element, id);
                e.Handled = true;
            }
        }

        private void ShowElementContextMenu(Point position, FrameworkElement element, string id)
        {
            var menu = new ContextMenu();

            if (element is Ellipse)
            {
                // Контекстное меню для вершины
                var changeLabelItem = new MenuItem { Header = "Change Label" };
                changeLabelItem.Click += (s, e) => ChangeVertexLabel(id);

                var changeColorItem = new MenuItem { Header = "Change Color" };
                changeColorItem.Click += (s, e) => ChangeVertexColor(id);

                var deleteVertexItem = new MenuItem { Header = "Delete Vertex" };
                deleteVertexItem.Click += (s, e) => DeleteVertex(id);

                menu.Items.Add(changeLabelItem);
                menu.Items.Add(changeColorItem);
                menu.Items.Add(new Separator());
                menu.Items.Add(deleteVertexItem);
            }
            else if (element is Line)
            {
                // Контекстное меню для ребра
                if (_graphModel.TryGetEdge(id, out var edge))
                {
                    var changeWeightItem = new MenuItem
                    {
                        Header = $"Change Weight (current: {edge.Weight ?? 1})"
                    };
                    changeWeightItem.Click += (s, e) => ChangeEdgeWeight(id);

                    var changeColorItem = new MenuItem { Header = "Change Color" };
                    changeColorItem.Click += (s, e) => ChangeEdgeColor(id);

                    var deleteEdgeItem = new MenuItem { Header = "Delete Edge" };
                    deleteEdgeItem.Click += (s, e) => DeleteEdge(id);

                    menu.Items.Add(changeWeightItem);
                    menu.Items.Add(changeColorItem);
                    menu.Items.Add(new Separator());
                    menu.Items.Add(deleteEdgeItem);
                }
            }

            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            menu.IsOpen = true;
        }

        private void OnCanvasKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteSelectedElements();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                ClearSelection();
                SetMode(WorkMode.Select);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.D1: // Ctrl+1 - Circle Layout
                        ApplyCircleLayout();
                        e.Handled = true;
                        break;
                    case Key.D2: // Ctrl+2 - Random Layout
                        ApplyRandomLayout();
                        e.Handled = true;
                        break;
                    case Key.A: // Ctrl+A - Select All
                        SelectAll();
                        e.Handled = true;
                        break;
                }
            }
        }

        private void SelectAll()
        {
            ClearSelection();

            if (_visualModel is GraphVisualModel concreteVisual)
            {
                // Выделяем все вершины
                foreach (var vertexId in _graphModel.Vertices.Keys)
                {
                    concreteVisual.SelectVertex(vertexId, true);
                    _selectedElements.Add(vertexId);
                }

                // Выделяем все рёбра
                foreach (var edgeId in _graphModel.Edges.Keys)
                {
                    concreteVisual.SelectEdge(edgeId, true);
                    _selectedElements.Add(edgeId);
                }

                StatusText.Text = $"Selected all: {_graphModel.Vertices.Count} vertices, {_graphModel.Edges.Count} edges";
            }
        }

        // === Вспомогательные методы для работы с элементами ===

        private FrameworkElement FindElementAtPoint(Point position)
        {
            // Ищем вершины по расстоянию до их центра
            foreach (var vertex in _graphModel.Vertices.Values)
            {
                var vertexPos = _visualModel.GetVertexPosition(vertex.Id);
                if (vertexPos.HasValue)
                {
                    // Проверяем попадание в круг радиусом 30 пикселей
                    var distance = Math.Sqrt(
                        Math.Pow(position.X - vertexPos.Value.X, 2) +
                        Math.Pow(position.Y - vertexPos.Value.Y, 2));

                    if (distance <= 30) // 30 пикселей от центра
                    {
                        Debug.WriteLine($"Found vertex {vertex.Id} at distance {distance}");
                        return new Ellipse() { Tag = vertex.Id };
                    }
                }
            }

            // Ищем рёбра (упрощённо - по близости к линии)
            foreach (var edge in _graphModel.Edges.Values)
            {
                var sourcePos = _visualModel.GetVertexPosition(edge.Source) ?? new Point(0, 0);
                var targetPos = _visualModel.GetVertexPosition(edge.Target) ?? new Point(0, 0);

                var distance = DistanceToLine(position, sourcePos, targetPos);
                if (distance < 8) // Порог 8 пикселей
                {
                    Debug.WriteLine($"Found edge {edge.Id} at distance {distance}");
                    return new Line() { Tag = edge.Id };
                }
            }

            return null;
        }

        private double DistanceToLine(Point p, Point lineStart, Point lineEnd)
        {
            var lineLength = Math.Sqrt(Math.Pow(lineEnd.X - lineStart.X, 2) + Math.Pow(lineEnd.Y - lineStart.Y, 2));
            if (lineLength == 0) return Math.Sqrt(Math.Pow(p.X - lineStart.X, 2) + Math.Pow(p.Y - lineStart.Y, 2));

            var t = Math.Max(0, Math.Min(1, ((p.X - lineStart.X) * (lineEnd.X - lineStart.X) +
                                            (p.Y - lineStart.Y) * (lineEnd.Y - lineStart.Y)) / (lineLength * lineLength)));

            var projection = new Point(
                lineStart.X + t * (lineEnd.X - lineStart.X),
                lineStart.Y + t * (lineEnd.Y - lineStart.Y)
            );

            return Math.Sqrt(Math.Pow(p.X - projection.X, 2) + Math.Pow(p.Y - projection.Y, 2));
        }

        private void SelectElement(FrameworkElement element)
        {
            if (element.Tag is string id)
            {
                var concreteVisual = _visualModel as GraphVisualModel;
                if (concreteVisual == null) return;

                if (element is Ellipse)
                {
                    concreteVisual.SelectVertex(id, true);
                    _selectedElements.Add(id);
                }
                else if (element is Line)
                {
                    concreteVisual.SelectEdge(id, true);
                    _selectedElements.Add(id);
                }

                StatusText.Text = $"Selected: {id}";
            }
        }

        private void ToggleElementSelection(FrameworkElement element)
        {
            if (element.Tag is string id)
            {
                var concreteVisual = _visualModel as GraphVisualModel;
                if (concreteVisual == null) return;

                if (_selectedElements.Contains(id))
                {
                    // Снимаем выделение
                    if (element is Ellipse)
                    {
                        concreteVisual.SelectVertex(id, false);
                        _selectedElements.Remove(id);
                    }
                    else if (element is Line)
                    {
                        concreteVisual.SelectEdge(id, false);
                        _selectedElements.Remove(id);
                    }
                }
                else
                {
                    // Добавляем выделение
                    SelectElement(element);
                }
            }
        }

        private void StartDragging(Point position)
        {
            var element = FindElementAtPoint(position);
            if (element is Ellipse ellipse && ellipse.Tag is string vertexId)
            {
                _isDragging = true;
                _draggedVertexId = vertexId;
                _dragStartPoint = position;
                GraphCanvas.CaptureMouse();
                StatusText.Text = $"Dragging vertex {vertexId}";
            }
        }

        private void StopDragging()
        {
            _isDragging = false;
            _draggedVertexId = null;
            GraphCanvas.ReleaseMouseCapture();
            StatusText.Text = "Drag completed";
        }

        // === Основные операции с графом ===

        private void AddVertexAtPosition(Point position)
        {
            try
            {
                // Генерируем уникальный ID
                string newId = GenerateVertexId();
                Debug.WriteLine($"Adding vertex {newId} at {position}");

                // Добавляем вершину в модель
                if (_graphModel.AddVertex(newId, $"Vertex {newId}"))
                {
                    // Добавляем вершину в визуальную модель
                    if (_visualModel is GraphVisualModel concreteVisual)
                    {
                        concreteVisual.AddVertex(newId, position);
                    }

                    StatusText.Text = $"Vertex {newId} added";
                }
                else
                {
                    MessageBox.Show($"Failed to add vertex {newId}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Add Vertex Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateEdge(string sourceId, string targetId)
        {
            try
            {
                // Генерируем ID ребра
                string edgeId = $"{sourceId}-{targetId}";
                Debug.WriteLine($"Creating edge {edgeId} from {sourceId} to {targetId}");

                // Запрашиваем вес у пользователя
                var dialog = new InputDialog("Enter edge weight:", $"Weight for {sourceId}-{targetId}", "1.0");
                if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.Answer))
                {
                    if (double.TryParse(dialog.Answer, out double weight))
                    {
                        // Добавляем ребро в модель
                        if (_graphModel.AddEdge(edgeId, sourceId, targetId, weight))
                        {
                            // Добавляем ребро в визуальную модель
                            if (_visualModel is GraphVisualModel concreteVisual)
                            {
                                concreteVisual.AddEdge(edgeId);
                            }

                            StatusText.Text = $"Edge {sourceId}-{targetId} created with weight {weight}";
                        }
                        else
                        {
                            MessageBox.Show($"Failed to create edge between {sourceId} and {targetId}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please enter a valid number for weight",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Create Edge Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangeVertexLabel(string vertexId)
        {
            if (_graphModel.TryGetVertex(vertexId, out var vertex))
            {
                var dialog = new InputDialog("Enter new label:", $"Change label for {vertexId}", vertex.Label);
                if (dialog.ShowDialog() == true)
                {
                    _graphModel.SetVertexLabel(vertexId, dialog.Answer);
                    StatusText.Text = $"Vertex {vertexId} label changed to '{dialog.Answer}'";
                }
            }
        }

        private void ChangeVertexColor(string vertexId)
        {
            // Простой выбор из 4 основных цветов
            var menu = new ContextMenu();

            var colors = new[]
            {
                new { Name = "🔵 Light Blue", Color = Colors.LightBlue },
                new { Name = "🟢 Light Green", Color = Colors.LightGreen },
                new { Name = "🔴 Red", Color = Colors.Red },
                new { Name = "🟡 Yellow", Color = Colors.Yellow },
                new { Name = "🟣 Purple", Color = Colors.Purple },
                new { Name = "🟠 Orange", Color = Colors.Orange }
            };

            foreach (var color in colors)
            {
                var menuItem = new MenuItem { Header = color.Name };
                menuItem.Click += (s, e) =>
                {
                    _visualModel.SetVertexColor(vertexId, color.Color);
                    StatusText.Text = $"Vertex {vertexId} color changed";
                };
                menu.Items.Add(menuItem);
            }

            menu.IsOpen = true;
        }

        private void ChangeEdgeWeight(string edgeId)
        {
            if (_graphModel.TryGetEdge(edgeId, out var edge))
            {
                var dialog = new InputDialog("Enter new weight:", $"Change weight for edge {edgeId}",
                    edge.Weight?.ToString() ?? "1.0");
                if (dialog.ShowDialog() == true)
                {
                    if (double.TryParse(dialog.Answer, out double newWeight))
                    {
                        _graphModel.SetEdgeWeight(edgeId, newWeight);
                        StatusText.Text = $"Edge {edgeId} weight changed to {newWeight}";
                        DrawGraph(); // Перерисовываем, чтобы отобразить новый вес
                    }
                    else
                    {
                        MessageBox.Show("Please enter a valid number",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ChangeEdgeColor(string edgeId)
        {
            // Простой выбор из 4 основных цветов
            var menu = new ContextMenu();

            var colors = new[]
            {
                new { Name = "⚫ Black", Color = Colors.Black },
                new { Name = "🔴 Red", Color = Colors.Red },
                new { Name = "🔵 Blue", Color = Colors.Blue },
                new { Name = "🟢 Green", Color = Colors.Green },
                new { Name = "🟣 Purple", Color = Colors.Purple },
                new { Name = "🟠 Orange", Color = Colors.Orange }
            };

            foreach (var color in colors)
            {
                var menuItem = new MenuItem { Header = color.Name };
                menuItem.Click += (s, e) =>
                {
                    _visualModel.SetEdgeColor(edgeId, color.Color);
                    StatusText.Text = $"Edge {edgeId} color changed";
                };
                menu.Items.Add(menuItem);
            }

            menu.IsOpen = true;
        }

        private void DeleteVertex(string vertexId)
        {
            var result = MessageBox.Show($"Delete vertex {vertexId} and all connected edges?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _graphModel.RemoveVertex(vertexId);
                if (_visualModel is GraphVisualModel concreteVisual)
                {
                    concreteVisual.RemoveVertex(vertexId);
                }
                _selectedElements.Remove(vertexId);
                StatusText.Text = $"Vertex {vertexId} deleted";
            }
        }

        private void DeleteEdge(string edgeId)
        {
            if (_graphModel.TryGetEdge(edgeId, out var edge))
            {
                var result = MessageBox.Show($"Delete edge {edge.Source}-{edge.Target}?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _graphModel.RemoveEdge(edgeId);
                    if (_visualModel is GraphVisualModel concreteVisual)
                    {
                        concreteVisual.RemoveEdge(edgeId);
                    }
                    _selectedElements.Remove(edgeId);
                    StatusText.Text = $"Edge {edgeId} deleted";
                }
            }
        }

        private void DeleteSelectedElements()
        {
            try
            {
                var verticesToDelete = new List<string>();
                var edgesToDelete = new List<string>();

                foreach (var elementId in _selectedElements.ToList())
                {
                    if (_graphModel.Vertices.ContainsKey(elementId))
                        verticesToDelete.Add(elementId);
                    else if (_graphModel.Edges.ContainsKey(elementId))
                        edgesToDelete.Add(elementId);
                }

                // Удаляем рёбра
                foreach (var edgeId in edgesToDelete)
                {
                    _graphModel.RemoveEdge(edgeId);
                    if (_visualModel is GraphVisualModel concreteVisual)
                    {
                        concreteVisual.RemoveEdge(edgeId);
                    }
                }

                // Удаляем вершины (после рёбер, чтобы не было проблем со ссылками)
                foreach (var vertexId in verticesToDelete)
                {
                    _graphModel.RemoveVertex(vertexId);
                    if (_visualModel is GraphVisualModel concreteVisual)
                    {
                        concreteVisual.RemoveVertex(vertexId);
                    }
                }

                ClearSelection();
                StatusText.Text = $"Deleted {verticesToDelete.Count} vertices and {edgesToDelete.Count} edges";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartTempEdge(string vertexId, Point position)
        {
            var startPos = _visualModel.GetVertexPosition(vertexId) ?? new Point(0, 0);

            _tempEdgeLine = new Line
            {
                X1 = startPos.X,
                Y1 = startPos.Y,
                X2 = position.X,
                Y2 = position.Y,
                Stroke = Brushes.Red,
                StrokeThickness = 3,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Opacity = 0.7
            };

            GraphCanvas.Children.Add(_tempEdgeLine);
        }

        private void ClearTempEdge()
        {
            if (_tempEdgeLine != null)
            {
                GraphCanvas.Children.Remove(_tempEdgeLine);
                _tempEdgeLine = null;
            }
        }

        // === Отрисовка графа ===

        private void DrawGraph()
        {
            Dispatcher.Invoke(() =>
            {
                GraphCanvas.Children.Clear();

                // 1. Рисуем рёбра (сначала, чтобы были под вершинами)
                foreach (var edge in _graphModel.Edges.Values)
                {
                    DrawEdge(edge);
                }

                // 2. Рисуем вершины (сверху рёбер)
                foreach (var vertex in _graphModel.Vertices.Values)
                {
                    DrawVertex(vertex);
                }
            });
        }

        private void DrawVertex(IVertex vertex)
        {
            var position = _visualModel.GetVertexPosition(vertex.Id) ?? new Point(100, 100);
            var color = _visualModel.VertexColors.ContainsKey(vertex.Id)
                ? _visualModel.VertexColors[vertex.Id]
                : _defaultVertexColor;

            var ellipse = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Tag = vertex.Id,
                ToolTip = vertex.Label ?? vertex.Id
            };

            Canvas.SetLeft(ellipse, position.X - 20);
            Canvas.SetTop(ellipse, position.Y - 20);
            GraphCanvas.Children.Add(ellipse);

            var text = new TextBlock
            {
                Text = vertex.Id,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Tag = vertex.Id
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
                : _defaultEdgeColor;

            var line = new Line
            {
                X1 = sourcePos.X,
                Y1 = sourcePos.Y,
                X2 = targetPos.X,
                Y2 = targetPos.Y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
                Tag = edge.Id,
                ToolTip = edge.Weight.HasValue ? $"Weight: {edge.Weight}" : "Edge"
            };

            GraphCanvas.Children.Add(line);

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
                    Padding = new Thickness(2),
                    Tag = edge.Id
                };

                Canvas.SetLeft(text, midX);
                Canvas.SetTop(text, midY);
                GraphCanvas.Children.Add(text);
            }
        }

        // === Вспомогательные методы ===

        private string GenerateVertexId()
        {
            // Ищем следующую букву начиная с F (после тестовых A-E)
            char nextChar = 'F';

            // Проверяем существующие вершины
            var existingIds = _graphModel.Vertices.Keys
                .Where(id => id.Length == 1 && char.IsLetter(id[0]))
                .Select(id => char.ToUpper(id[0]))
                .ToHashSet();

            // Находим первую свободную букву от F до Z
            for (char c = 'F'; c <= 'Z'; c++)
            {
                if (!existingIds.Contains(c))
                {
                    return c.ToString();
                }
            }

            // Если все буквы заняты, переходим к числам
            int num = 1;
            while (_graphModel.Vertices.ContainsKey($"V{num}"))
            {
                num++;
            }
            return $"V{num}";
        }

        private void UpdateStatus()
        {
            Dispatcher.Invoke(() =>
            {
                VertexCountText.Text = $"Vertices: {_graphModel.Vertices.Count}";
                EdgeCountText.Text = $"Edges: {_graphModel.Edges.Count}";

                if (_selectedElements.Count > 0)
                {
                    StatusText.Text = $"{_selectedElements.Count} elements selected";
                }
                else
                {
                    StatusText.Text = $"Ready - {_graphModel.Vertices.Count} vertices, {_graphModel.Edges.Count} edges";
                }
            });
        }

        private void OpenGraph()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json",
                    Title = "Open Graph"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var serializer = new GraphSerializer();
                    var loadedGraph = serializer.LoadFromFile(openFileDialog.FileName);

                    if (loadedGraph is GraphModel graphModel)
                    {
                        // Сбрасываем текущий граф
                        _graphModel = graphModel;

                        // Создаем новую визуальную модель
                        var newVisualModel = new GraphVisualModel();
                        newVisualModel.InitializeFromGraph(_graphModel);
                        newVisualModel.VisualChanged += OnVisualChanged;

                        // Отписываемся от старой визуальной модели
                        if (_visualModel is GraphVisualModel oldVisual)
                        {
                            oldVisual.VisualChanged -= OnVisualChanged;
                        }

                        _visualModel = newVisualModel;

                        // Сбрасываем состояние
                        _selectedElements.Clear();
                        _edgeStartVertexId = null;
                        ClearTempEdge();
                        SetMode(WorkMode.Select);

                        // Отрисовываем новый граф
                        DrawGraph();
                        UpdateStatus();

                        StatusText.Text = $"Graph loaded from {openFileDialog.FileName}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading graph: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveGraph()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json",
                    Title = "Save Graph",
                    AddExtension = true
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var serializer = new GraphSerializer();
                    serializer.SaveToFile(_graphModel, saveFileDialog.FileName);

                    StatusText.Text = $"Graph saved to {saveFileDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving graph: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Класс InputDialog (оставляем как есть)
    public class InputDialog : Window
    {
        public string Answer { get; private set; }

        public InputDialog(string question, string title, string defaultValue = "")
        {
            Title = title;
            Width = 300;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            stackPanel.Children.Add(new TextBlock
            {
                Text = question,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var textBox = new TextBox
            {
                Text = defaultValue,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(textBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            okButton.Click += (s, e) =>
            {
                Answer = textBox.Text;
                DialogResult = true;
                Close();
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                IsCancel = true
            };
            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);

            Content = stackPanel;

            Loaded += (s, e) => textBox.Focus();
        }
    }
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
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
        
