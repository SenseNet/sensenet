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
        public InvalidPackageException(string message) : base(EventId.Packaging, message) { }
        public InvalidPackageException(string message, Exception inner) : base(EventId.Packaging, message, inner) { }
        protected InvalidPackageException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
    [Serializable]
    public class PackagePreconditionException : PackagingException
    {
        public PackagePreconditionException(string message) : base(EventId.Packaging, message) { }
        public PackagePreconditionException(string message, Exception inner) : base(EventId.Packaging, message, inner) { }
        protected PackagePreconditionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
