using System;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    [Serializable]
    internal class AddDocumentActivity : DocumentIndexingActivity
    {
        protected override Task<bool> ProtectedExecuteAsync(CancellationToken cancellationToken)
        {
            return IndexManager.AddDocumentAsync(Document, Versioning, cancellationToken);
        }
    }
}