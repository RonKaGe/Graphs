using GraphEditor.Core;
using GraphEditor.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GraphEditor.Services
{
    public class ContextMenuService
    {
        private readonly CommandService _commandService;
        private readonly IGraphModel _graphModel;

        public ContextMenuService(CommandService commandService, IGraphModel graphModel)
        {
            _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _graphModel = graphModel ?? throw new ArgumentNullException(nameof(graphModel));
        }

        public void ShowVertexContextMenu(string vertexId, Point position)
        {
            var menu = new ContextMenu();

            // Delete Vertex
            var deleteItem = new MenuItem
            {
                Header = "🗑️ Delete Vertex",
                Tag = vertexId,
                FontSize = 12,
                Padding = new Thickness(8, 4, 8, 4)  // Исправлено
            };
            deleteItem.Click += (s, e) => DeleteVertex(vertexId);

            // Change Label
            var changeLabelItem = new MenuItem
            {
                Header = "✏️ Change Label",
                Tag = vertexId,
                FontSize = 12,
                Padding = new Thickness(8, 4, 8, 4)  // Исправлено
            };
            changeLabelItem.Click += (s, e) => ChangeVertexLabel(vertexId);

            // Change Color
            var changeColorItem = new MenuItem
            {
                Header = "🎨 Change Color",
                Tag = vertexId,
                FontSize = 12,
                Padding = new Thickness(8, 4, 8, 4)  // Исправлено
            };
            changeColorItem.Click += (s, e) => ShowColorMenuForVertex(vertexId, position);

            // Add Edge From Here
            var addEdgeItem = new MenuItem
            {
                Header = "➖ Add Edge From Here",
                Tag = vertexId,
                FontSize = 12,
                Padding = new Thickness(8, 4, 8, 4)  // Исправлено
            };
            addEdgeItem.Click += (s, e) => StartEdgeFromVertex(vertexId);

            menu.Items.Add(changeLabelItem);
            menu.Items.Add(changeColorItem);
            menu.Items.Add(addEdgeItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(deleteItem);

            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            menu.IsOpen = true;
        }

        public void ShowEdgeContextMenu(string edgeId, Point position)
        {
            if (!_graphModel.TryGetEdge(edgeId, out var edge))
                return;

            var menu = new ContextMenu();

            // Change Weight
            var changeWeightItem = new MenuItem
            {
                Header = $"⚖️ Change Weight (current: {edge.Weight ?? 1})",
                Tag = edgeId,
                FontSize = 12,
                Padding = new Thickness(8, 4, 8, 4)  // Исправлено
            };
            changeWeightItem.Click += (s, e) => ChangeEdgeWeight(edgeId);

            // Change Color
            var changeColorItem = new MenuItem
            {
                Header = "🎨 Change Color",
                Tag = edgeId,
                FontSize = 12,
                Padding = new Thickness(8, 4, 8, 4)  // Исправлено
            };
            changeColorItem.Click += (s, e) => ShowColorMenuForEdge(edgeId, position);

            // Delete Edge
            var deleteItem = new MenuItem
            {
                Header = "🗑️ Delete Edge",
                Tag = edgeId,
                FontSize = 12,
                Padding = new Thickness(8, 4, 8, 4)  // Исправлено
            };
            deleteItem.Click += (s, e) => DeleteEdge(edgeId);

            menu.Items.Add(changeWeightItem);
            menu.Items.Add(changeColorItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(deleteItem);

            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            menu.IsOpen = true;
        }

        private void DeleteVertex(string vertexId)
        {
            var result = MessageBox.Show(
                $"Delete vertex '{vertexId}' and all connected edges?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _commandService.RemoveVertex(vertexId);
            }
        }

        private void DeleteEdge(string edgeId)
        {
            var result = MessageBox.Show(
                $"Delete edge '{edgeId}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _commandService.RemoveEdge(edgeId);
            }
        }

        private void ChangeVertexLabel(string vertexId)
        {
            var currentLabel = _graphModel.Vertices[vertexId].Label ?? "";
            var dialog = new InputDialog(
                $"Enter new label for vertex {vertexId}:",
                "Change Label",
                currentLabel);

            if (dialog.ShowDialog() == true)
            {
                _commandService.SetVertexLabel(vertexId, dialog.Answer);
            }
        }

        private void ChangeEdgeWeight(string edgeId)
        {
            if (_graphModel.TryGetEdge(edgeId, out var edge))
            {
                var dialog = new InputDialog(
                    $"Enter new weight for edge {edge.Source}-{edge.Target}:",
                    "Change Weight",
                    edge.Weight?.ToString() ?? "1.0");

                if (dialog.ShowDialog() == true && double.TryParse(dialog.Answer, out double newWeight))
                {
                    _commandService.SetEdgeWeight(edgeId, newWeight);
                }
            }
        }

        private void ShowColorMenuForVertex(string vertexId, Point position)
        {
            var menu = new ContextMenu();

            var colors = new[]
            {
                new { Name = "🔵 Light Blue", Color = Colors.LightBlue },
                new { Name = "🟢 Light Green", Color = Colors.LightGreen },
                new { Name = "🔴 Light Coral", Color = Colors.LightCoral },
                new { Name = "🟡 Light Yellow", Color = Colors.LightYellow },
                new { Name = "🟣 Light Pink", Color = Colors.LightPink },
                new { Name = "⚪ Light Gray", Color = Colors.LightGray }
            };

            foreach (var colorInfo in colors)
            {
                var item = new MenuItem
                {
                    Header = colorInfo.Name,
                    Background = new SolidColorBrush(colorInfo.Color),
                    FontSize = 12,
                    Padding = new Thickness(8, 4, 8, 4),  // Исправлено
                    Tag = colorInfo.Color
                };

                item.Click += (s, e) =>
                    _commandService.SetVertexColor(vertexId, colorInfo.Color);

                menu.Items.Add(item);
            }

            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            menu.IsOpen = true;
        }

        private void ShowColorMenuForEdge(string edgeId, Point position)
        {
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

            foreach (var colorInfo in colors)
            {
                var item = new MenuItem
                {
                    Header = colorInfo.Name,
                    Background = new SolidColorBrush(colorInfo.Color),
                    FontSize = 12,
                    Padding = new Thickness(8, 4, 8, 4),  // Исправлено
                    Tag = colorInfo.Color
                };

                item.Click += (s, e) =>
                    _commandService.SetEdgeColor(edgeId, colorInfo.Color);

                menu.Items.Add(item);
            }

            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            menu.IsOpen = true;
        }

        private void StartEdgeFromVertex(string vertexId)
        {
            // Эта функция может быть связана с InteractionService
            // Пока просто сообщение
            MessageBox.Show($"Would start edge from vertex {vertexId}\n(Connect to another vertex)",
                "Add Edge", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // Копия InputDialog из MainWindow (чтобы не было зависимостей)
    public class InputDialog : Window
    {
        public string Answer { get; private set; }

        public InputDialog(string question, string title, string defaultValue = "")
        {
            Title = title;
            Width = 300;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var stackPanel = new StackPanel { Margin = new Thickness(10, 10, 10, 10) };  // Исправлено

            stackPanel.Children.Add(new TextBlock
            {
                Text = question,
                Margin = new Thickness(0, 0, 0, 10)  // Исправлено
            });

            var textBox = new TextBox
            {
                Text = defaultValue,
                Margin = new Thickness(0, 0, 0, 10)  // Исправлено
            };
            stackPanel.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),  // Исправлено
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
}