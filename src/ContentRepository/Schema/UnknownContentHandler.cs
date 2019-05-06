using System;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Storage
{
    [ContentHandler]
    public class UnknownContentHandler : Node
    {
        public UnknownContentHandler(Node parent) : this(parent, null) { }
        public UnknownContentHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected UnknownContentHandler(NodeToken nt) : base(nt) { }
        public override bool IsContentType { get; } = false;

        public override void Save(NodeSaveSettings settings)
        {
            throw new InvalidOperationException("Cannot save a content with an unknown handler.");
        }
    }
}
