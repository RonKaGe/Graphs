using GraphEditor.Core;
using System;
using System.Text;
using GraphEditor.Services;

namespace GraphEditor.Services
{
    public class MatrixService
    {
        public string FormatAdjacencyMatrix(double[,] matrix, IGraphModel graph)
        {
            if (matrix == null || matrix.Length == 0)
                return "Empty matrix";

            int size = matrix.GetLength(0);
            var sb = new StringBuilder();

            sb.AppendLine("=== ADJACENCY MATRIX ===");
            sb.AppendLine($"Size: {size}×{size}");
            sb.AppendLine($"Directed: {graph.IsDirected}");
            sb.AppendLine($"Vertices: {graph.Vertices.Count}, Edges: {graph.Edges.Count}");
            sb.AppendLine();

            // Заголовок
            sb.Append("     ");
            for (int j = 0; j < size; j++)
            {
                sb.Append($"{j + 1,6}");
            }
            sb.AppendLine();
            sb.AppendLine(new string('-', 7 * size + 5));

            // Данные
            for (int i = 0; i < size; i++)
            {
                sb.Append($"{i + 1,3} |");
                for (int j = 0; j < size; j++)
                {
                    if (double.IsPositiveInfinity(matrix[i, j]))
                        sb.Append("     ∞");
                    else if (matrix[i, j] == 0)
                        sb.Append("     0");
                    else
                        sb.Append($"{matrix[i, j],6:F1}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}