using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;
using Lucene.Net.Index;
using SenseNet.Diagnostics;
using System.Diagnostics;
using SenseNet.ContentRepository.Storage;
using Lucene.Net.Documents;
using Lucene.Net.Util;

namespace SenseNet.Search.Indexing.Activities
{
    [Serializable]
    internal class UpdateDocumentActivity : LuceneDocumentActivity
    {
        protected override bool ProtectedExecute()
        {
            return IndexManager.UpdateDocument(Document, Versioning);
        }
    }
}