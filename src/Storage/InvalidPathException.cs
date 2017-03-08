using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// This is an exception thrown by the system when the path of the node is not valid.
    /// </summary>
	[global::System.Serializable]
	public class InvalidPathException : Exception
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidPathException"/> class.
        /// </summary>
		public InvalidPathException() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidPathException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
		public InvalidPathException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidPathException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
		public InvalidPathException(string message, Exception inner) : base(message, inner) { }
		protected InvalidPathException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}
}