using System;
using System.Runtime.Serialization;

namespace SenseNet.ContentRepository.Sharing
{
    /// <summary>
    /// Thrown when the query contains sharing expressions that cannot be combined 
    /// correctly with other parts of the query.
    /// </summary>
    [Serializable]
    public class InvalidContentSharingQueryException : Exception
    {
        public InvalidContentSharingQueryException()
        {
        }

        public InvalidContentSharingQueryException(string message) : base(message)
        {
        }

        public InvalidContentSharingQueryException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InvalidContentSharingQueryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
