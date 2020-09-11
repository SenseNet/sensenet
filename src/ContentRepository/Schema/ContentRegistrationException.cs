using System;

namespace SenseNet.ContentRepository.Schema
{
	[global::System.Serializable]
	public class ContentRegistrationException : Exception
	{
		private string _contentTypeName;
		private string _fieldName;

		public string ContentTypeName => _contentTypeName ?? "";

        public string FieldName => _fieldName ?? "";

        public ContentRegistrationException() { }
		public ContentRegistrationException(string message) : this(message, (Exception)null) { }
		public ContentRegistrationException(string message, string contentTypeName) : this(message, (Exception)null, contentTypeName) { }
		public ContentRegistrationException(string message, string contentTypeName, string fieldName) : this(message, null, contentTypeName, fieldName) { }
		public ContentRegistrationException(string message, Exception inner) : this(message, inner, null) { }
		public ContentRegistrationException(string message, Exception inner, string contentTypeName) : this(message, inner, contentTypeName, null) { }
		public ContentRegistrationException(string message, Exception inner, string contentTypeName, string fieldName)
			: base(string.Format(message, contentTypeName ?? string.Empty, fieldName ?? string.Empty), inner)
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