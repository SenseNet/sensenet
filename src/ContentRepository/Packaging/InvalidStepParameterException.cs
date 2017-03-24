using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Packaging
{
    [Serializable]
    public class InvalidStepParameterException : PackagingException
    {
        public InvalidStepParameterException() : base(PackagingExceptionType.InvalidStepParameter) { }
        public InvalidStepParameterException(string message) : base(message, PackagingExceptionType.InvalidStepParameter) { }
        public InvalidStepParameterException(string message, Exception inner) : base(message, inner, PackagingExceptionType.InvalidStepParameter) { }
        protected InvalidStepParameterException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
    [Serializable]
    public class InvalidParameterException : PackagingException
    {
        public InvalidParameterException() : base(PackagingExceptionType.InvalidParameter) { }
        public InvalidParameterException(string message) : base(message, PackagingExceptionType.InvalidParameter) { }
        public InvalidParameterException(string message, Exception inner) : base(message, inner, PackagingExceptionType.InvalidParameter) { }
        protected InvalidParameterException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}
