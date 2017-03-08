using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.AppModel
{
    [Serializable]
    public class RepositoryEventException : ApplicationException
    {
        public IEnumerable<Exception> Exceptions { get; private set; }

        public RepositoryEventException() { Exceptions = new Exception[0]; }
        public RepositoryEventException(string message) : base(message) { Exceptions = new Exception[0]; }
        public RepositoryEventException(string message, Exception inner) : base(message, inner) { Exceptions = new Exception[0]; }
        protected RepositoryEventException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public RepositoryEventException(string contextPath, RepositoryEventBase @event, IEnumerable<Exception> exceptions)
            : base(MessageHelper(contextPath, @event, exceptions))
        {
            this.Exceptions = exceptions;
        }
        private static string MessageHelper(string contextPath, RepositoryEventBase @event, IEnumerable<Exception> exceptions)
        {
            var exCount = exceptions.Count();
            return String.Format("Event {0} handlers threw {1} exception{2}. Context path: {3}. For more information see Exceptions property.",
                @event.EventName, exCount, exCount == 1 ? "" : "s", contextPath);
        }
    }

}
