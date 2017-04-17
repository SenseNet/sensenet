using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Packaging
{
    [Serializable]
    public class InvalidStepParameterException : PackagingException
    {
        public InvalidStepParameterException(PackagingExceptionType errorType = PackagingExceptionType.InvalidStepParameter) : base(errorType) { }
        public InvalidStepParameterException(string message, PackagingExceptionType errorType = PackagingExceptionType.InvalidStepParameter) : base(message, errorType) { }
        public InvalidStepParameterException(string message, Exception inner, PackagingExceptionType errorType = PackagingExceptionType.InvalidStepParameter) : base(message, inner, errorType) { }
        protected InvalidStepParameterException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
    [Serializable]
    public class InvalidParameterException : PackagingException
    {
        public InvalidParameterException(PackagingExceptionType errorType = PackagingExceptionType.InvalidParameter) : base(errorType) { }
        public InvalidParameterException(string message, PackagingExceptionType errorType = PackagingExceptionType.InvalidParameter) : base(message, errorType) { }
        public InvalidParameterException(string message, Exception inner, PackagingExceptionType errorType = PackagingExceptionType.InvalidParameter) : base(message, inner, errorType) { }
        protected InvalidParameterException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}
