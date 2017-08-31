using System;
using System.Collections.Generic;
using System.IO;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Search
{
    public interface IIndexPopulator
    {
        void ClearAndPopulateAll(TextWriter consoleWriter = null);
        void RepopulateTree(string newPath);
        void PopulateTree(string newPath, int nodeId);
        object BeginPopulateNode(Node node, NodeSaveSettings settings, string originalPath, string newPath);
        void CommitPopulateNode(object data, IndexDocumentData indexDocument);
        void FinalizeTextExtracting(object data, IndexDocumentData indexDocument);
        void DeleteTree(string path, int nodeId, bool moveOrRename);
        void DeleteForest(IEnumerable<int> idSet, bool moveOrRename);
        void DeleteForest(IEnumerable<string> pathSet, bool moveOrRename);

        void RebuildIndex(Node node, bool recursive = false, IndexRebuildLevel rebuildLevel = IndexRebuildLevel.IndexOnly);

        event EventHandler<NodeIndexedEvenArgs> NodeIndexed;
    }

    public class NodeIndexedEvenArgs : EventArgs
    {
        public string Path { get; private set; }
        public NodeIndexedEvenArgs(string path) { Path = path; }
    }
    internal class NullPopulator : IIndexPopulator
    {
        public static NullPopulator Instance = new NullPopulator();

        private static readonly object PopulatorData = new object();

        public void ClearAndPopulateAll(TextWriter consoleWriter = null) { }
        public void RepopulateTree(string newPath) { }
        public void PopulateTree(string newPath, int nodeId) { }
        public object BeginPopulateNode(Node node, NodeSaveSettings settings, string originalPath, string newPath) { return PopulatorData; }
        public void CommitPopulateNode(object data, IndexDocumentData indexDocument) { }
        public void FinalizeTextExtracting(object data, IndexDocumentData indexDocument) { }
        public void DeleteTree(string path, int nodeId, bool moveOrRename) { }
#pragma warning disable 0067
        // suppressed because it is not used but the interface declares.
        public event EventHandler<NodeIndexedEvenArgs> NodeIndexed;
#pragma warning restore 0067
        public void DeleteForest(IEnumerable<int> idSet, bool moveOrRename) { }
        public void DeleteForest(IEnumerable<string> pathSet, bool moveOrRename) { }

        public void RebuildIndex(Node node, bool recursive = false, IndexRebuildLevel rebuildLevel = IndexRebuildLevel.IndexOnly)
        {
            if (rebuildLevel == IndexRebuildLevel.IndexOnly)
                return;

            using (var op = SnTrace.Index.StartOperation("NullPopulator.RefreshIndex. Version: {0}, VersionId: {1}, recursive: {2}, level: {3}", node.Version, node.VersionId, recursive, rebuildLevel))
            {
                bool hasBinary;
                using (new SenseNet.ContentRepository.Storage.Security.SystemAccount())
                {
                    if (recursive)
                    {
                        using (SenseNet.ContentRepository.Storage.TreeLock.Acquire(node.Path))
                        {
                            foreach (var n in NodeEnumerator.GetNodes(node.Path))
                                DataBackingStore.SaveIndexDocument(n, false, false, out hasBinary);
                        }
                    }
                    else
                    {
                        SenseNet.ContentRepository.Storage.TreeLock.AssertFree(node.Path);
                        DataBackingStore.SaveIndexDocument(node, false, false, out hasBinary);
                    }
                }
                op.Successful = true;
            }
        }

    }
}
