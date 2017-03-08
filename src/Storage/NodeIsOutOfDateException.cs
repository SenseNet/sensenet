using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage
{
    [Serializable]
    public class NodeIsOutOfDateException : ApplicationException
    {
        public NodeIsOutOfDateException() { }
        public NodeIsOutOfDateException(string message) : base(message) { }
        public NodeIsOutOfDateException(string message, Exception inner) : base(message, inner) { }
        protected NodeIsOutOfDateException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public NodeIsOutOfDateException(int nodeId, string path, int versionId, VersionNumber versionNumber, Exception inner, long timestamp)
            : base(FormatMessage(inner, nodeId, path, versionId, versionNumber, timestamp), inner) { }

        private static string FormatMessage(Exception inner, int nodeId, string path, int versionId, VersionNumber versionNumber, long timestamp)
        {
            string message = null;
            if (inner != null)
                message = inner.Message;
            if (String.IsNullOrEmpty(message))
                message = "Node is out of date";
            return String.Format("{0} NodeId: {1}, VersionId: {2}, Version: {3}, Path: {4}, Invalid timestamp: {5}", message, nodeId, versionId, versionNumber, path, timestamp);
        }
    }
}
