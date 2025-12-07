using GraphEditor.Core;
using GraphEditor.Core.Models;
using GraphEditor.Core.Services;
using GraphEditor.Visual;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GraphEditor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // === Модели данных ===
        private readonly IGraphModel _graphModel;
        private readonly GraphVisualModel _visualModel;
        private readonly GraphSerializer _serializer;

        // === Алгоритмы расположения ===
        private readonly CircleLayout _circleLayout;
        private readonly RandomLayout _randomLayout;
        private readonly ForceLayout _forceLayout;

        // === Команды ===
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand AboutCommand { get; }
        public ICommand CircleLayoutCommand { get; }
        public ICommand RandomLayoutCommand { get; }
        public ICommand ForceLayoutCommand { get; }
        public ICommand ResetColorsCommand { get; }

        // === Свойства для привязки ===
        private string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value);
        }

        private string _layoutStatus = "No layout applied";
        public string LayoutStatus
        {
            get => _layoutStatus;
            set => SetField(ref _layoutStatus, value);
        }

        private int _vertexCount;
        public int VertexCount
        {
            get => _vertexCount;
            set => SetField(ref _vertexCount, value);
        }

        private int _edgeCount;
        public int EdgeCount
        {
            get => _edgeCount;
            set => SetField(ref _edgeCount, value);
        }

        // === Конструктор ===
        public MainViewModel()
        {
            // Инициализация моделей
            _graphModel = new ObservableGraphModel(isDirected: false);
            _visualModel = new GraphVisualModel();
            _serializer = new GraphSerializer();

            // Инициализация алгоритмов
            _circleLayout = new CircleLayout();
            _randomLayout = new RandomLayout();
            _forceLayout = new ForceLayout();

            // Инициализация визуальной модели
            _visualModel.InitializeFromGraph(_graphModel);

            // Инициализация команд
            OpenCommand = new RelayCommand(OpenGraph);
            SaveCommand = new RelayCommand(SaveGraph);
            ExitCommand = new RelayCommand(ExitApplication);
            AboutCommand = new RelayCommand(ShowAbout);
            CircleLayoutCommand = new RelayCommand(ApplyCircleLayout);
            RandomLayoutCommand = new RelayCommand(ApplyRandomLayout);
            ForceLayoutCommand = new RelayCommand(ApplyForceLayout);
            ResetColorsCommand = new RelayCommand(ResetColors);

            // Подписка на изменения
            _graphModel.Changed += OnGraphChanged;
            _visualModel.VisualChanged += OnVisualChanged;

            // Обновление счётчиков
            UpdateCounters();
        }

        // === Методы команд ===
        private void OpenGraph()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Graph files (*.json)|*.json|All files (*.*)|*.*",
                    Title = "Open Graph"
                };

                if (dialog.ShowDialog() == true)
                {
                    var loadedGraph = _serializer.LoadFromFile(dialog.FileName);

                    // Обновляем текущий граф
                    ReplaceGraph(loadedGraph);

                    StatusText = $"Opened: {System.IO.Path.GetFileName(dialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveGraph()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Graph files (*.json)|*.json|All files (*.*)|*.*",
                    Title = "Save Graph",
                    DefaultExt = ".json"
                };

                if (dialog.ShowDialog() == true)
                {
                    _serializer.SaveToFile(_graphModel, dialog.FileName);
                    StatusText = $"Saved: {System.IO.Path.GetFileName(dialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitApplication()
        {
            Application.Current.Shutdown();
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "Graph Editor v1.0\n\n" +
                "Team Project:\n" +
                "- UI & Integration: Антон\n" +
                "- Graph Model: Артём\n" +
                "- Visual Components: Антон\n\n" +
                "Using MVVM pattern with clean architecture.",
                "About Graph Editor",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ApplyCircleLayout()
        {
            _visualModel.ApplyLayout(_graphModel, _circleLayout);
            LayoutStatus = "Circle Layout";
            StatusText = "Applied circle layout";
        }

        private void ApplyRandomLayout()
        {
            _visualModel.ApplyLayout(_graphModel, _randomLayout);
            LayoutStatus = "Random Layout";
            StatusText = "Applied random layout";
        }

        private void ApplyForceLayout()
        {
            _visualModel.ApplyLayout(_graphModel, _forceLayout);
            LayoutStatus = "Force Layout";
            StatusText = "Applied force layout";
        }

        private void ResetColors()
        {
            _visualModel.ResetColors(Colors.LightBlue, Colors.Black);
            StatusText = "Colors reset to default";
        }

        // === Вспомогательные методы ===
        private void ReplaceGraph(IGraphModel newGraph)
        {
            // Очищаем старый граф
            foreach (var vertexId in _graphModel.Vertices.Keys)
                _graphModel.RemoveVertex(vertexId);

            // Добавляем новые вершины
            foreach (var vertex in newGraph.Vertices.Values)
                _graphModel.AddVertex(vertex.Id, vertex.Label);

            // Добавляем новые рёбра
            foreach (var edge in newGraph.Edges.Values)
                _graphModel.AddEdge(edge.Id, edge.Source, edge.Target, edge.Weight, edge.Capacity);

            // Обновляем визуальную модель
            _visualModel.InitializeFromGraph(_graphModel);
        }

        private void OnGraphChanged(object? sender, EventArgs e)
        {
            UpdateCounters();
        }

        private void OnVisualChanged(object? sender, EventArgs e)
        {
            // Можно добавить обновление UI здесь
        }

        private void UpdateCounters()
        {
            VertexCount = _graphModel.Vertices.Count;
            EdgeCount = _graphModel.Edges.Count;
        }

        // === Свойства для доступа из View ===
        public IGraphModel GraphModel => _graphModel;
        public GraphVisualModel VisualModel => _visualModel;

        // === INotifyPropertyChanged реализация ===
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    // === RelayCommand реализация ===
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