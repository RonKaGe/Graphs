using System.Collections.Generic;

namespace GraphEditor.Algorithms
{
    public interface IAlgorithmResult
    {
        bool Success { get; }
        string Message { get; }
        Dictionary<string, object> Data { get; }
    }

    public class AlgorithmResult : IAlgorithmResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Data { get; set; }

        public AlgorithmResult()
        {
            Data = new Dictionary<string, object>();
        }
    }
}