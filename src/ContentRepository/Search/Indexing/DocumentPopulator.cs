using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.Search.Indexing
{
    public class DocumentPopulator : IIndexPopulator
    {
        private class DocumentPopulatorData
        {
            internal Node Node { get; set; }
            internal NodeHead NodeHead { get; set; }
            internal NodeSaveSettings Settings { get; set; }
            internal string OriginalPath { get; set; }
            internal string NewPath { get; set; }
            internal bool IsNewNode { get; set; }
        }
        private class DeleteVersionPopulatorData
        {
            internal Node OldVersion { get; set; }
            internal Node LastDraftAfterDelete { get; set; }
        }

        /*======================================================================================================= IIndexPopulator Members */

        // caller: IndexPopulator.Populator, Import.Importer, Tests
        public void ClearAndPopulateAll(TextWriter consoleWriter = null)
        {
            using (var op = SnTrace.Index.StartOperation("IndexPopulator ClearAndPopulateAll"))
            {
                // recreate
                consoleWriter?.Write("  Cleanup index ... ");
                IndexManager.ClearIndex();
                consoleWriter?.WriteLine("ok");

                IndexManager.AddDocuments(
                    SearchManager.LoadIndexDocumentsByPath("/Root", IndexManager.GetNotIndexedNodeTypes())
                        .Select(d =>
                        {
                            var indexDoc = IndexManager.CompleteIndexDocument(d);
                            OnNodeIndexed(d.Path);
                            return indexDoc;
                        }));

                consoleWriter?.Write("  Commiting ... ");
                IndexManager.Commit(); // explicit commit
                consoleWriter?.WriteLine("ok");

                consoleWriter?.Write("  Deleting indexing activities ... ");
                IndexManager.DeleteAllIndexingActivities();
                op.Successful = true;
            }
        }

        // caller: IndexPopulator.Populator
        public void RepopulateTree(string path)
        {
            using (var op = SnTrace.Index.StartOperation("IndexPopulator RepopulateTree"))
            {
                IndexManager.IndexingEngine.WriteIndex(new[] { new SnTerm(IndexFieldName.InTree, path) }, null
,                    addition: SearchManager.LoadIndexDocumentsByPath(path, IndexManager.GetNotIndexedNodeTypes())
                        .Select(IndexManager.CompleteIndexDocument));
                op.Successful = true;
            }
        }

        // caller: CommitPopulateNode (rename), Node.MoveTo, Node.MoveMoreInternal
        public void PopulateTree(string path, int nodeId)
        {
            // add new tree
            CreateTreeActivityAndExecute(IndexingActivityType.AddTree, path, nodeId, null);
        }

        // caller: Node.Save, Node.SaveCopied
        public object BeginPopulateNode(Node node, NodeSaveSettings settings, string originalPath, string newPath)
        {
            var populatorData = new DocumentPopulatorData
            {
                Node = node,
                Settings = settings,
                OriginalPath = originalPath,
                NewPath = newPath,
                NodeHead = settings.NodeHead,
                IsNewNode = node.Id == 0,
            };
            return populatorData;
        }
        public void CommitPopulateNode(object data, IndexDocumentData indexDocument = null)
        {
            var state = (DocumentPopulatorData)data;
            var versioningInfo = GetVersioningInfo(state);

            var settings = state.Settings;

            using (var op = SnTrace.Index.StartOperation("DocumentPopulator.CommitPopulateNode. Version: {0}, VersionId: {1}, Path: {2}", state.Node.Version, state.Node.VersionId, state.Node.Path))
            {
                if (!state.OriginalPath.Equals(state.NewPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    DeleteTree(state.OriginalPath, state.Node.Id);
                    PopulateTree(state.NewPath, state.Node.Id);
                }
                else if (state.IsNewNode)
                {
                    CreateBrandNewNode(state.Node, versioningInfo, indexDocument);
                }
                else if (state.Settings.IsNewVersion())
                {
                    AddNewVersion(state.Node, versioningInfo, indexDocument);
                }
                else
                {
                    UpdateVersion(state, versioningInfo, indexDocument);
                }

                OnNodeIndexed(state.Node.Path);

                op.Successful = true;
            }
        }
        public void FinalizeTextExtracting(object data, IndexDocumentData indexDocument)
        {
            var state = (DocumentPopulatorData)data;
            var versioningInfo = GetVersioningInfo(state);
            UpdateVersion(state, versioningInfo, indexDocument);
        }

        private VersioningInfo GetVersioningInfo(DocumentPopulatorData state)
        {
            var settings = state.Settings;

            var mustReindex = new List<int>();
            if (settings.LastMajorVersionIdBefore != settings.LastMajorVersionIdAfter)
            {
                mustReindex.Add(settings.LastMajorVersionIdBefore);
                mustReindex.Add(settings.LastMajorVersionIdAfter);
            }
            if (settings.LastMinorVersionIdBefore != settings.LastMinorVersionIdAfter)
            {
                mustReindex.Add(settings.LastMinorVersionIdBefore);
                mustReindex.Add(settings.LastMinorVersionIdAfter);
            }

            return new VersioningInfo
            {
                LastDraftVersionId = settings.LastMinorVersionIdAfter,
                LastPublicVersionId = settings.LastMajorVersionIdAfter,
                Delete = settings.DeletableVersionIds.ToArray(),
                Reindex = mustReindex.Except(new[] { 0, state.Node.VersionId }).Except(settings.DeletableVersionIds).ToArray()
            };
        }

        // caller: CommitPopulateNode (rename), Node.MoveTo, Node.ForceDelete
        public void DeleteTree(string path, int nodeId)
        {
            // add new tree
            CreateTreeActivityAndExecute(IndexingActivityType.RemoveTree, path, nodeId, null);
        }

        // caller: Node.DeleteMoreInternal
        public void DeleteForest(IEnumerable<Int32> idSet)
        {
            foreach (var head in NodeHead.Get(idSet))
                DeleteTree(head.Path, head.Id);
        }
        // caller: Node.MoveMoreInternal
        public void DeleteForest(IEnumerable<string> pathSet)
        {
            foreach (var head in NodeHead.Get(pathSet))
                DeleteTree(head.Path, head.Id);
        }

        public void RebuildIndex(Node node, bool recursive = false, IndexRebuildLevel rebuildLevel = IndexRebuildLevel.IndexOnly)
        {
            using (var op = SnTrace.Index.StartOperation("DocumentPopulator.RefreshIndex. Version: {0}, VersionId: {1}, recursive: {2}, level: {3}", node.Version, node.VersionId, recursive, rebuildLevel))
            {
                using (new SenseNet.ContentRepository.Storage.Security.SystemAccount())
                {
                    var databaseAndIndex = rebuildLevel == IndexRebuildLevel.DatabaseAndIndex;
                    if (recursive)
                        RebuildIndex_Recursive(node, databaseAndIndex);
                    else
                        RebuildIndex_NoRecursive(node, databaseAndIndex);
                }
                op.Successful = true;
            }
        }
        private void RebuildIndex_NoRecursive(Node node, bool databaseAndIndex)
        {
            TreeLock.AssertFree(node.Path);

            var head = NodeHead.Get(node.Id);
            bool hasBinary;
            if (databaseAndIndex)
                foreach (var version in head.Versions.Select(v => Node.LoadNodeByVersionId(v.VersionId)))
                    DataBackingStore.SaveIndexDocument(version, false, false, out hasBinary);

            var versioningInfo = new VersioningInfo
            {
                LastDraftVersionId = head.LastMinorVersionId,
                LastPublicVersionId = head.LastMajorVersionId,
                Delete = new int[0],
                Reindex = new int[0]
            };

            CreateActivityAndExecute(IndexingActivityType.Rebuild, node.Path, node.Id, 0, 0, versioningInfo, null);
        }

        private void RebuildIndex_Recursive(Node node, bool databaseAndIndex)
        {
            bool hasBinary;
            using (TreeLock.Acquire(node.Path))
            {
                DeleteTree(node.Path, node.Id);
                if (databaseAndIndex)
                    foreach (var n in NodeQuery.QueryNodesByPath(node.Path, true).Nodes)
                        DataBackingStore.SaveIndexDocument(n, false, false, out hasBinary);
                PopulateTree(node.Path, node.Id);
            }
        }


        public event EventHandler<NodeIndexedEvenArgs> NodeIndexed;
        protected void OnNodeIndexed(string path)
        {
            if (NodeIndexed == null)
                return;
            NodeIndexed(null, new NodeIndexedEvenArgs(path));
        }


        /*================================================================================================================================*/

        // caller: CommitPopulateNode
        private static void CreateBrandNewNode(Node node, VersioningInfo versioningInfo, IndexDocumentData indexDocumentData)
        {
            CreateActivityAndExecute(IndexingActivityType.AddDocument, node.Path, node.Id, node.VersionId, node.VersionTimestamp, versioningInfo, indexDocumentData);
        }
        // caller: CommitPopulateNode
        private static void AddNewVersion(Node newVersion, VersioningInfo versioningInfo, IndexDocumentData indexDocumentData)
        {
            CreateActivityAndExecute(IndexingActivityType.AddDocument, newVersion.Path, newVersion.Id, newVersion.VersionId, newVersion.VersionTimestamp, versioningInfo, indexDocumentData);
        }
        // caller: CommitPopulateNode
        private static void UpdateVersion(DocumentPopulatorData state, VersioningInfo versioningInfo, IndexDocumentData indexDocumentData)
        {
            CreateActivityAndExecute(IndexingActivityType.UpdateDocument, state.OriginalPath, state.Node.Id, state.Node.VersionId, state.Node.VersionTimestamp, versioningInfo, indexDocumentData);
        }

        // caller: ClearAndPopulateAll, RepopulateTree
        private static IEnumerable<Node> GetVersions(Node node)
        {
            var versionNumbers = Node.GetVersionNumbers(node.Id);
            var versions = from versionNumber in versionNumbers select Node.LoadNode(node.Id, versionNumber);
            return versions.ToArray();
        }
        /*================================================================================================================================*/

        private static IndexingActivityBase CreateActivity(IndexingActivityType type, string path, int nodeId, int versionId, long versionTimestamp, VersioningInfo versioningInfo, IndexDocumentData indexDocumentData)
        {
            var activity = (IndexingActivityBase)IndexingActivityFactory.Instance.CreateActivity(type);
            activity.Path = path.ToLowerInvariant();
            activity.NodeId = nodeId;
            activity.VersionId = versionId;
            activity.VersionTimestamp = versionTimestamp;

            if (indexDocumentData != null)
            {
                var docAct = activity as DocumentIndexingActivity;
                if (docAct != null)
                    docAct.IndexDocumentData = indexDocumentData;
            }

            var documentActivity = activity as DocumentIndexingActivity;
            if (documentActivity != null)
                documentActivity.Versioning = versioningInfo;

            return activity;
        }
        private static IndexingActivityBase CreateTreeActivity(IndexingActivityType type, string path, int nodeId, IndexDocumentData indexDocumentData)
        {
            var activity = (IndexingActivityBase)IndexingActivityFactory.Instance.CreateActivity(type);
            activity.Path = path.ToLowerInvariant();
            activity.NodeId = nodeId;

            if (indexDocumentData != null)
            {
                var docAct = activity as DocumentIndexingActivity;
                if (docAct != null)
                    docAct.IndexDocumentData = indexDocumentData;
            }

            return activity;
        }
        private static void CreateActivityAndExecute(IndexingActivityType type, string path, int nodeId, int versionId, long versionTimestamp, VersioningInfo versioningInfo, IndexDocumentData indexDocumentData)
        {
            ExecuteActivity(CreateActivity(type, path, nodeId, versionId, versionTimestamp, versioningInfo, indexDocumentData));
        }
        private static void CreateTreeActivityAndExecute(IndexingActivityType type, string path, int nodeId, IndexDocumentData indexDocumentData)
        {
            ExecuteActivity(CreateTreeActivity(type, path, nodeId, indexDocumentData));
        }
        private static void ExecuteActivity(IndexingActivityBase activity)
        {
            IndexManager.RegisterActivity(activity);
            IndexManager.ExecuteActivity(activity);
        }
    }

    public class NullPopulator : IIndexPopulator
    {
        public static NullPopulator Instance = new NullPopulator();

        private static readonly object PopulatorData = new object();

        public void ClearAndPopulateAll(TextWriter consoleWriter = null) { }
        public void RepopulateTree(string newPath) { }
        public void PopulateTree(string newPath, int nodeId) { }
        public object BeginPopulateNode(Node node, NodeSaveSettings settings, string originalPath, string newPath) { return PopulatorData; }
        public void CommitPopulateNode(object data, IndexDocumentData indexDocument) { }
        public void FinalizeTextExtracting(object data, IndexDocumentData indexDocument) { }
        public void DeleteTree(string path, int nodeId) { }
#pragma warning disable 0067
        // suppressed because it is not used but the interface declares.
        public event EventHandler<NodeIndexedEvenArgs> NodeIndexed;
#pragma warning restore 0067
        public void DeleteForest(IEnumerable<int> idSet) { }
        public void DeleteForest(IEnumerable<string> pathSet) { }

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
