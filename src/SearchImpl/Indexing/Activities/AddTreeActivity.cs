using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Search.Indexing.Activities
{
    [Serializable]
    internal class AddTreeActivity : TreeIndexingActivity
    {
        protected override bool ProtectedExecute()
        {
            return IndexManager.AddTree(TreeRoot, this.MoveOrRename ?? false, this.Id, this.IsUnprocessedActivity);
        }
    }
}
