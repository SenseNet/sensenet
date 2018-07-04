using System;
using SenseNet.ContentRepository.Storage;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Defines an event argument containing the path of the node that just has been indexed.
    /// </summary>
    public class NodeIndexedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the path of the currently indexed node.
        /// </summary>
        public string Path { get; }
        public int NodeId { get; }
        public int VersionId { get; }
        public string Version { get; }

        /// <summary>
        /// Initializes a new NodeIndexedEventArgs instance.
        /// </summary>
        public NodeIndexedEventArgs(string path, int nodeId = 0, int versionId = 0, string version = null)
        {
            Path = path;
            NodeId = nodeId;
            VersionId = versionId;
            Version = version;
        }
    }
}
