using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository
{
    [Serializable]
    public class UnknownActionException : Exception
    {
        public UnknownActionException() { }
        public UnknownActionException(string message) : base(message) { }
        public UnknownActionException(string message, Exception inner) : base(message, inner) { }
        public UnknownActionException(string message, string actionName) : base(message)
        {
            this.ActionName = actionName;
        }

        protected UnknownActionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public string ActionName
        {
            get; private set;
        }
    }
}
