using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Portal.OData.Parser
{
    [Serializable]
    public class ODataParserException : Exception
    {
        public int Line { get; private set; }
        public int Column { get; private set; }

        public ODataParserException(int line, int column) { Line = line; Column = column; }
        public ODataParserException(string message, int line, int column) : base(message) { Line = line; Column = column; }
        public ODataParserException(string message, int line, int column, Exception inner) : base(message, inner) { Line = line; Column = column; }

        protected ODataParserException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
