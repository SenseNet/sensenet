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
            ThrowException("save");
        }
        public override void CopyTo(Node target)
        {
            ThrowException("copy");
        }
        public override void CopyTo(Node target, string newName)
        {
            ThrowException("copy");
        }
        public override void Delete()
        {
            ThrowException("delete");
        }
        public override void FinalizeContent()
        {
            ThrowException("finalize");
        }
        public override void ForceDelete()
        {
            ThrowException("delete");
        }
        public override void MoveTo(Node target)
        {
            ThrowException("move");
        }

        private static void ThrowException(string operation)
        {
            throw new SnNotSupportedException($"Cannot {operation} a content with an unknown handler.");
        }
    }
}
