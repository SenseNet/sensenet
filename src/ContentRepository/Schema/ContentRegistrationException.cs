using System;
using System.Collections.Generic;
using System.Text;

namespace  SenseNet.ContentRepository.Schema
{
	[global::System.Serializable]
	public class ContentRegistrationException : Exception
	{
		private string _contentTypeName;
		private string _fieldName;

		public string ContentTypeName
		{
			get { return _contentTypeName ?? ""; }
		}
		public string FieldName
		{
			get { return _fieldName ?? ""; }
		}

		public ContentRegistrationException() { }
		public ContentRegistrationException(string message) : this(message, (Exception)null) { }
		public ContentRegistrationException(string message, string contentTypeName) : this(message, (Exception)null, contentTypeName) { }
		public ContentRegistrationException(string message, string contentTypeName, string fieldName) : this(message, null, contentTypeName, fieldName) { }
		public ContentRegistrationException(string message, Exception inner) : this(message, inner, null) { }
		public ContentRegistrationException(string message, Exception inner, string contentTypeName) : this(message, inner, contentTypeName, null) { }
		public ContentRegistrationException(string message, Exception inner, string contentTypeName, string fieldName)
			: base(String.Concat(message,
				contentTypeName == null ? "" : String.Concat("[ContentType: ", contentTypeName, "]"),
				fieldName == null ? "" : String.Concat("[Field: ", fieldName, "]")),
			inner)
		{
			_contentTypeName = contentTypeName;
			_fieldName = fieldName;
		}

		protected ContentRegistrationException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}
}