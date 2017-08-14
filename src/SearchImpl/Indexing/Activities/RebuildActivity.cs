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
            IndexManager.DeleteDocuments(new[] { new SnTerm(IndexFieldName.NodeId, this.NodeId)}, versioningInfo);

            // add documents of all versions
            var docs = IndexManager.LoadIndexDocumentsByVersionId(head.Versions.Select(v => v.VersionId).ToArray());
            foreach (var doc in docs)
                IndexManager.AddDocument(doc, versioningInfo);

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
