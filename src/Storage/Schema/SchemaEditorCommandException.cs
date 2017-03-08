using System;
using System.Runtime.Serialization;

namespace SenseNet.ContentRepository.Storage.Schema
{
	[Serializable]
	public class SchemaEditorCommandException : Exception
	{
		public SchemaEditorCommandException() : base() { }
		public SchemaEditorCommandException(string message) : base(message) { }
		public SchemaEditorCommandException(string message, Exception innerException) : base(message, innerException) { }
		protected SchemaEditorCommandException(SerializationInfo info, StreamingContext context) : base(info, context) { }

	}
}