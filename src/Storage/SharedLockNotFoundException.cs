using System;
using System.Runtime.Serialization;

namespace SenseNet.ContentRepository.Storage
{
    [Serializable]
    public class SharedLockNotFoundException : Exception
    {public SharedLockNotFoundException()
        {
        }

        public SharedLockNotFoundException(string message) : base(message)
        {
        }

        public SharedLockNotFoundException(string message, Exception inner) : base(message, inner)
        {
        }

        protected SharedLockNotFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
