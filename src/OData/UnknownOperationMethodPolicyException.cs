using System;
using System.Runtime.Serialization;

namespace SenseNet.OData
{
    /// <summary>
    /// Thrown when an operation method is configured with an unknown policy.
    /// </summary>
    [Serializable]
    public class UnknownOperationMethodPolicyException : ODataException
    {
        public UnknownOperationMethodPolicyException() : base(ODataExceptionCode.Forbidden)
        {
        }

        public UnknownOperationMethodPolicyException(string message) : base(message, ODataExceptionCode.Forbidden)
        {
        }

        public UnknownOperationMethodPolicyException(string message, Exception inner)
            : base(message, ODataExceptionCode.Forbidden, inner)
        {
        }

        protected UnknownOperationMethodPolicyException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
