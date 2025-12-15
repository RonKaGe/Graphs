using GraphEditor.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GraphEditor.Views
{
    public class AlgorithmDialog : Window
    {
        private readonly AlgorithmService _algorithmService;
        private readonly List<string> _availableVertices;

        public ComboBox _comboAlgorithms;
        private TextBlock _txtDescription;
        private StackPanel _parametersPanel;
        private Dictionary<string, Control> _parameterControls;
        private GroupBox _parametersGroup;

        public AlgorithmInfo SelectedAlgorithm { get; private set; }
        public Dictionary<string, string> Parameters { get; private set; }

        public AlgorithmDialog(AlgorithmService algorithmService, List<string> availableVertices)
        {
            InitializeComponent();

            _algorithmService = algorithmService ?? throw new ArgumentNullException(nameof(algorithmService));
            _availableVertices = availableVertices ?? new List<string>();
            Parameters = new Dictionary<string, string>();
            _parameterControls = new Dictionary<string, Control>();

            LoadAlgorithms();

            Console.WriteLine($"AlgorithmDialog created with {_availableVertices.Count} available vertices");
            Console.WriteLine($"Available vertices: {string.Join(", ", _availableVertices)}");
        }

        private void InitializeComponent()
        {
            Title = "Run Algorithm";
            Width = 500;
            Height = 450;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var mainGrid = new Grid { Margin = new Thickness(10) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Заголовок
            var titleText = new TextBlock
            {
                Text = "Select Algorithm and Parameters",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(titleText, 0);
            mainGrid.Children.Add(titleText);

            // Основное содержимое
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 5, 0, 5)
            };
            var contentPanel = new StackPanel();

            // Выбор алгоритма
            var algorithmGroup = new GroupBox
            {
                Header = "Algorithm",
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(5)
            };
            _comboAlgorithms = new ComboBox
            {
                DisplayMemberPath = "Name",
                MinHeight = 30,
                Margin = new Thickness(2)
            };
            _comboAlgorithms.SelectionChanged += ComboAlgorithms_SelectionChanged;
            algorithmGroup.Content = _comboAlgorithms;
            contentPanel.Children.Add(algorithmGroup);

            // Описание
            var descriptionGroup = new GroupBox
            {
                Header = "Description",
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(5)
            };
            _txtDescription = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 60,
                Margin = new Thickness(2),
                Text = "Select an algorithm from the list above"
            };
            descriptionGroup.Content = _txtDescription;
            contentPanel.Children.Add(descriptionGroup);

            // Параметры
            _parametersGroup = new GroupBox
            {
                Header = "Parameters",
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(5),
                Visibility = Visibility.Collapsed
            };
            _parametersPanel = new StackPanel();
            _parametersGroup.Content = _parametersPanel;
            contentPanel.Children.Add(_parametersGroup);

            scrollViewer.Content = contentPanel;
            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            // Кнопки
            var buttonPanel = new DockPanel
            {
                LastChildFill = false,
                Margin = new Thickness(0, 10, 0, 0),
                Height = 40
            };

            var runButton = new Button
            {
                Content = "Run",
                IsDefault = true,
                Width = 100,
                Height = 30,
                Margin = new Thickness(5)
            };
            runButton.Click += BtnRun_Click;
            DockPanel.SetDock(runButton, Dock.Right);

            var cancelButton = new Button
            {
                Content = "Cancel",
                IsCancel = true,
                Width = 100,
                Height = 30,
                Margin = new Thickness(5)
            };
            DockPanel.SetDock(cancelButton, Dock.Right);

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(runButton);

            Grid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        public void SelectAlgorithmById(string algorithmId)
        {
            foreach (AlgorithmInfo algo in _comboAlgorithms.Items)
            {
                if (algo.Id == algorithmId)
                {
                    _comboAlgorithms.SelectedItem = algo;
                    Console.WriteLine($"Selected algorithm: {algorithmId}");
                    break;
                }
            }
        }

        private void LoadAlgorithms()
        {
            var algorithms = _algorithmService.GetAvailableAlgorithms();
            _comboAlgorithms.ItemsSource = algorithms;

            if (algorithms.Count > 0)
            {
                _comboAlgorithms.SelectedIndex = 0;
                Console.WriteLine($"Loaded {algorithms.Count} algorithms");
            }
            else
            {
                Console.WriteLine("No algorithms available!");
            }
        }

        private void ComboAlgorithms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_comboAlgorithms.SelectedItem is AlgorithmInfo algorithm)
            {
                SelectedAlgorithm = algorithm;
                _txtDescription.Text = algorithm.Description;

                Console.WriteLine($"Algorithm selected: {algorithm.Name}");
                Console.WriteLine($"Requires parameters: {algorithm.RequiresParameters}");
                Console.WriteLine($"Parameter count: {algorithm.Parameters?.Count ?? 0}");

                // Очищаем панель параметров
                _parametersPanel.Children.Clear();
                _parameterControls.Clear();
                Parameters.Clear();

                if (algorithm.RequiresParameters && algorithm.Parameters != null && algorithm.Parameters.Count > 0)
                {
                    _parametersGroup.Visibility = Visibility.Visible;

                    Console.WriteLine($"Available vertices: {string.Join(", ", _availableVertices)}");

                    foreach (var param in algorithm.Parameters)
                    {
                        Console.WriteLine($"Creating parameter: {param.Name}, Type: {param.Type}, Required: {param.IsRequired}");

                        var grid = new Grid();
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var label = new TextBlock
                        {
                            Text = param.Name + (param.IsRequired ? " *" : ""),
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(5, 8, 5, 8),
                            FontWeight = param.IsRequired ? FontWeights.Bold : FontWeights.Normal
                        };
                        Grid.SetColumn(label, 0);

                        Control inputControl;

                        if (param.Type == "vertex")
                        {
                            var comboBox = new ComboBox
                            {
                                ItemsSource = _availableVertices,
                                Margin = new Thickness(5, 5, 5, 5),
                                MinHeight = 28
                            };

                            // Выбираем значение по умолчанию
                            if (!string.IsNullOrEmpty(param.DefaultValue) && _availableVertices.Contains(param.DefaultValue))
                            {
                                comboBox.SelectedItem = param.DefaultValue;
                            }
                            else if (_availableVertices.Count > 0)
                            {
                                // Автоматически выбираем первую доступную вершину
                                comboBox.SelectedIndex = 0;
                            }

                            comboBox.SelectionChanged += (s, ev) =>
                            {
                                if (comboBox.SelectedItem != null)
                                {
                                    Console.WriteLine($"Parameter {param.Name} set to: {comboBox.SelectedItem}");
                                }
                            };

                            inputControl = comboBox;
                        }
                        else
                        {
                            var textBox = new TextBox
                            {
                                Text = param.DefaultValue ?? "",
                                Margin = new Thickness(5, 5, 5, 5),
                                MinHeight = 28
                            };

                            inputControl = textBox;
                        }

                        inputControl.Tag = param.Name;
                        Grid.SetColumn(inputControl, 1);

                        grid.Children.Add(label);
                        grid.Children.Add(inputControl);
                        _parametersPanel.Children.Add(grid);
                        _parameterControls[param.Name] = inputControl;
                    }
                }
                else
                {
                    _parametersGroup.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Run button clicked");

            // Собираем параметры
            Parameters.Clear();
            foreach (var kvp in _parameterControls)
            {
                string value = "";

                if (kvp.Value is ComboBox comboBox)
                {
                    value = comboBox.SelectedItem?.ToString() ?? "";
                    Console.WriteLine($"Parameter {kvp.Key} (ComboBox): {value}");
                }
                else if (kvp.Value is TextBox textBox)
                {
                    value = textBox.Text ?? "";
                    Console.WriteLine($"Parameter {kvp.Key} (TextBox): {value}");
                }

                Parameters[kvp.Key] = value;
            }

            // Проверяем обязательные параметры
            if (SelectedAlgorithm?.Parameters != null)
            {
                foreach (var param in SelectedAlgorithm.Parameters)
                {
                    if (param.IsRequired)
                    {
                        if (!Parameters.ContainsKey(param.Name) || string.IsNullOrEmpty(Parameters[param.Name]))
                        {
                            MessageBox.Show($"Parameter '{param.Name}' is required",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }
            }

            Console.WriteLine($"All parameters validated. Selected algorithm: {SelectedAlgorithm?.Id}");
            DialogResult = true;
            Close();
        }
    }
}