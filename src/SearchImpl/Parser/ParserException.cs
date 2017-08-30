using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Search.Parser
{
	[global::System.Serializable]
	public class ParserException_OLD : Exception //UNDONE:!!! Delete ASAP
    {
		public LineInfo_OLD LineInfo { get; private set; }

		public ParserException_OLD(LineInfo_OLD lineInfo) : base(MessageHelper(null, lineInfo))
		{
			LineInfo = lineInfo;
		}
		public ParserException_OLD(string message, LineInfo_OLD lineInfo) : base(MessageHelper(message, lineInfo))
		{
			LineInfo = lineInfo;
		}
		public ParserException_OLD(string message, Exception inner, LineInfo_OLD lineInfo) : base(MessageHelper(message, lineInfo), inner)
		{
			LineInfo = lineInfo;
		}
		protected ParserException_OLD(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }

        private static string MessageHelper(string message, LineInfo_OLD lineInfo)
        {
            return String.Concat(message ?? "Unknown parser error.", " ", lineInfo ?? LineInfo_OLD.NullValue);
        }
	}
}
