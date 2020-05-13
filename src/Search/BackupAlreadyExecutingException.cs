using System;
using System.Runtime.Serialization;

namespace SenseNet.Search
{
    [Serializable]
    public class BackupAlreadyExecutingException : Exception
    {
        public BackupAlreadyExecutingException() { }

        public BackupAlreadyExecutingException(string message) : base(message) { }

        public BackupAlreadyExecutingException(string message, Exception inner) : base(message, inner) { }

        protected BackupAlreadyExecutingException(SerializationInfo info,StreamingContext context)
            : base(info, context) { }
    }
}
