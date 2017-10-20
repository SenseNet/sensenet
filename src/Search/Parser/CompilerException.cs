using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser
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
