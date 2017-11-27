using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Portal.OData.Parser
{
    /// <summary>
    /// Represents a parsing error of an OData request.
    /// </summary>
    [Serializable]
    public class ODataParserException : Exception
    {
        /// <summary>
        /// Gets the line number of the error position in the source. 
        /// </summary>
        public int Line { get; private set; }
        /// <summary>
        /// Gets the column number of the error position in the source. 
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ODataParserException.
        /// </summary>
        /// <param name="line">The line number of the error position in the source. </param>
        /// <param name="column">The column number of the error position in the source. </param>
        public ODataParserException(int line, int column) { Line = line; Column = column; }
        /// <summary>
        /// Initializes a new instance of the ODataParserException.
        /// </summary>
        /// <param name="message">The message of the error.</param>
        /// <param name="line">The line number of the error position in the source. </param>
        /// <param name="column">The column number of the error position in the source. </param>
        public ODataParserException(string message, int line, int column) : base(message) { Line = line; Column = column; }
        /// <summary>
        /// Initializes a new instance of the ODataParserException.
        /// </summary>
        /// <param name="message">The message of the error.</param>
        /// <param name="line">The line number of the error position in the source. </param>
        /// <param name="column">The column number of the error position in the source. </param>
        /// <param name="inner">The original exception that is wrapped by.</param>
        public ODataParserException(string message, int line, int column, Exception inner) : base(message, inner) { Line = line; Column = column; }

        /// <inheritdoc />
        protected ODataParserException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
