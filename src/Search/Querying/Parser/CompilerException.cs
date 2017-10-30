using System;
using System.Runtime.Serialization;

namespace SenseNet.Search.Querying.Parser
{
    [Serializable]
    public class CompilerException : Exception
    {
        public CompilerException() { }
        public CompilerException(string message) : base(message) { }
        public CompilerException(string message, Exception inner) : base(message, inner) { }
        protected CompilerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
