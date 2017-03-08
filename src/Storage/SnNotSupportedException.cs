using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage
{
    [Serializable]
    public class SnNotSupportedException : RepositoryException
    {
        private static readonly string DefaultMessage = "Not supported in this version.";

        public SnNotSupportedException() : base(EventId.NotSupported, DefaultMessage) { }
        public SnNotSupportedException(string message) : base(EventId.NotSupported, message) { }
        public SnNotSupportedException(string message, Exception inner) : base(EventId.NotSupported, message, inner) { }
        protected SnNotSupportedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

    }
}
