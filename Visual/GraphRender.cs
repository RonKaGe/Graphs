using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GraphEditor.Core;

namespace GraphEditor.Visual
{
    public static class GraphRenderer
    {
        public static void DrawGraph(Canvas canvas, IGraphModel graph, GraphVisualModel visualModel)
        {
            canvas.Children.Clear();

            // 1. Рисуем рёбра
            foreach (var edge in graph.Edges.Values)
            {
                DrawEdge(canvas, edge, visualModel);
            }

            // 2. Рисуем вершины
            foreach (var vertex in graph.Vertices.Values)
            {
                DrawVertex(canvas, vertex, visualModel);
            }
        }

        private static void DrawVertex(Canvas canvas, IVertex vertex, GraphVisualModel visualModel)
        {
            var position = visualModel.GetVertexPosition(vertex.Id) ?? new Point(100, 100);
            var color = visualModel.VertexColors.ContainsKey(vertex.Id)
                ? visualModel.VertexColors[vertex.Id]
                : Colors.LightBlue;

            // Эллипс вершины
            var ellipse = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Tag = vertex.Id
            };

            Canvas.SetLeft(ellipse, position.X - 20);
            Canvas.SetTop(ellipse, position.Y - 20);

            canvas.Children.Add(ellipse);

            // Текст с ID вершины
            var text = new TextBlock
            {
                Text = vertex.Id,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };

            Canvas.SetLeft(text, position.X - 10);
            Canvas.SetTop(text, position.Y - 12);

            canvas.Children.Add(text);
        }

        private static void DrawEdge(Canvas canvas, IEdge edge, GraphVisualModel visualModel)
        {
            var sourcePos = visualModel.GetVertexPosition(edge.Source) ?? new Point(0, 0);
            var targetPos = visualModel.GetVertexPosition(edge.Target) ?? new Point(100, 100);
            var color = visualModel.EdgeColors.ContainsKey(edge.Id)
                ? visualModel.EdgeColors[edge.Id]
                : Colors.Black;

            // Линия ребра
            var line = new Line
            {
                X1 = sourcePos.X,
                Y1 = sourcePos.Y,
                X2 = targetPos.X,
                Y2 = targetPos.Y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
                Tag = edge.Id
            };

            canvas.Children.Add(line);

            // Текст с весом (если есть)
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
                    Padding = new Thickness(2)
                };

                Canvas.SetLeft(text, midX);
                Canvas.SetTop(text, midY);

                canvas.Children.Add(text);
            }
        }
    }
}