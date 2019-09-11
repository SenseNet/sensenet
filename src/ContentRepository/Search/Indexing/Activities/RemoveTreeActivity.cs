using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Search;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    [Serializable]
    internal class RemoveTreeActivity : TreeIndexingActivity
    {
        protected override Task<bool> ProtectedExecuteAsync(CancellationToken cancellationToken)
        {
            return IndexManager.DeleteDocumentsAsync(new[]
            {
                new SnTerm(IndexFieldName.InTree, TreeRoot),
                new SnTerm(IndexFieldName.Path, TreeRoot)
            }, null, cancellationToken);
        }
    }
}