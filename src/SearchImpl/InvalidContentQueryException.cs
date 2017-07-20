using System;

namespace SenseNet.ContentRepository.Search
{
    [Serializable]
    public class InvalidContentQueryException : Exception
    {
        public string QueryText { get; private set; }

        public InvalidContentQueryException(string queryText, string message = null, Exception innerException = null)
            : base((message ?? "Invalid content query"), innerException)
        {
            QueryText = queryText;
        }

        protected InvalidContentQueryException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
