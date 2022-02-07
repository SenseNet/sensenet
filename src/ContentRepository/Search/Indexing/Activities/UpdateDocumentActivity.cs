using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    [Serializable]
    internal class UpdateDocumentActivity : DocumentIndexingActivity
    {
        protected override Task<bool> ProtectedExecuteAsync(CancellationToken cancellationToken)
        {
            return ((IndexManager_INSTANCE)Providers.Instance.IndexManager).UpdateDocumentAsync(Document, Versioning, cancellationToken);
        }
    }
}