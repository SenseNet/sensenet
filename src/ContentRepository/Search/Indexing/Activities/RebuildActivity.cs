using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    [Serializable]
    internal class RebuildActivity : IndexingActivityBase
    {
        private static readonly int[] EmptyIntArray = new int[0];
        protected override async Task<bool> ProtectedExecuteAsync(CancellationToken cancellationToken)
        {
            // getting common versioning info
            var head = NodeHead.Get(NodeId);
            var versioningInfo = new VersioningInfo
            {
                Delete = EmptyIntArray,
                Reindex = EmptyIntArray,
                LastDraftVersionId = head.LastMinorVersionId,
                LastPublicVersionId = head.LastMajorVersionId
            };

            // delete documents by NodeId
            await IndexManager.DeleteDocumentsAsync(new[] {new SnTerm(IndexFieldName.NodeId, NodeId)}, 
                versioningInfo, cancellationToken).ConfigureAwait(false);

            // add documents of all versions
            var docs = IndexManager.LoadIndexDocumentsByVersionId(head.Versions.Select(v => v.VersionId).ToArray());

            //TODO: can we make this parallel?
            foreach (var doc in docs)
                await IndexManager.AddDocumentAsync(doc, versioningInfo, cancellationToken).ConfigureAwait(false);

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
