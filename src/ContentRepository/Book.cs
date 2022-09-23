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


        [Obsolete("Use async version instead.", true)]
        public override void Save(SavingMode mode)
        {
            SaveAsync(mode, CancellationToken.None).GetAwaiter().GetResult();
        }
        public override async System.Threading.Tasks.Task SaveAsync(SavingMode mode, CancellationToken cancel)
        {
            this["Genre"] = ParentName;
            await base.SaveAsync(mode, cancel).ConfigureAwait(false);
        }
    }
}