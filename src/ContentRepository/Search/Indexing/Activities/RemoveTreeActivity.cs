using System;
using Lucene.Net.Index;
using SenseNet.Diagnostics;

namespace SenseNet.Search.Indexing.Activities
{
    [Serializable]
    internal class RemoveTreeActivity : LuceneTreeActivity
    {
        protected override bool ProtectedExecute()
        {
            var terms = new[] { new Term("InTree", TreeRoot), new Term("Path", TreeRoot) };
            return LuceneManager.DeleteDocuments(terms, MoveOrRename ?? false, this.Id, this.IsUnprocessedActivity, null);
        }
    }
}