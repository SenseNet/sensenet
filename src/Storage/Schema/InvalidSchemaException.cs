using System;
using System.Runtime.Serialization;

namespace SenseNet.ContentRepository.Storage.Schema
{
	[Serializable]
	public class InvalidSchemaException : Exception
	{
		public InvalidSchemaException() : base() { }
		public InvalidSchemaException(string message) : base(message) { }
		public InvalidSchemaException(string message, Exception innerException) : base(message, innerException) { }
		protected InvalidSchemaException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}