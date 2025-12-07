using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GraphEditor.Core;

namespace GraphEditor.Visual
{
    // Интерфейс для алгоритмов расположения (уже есть в Contracts.cs)
    // Просто реализуем его

    public class CircleLayout : ILayoutAlgorithm
    {
        public double Radius { get; set; } = 200;
        public Point Center { get; set; } = new Point(400, 300);

        public IReadOnlyDictionary<string, Point> ComputePositions(IGraphModel graph)
        {
            var result = new Dictionary<string, Point>();
            var vertices = graph.Vertices.Keys.ToList();

            for (int i = 0; i < vertices.Count; i++)
            {
                double angle = 2 * Math.PI * i / vertices.Count;
                result[vertices[i]] = new Point(
                    Center.X + Radius * Math.Cos(angle),
                    Center.Y + Radius * Math.Sin(angle)
                );
            }

            return result;
        }
    }

    public class RandomLayout : ILayoutAlgorithm
    {
        private readonly Random _random = new Random();

        public IReadOnlyDictionary<string, Point> ComputePositions(IGraphModel graph)
        {
            var result = new Dictionary<string, Point>();

            foreach (var vertexId in graph.Vertices.Keys)
            {
                result[vertexId] = new Point(
                    _random.Next(100, 700),
                    _random.Next(100, 500)
                );
            }

            return result;
        }
    }

    public class ForceLayout : ILayoutAlgorithm
    {
        public IReadOnlyDictionary<string, Point> ComputePositions(IGraphModel graph)
        {
            var result = new Dictionary<string, Point>();
            var random = new Random();

            // Простая реализация - случайные позиции
            foreach (var vertexId in graph.Vertices.Keys)
            {
                result[vertexId] = new Point(
                    random.Next(100, 700),
                    random.Next(100, 500)
                );
            }

            return result;
        }
    }
}