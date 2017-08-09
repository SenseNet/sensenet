using SenseNet.ContentRepository.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Indexing.Activities
{
    [Serializable]
    internal class RebuildActivity : LuceneIndexingActivity
    {
        private static readonly int[] EmptyIntArray = new int[0];
        protected override bool ProtectedExecute()
        {
            // getting common versioning info
            var head = NodeHead.Get(this.NodeId);
            var versioningInfo = new VersioningInfo
            {
                Delete = EmptyIntArray,
                Reindex = EmptyIntArray,
                LastDraftVersionId = head.LastMinorVersionId,
                LastPublicVersionId = head.LastMajorVersionId
            };

            // delete documents by NodeId
            IndexManager.DeleteDocuments(new[] { IndexManager.GetNodeIdTerm(this.NodeId) }, false, this.Id, false, versioningInfo);

            // add documents of all versions
            var documents = IndexDocumentInfo.GetDocuments(head.Versions.Select(v => v.VersionId));
            foreach (var document in documents)
                IndexManager.AddDocument(document, this.Id, this.IsUnprocessedActivity, versioningInfo);

            return true;
        }


        protected override string GetExtension()
        {
            return null;
        }
        protected override void SetExtension(string value)
        {
            // do nothing
        }

    }
}
