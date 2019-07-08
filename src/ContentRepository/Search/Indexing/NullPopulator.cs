using System;
using System.Collections.Generic;
using System.IO;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Search.Indexing
{
    internal class NullPopulator : IIndexPopulator
    {
        public static NullPopulator Instance = new NullPopulator();

        private static readonly object PopulatorData = new object();

        public void ClearAndPopulateAll(TextWriter consoleWriter = null) { }
        public void RebuildIndexDirectly(string path, IndexRebuildLevel level = IndexRebuildLevel.IndexOnly) { }
        public void AddTree(string path, int nodeId) { }
        public object BeginPopulateNode(Node node, NodeSaveSettings settings, string originalPath, string newPath) { return PopulatorData; }
        public void CommitPopulateNode(object data, IndexDocumentData indexDocument) { }
        public void FinalizeTextExtracting(object data, IndexDocumentData indexDocument) { }
        public void DeleteTree(string path, int nodeId) { }
#pragma warning disable 0067
        // suppressed because it is not used but the interface declares.
        public event EventHandler<NodeIndexedEventArgs> NodeIndexed;
        public event EventHandler<NodeIndexedEventArgs> IndexDocumentRefreshed;
        public event EventHandler<NodeIndexingErrorEventArgs> IndexingError;
#pragma warning restore 0067
        public void DeleteForest(IEnumerable<int> idSet) { }
        public void DeleteForest(IEnumerable<string> pathSet) { }

        public void RebuildIndex(Node node, bool recursive = false, IndexRebuildLevel rebuildLevel = IndexRebuildLevel.IndexOnly)
        {
            // do nothing in case of IndexOnly level, because this is a NULL populator
            if (rebuildLevel == IndexRebuildLevel.IndexOnly)
                return;

            using (var op = SnTrace.Index.StartOperation("NullPopulator.RefreshIndex. Version: {0}, VersionId: {1}, recursive: {2}, level: {3}", node.Version, node.VersionId, recursive, rebuildLevel))
            {
                using (new Storage.Security.SystemAccount())
                {
                    if (recursive)
                    {
                        using (TreeLock.Acquire(node.Path))
                        {
                            foreach (var n in NodeEnumerator.GetNodes(node.Path))
                                DataStore.SaveIndexDocumentAsync(node, false, false).Wait();
                        }
                    }
                    else
                    {
                        TreeLock.AssertFree(node.Path);
                        DataStore.SaveIndexDocumentAsync(node, false, false).Wait();
                    }
                }
                op.Successful = true;
            }
        }
    }
}
