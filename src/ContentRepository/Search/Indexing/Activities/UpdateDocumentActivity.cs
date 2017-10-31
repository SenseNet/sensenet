using System;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    [Serializable]
    internal class UpdateDocumentActivity : DocumentIndexingActivity
    {
        protected override bool ProtectedExecute()
        {
            return IndexManager.UpdateDocument(Document, Versioning);
        }
    }
}