using System;
using System.Runtime.Serialization;

namespace SenseNet.Search.Querying.Parser
{
    /// <summary>
    /// Defines an exception class for any query compilation error.
    /// </summary>
    [Serializable]
    public class CompilerException : Exception
    {
        /// <inheritdoc />
        public CompilerException() { }
        /// <inheritdoc />
        public CompilerException(string message) : base(message) { }
        /// <inheritdoc />
        public CompilerException(string message, Exception inner) : base(message, inner) { }
        /// <inheritdoc />
        protected CompilerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
