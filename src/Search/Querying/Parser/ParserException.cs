using System;

namespace SenseNet.Search.Querying.Parser
{
    /// <summary>
    /// Defines an exception class for any CQL query parsing error.
    /// </summary>
    [Serializable]
	public class ParserException : Exception
	{
        /// <summary>
        /// Gets the query position information where the exception was caused.
        /// </summary>
		public LineInfo LineInfo { get; }

        /// <summary>
        /// Initializes a new instance of the ParserException with the relevant query position information.
        /// </summary>
	    public ParserException(LineInfo lineInfo) : base(MessageHelper(null, lineInfo))
		{
			LineInfo = lineInfo;
		}

	    /// <summary>
	    /// Initializes a new instance of the ParserException with a message and the relevant query position information.
	    /// </summary>
        public ParserException(string message, LineInfo lineInfo) : base(MessageHelper(message, lineInfo))
		{
			LineInfo = lineInfo;
		}

	    /// <summary>
	    /// Initializes a new instance of the ParserException with a message, the inner exception and the relevant query position information.
	    /// </summary>
        public ParserException(string message, Exception inner, LineInfo lineInfo) : base(MessageHelper(message, lineInfo), inner)
		{
			LineInfo = lineInfo;
		}

	    /// <inheritdoc />
		protected ParserException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }

        private static string MessageHelper(string message, LineInfo lineInfo)
        {
            return string.Concat(message ?? "Unknown parser error.", " ", lineInfo ?? LineInfo.NullValue);
        }
	}
}
