using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage
{
    [Serializable]
    public class InvalidContentException : ApplicationException
    {
        public InvalidContentException() { }
        public InvalidContentException(string message) : base(message) { }
        public InvalidContentException(string message, Exception inner) : base(message, inner) { }
        protected InvalidContentException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
