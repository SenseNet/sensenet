using System;
using System.Diagnostics;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    [Serializable]
    internal class AddDocumentActivity : DocumentIndexingActivity
    {
        protected override bool ProtectedExecute()
        {
            Trace.WriteLine($"TMPINVEST: AddDocumentActivity");

            return IndexManager.AddDocument(Document, Versioning);
        }
    }
}