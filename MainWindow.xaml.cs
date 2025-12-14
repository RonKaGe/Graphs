using GraphEditor.Algorithms;
using GraphEditor.Core;
using GraphEditor.Core.Models;
using GraphEditor.Services;
using GraphEditor.Visual;
using Microsoft.Win32;
using System;
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

            // Настройка событий
            SetupEventHandlers();

            // Первая отрисовка
            RedrawGraph();
            UpdateStatus();
        }

        private void InitializeTestGraph()
        {
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
        }

        private void ShowAlgorithmDialog()
        {
            var dialog = new AlgorithmDialog(_algorithmService, _graphModel.Vertices.Keys.ToList());
            dialog.Owner = this;

            if (dialog.ShowDialog() == true && dialog.SelectedAlgorithm != null)
            {
                var result = _algorithmService.RunAlgorithm(dialog.SelectedAlgorithm.Id, dialog.Parameters);

                Console.WriteLine($"Алгоритм {dialog.SelectedAlgorithm.Id} выполнен: {result.Success}");
                Console.WriteLine($"Сообщение: {result.Message}");

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

            // ДОБАВЛЕНО: Подписка на события правого клика
            _interactionService.VertexRightClicked += OnVertexRightClicked;
            _interactionService.EdgeRightClicked += OnEdgeRightClicked;

            // === Подписка на события CommandService ===
            _commandService.OperationCompleted += OnOperationCompleted;
            _commandService.ErrorOccurred += OnErrorOccurred;
            _commandService.VisualChanged += OnVisualChanged;

            // Обработчики кнопок
            BtnAlgorithms.Click += (s, e) => ShowAlgorithmDialog();
            BtnResetAlgorithm.Click += (s, e) =>
            {
                _visualModel.ResetColors();
                StatusText.Text = "Сброс визуализации алгоритма";
            };

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
        }



        // === Обработчики событий Canvas ===

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(GraphCanvas);
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

        // ДОБАВЛЕНО: Обработчики правого клика
        private void OnVertexRightClicked(string vertexId, Point position)
        {
            _contextMenuService.ShowVertexContextMenu(vertexId, position);
        }

        private void OnEdgeRightClicked(string edgeId, Point position)
        {
            _contextMenuService.ShowEdgeContextMenu(edgeId, position);
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
            Console.WriteLine($"RedrawGraph: {_graphModel.Vertices.Count} вершин, {_graphModel.Edges.Count} рёбер");

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
                Tag = vertex.Id
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
                Tag = edge.Id
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
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = ".txt",
                Title = "Open Graph"
            };

            if (dialog.ShowDialog() == true)
            {
                _commandService.LoadGraph(dialog.FileName);
            }
        }

        private void SaveGraph()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = ".txt",
                Title = "Save Graph"
            };

            if (dialog.ShowDialog() == true)
            {
                _commandService.SaveGraph(dialog.FileName);
            }
        }

        private void ShowAbout()
        {
            MessageBox.Show("Graph Editor v1.0\nA simple graph visualization and editing tool",
                "About", MessageBoxButton.OK, MessageBoxImage.Information);
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