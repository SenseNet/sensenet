using System;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    [Serializable]
    internal class AddTreeActivity : TreeIndexingActivity
    {
        public override string TraceMessage => $"NodeId: {NodeId}, VersionId: {VersionId}, Path: {Path}";

        protected override Task<bool> ProtectedExecuteAsync(CancellationToken cancellationToken)
        {
            return IndexManager.AddTreeAsync(TreeRoot, Id, IsUnprocessedActivity, cancellationToken);
        }
    }
}
