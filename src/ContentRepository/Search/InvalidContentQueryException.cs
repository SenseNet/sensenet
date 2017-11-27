using System;

namespace SenseNet.ContentRepository.Search
{
    /// <summary>
    /// Defines an exception that will be thrown in the execution when occurs any error in the CQL query or in any other extension.
    /// </summary>
    [Serializable]
    public class InvalidContentQueryException : Exception
    {
        /// <summary>
        /// Gets the query text.
        /// </summary>
        public string QueryText { get; }

        /// <summary>
        /// Initializes a nem instance of the InvalidContentQueryException.
        /// </summary>
        /// <param name="queryText">The CQL query.</param>
        /// <param name="message">Optional message that overrides the automated text.</param>
        /// <param name="innerException">Wrapped exception if there is.</param>
        public InvalidContentQueryException(string queryText, string message = null, Exception innerException = null)
            : base(message ?? "Invalid content query", innerException)
        {
            QueryText = queryText;
        }

        /// <inheritdoc />
        protected InvalidContentQueryException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
