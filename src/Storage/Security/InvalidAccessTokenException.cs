using System;
using System.Runtime.Serialization;

namespace SenseNet.ContentRepository.Storage.Security
{
    [Serializable]
    public class InvalidAccessTokenException : Exception
    {
        public InvalidAccessTokenException()
        {
        }

        public InvalidAccessTokenException(string message) : base(message)
        {
        }

        public InvalidAccessTokenException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InvalidAccessTokenException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
