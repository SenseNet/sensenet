using System;
using System.Net;
using System.Runtime.Serialization;

namespace SenseNet.Services.Wopi
{
    [Serializable]
    public class InvalidWopiRequestException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public InvalidWopiRequestException(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        public InvalidWopiRequestException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public InvalidWopiRequestException(HttpStatusCode statusCode, string message, Exception inner) : base(message, inner)
        {
            StatusCode = statusCode;
        }

        protected InvalidWopiRequestException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
