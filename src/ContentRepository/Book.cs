using System;
using System.Threading;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class Book : Folder
    {
        public Book(Node parent) : this(parent, null) { }
        public Book(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected Book(NodeToken nt) : base(nt) { }

        public override async System.Threading.Tasks.Task SaveAsync(SavingMode mode, CancellationToken cancel)
        {
            this["Genre"] = ParentName;
            await base.SaveAsync(mode, cancel).ConfigureAwait(false);
        }
    }
}