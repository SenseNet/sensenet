using System;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    [Serializable]
    internal class AddTreeActivity : TreeIndexingActivity
    {
        protected override bool ProtectedExecute()
        {
            return IndexManager.AddTree(TreeRoot, Id, IsUnprocessedActivity);
        }
    }
}
