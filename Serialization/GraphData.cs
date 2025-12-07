using System.Collections.Generic;
using System.Windows;

namespace GraphEditor.Serialization
{
    public class GraphData
    {
        public GraphModelData GraphModel { get; set; } = new GraphModelData();
        public VisualModelData VisualModel { get; set; } = new VisualModelData();
        public SettingsData Settings { get; set; } = new SettingsData();
    }

    public class GraphModelData
    {
        public bool IsDirected { get; set; }
        public bool AllowParallelEdges { get; set; }
        public bool AllowSelfLoops { get; set; }

        public List<VertexData> Vertices { get; set; } = new List<VertexData>();
        public List<EdgeData> Edges { get; set; } = new List<EdgeData>();
    }

    public class VertexData
    {
        public string Id { get; set; } = string.Empty;
        public string? Label { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    public class EdgeData
    {
        public string Id { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public double? Weight { get; set; }
        public double? Capacity { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    public class VisualModelData
    {
        public Dictionary<string, PointData> VertexPositions { get; set; } = new Dictionary<string, PointData>();
        public Dictionary<string, ColorData> VertexColors { get; set; } = new Dictionary<string, ColorData>();
        public Dictionary<string, ColorData> EdgeColors { get; set; } = new Dictionary<string, ColorData>();
    }

    public class PointData
    {
        public double X { get; set; }
        public double Y { get; set; }

        public PointData() { }

        public PointData(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static PointData FromPoint(Point point) => new PointData(point.X, point.Y);
        public Point ToPoint() => new Point(X, Y);
    }

    public class ColorData
    {
        public byte A { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public ColorData() { }

        public ColorData(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        public static ColorData FromColor(System.Windows.Media.Color color)
            => new ColorData(color.A, color.R, color.G, color.B);

        public System.Windows.Media.Color ToColor()
            => System.Windows.Media.Color.FromArgb(A, R, G, B);
    }

    public class SettingsData
    {
        public string LayoutType { get; set; } = "None";
        public double CanvasWidth { get; set; } = 800;
        public double CanvasHeight { get; set; } = 600;
        public DateTime SavedAt { get; set; } = DateTime.Now;
    }
}