using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Diagnostics;

namespace SenseNet.Packaging
{
    [Serializable]
    public class InvalidPackageException : PackagingException
    {
        public InvalidPackageException(string message, PackagingExceptionType errorType = PackagingExceptionType.NotDefined) : base(EventId.Packaging, message, errorType) { }
        public InvalidPackageException(string message, Exception inner, PackagingExceptionType errorType = PackagingExceptionType.NotDefined) : base(EventId.Packaging, message, inner, errorType) { }
        protected InvalidPackageException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
    [Serializable]
    public class PackagePreconditionException : PackagingException
    {
        public PackagePreconditionException(string message, PackagingExceptionType errorType = PackagingExceptionType.NotDefined) : base(EventId.Packaging, message, errorType) { }
        public PackagePreconditionException(string message, Exception inner, PackagingExceptionType errorType = PackagingExceptionType.NotDefined) : base(EventId.Packaging, message, inner, errorType) { }
        protected PackagePreconditionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
