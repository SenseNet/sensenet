using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SenseNet.Search.Querying
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
