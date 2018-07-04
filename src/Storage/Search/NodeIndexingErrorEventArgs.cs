using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Defines an event argument containing the identifiers of the node that could not be indexed.
    /// </summary>
    public class NodeIndexingErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the id of the currently indexed node.
        /// </summary>
        public int NodeId { get; }
        /// <summary>
        /// Gets the id of the currently indexed version.
        /// </summary>
        public int VersionId { get; }
        /// <summary>
        /// Gets the path of the currently indexed node.
        /// </summary>
        public string Path { get; }
        /// <summary>
        /// Gets the indexing error.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Initializes a new NodeIndexedEventArgs instance.
        /// </summary>
        public NodeIndexingErrorEventArgs(int nodeId, int versionId, string path, Exception exception)
        {
            NodeId = nodeId;
            VersionId = versionId;
            Path = path;
            Exception = exception;
        }
    }
}
