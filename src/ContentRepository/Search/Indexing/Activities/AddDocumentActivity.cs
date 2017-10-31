using System;
using SenseNet.ContentRepository.Search.Indexing;

namespace SenseNet.Search.Indexing.Activities
{
    [Serializable]
    internal class AddDocumentActivity : DocumentIndexingActivity
    {
        protected override bool ProtectedExecute()
        {
            return IndexManager.AddDocument(Document, Versioning);
        }
    }
}