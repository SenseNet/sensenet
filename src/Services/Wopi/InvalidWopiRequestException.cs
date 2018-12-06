using System;
using System.Runtime.Serialization;

namespace SenseNet.Services.Wopi
{
    [Serializable]
    public class InvalidWopiRequestException : Exception
    {
        public InvalidWopiRequestException()
        {
        }

        public InvalidWopiRequestException(string message) : base(message)
        {
        }

        public InvalidWopiRequestException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InvalidWopiRequestException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
