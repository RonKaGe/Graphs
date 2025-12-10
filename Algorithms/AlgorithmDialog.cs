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

        private ComboBox _comboAlgorithms;
        private TextBlock _txtDescription;
        private StackPanel _parametersPanel;
        private Dictionary<string, Control> _parameterControls;

        public AlgorithmInfo SelectedAlgorithm { get; private set; }
        public Dictionary<string, string> Parameters { get; private set; }

        public AlgorithmDialog(AlgorithmService algorithmService, List<string> availableVertices)
        {
            InitializeComponent();

            _algorithmService = algorithmService;
            _availableVertices = availableVertices;
            Parameters = new Dictionary<string, string>();
            _parameterControls = new Dictionary<string, Control>();

            LoadAlgorithms();
        }

        private void InitializeComponent()
        {
            Title = "Run Algorithm";
            Width = 500;
            Height = 400;
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
            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var contentPanel = new StackPanel();

            // Выбор алгоритма
            var algorithmGroup = new GroupBox
            {
                Header = "Algorithm",
                Margin = new Thickness(0, 0, 0, 10)
            };
            _comboAlgorithms = new ComboBox
            {
                DisplayMemberPath = "Name",
                MinHeight = 30
            };
            _comboAlgorithms.SelectionChanged += ComboAlgorithms_SelectionChanged;
            algorithmGroup.Content = _comboAlgorithms;
            contentPanel.Children.Add(algorithmGroup);

            // Описание
            var descriptionGroup = new GroupBox
            {
                Header = "Description",
                Margin = new Thickness(0, 0, 0, 10)
            };
            _txtDescription = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 40,
                Margin = new Thickness(5)
            };
            descriptionGroup.Content = _txtDescription;
            contentPanel.Children.Add(descriptionGroup);

            // Параметры
            var parametersGroup = new GroupBox
            {
                Header = "Parameters",
                Name = "GroupParameters",
                Margin = new Thickness(0, 0, 0, 10),
                Visibility = Visibility.Collapsed
            };
            _parametersPanel = new StackPanel();
            parametersGroup.Content = _parametersPanel;
            contentPanel.Children.Add(parametersGroup);

            scrollViewer.Content = contentPanel;
            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            // Кнопки
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var runButton = new Button
            {
                Content = "Run",
                IsDefault = true,
                Width = 80,
                Margin = new Thickness(5),
                Padding = new Thickness(10, 5, 10, 5)
            };
            runButton.Click += BtnRun_Click;

            var cancelButton = new Button
            {
                Content = "Cancel",
                IsCancel = true,
                Width = 80,
                Margin = new Thickness(5),
                Padding = new Thickness(10, 5, 10, 5)
            };

            buttonPanel.Children.Add(runButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        private void LoadAlgorithms()
        {
            var algorithms = _algorithmService.GetAvailableAlgorithms();
            _comboAlgorithms.ItemsSource = algorithms;

            if (algorithms.Count > 0)
            {
                _comboAlgorithms.SelectedIndex = 0;
            }
        }

        private void ComboAlgorithms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_comboAlgorithms.SelectedItem is AlgorithmInfo algorithm)
            {
                SelectedAlgorithm = algorithm;
                _txtDescription.Text = algorithm.Description;

                // Очищаем панель параметров
                _parametersPanel.Children.Clear();
                _parameterControls.Clear();
                Parameters.Clear();

                if (algorithm.RequiresParameters && algorithm.Parameters != null)
                {
                    (Content as Grid).Children
                        .OfType<GroupBox>()
                        .First(g => g.Name == "GroupParameters")
                        .Visibility = Visibility.Visible;

                    foreach (var param in algorithm.Parameters)
                    {
                        var grid = new Grid();
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var label = new TextBlock
                        {
                            Text = param.Name + (param.IsRequired ? " *" : ""),
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(5)
                        };
                        Grid.SetColumn(label, 0);

                        Control inputControl;

                        if (param.Type == "vertex")
                        {
                            var comboBox = new ComboBox
                            {
                                ItemsSource = _availableVertices,
                                SelectedItem = param.DefaultValue ?? (_availableVertices.Count > 0 ? _availableVertices[0] : null),
                                Margin = new Thickness(5)
                            };
                            inputControl = comboBox;
                        }
                        else
                        {
                            var textBox = new TextBox
                            {
                                Text = param.DefaultValue ?? "",
                                Margin = new Thickness(5)
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
                    (Content as Grid).Children
                        .OfType<GroupBox>()
                        .First(g => g.Name == "GroupParameters")
                        .Visibility = Visibility.Collapsed;
                }
            }
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            // Собираем параметры
            foreach (var kvp in _parameterControls)
            {
                string value = "";

                if (kvp.Value is ComboBox comboBox)
                {
                    value = comboBox.SelectedItem?.ToString() ?? "";
                }
                else if (kvp.Value is TextBox textBox)
                {
                    value = textBox.Text;
                }

                Parameters[kvp.Key] = value;
            }

            // Проверяем обязательные параметры
            if (SelectedAlgorithm?.Parameters != null)
            {
                foreach (var param in SelectedAlgorithm.Parameters)
                {
                    if (param.IsRequired &&
                        (!Parameters.ContainsKey(param.Name) || string.IsNullOrEmpty(Parameters[param.Name])))
                    {
                        MessageBox.Show($"Parameter '{param.Name}' is required",
                            "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }

            DialogResult = true;
            Close();
        }
    }
}