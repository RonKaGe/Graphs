using GraphEditor.Algorithms;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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

            _algorithmService = algorithmService;
            _availableVertices = availableVertices;
            Parameters = new Dictionary<string, string>();
            _parameterControls = new Dictionary<string, Control>();

            LoadAlgorithms();
        }

        private void InitializeComponent()
        {
            Title = "Запустить алгоритм";
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
                Text = "Выберите алгоритм и параметры",
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
                Margin = new Thickness(0, 5, 0, 5)
            };
            var contentPanel = new StackPanel();

            // Выбор алгоритма
            var algorithmGroup = new GroupBox
            {
                Header = "Алгоритм",
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(5)
            };
            _comboAlgorithms = new ComboBox
            {
                DisplayMemberPath = "Name",  // ИСПРАВЛЕНО: должно быть "Name", а не "Имя"
                MinHeight = 30,
                Margin = new Thickness(2)
            };
            _comboAlgorithms.SelectionChanged += ComboAlgorithms_SelectionChanged;
            algorithmGroup.Content = _comboAlgorithms;
            contentPanel.Children.Add(algorithmGroup);

            // Описание
            var descriptionGroup = new GroupBox
            {
                Header = "Описание",
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(5)
            };
            _txtDescription = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 60,
                Margin = new Thickness(2),
                Text = "Выберите алгоритм из списка выше"
            };
            descriptionGroup.Content = _txtDescription;
            contentPanel.Children.Add(descriptionGroup);

            // Параметры
            _parametersGroup = new GroupBox  // ИСПРАВЛЕНО: используем поле
            {
                Header = "Параметры",
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
                Content = "Запустить",  // ИСПРАВЛЕНО: русский текст
                IsDefault = true,
                Width = 100,
                Height = 30,
                Margin = new Thickness(5)
            };
            runButton.Click += BtnRun_Click;
            DockPanel.SetDock(runButton, Dock.Right);

            var cancelButton = new Button
            {
                Content = "Отмена",  // ИСПРАВЛЕНО: русский текст
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

                // НАХОДИМ GroupBox по Header (один раз и сохраняем в переменную)
                var parametersGroupBox = (Content as Grid)?.Children
                    .OfType<GroupBox>()
                    .FirstOrDefault(g => g.Header?.ToString() == "Параметры");

                if (algorithm.RequiresParameters && algorithm.Parameters != null)
                {
                    //  используем найденный GroupBox
                    if (parametersGroupBox != null)
                        parametersGroupBox.Visibility = Visibility.Visible;

                    int vertexParamIndex = 0; // Счётчик для параметров-вершин

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
                                Margin = new Thickness(5)
                            };

                            // ВЫБИРАЕМ РАЗНЫЕ ВЕРШИНЫ ДЛЯ РАЗНЫХ ПАРАМЕТРОВ
                            if (vertexParamIndex == 0 && _availableVertices.Count > 0)
                            {
                                // Первый параметр-вершина: выбираем первую вершину
                                comboBox.SelectedItem = _availableVertices[0];
                            }
                            else if (vertexParamIndex == 1 && _availableVertices.Count > 1)
                            {
                                // Второй параметр-вершина: выбираем вторую вершину
                                comboBox.SelectedItem = _availableVertices[1];
                            }
                            else if (vertexParamIndex >= 2 && _availableVertices.Count > vertexParamIndex)
                            {
                                // Третий и далее: выбираем по порядку
                                comboBox.SelectedItem = _availableVertices[vertexParamIndex];
                            }
                            else if (param.DefaultValue != null)
                            {
                                // Или используем значение по умолчанию
                                comboBox.SelectedItem = param.DefaultValue;
                            }
                            else if (_availableVertices.Count > 0)
                            {
                                // Или просто первую
                                comboBox.SelectedItem = _availableVertices[0];
                            }

                            vertexParamIndex++;
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
                    //  скрываем найденный GroupBox
                    if (parametersGroupBox != null)
                        parametersGroupBox.Visibility = Visibility.Collapsed;
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
                        MessageBox.Show($"Параметр '{param.Name}' требуется",
                            "Ошибка проверки", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }

            DialogResult = true;
            Close();
        }
    }
}