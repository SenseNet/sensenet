using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Search.Parser
{
	[global::System.Serializable]
	public class ParserException : Exception
	{
		public LineInfo LineInfo { get; private set; }

		public ParserException(LineInfo lineInfo) : base(MessageHelper(null, lineInfo))
		{
			LineInfo = lineInfo;
		}
		public ParserException(string message, LineInfo lineInfo) : base(MessageHelper(message, lineInfo))
		{
			LineInfo = lineInfo;
		}
		public ParserException(string message, Exception inner, LineInfo lineInfo) : base(MessageHelper(message, lineInfo), inner)
		{
			LineInfo = lineInfo;
		}
		protected ParserException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }

        private static string MessageHelper(string message, LineInfo lineInfo)
        {
            return String.Concat(message ?? "Unknown parser error.", " ", lineInfo ?? LineInfo.NullValue);
        }
	}
}
