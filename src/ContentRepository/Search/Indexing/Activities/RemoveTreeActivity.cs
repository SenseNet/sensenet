using System;
using SenseNet.Search;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    [Serializable]
    internal class RemoveTreeActivity : TreeIndexingActivity
    {
        protected override bool ProtectedExecute()
        {
            return IndexManager.DeleteDocuments(new[]
            {
                new SnTerm(IndexFieldName.InTree, TreeRoot),
                new SnTerm(IndexFieldName.Path, TreeRoot)
            }, null);
        }
    }
}