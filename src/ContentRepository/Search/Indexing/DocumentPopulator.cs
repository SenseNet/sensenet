using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.Search.Indexing
{
    internal class DocumentPopulator : IIndexPopulator
    {
        private class DocumentPopulatorData
        {
            internal Node Node { get; set; }
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            internal NodeHead NodeHead { get; set; }
            internal NodeSaveSettings Settings { get; set; }
            internal string OriginalPath { get; set; }
            internal string NewPath { get; set; }
            internal bool IsNewNode { get; set; }
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

                IndexManager.AddDocuments(LoadIndexDocumentsByPath("/Root"));

                // delete progress characters
                consoleWriter?.Write("                                             \n");
                consoleWriter?.Write("  Commiting ... ");
                IndexManager.Commit(); // explicit commit
                consoleWriter?.WriteLine("ok");

                consoleWriter?.Write("  Deleting indexing activities ... ");
                IndexManager.DeleteAllIndexingActivities();
                op.Successful = true;
            }
        }

        // caller: IndexPopulator.Populator
        public void RebuildIndexDirectly(string path, IndexRebuildLevel level = IndexRebuildLevel.IndexOnly)
        {
            if (level == IndexRebuildLevel.DatabaseAndIndex)
            {
                using (var op2 = SnTrace.Index.StartOperation("IndexPopulator: Rebuild index documents."))
                {
                    using (new SystemAccount())
                    {
                        foreach (var node in Node.LoadNode(path).LoadVersions())
                        {
                            SnTrace.Test.Write("@@ WriteDoc: " + node.Path);
                            DataStore.SaveIndexDocument(node, false, false, out _);
                            OnIndexDocumentRefreshed(node.Path, node.Id, node.VersionId, node.Version.ToString());
                        }

                        Parallel.ForEach(NodeQuery.QueryNodesByPath(path, true).Nodes,
                            n =>
                            {
                                foreach (var node in n.LoadVersions())
                                {
                                    SnTrace.Test.Write("@@ WriteDoc: " + node.Path);
                                    DataStore.SaveIndexDocument(node, false, false, out _);
                                    OnIndexDocumentRefreshed(node.Path, node.Id, node.VersionId, node.Version.ToString());
                                }
                            });
                    }
                    op2.Successful = true;
                }
            }

            using (var op = SnTrace.Index.StartOperation("IndexPopulator: Rebuild index."))
            {
                IndexManager.IndexingEngine.WriteIndex(
                    new[] {new SnTerm(IndexFieldName.InTree, path)},
                    null,
                    LoadIndexDocumentsByPath(path));
                op.Successful = true;
            }
        }

        // caller: CommitPopulateNode (rename), Node.MoveTo, Node.MoveMoreInternal
        public void AddTree(string path, int nodeId)
        {
            // add new tree
            CreateTreeActivityAndExecute(IndexingActivityType.AddTree, path, nodeId, null);
        }

        // caller: Node.Save, Node.SaveCopied
        public object BeginPopulateNode(Node node, NodeSaveSettings settings, string originalPath, string newPath)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (originalPath == null)
                throw new ArgumentNullException(nameof(originalPath));
            if (newPath == null)
                throw new ArgumentNullException(nameof(newPath));

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

            using (var op = SnTrace.Index.StartOperation("DocumentPopulator.CommitPopulateNode. Version: {0}, VersionId: {1}, Path: {2}", state.Node.Version, state.Node.VersionId, state.Node.Path))
            {
                if (!state.OriginalPath.Equals(state.NewPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    DeleteTree(state.OriginalPath, state.Node.Id);
                    AddTree(state.NewPath, state.Node.Id);
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

                var node = state.Node;
                OnNodeIndexed(node.Path, node.Id, node.VersionId, node.Version.ToString());

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
            CreateTreeActivityAndExecute(IndexingActivityType.RemoveTree, path, nodeId, null);
        }

        // caller: Node.DeleteMoreInternal
        public void DeleteForest(IEnumerable<Int32> idSet)
        {
            if (idSet == null)
                throw new ArgumentNullException(nameof(idSet));

            foreach (var head in NodeHead.Get(idSet))
                DeleteTree(head.Path, head.Id);
        }
        // caller: Node.MoveMoreInternal
        public void DeleteForest(IEnumerable<string> pathSet)
        {
            if (pathSet == null)
                throw new ArgumentNullException(nameof(pathSet));

            foreach (var head in NodeHead.Get(pathSet))
                DeleteTree(head.Path, head.Id);
        }

        public void RebuildIndex(Node node, bool recursive = false, IndexRebuildLevel rebuildLevel = IndexRebuildLevel.IndexOnly)
        {
            using (var op = SnTrace.Index.StartOperation("DocumentPopulator.RefreshIndex. Version: {0}, VersionId: {1}, recursive: {2}, level: {3}", node.Version, node.VersionId, recursive, rebuildLevel))
            {
                using (new SystemAccount())
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
            if (databaseAndIndex)
            {
                foreach (var version in head.Versions.Select(v => Node.LoadNodeByVersionId(v.VersionId)))
                    DataStore.SaveIndexDocument(version, false, false, out _);
            }

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
            using (TreeLock.Acquire(node.Path))
            {
                DeleteTree(node.Path, node.Id);
                if (databaseAndIndex)
                {
                    DataStore.SaveIndexDocument(node, false, false, out _);

                    Parallel.ForEach(NodeQuery.QueryNodesByPath(node.Path, true).Nodes,
                        n => { DataStore.SaveIndexDocument(n, false, false, out _); });
                }

                AddTree(node.Path, node.Id);
            }
        }


        public event EventHandler<NodeIndexedEventArgs> NodeIndexed;
        protected void OnNodeIndexed(string path, int nodeId = 0, int versionId = 0, string version = null)
        {
            NodeIndexed?.Invoke(null, new NodeIndexedEventArgs(path, nodeId, versionId, version));
        }
        public event EventHandler<NodeIndexedEventArgs> IndexDocumentRefreshed;
        protected void OnIndexDocumentRefreshed(string path, int nodeId = 0, int versionId = 0, string version = null)
        {
            IndexDocumentRefreshed?.Invoke(null, new NodeIndexedEventArgs(path, nodeId, versionId, version));
        }

        public event EventHandler<NodeIndexingErrorEventArgs> IndexingError;
        protected void OnIndexingError(IndexDocumentData doc, Exception exception)
        {
            IndexingError?.Invoke(null, new NodeIndexingErrorEventArgs(doc.NodeId, doc.VersionId, doc.Path, exception));
        }

        /*================================================================================================================================*/

        private IEnumerable<IndexDocument> LoadIndexDocumentsByPath(string path)
        {
            return SearchManager.LoadIndexDocumentsByPath(path, IndexManager.GetNotIndexedNodeTypes())
                .Select(d =>
                {
                    try
                    {
                        var indexDoc = IndexManager.CompleteIndexDocument(d);
                        OnNodeIndexed(d.Path, d.NodeId, d.VersionId, indexDoc.Version);
                        return indexDoc;
                    }
                    catch (Exception e)
                    {
                        OnIndexingError(d, e);
                        return null;
                    }
                })
                .Where(d => d != null);
        }

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
                if (activity is DocumentIndexingActivity docAct)
                    docAct.IndexDocumentData = indexDocumentData;
            }

            if (activity is DocumentIndexingActivity documentActivity)
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
                if (activity is DocumentIndexingActivity docAct)
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
}
