using System;

namespace SenseNet.ContentRepository.Search
{
    //UNDONE:!!!! XMLDOC ContentRepository
    [Serializable]
    public class InvalidContentQueryException : Exception
    {
        //UNDONE:!!!! XMLDOC ContentRepository
        public string QueryText { get; }

        //UNDONE:!!!! XMLDOC ContentRepository
        public InvalidContentQueryException(string queryText, string message = null, Exception innerException = null)
            : base(message ?? "Invalid content query", innerException)
        {
            QueryText = queryText;
        }

        //UNDONE:!!!! XMLDOC ContentRepository
        protected InvalidContentQueryException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
