using System;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    [Serializable]
    internal class AddTreeActivity : TreeIndexingActivity
    {
        protected override Task<bool> ProtectedExecuteAsync(CancellationToken cancellationToken)
        {
            return IndexManager.AddTreeAsync(TreeRoot, Id, IsUnprocessedActivity, cancellationToken);
        }
    }
}
