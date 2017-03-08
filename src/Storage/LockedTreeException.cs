using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage
{
    [Serializable]
    public class LockedTreeException : RepositoryException
    {
        public LockedTreeException() : base(EventId.TreeLock) { }
        public LockedTreeException(string message) : base(EventId.TreeLock, message) { }
        public LockedTreeException(string message, Exception inner) : base(EventId.TreeLock, message, inner) { }
        protected LockedTreeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
