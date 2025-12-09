using GraphEditor.Core;
using GraphEditor.Visual;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GraphEditor
{
    public static class RenderServices
    {
        public static void DrawGraph(Canvas canvas, IGraphModel graph, IGraphVisualModel visualModel)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (visualModel == null) throw new ArgumentNullException(nameof(visualModel));

            canvas.Children.Clear();

            // 1. Рисуем рёбра
            foreach (var edge in graph.Edges.Values)
            {
                DrawEdge(canvas, edge, visualModel);
            }

            // 2. Рисуем вершины (поверх рёбер)
            foreach (var vertex in graph.Vertices.Values)
            {
                DrawVertex(canvas, vertex, visualModel);
            }
        }

        private static void DrawVertex(Canvas canvas, IVertex vertex, IGraphVisualModel visualModel)
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
                Tag = vertex.Id,
                ToolTip = vertex.Label ?? vertex.Id
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
                FontSize = 14,
                Tag = vertex.Id
            };

            Canvas.SetLeft(text, position.X - 10);
            Canvas.SetTop(text, position.Y - 12);
            canvas.Children.Add(text);
        }

        private static void DrawEdge(Canvas canvas, IEdge edge, IGraphVisualModel visualModel)
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
                Tag = edge.Id,
                ToolTip = edge.Weight.HasValue ? $"Weight: {edge.Weight}" : "Edge"
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
                    Padding = new Thickness(2),
                    Tag = edge.Id
                };

                Canvas.SetLeft(text, midX);
                Canvas.SetTop(text, midY);
                canvas.Children.Add(text);
            }
        }

        public static void HighlightElement(Canvas canvas, string elementId, Color highlightColor)
        {
            if (canvas == null || string.IsNullOrEmpty(elementId))
                return;

            // Ищем элемент по Tag
            foreach (var child in canvas.Children)
            {
                if (child is FrameworkElement element && element.Tag as string == elementId)
                {
                    if (element is Ellipse ellipse)
                    {
                        ellipse.Stroke = new SolidColorBrush(highlightColor);
                        ellipse.StrokeThickness = 3;
                    }
                    else if (element is Line line)
                    {
                        line.Stroke = new SolidColorBrush(highlightColor);
                        line.StrokeThickness = 3;
                    }
                    break;
                }
            }
        }

        public static void ClearHighlight(Canvas canvas)
        {
            if (canvas == null) return;

            foreach (var child in canvas.Children)
            {
                if (child is Ellipse ellipse)
                {
                    ellipse.Stroke = Brushes.Black;
                    ellipse.StrokeThickness = 2;
                }
                else if (child is Line line)
                {
                    line.Stroke = Brushes.Black;
                    line.StrokeThickness = 2;
                }
            }
        }

        public static void DrawTempEdge(Canvas canvas, Point start, Point end)
        {
            ClearTempEdge(canvas);

            var line = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Opacity = 0.7,
                Tag = "TEMP_EDGE"
            };

            canvas.Children.Add(line);
        }

        public static void ClearTempEdge(Canvas canvas)
        {
            if (canvas == null) return;

            // Удаляем все временные рёбра
            var tempEdges = new List<UIElement>();
            foreach (var child in canvas.Children)
            {
                if (child is FrameworkElement element && element.Tag as string == "TEMP_EDGE")
                {
                    tempEdges.Add((UIElement)child);
                }
            }

            foreach (var edge in tempEdges)
            {
                canvas.Children.Remove(edge);
            }
        }

        public static void UpdateStatusBar(TextBlock statusText, TextBlock vertexCountText,
            TextBlock edgeCountText, IGraphModel graph, string statusMessage = "")
        {
            if (statusText != null)
                statusText.Text = string.IsNullOrEmpty(statusMessage)
                    ? $"Ready - {graph.Vertices.Count} vertices, {graph.Edges.Count} edges"
                    : statusMessage;

            if (vertexCountText != null)
                vertexCountText.Text = $"Vertices: {graph.Vertices.Count}";

            if (edgeCountText != null)
                edgeCountText.Text = $"Edges: {graph.Edges.Count}";
        }
    }
}