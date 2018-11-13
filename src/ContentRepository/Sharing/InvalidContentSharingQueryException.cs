using System;
using System.Runtime.Serialization;

namespace SenseNet.ContentRepository.Sharing
{
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
