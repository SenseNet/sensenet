using System;

namespace SenseNet.ContentRepository.Search.Indexing
{
    public class NodeIndexedEventArgs : EventArgs
    {
        public string Path { get; }
        public NodeIndexedEventArgs(string path) { Path = path; }
    }
}
