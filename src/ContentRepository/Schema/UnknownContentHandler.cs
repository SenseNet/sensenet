using System;
using System.Threading;
using System.Threading.Tasks;
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

        [Obsolete("Use async version instead.", true)]
        public override void Save(NodeSaveSettings settings)
        {
            ThrowException("save");
        }
        public override Task SaveAsync(CancellationToken cancel)
        {
            ThrowException("save");
            return Task.CompletedTask;
        }
        public override Task SaveAsync(NodeSaveSettings settings, CancellationToken cancel)
        {
            ThrowException("save");
            return Task.CompletedTask;
        }

        public override void CopyTo(Node target)
        {
            ThrowException("copy");
        }
        public override void CopyTo(Node target, string newName)
        {
            ThrowException("copy");
        }
        [Obsolete("Use async version instead", true)]
        public override void Delete()
        {
            ThrowException("delete");
        }
        public override Task DeleteAsync(CancellationToken cancel)
        {
            ThrowException("delete");
            return Task.CompletedTask;
        }

        [Obsolete("Use async version instead.", true)]
        public override void FinalizeContent()
        {
            ThrowException("finalize");
        }
        public override Task FinalizeContentAsync(CancellationToken cancel)
        {
            ThrowException("finalize");
            return Task.CompletedTask;
        }
        [Obsolete("Use async version instead", true)]
        public override void ForceDelete()
        {
            ThrowException("delete");
        }

        public override Task ForceDeleteAsync(CancellationToken cancel)
        {
            ThrowException("delete");
            return Task.CompletedTask;
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
