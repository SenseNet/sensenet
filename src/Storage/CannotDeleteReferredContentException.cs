using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage
{
    [Serializable]
    public class CannotDeleteReferredContentException : Exception
    {
        public int TotalCountOfReferrers { get; private set; }
        public IEnumerable<string> Referrers { get; private set; }

        public CannotDeleteReferredContentException(IEnumerable<Node> referrers, int totalCountOfReferrers) : base(GetMessage(referrers, totalCountOfReferrers))
        {
            TotalCountOfReferrers = totalCountOfReferrers;
            Referrers = referrers == null ? new string[0] : referrers.Select(r => r.Path).ToArray();
        }

        private static string GetMessage(IEnumerable<Node> referrers, int totalCountOfReferrers)
        {
            return String.Format("You cannot delete this content because it is referenced by another content.  Referrers count: {0}\r\n{1}{2}",
                totalCountOfReferrers, String.Join("\r\n", referrers.Select(r => r.Path)), totalCountOfReferrers > referrers.Count() ? "\r\n...\r\n" : "\r\n");
        }

        protected CannotDeleteReferredContentException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
