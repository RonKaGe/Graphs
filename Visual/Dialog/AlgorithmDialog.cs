using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GraphEditor.Views.Dialogs
{
    public class AlgorithmDialog : Window
    {
        private Dictionary<string, ComboBox> _controls = new Dictionary<string, ComboBox>();

        public AlgorithmDialog(string title)
        {
            Title = title;
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var mainPanel = new StackPanel { Margin = new Thickness(10) };

            mainPanel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            });

            var contentPanel = new StackPanel();
            mainPanel.Children.Add(contentPanel);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var okButton = new Button
            {
                Content = "Run",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            okButton.Click += (s, e) => DialogResult = true;

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                IsCancel = true
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            mainPanel.Children.Add(buttonPanel);

            Content = mainPanel;
        }

        public void AddVertexSelector(string label, List<string> vertices, string defaultVertex = null, bool optional = false)
        {
            var panel = Content as StackPanel;
            var contentPanel = panel.Children[1] as StackPanel;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var textBlock = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(textBlock, 0);

            var comboBox = new ComboBox
            {
                ItemsSource = vertices,
                SelectedItem = defaultVertex ?? (vertices.Count > 0 ? vertices[0] : null),
                Margin = new Thickness(0, 5, 0, 5)
            };
            Grid.SetColumn(comboBox, 1);

            grid.Children.Add(textBlock);
            grid.Children.Add(comboBox);

            contentPanel.Children.Add(grid);
            _controls[label] = comboBox;

            if (optional)
            {
                var checkBox = new CheckBox
                {
                    Content = "Find all distances (no target)",
                    Margin = new Thickness(0, 0, 0, 10)
                };
                contentPanel.Children.Add(checkBox);
            }
        }

        public string GetSelectedVertex(string label)
        {
            if (_controls.ContainsKey(label))
                return _controls[label].SelectedItem as string;
            return null;
        }
    }
}