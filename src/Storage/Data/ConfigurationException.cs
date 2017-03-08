using System;

namespace SenseNet.ContentRepository.Storage.Data
{

	[global::System.Serializable]
	public class ConfigurationException : Exception
	{
		public ConfigurationException() { }
		public ConfigurationException(string message) : base(message) { }
		public ConfigurationException(string message, Exception inner) : base(message, inner) { }
		protected ConfigurationException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}
}