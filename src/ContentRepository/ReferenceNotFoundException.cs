using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace SenseNet.ContentRepository
{
	[global::System.Serializable]
	public class ReferenceNotFoundException : Exception
	{
		public ReferenceNotFoundException(string message, string referenceInfo) : base(MessageHelper(message, referenceInfo)) { }
		public ReferenceNotFoundException(string message, string referenceInfo, Exception inner) : base(MessageHelper(message, referenceInfo), inner) { }
		protected ReferenceNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }

		private static string MessageHelper(string message, string referenceInfo)
		{
			if (String.IsNullOrEmpty(referenceInfo))
				return message;
			if (String.IsNullOrEmpty(message))
				return String.Concat("Missing reference: ", referenceInfo);
			return String.Concat("Missing reference: ", referenceInfo, ". ", message);
		}
	}
}