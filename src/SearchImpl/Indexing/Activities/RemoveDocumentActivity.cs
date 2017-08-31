using System;

namespace SenseNet.Search.Indexing.Activities
{
    [Serializable]
    internal class RemoveDocumentActivity : DocumentIndexingActivity
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