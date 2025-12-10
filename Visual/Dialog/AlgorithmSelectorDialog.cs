using System.Windows;
using System.Windows.Controls;

namespace GraphEditor.Views.Dialogs
{
    public class AlgorithmSelectorDialog : Window
    {
        public string SelectedAlgorithm { get; private set; }

        public AlgorithmSelectorDialog()
        {
            Title = "Select Algorithm";
            Width = 300;
            Height = 250;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            stack