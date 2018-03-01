using System;

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

        /// <summary>
        /// Initializes a new NodeIndexedEventArgs instance.
        /// </summary>
        public NodeIndexedEventArgs(string path) { Path = path; }
    }
}
