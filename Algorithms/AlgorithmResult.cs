using System.Collections.Generic;

namespace GraphEditor.Algorithms
{
    public class AlgorithmResult
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