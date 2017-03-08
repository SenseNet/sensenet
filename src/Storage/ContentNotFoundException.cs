using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage
{
    [Serializable]
    public class ContentNotFoundException : Exception
    {
        public string Path { get; private set; }
        public int Id { get; private set; }

        public ContentNotFoundException(string idOrPathOrMessage) : base(GetMessage(idOrPathOrMessage)) { }
        public ContentNotFoundException(string idOrPathOrMessage, Exception inner) : base(GetMessage(idOrPathOrMessage), inner) { }
        protected ContentNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        private static string GetMessage(string msg)
        {
            int id;
            if(int.TryParse(msg, out id) || (RepositoryPath.IsValidPath(msg) == RepositoryPath.PathResult.Correct))
                return "Content not found: " + msg;
            return msg;
        }
    }
}
