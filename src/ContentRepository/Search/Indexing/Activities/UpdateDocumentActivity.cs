using System;
using SenseNet.ContentRepository.Search.Indexing;

namespace SenseNet.Search.Indexing.Activities
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