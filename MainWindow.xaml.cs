using GraphEditor.Algorithms;
using GraphEditor.Core;
using GraphEditor.Core.Models;
using GraphEditor.Services;
using GraphEditor.Visual;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GraphEditor.Views;

namespace GraphEditor
{
    public partial class MainWindow : Window
    {
        private readonly InteractionService _interactionService;
        private readonly CommandService _commandService;
        private readonly ObservableGraphModel _graphModel;
        private readonly GraphVisualModel _visualModel;
        private readonly ContextMenuService _contextMenuService;
        private readonly AlgorithmService _algorithmService;
        private readonly MatrixService _matrixService = new MatrixService();

        // Поля для обработки двойного клика
        private DateTime _lastClickTime = DateTime.MinValue;
        private string _lastClickedElementId = null;
        private const int DoubleClickInterval = 500;

        public MainWindow()
        {
            InitializeComponent();

            // Создаём модели
            _graphModel = new ObservableGraphModel(isDirected: false, allowParallelEdges: true, allowSelfLoops: true);
            _visualModel = new GraphVisualModel();

            // Инициализация тестового графа
            InitializeTestGraph();
            _visualModel.InitializeFromGraph(_graphModel);

            // Создаём сервисы
            _commandService = new CommandService(_graphModel, _visualModel);
            _interactionService = new InteractionService(GraphCanvas, _graphModel, _visualModel);
            _contextMenuService = new ContextMenuService(_commandService, _graphModel);
            _algorithmService = new AlgorithmService(_graphModel, _visualModel);

            // ДОБАВЬТЕ ЭТИ СТРОКИ ДЛЯ ПОДПИСКИ НА СОБЫТИЯ МЫШИ:
            GraphCanvas.MouseRightButtonDown += OnCanvasRightButtonDown;
            GraphCanvas.MouseLeftButtonDown += OnCanvasMouseDown;
            GraphCanvas.MouseMove += OnCanvasMouseMove;
            GraphCanvas.MouseLeftButtonUp += OnCanvasMouseUp;
            GraphCanvas.PreviewKeyDown += OnCanvasKeyDown;

            // Подписка на события из ContextMenuService
            _contextMenuService.StartEdgeFromVertexRequested += StartEdgeFromVertex;

            // Настройка событий
            SetupEventHandlers();

            // ИНИЦИАЛИЗАЦИЯ МАТРИЦЫ
            InitializeMatrixPanel();

            // Первая отрисовка
            RedrawGraph();
            UpdateStatus();

            Console.WriteLine("MainWindow initialized successfully");
            Console.WriteLine($"Graph has {_graphModel.Vertices.Count} vertices and {_graphModel.Edges.Count} edges");
        }

        private void InitializeTestGraph()
        {
            try
            {
                Console.WriteLine("Initializing test graph...");

                _graphModel.AddVertex("A", "Vertex A");
                _graphModel.AddVertex("B", "Vertex B");
                _graphModel.AddVertex("C", "Vertex C");
                _graphModel.AddVertex("D", "Vertex D");
                _graphModel.AddVertex("E", "Vertex E");

                _graphModel.AddEdge("AB", "A", "B", 5.0, 10.0); // weight=5, capacity=10
                _graphModel.AddEdge("BC", "B", "C", 3.0, 8.0);
                _graphModel.AddEdge("CD", "C", "D", 7.0, 15.0);
                _graphModel.AddEdge("DE", "D", "E", 2.0, 5.0);
                _graphModel.AddEdge("EA", "E", "A", 4.0, 12.0);

                // Добавим ещё рёбра для лучшего тестирования
                _graphModel.AddEdge("AC", "A", "C", 6.0, 7.0);
                _graphModel.AddEdge("BD", "B", "D", 4.0, 9.0);

                Console.WriteLine("Test graph initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing test graph: {ex.Message}");
            }
        }

        private void ShowAlgorithmDialog()
        {
            try
            {
                // Получаем список доступных вершин
                var availableVertices = _graphModel.Vertices.Keys.ToList();

                if (availableVertices.Count == 0)
                {
                    MessageBox.Show("No vertices in graph. Add vertices first.", "Info",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Console.WriteLine($"Showing algorithm dialog. Available vertices: {string.Join(", ", availableVertices)}");

                var dialog = new AlgorithmDialog(_algorithmService, availableVertices);
                dialog.Owner = this;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() == true && dialog.SelectedAlgorithm != null)
                {
                    Console.WriteLine($"Algorithm selected: {dialog.SelectedAlgorithm.Id}");
                    Console.WriteLine($"Parameters: {string.Join(", ", dialog.Parameters.Select(p => $"{p.Key}={p.Value}"))}");

                    // 1. Сбросить цвета перед новым алгоритмом
                    ResetAlgorithmVisualization();

                    // 2. Выполнить алгоритм
                    var result = _algorithmService.RunAlgorithm(dialog.SelectedAlgorithm.Id, dialog.Parameters);

                    // 3. Сразу перерисовать граф (ДО MessageBox!)
                    RedrawGraph();
                    UpdateStatus();

                    // 4. Показать результат
                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Algorithm Result",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Algorithm Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    Console.WriteLine("Algorithm dialog cancelled");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShowAlgorithmDialog: {ex}");
                MessageBox.Show($"Error showing algorithm dialog:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetAlgorithmVisualization()
        {
            try
            {
                Console.WriteLine("Resetting algorithm visualization...");
                _algorithmService.ResetVisualization();
                RedrawGraph();
                UpdateStatus();
                StatusText.Text = "Algorithm visualization reset";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting algorithm visualization: {ex.Message}");
            }
        }

        private void SetupEventHandlers()
        {
            // === Подписка на события InteractionService ===
            _interactionService.StatusChanged += OnStatusChanged;
            _interactionService.VertexAddRequested += OnVertexAddRequested;
            _interactionService.EdgeAddRequested += OnEdgeAddRequested;
            _interactionService.ElementSelected += OnElementSelected;
            _interactionService.SelectionCleared += OnSelectionCleared;
            _interactionService.VisualChanged += OnVisualChanged;
            _interactionService.DeleteRequested += OnDeleteRequested;
            _interactionService.VertexRightClicked += OnVertexRightClicked;
            _interactionService.EdgeRightClicked += OnEdgeRightClicked;
            _interactionService.EmptySpaceRightClicked += OnEmptySpaceRightClicked;

            // === Подписка на события CommandService ===
            _commandService.OperationCompleted += OnOperationCompleted;
            _commandService.ErrorOccurred += OnErrorOccurred;
            _commandService.VisualChanged += OnVisualChanged;

            // === События графа ===
            _graphModel.Changed += OnGraphChanged;

            // === Обработчики кнопок ===
            BtnSelectMode.Click += (s, e) => _interactionService.SetMode(WorkMode.Select);
            BtnDragMode.Click += (s, e) => _interactionService.SetMode(WorkMode.Drag);
            BtnAddVertex.Click += (s, e) => _interactionService.SetMode(WorkMode.AddVertex);
            BtnAddEdge.Click += (s, e) => _interactionService.SetMode(WorkMode.AddEdge);

            BtnCircleLayout.Click += (s, e) => _commandService.ApplyCircleLayout();
            BtnRandomLayout.Click += (s, e) => _commandService.ApplyRandomLayout();
            BtnResetColors.Click += (s, e) => _commandService.ResetColors();
            BtnDelete.Click += (s, e) => DeleteSelected();

            // === АЛГОРИТМЫ: ОБРАБОТЧИКИ КНОПОК ===
            //BtnAlgorithms.Click += (s, e) => ShowAlgorithmDialog();
            //BtnResetAlgorithm.Click += (s, e) => ResetAlgorithmVisualization();

            // === Обработчики меню ===
            MenuItemLayoutCircle.Click += (s, e) => _commandService.ApplyCircleLayout();
            MenuItemLayoutRandom.Click += (s, e) => _commandService.ApplyRandomLayout();
            MenuItemViewResetColors.Click += (s, e) => _commandService.ResetColors();
            MenuItemEditClearSelection.Click += (s, e) => _interactionService.SetMode(WorkMode.Select);
            MenuItemFileOpen.Click += (s, e) => OpenGraph();
            MenuItemFileSave.Click += (s, e) => SaveGraph();
            MenuItemFileExit.Click += (s, e) => Close();
            MenuItemEditSelectAll.Click += (s, e) => SelectAll();
            MenuItemHelpAbout.Click += (s, e) => ShowAbout();

            // === ИСПРАВЛЕННЫЕ обработчики для меню алгоритмов ===
            MenuItemAlgoDijkstra.Click += (s, e) => ShowAlgorithmDialog();
            MenuItemAlgoMST.Click += (s, e) => ShowAlgorithmDialog();
            MenuItemAlgoMaxFlow.Click += (s, e) => ShowAlgorithmDialog();

            Console.WriteLine("Event handlers setup complete");
        }

        // === Обработчики событий Canvas ===

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(GraphCanvas);

            if (e.ChangedButton == MouseButton.Left)
            {
                var elementId = FindElementAtPosition(position);

                // Проверка на двойной клик по ребру
                if (!string.IsNullOrEmpty(elementId) && _graphModel.Edges.ContainsKey(elementId))
                {
                    var now = DateTime.Now;
                    var timeSinceLastClick = (now - _lastClickTime).TotalMilliseconds;

                    if (_lastClickedElementId == elementId && timeSinceLastClick < DoubleClickInterval)
                    {
                        // Двойной клик по ребру - изменение веса
                        OnEdgeDoubleClicked(elementId);
                        _lastClickTime = DateTime.MinValue;
                        _lastClickedElementId = null;
                        return; // Не передаем дальше в InteractionService
                    }

                    // Запоминаем для следующего клика
                    _lastClickTime = now;
                    _lastClickedElementId = elementId;
                }
                else
                {
                    // Сбрасываем, если кликнули не на ребре
                    _lastClickTime = DateTime.MinValue;
                    _lastClickedElementId = null;
                }
            }

            // Передаем в InteractionService (обычный клик или правый клик)
            _interactionService.HandleMouseDown(position, e.ChangedButton, Keyboard.Modifiers);
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(GraphCanvas);
            _interactionService.HandleMouseMove(position);
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            _interactionService.HandleMouseUp();
        }

        private void OnCanvasRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(GraphCanvas);
            _interactionService.HandleMouseDown(position, e.ChangedButton, Keyboard.Modifiers);
        }

        private void OnCanvasKeyDown(object sender, KeyEventArgs e)
        {
            _interactionService.HandleKeyDown(e.Key, Keyboard.Modifiers);
        }

        // === Вспомогательные методы для поиска элементов ===

        private string FindElementAtPosition(Point position)
        {
            // Сначала ищем вершину
            var vertexId = FindVertexAtPosition(position);
            if (!string.IsNullOrEmpty(vertexId))
                return vertexId;

            // Потом ищем ребро
            return FindEdgeAtPosition(position);
        }

        private string FindVertexAtPosition(Point position)
        {
            foreach (var vertex in _graphModel.Vertices.Values)
            {
                var vertexPos = _visualModel.GetVertexPosition(vertex.Id);
                if (vertexPos.HasValue)
                {
                    var distance = Math.Sqrt(
                        Math.Pow(position.X - vertexPos.Value.X, 2) +
                        Math.Pow(position.Y - vertexPos.Value.Y, 2));

                    if (distance <= 50)
                        return vertex.Id;
                }
            }
            return null;
        }

        private string FindEdgeAtPosition(Point position)
        {
            foreach (var edge in _graphModel.Edges.Values)
            {
                var sourcePos = _visualModel.GetVertexPosition(edge.Source) ?? new Point(0, 0);
                var targetPos = _visualModel.GetVertexPosition(edge.Target) ?? new Point(0, 0);

                var distance = DistanceToLine(position, sourcePos, targetPos);
                if (distance < 10)
                    return edge.Id;
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

        // === Обработчики событий сервисов ===

        private void OnStatusChanged(string message)
        {
            Dispatcher.Invoke(() => StatusText.Text = message);
        }

        private void OnVertexAddRequested(Point position)
        {
            _commandService.AddVertexAtPosition(position);
        }

        private void OnEdgeAddRequested(string source, string target)
        {
            var dialog = new InputDialog($"Enter weight for edge {source}-{target}:", "Edge Weight", "1.0");
            if (dialog.ShowDialog() == true && double.TryParse(dialog.Answer, out double weight))
            {
                string edgeId = $"{source}-{target}";
                _commandService.AddEdge(edgeId, source, target, weight);
            }
        }

        // Метод для обработки двойного клика по ребру
        private void OnEdgeDoubleClicked(string edgeId)
        {
            if (_graphModel.TryGetEdge(edgeId, out var edge))
            {
                var dialog = new InputDialog($"Enter new weight for edge {edge.Source}-{edge.Target}:",
                    "Change Edge Weight",
                    edge.Weight?.ToString() ?? "1.0");

                if (dialog.ShowDialog() == true && double.TryParse(dialog.Answer, out double newWeight))
                {
                    _commandService.SetEdgeWeight(edgeId, newWeight);
                }
            }
        }

        private void OnElementSelected(string elementId)
        {
            Dispatcher.Invoke(() => StatusText.Text = $"Selected: {elementId}");
        }

        private void OnSelectionCleared()
        {
            // Ничего не делаем
        }

        private void OnVisualChanged()
        {
            Dispatcher.Invoke(RedrawGraph);
        }

        private void OnDeleteRequested(string elementId)
        {
            if (_graphModel.Vertices.ContainsKey(elementId))
            {
                if (MessageBox.Show($"Delete vertex '{elementId}' and all connected edges?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _commandService.RemoveVertex(elementId);
                }
            }
            else if (_graphModel.Edges.ContainsKey(elementId))
            {
                if (MessageBox.Show($"Delete edge '{elementId}'?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _commandService.RemoveEdge(elementId);
                }
            }
        }

        // Обработчики правого клика
        private void OnVertexRightClicked(string vertexId, Point position)
        {
            _contextMenuService.ShowVertexContextMenu(vertexId, position);
        }

        private void OnEdgeRightClicked(string edgeId, Point position)
        {
            _contextMenuService.ShowEdgeContextMenu(edgeId, position);
        }

        // НОВЫЙ МЕТОД: Обработка правого клика в пустом месте
        private void OnEmptySpaceRightClicked(Point position)
        {
            _contextMenuService.ShowEmptySpaceContextMenu(position);
        }

        // Метод для запуска создания ребра из вершины
        private void StartEdgeFromVertex(string vertexId)
        {
            _interactionService.StartEdgeFromVertex(vertexId);
        }

        private void OnOperationCompleted(string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
                UpdateStatus();
            });
        }

        private void OnErrorOccurred(string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"Error: {message}";
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void OnGraphChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatus();
                RedrawGraph();
            });
        }

        // === Вспомогательные методы ===

        private void RedrawGraph()
        {
            Console.WriteLine($"=== RedrawGraph ===");
            Console.WriteLine($"Vertices: {_graphModel.Vertices.Count}, Edges: {_graphModel.Edges.Count}");

            GraphCanvas.Children.Clear();

            // Рисуем рёбра
            foreach (var edge in _graphModel.Edges.Values)
            {
                DrawEdge(edge);
            }

            // Рисуем вершины
            foreach (var vertex in _graphModel.Vertices.Values)
            {
                DrawVertex(vertex);
            }

            Console.WriteLine("RedrawGraph complete");
        }

        private void DrawVertex(IVertex vertex)
        {
            var position = _visualModel.GetVertexPosition(vertex.Id) ?? new Point(100, 100);
            var color = _visualModel.VertexColors.ContainsKey(vertex.Id)
                ? _visualModel.VertexColors[vertex.Id]
                : Colors.LightBlue;

            var ellipse = new System.Windows.Shapes.Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Tag = vertex.Id,
                ToolTip = $"{vertex.Id}\nPosition: ({position.X:F0}, {position.Y:F0})"
            };

            System.Windows.Controls.Canvas.SetLeft(ellipse, position.X - 20);
            System.Windows.Controls.Canvas.SetTop(ellipse, position.Y - 20);
            GraphCanvas.Children.Add(ellipse);

            var text = new System.Windows.Controls.TextBlock
            {
                Text = vertex.Id,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Tag = vertex.Id
            };

            System.Windows.Controls.Canvas.SetLeft(text, position.X - 10);
            System.Windows.Controls.Canvas.SetTop(text, position.Y - 12);
            GraphCanvas.Children.Add(text);
        }

        private void DrawEdge(IEdge edge)
        {
            var sourcePos = _visualModel.GetVertexPosition(edge.Source) ?? new Point(0, 0);
            var targetPos = _visualModel.GetVertexPosition(edge.Target) ?? new Point(100, 100);
            var color = _visualModel.EdgeColors.ContainsKey(edge.Id)
                ? _visualModel.EdgeColors[edge.Id]
                : Colors.Black;

            var line = new System.Windows.Shapes.Line
            {
                X1 = sourcePos.X,
                Y1 = sourcePos.Y,
                X2 = targetPos.X,
                Y2 = targetPos.Y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
                Tag = edge.Id,
                ToolTip = $"Edge {edge.Id}\n{edge.Source} → {edge.Target}\n" +
                         $"Weight: {edge.Weight ?? 1.0:F2}\n" +
                         $"Capacity: {edge.Capacity ?? 1.0:F2}"
            };

            GraphCanvas.Children.Add(line);

            if (edge.Weight.HasValue)
            {
                var midX = (sourcePos.X + targetPos.X) / 2;
                var midY = (sourcePos.Y + targetPos.Y) / 2;

                var text = new System.Windows.Controls.TextBlock
                {
                    Text = edge.Weight.Value.ToString("F1"),
                    Foreground = Brushes.Red,
                    Background = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(2),
                    Tag = edge.Id
                };

                System.Windows.Controls.Canvas.SetLeft(text, midX);
                System.Windows.Controls.Canvas.SetTop(text, midY);
                GraphCanvas.Children.Add(text);
            }
        }

        private void UpdateStatus()
        {
            VertexCountText.Text = $"Vertices: {_graphModel.Vertices.Count}";
            EdgeCountText.Text = $"Edges: {_graphModel.Edges.Count}";
            ModeText.Text = $"Mode: {_interactionService.GetCurrentMode()}";

            // Обновляем матрицу при изменении графа
            UpdateMatrixDisplay();
        }

        private void DeleteSelected()
        {
            // Получаем выделенный элемент из InteractionService
            var selectedId = _interactionService.SelectedElementId;

            if (!string.IsNullOrEmpty(selectedId))
            {
                // Используем уже существующий обработчик OnDeleteRequested
                OnDeleteRequested(selectedId);
            }
            else
            {
                MessageBox.Show("Please select an element first", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SelectAll()
        {
            MessageBox.Show("Select All feature not implemented yet", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenGraph()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Graph files (*.graph)|*.graph|JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".graph",
                Title = "Open Graph"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _commandService.LoadGraph(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading graph:\n{ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveGraph()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Graph files (*.graph)|*.graph|JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".graph",
                Title = "Save Graph"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _commandService.SaveGraph(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving graph:\n{ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowAbout()
        {
            MessageBox.Show("Graph Editor v1.0\nA simple graph visualization and editing tool\n\n" +
                          "Features:\n" +
                          "• Create and edit graphs\n" +
                          "• Visualize with different layouts\n" +
                          "• Run algorithms (Dijkstra, MST, Max Flow)\n" +
                          "• Save/load graphs to JSON format\n" +
                          "• View adjacency matrix",
                "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // === МЕТОДЫ ДЛЯ МАТРИЦЫ ===

        private void InitializeMatrixPanel()
        {
            try
            {
                // Подписываемся на события для обновления матрицы
                _graphModel.Changed += OnGraphChangedForMatrix;
                _visualModel.VisualChanged += OnGraphChangedForMatrix;

                // Обработчики кнопок
                BtnCopyMatrix.Click += (s, e) => CopyMatrixToClipboard();
                BtnRefreshMatrix.Click += (s, e) => UpdateMatrixDisplay();

                // Первоначальное обновление
                UpdateMatrixDisplay();

                Console.WriteLine("Matrix panel initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing matrix panel: {ex.Message}");
                MatrixTextBox.Text = $"Error: {ex.Message}";
            }
        }

        private void OnGraphChangedForMatrix(object sender, EventArgs e)
        {
            // Обновляем матрицу в UI потоке
            Dispatcher.Invoke(() => UpdateMatrixDisplay());
        }

        private void UpdateMatrixDisplay()
        {
            try
            {
                if (_graphModel.Vertices.Count == 0)
                {
                    MatrixTextBox.Text = "Graph is empty.\nAdd vertices to see the matrix.";
                    return;
                }

                // Строим матрицу смежности
                var matrix = _graphModel.BuildAdjacencyMatrix(true);

                // Форматируем матрицу
                var matrixText = _matrixService.FormatAdjacencyMatrix(matrix, _graphModel);

                // Добавляем информацию о вершинах
                var vertexInfo = BuildVertexIndexInfo();

                MatrixTextBox.Text = vertexInfo + "\n\n" + matrixText;
            }
            catch (Exception ex)
            {
                MatrixTextBox.Text = $"Error building matrix:\n{ex.Message}";
                Console.WriteLine($"Error in UpdateMatrixDisplay: {ex}");
            }
        }

        private string BuildVertexIndexInfo()
        {
            try
            {
                var vertices = _graphModel.Vertices.Keys.OrderBy(id => id).ToList();
                if (vertices.Count == 0) return "";

                var sb = new System.Text.StringBuilder();

                sb.AppendLine("=== VERTEX INDEX MAPPING ===");
                sb.AppendLine("Rows and columns correspond to:");
                sb.AppendLine();

                for (int i = 0; i < vertices.Count; i++)
                {
                    sb.AppendLine($"  Row/Col {i + 1,2} → Vertex '{vertices[i]}'");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error building vertex info: {ex.Message}";
            }
        }

        private void CopyMatrixToClipboard()
        {
            try
            {
                if (string.IsNullOrEmpty(MatrixTextBox.Text))
                {
                    MessageBox.Show("Matrix is empty", "Info",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                System.Windows.Clipboard.SetText(MatrixTextBox.Text);
                StatusText.Text = "Matrix copied to clipboard!";

                // Визуальная обратная связь
                BtnCopyMatrix.Content = "✓ Copied!";

                // Через секунду возвращаем обратно
                System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => BtnCopyMatrix.Content = "📋 Copy");
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy matrix:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Простой диалог для ввода
    public class InputDialog : Window
    {
        public string Answer { get; private set; }

        public InputDialog(string question, string title, string defaultValue = "")
        {
            Title = title;
            Width = 300;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var stackPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };

            stackPanel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = question,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var textBox = new System.Windows.Controls.TextBox
            {
                Text = defaultValue,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(textBox);

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            var okButton = new System.Windows.Controls.Button
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

            var cancelButton = new System.Windows.Controls.Button
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
}