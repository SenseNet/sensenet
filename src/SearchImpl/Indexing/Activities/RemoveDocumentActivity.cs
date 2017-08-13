using System;
using Lucene.Net.Index;
using Lucene.Net.Util;
using SenseNet.Diagnostics;

namespace SenseNet.Search.Indexing.Activities
{
    [Serializable]
    internal class RemoveDocumentActivity : LuceneDocumentActivity
    {
        protected override bool ProtectedExecute()
        {
            return IndexManager.DeleteDocument(VersionId, Versioning);
        }

        public override IndexDocument CreateDocument()
        {
            throw new InvalidOperationException();
        }
    }
}