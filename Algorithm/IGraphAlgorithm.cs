using GraphEditor.Core;
using Graphs.Algorithm;
using System.Collections.Generic;

namespace GraphEditor.Algorithms
{
    public interface IGraphAlgorithm
    {
        string Name { get; }
        string Description { get; }

        // Проверка, применим ли алгоритм к данному графу
        bool CanExecute(IGraphModel graph, out string errorMessage);

        // Выполнение алгоритма
        AlgorithmResult Execute(IGraphModel graph, Dictionary<string, object> parameters);
    }
}