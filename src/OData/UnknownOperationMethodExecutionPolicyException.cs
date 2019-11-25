using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SenseNet.OData
{
    [Serializable]
    public class UnknownOperationMethodExecutionPolicyException : ODataException
    {
        public UnknownOperationMethodExecutionPolicyException() : base(ODataExceptionCode.Forbidden)
        {
        }

        public UnknownOperationMethodExecutionPolicyException(string message) : base(message, ODataExceptionCode.Forbidden)
        {
        }

        public UnknownOperationMethodExecutionPolicyException(string message, Exception inner)
            : base(message, ODataExceptionCode.Forbidden, inner)
        {
        }

        protected UnknownOperationMethodExecutionPolicyException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
