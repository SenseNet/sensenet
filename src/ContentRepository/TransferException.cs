using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace SenseNet.ContentRepository
{
	[global::System.Serializable]
	public class TransferException : Exception
	{
		public TransferException(bool import, string message, string contentHandlerInfo, string contentHandlerName, string fieldName)
			: base(MessageHelper(import, message, contentHandlerInfo, contentHandlerName, fieldName)) { }
		public TransferException(bool import, string message, string contentHandlerInfo, string contentHandlerName, string fieldName, Exception inner)
			: base(MessageHelper(import, message, contentHandlerInfo, contentHandlerName, fieldName), inner) { }
		protected TransferException(SerializationInfo info, StreamingContext context)
			: base(info, context) { }

		private static string MessageHelper(bool import, string message, string contentHandlerInfo, string contentHandlerName, string fieldName)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("An exception occurred while ");
			sb.Append(import ? "importing" : "exporting");
			sb.Append(" data: ");
			if (!String.IsNullOrEmpty(message))
				sb.Append(message).Append(". ");
			if(!String.IsNullOrEmpty(contentHandlerInfo))
				sb.Append(" Content: ").Append(contentHandlerInfo);
			if(!String.IsNullOrEmpty(contentHandlerName))
				sb.Append(", ContentType: ").Append(contentHandlerName);
			if(!String.IsNullOrEmpty(fieldName))
				sb.Append(", Field: ").Append(fieldName);
			return sb.ToString();
		}
	}
}