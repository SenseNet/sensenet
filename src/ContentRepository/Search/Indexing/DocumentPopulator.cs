using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using STT=System.Threading.Tasks;

namespace SenseNet.ContentRepository.Search.Indexing
{
    internal class DocumentPopulator : IIndexPopulator
    {
        private DataStore DataStore => Providers.Instance.DataStore;

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
        public async STT.Task ClearAndPopulateAllAsync(CancellationToken cancellationToken, TextWriter consoleWriter = null)
        {
            using (var op = SnTrace.Index.StartOperation("IndexPopulator ClearAndPopulateAll"))
            {
                // recreate
                consoleWriter?.Write("  Cleanup index ... ");
                await IndexManager.ClearIndexAsync(cancellationToken).ConfigureAwait(false);
                consoleWriter?.WriteLine("ok");

                await IndexManager.AddDocumentsAsync(LoadIndexDocumentsByPath("/Root"), cancellationToken).ConfigureAwait(false);

                // delete progress characters
                consoleWriter?.Write("                                             \n");
                consoleWriter?.Write("  Commiting ... ");
                await IndexManager.CommitAsync(cancellationToken).ConfigureAwait(false); // explicit commit
                consoleWriter?.WriteLine("ok");

                consoleWriter?.Write("  Deleting indexing activities ... ");
                await IndexManager.DeleteAllIndexingActivitiesAsync(cancellationToken).ConfigureAwait(false);
                op.Successful = true;
            }
        }

        // caller: IndexPopulator.Populator
        public async STT.Task RebuildIndexDirectlyAsync(string path, CancellationToken cancellationToken, 
            IndexRebuildLevel level = IndexRebuildLevel.IndexOnly)
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
                            await DataStore.SaveIndexDocumentAsync(node, false, false, cancellationToken)
                                .ConfigureAwait(false);

                            OnIndexDocumentRefreshed(node.Path, node.Id, node.VersionId, node.Version.ToString());
                        }

                        //TODO: [async] make this parallel async (TPL DataFlow)
                        Parallel.ForEach(NodeQuery.QueryNodesByPath(path, true).Nodes,
                            n =>
                            {
                                foreach (var node in n.LoadVersions())
                                {
                                    SnTrace.Test.Write("@@ WriteDoc: " + node.Path);
                                    DataStore.SaveIndexDocumentAsync(node, false, false, cancellationToken)
                                        .GetAwaiter().GetResult();
                                    OnIndexDocumentRefreshed(node.Path, node.Id, node.VersionId, node.Version.ToString());
                                }
                            });
                    }
                    op2.Successful = true;
                }
            }

            using (var op = SnTrace.Index.StartOperation("IndexPopulator: Rebuild index."))
            {
                await IndexManager.IndexingEngine.WriteIndexAsync(
                    new[] {new SnTerm(IndexFieldName.InTree, path)},
                    null,
                    LoadIndexDocumentsByPath(path),
                    cancellationToken).ConfigureAwait(false);
                op.Successful = true;
            }
        }

        // caller: CommitPopulateNode (rename), Node.MoveTo, Node.MoveMoreInternal
        public STT.Task AddTreeAsync(string path, int nodeId, CancellationToken cancellationToken)
        {
            // add new tree
            return CreateTreeActivityAndExecuteAsync(IndexingActivityType.AddTree, path, nodeId, null, cancellationToken);
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
        public async STT.Task CommitPopulateNodeAsync(object data, IndexDocumentData indexDocument, CancellationToken cancellationToken)
        {
            var state = (DocumentPopulatorData)data;
            var versioningInfo = GetVersioningInfo(state);

            using (var op = SnTrace.Index.StartOperation("DocumentPopulator.CommitPopulateNode. Version: {0}, VersionId: {1}, Path: {2}", state.Node.Version, state.Node.VersionId, state.Node.Path))
            {
                if (!state.OriginalPath.Equals(state.NewPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    await DeleteTreeAsync(state.OriginalPath, state.Node.Id, cancellationToken).ConfigureAwait(false);
                    await AddTreeAsync(state.NewPath, state.Node.Id, cancellationToken).ConfigureAwait(false);
                }
                else if (state.IsNewNode)
                {
                    await CreateBrandNewNodeAsync(state.Node, versioningInfo, indexDocument, cancellationToken).ConfigureAwait(false);
                }
                else if (state.Settings.IsNewVersion())
                {
                    await AddNewVersionAsync(state.Node, versioningInfo, indexDocument, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await UpdateVersionAsync(state, versioningInfo, indexDocument, cancellationToken).ConfigureAwait(false);
                }

                var node = state.Node;
                OnNodeIndexed(node.Path, node.Id, node.VersionId, node.Version.ToString());

                op.Successful = true;
            }
        }
        public STT.Task FinalizeTextExtractingAsync(object data, IndexDocumentData indexDocument, CancellationToken cancellationToken)
        {
            var state = (DocumentPopulatorData)data;
            var versioningInfo = GetVersioningInfo(state);

            return UpdateVersionAsync(state, versioningInfo, indexDocument, cancellationToken);
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
        public STT.Task DeleteTreeAsync(string path, int nodeId, CancellationToken cancellationToken)
        {
            return CreateTreeActivityAndExecuteAsync(IndexingActivityType.RemoveTree, path, nodeId, null, cancellationToken);
        }

        // caller: Node.DeleteMoreInternal
        public async STT.Task DeleteForestAsync(IEnumerable<int> idSet, CancellationToken cancellationToken)
        {
            if (idSet == null)
                throw new ArgumentNullException(nameof(idSet));

            //TODO: [async] make this parallel async (TPL DataFlow)
            foreach (var head in NodeHead.Get(idSet))
                await DeleteTreeAsync(head.Path, head.Id, cancellationToken).ConfigureAwait(false);
        }
        // caller: Node.MoveMoreInternal
        public async STT.Task DeleteForestAsync(IEnumerable<string> pathSet, CancellationToken cancellationToken)
        {
            if (pathSet == null)
                throw new ArgumentNullException(nameof(pathSet));

            //TODO: [async] make this parallel async (TPL DataFlow)
            foreach (var head in NodeHead.Get(pathSet))
                await DeleteTreeAsync(head.Path, head.Id, cancellationToken).ConfigureAwait(false);
        }

        public async STT.Task RebuildIndexAsync(Node node, CancellationToken cancellationToken, bool recursive = false,
            IndexRebuildLevel rebuildLevel = IndexRebuildLevel.IndexOnly)
        {
            using (var op = SnTrace.Index.StartOperation("DocumentPopulator.RefreshIndex. Version: {0}, VersionId: {1}, recursive: {2}, level: {3}", node.Version, node.VersionId, recursive, rebuildLevel))
            {
                using (new SystemAccount())
                {
                    var databaseAndIndex = rebuildLevel == IndexRebuildLevel.DatabaseAndIndex;
                    if (recursive)
                        await RebuildIndex_RecursiveAsync(node, databaseAndIndex, cancellationToken).ConfigureAwait(false);
                    else
                        await RebuildIndex_NoRecursiveAsync(node, databaseAndIndex, cancellationToken).ConfigureAwait(false);
                }
                op.Successful = true;
            }
        }
        private async STT.Task RebuildIndex_NoRecursiveAsync(Node node, bool databaseAndIndex, CancellationToken cancellationToken)
        {
            await TreeLock.AssertFreeAsync(cancellationToken, node.Path).ConfigureAwait(false);

            var head = NodeHead.Get(node.Id);
            if (databaseAndIndex)
            {
                foreach (var version in head.Versions.Select(v => Node.LoadNodeByVersionId(v.VersionId)))
                    await DataStore.SaveIndexDocumentAsync(version, false, false, cancellationToken)
                        .ConfigureAwait(false);
            }

            var versioningInfo = new VersioningInfo
            {
                LastDraftVersionId = head.LastMinorVersionId,
                LastPublicVersionId = head.LastMajorVersionId,
                Delete = new int[0],
                Reindex = new int[0]
            };

            await CreateActivityAndExecuteAsync(IndexingActivityType.Rebuild, node.Path, node.Id, 0, 0, versioningInfo,
                null, cancellationToken).ConfigureAwait(false);
        }

        private async STT.Task RebuildIndex_RecursiveAsync(Node node, bool databaseAndIndex, CancellationToken cancellationToken)
        {
            using (await TreeLock.AcquireAsync(cancellationToken, node.Path).ConfigureAwait(false))
            {
                await DeleteTreeAsync(node.Path, node.Id, cancellationToken).ConfigureAwait(false);

                if (databaseAndIndex)
                {
                    await DataStore.SaveIndexDocumentAsync(node, false, false, cancellationToken).ConfigureAwait(false);

                    //TODO: [async] make this parallel async (TPL DataFlow TransformBlock)
                    Parallel.ForEach(NodeQuery.QueryNodesByPath(node.Path, true).Nodes,
                        n => { DataStore.SaveIndexDocumentAsync(node, false, false, CancellationToken.None)
                            .GetAwaiter().GetResult(); });
                }

                await AddTreeAsync(node.Path, node.Id, cancellationToken).ConfigureAwait(false);
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
        private static STT.Task CreateBrandNewNodeAsync(Node node, VersioningInfo versioningInfo, IndexDocumentData indexDocumentData, CancellationToken cancellationToken)
        {
            return CreateActivityAndExecuteAsync(IndexingActivityType.AddDocument, node.Path, node.Id, node.VersionId, node.VersionTimestamp, versioningInfo, indexDocumentData, cancellationToken);
        }
        // caller: CommitPopulateNode
        private static STT.Task AddNewVersionAsync(Node newVersion, VersioningInfo versioningInfo, IndexDocumentData indexDocumentData, CancellationToken cancellationToken)
        {
            return CreateActivityAndExecuteAsync(IndexingActivityType.AddDocument, newVersion.Path, newVersion.Id, newVersion.VersionId, newVersion.VersionTimestamp, versioningInfo, indexDocumentData, cancellationToken);
        }
        // caller: CommitPopulateNode
        private static STT.Task UpdateVersionAsync(DocumentPopulatorData state, VersioningInfo versioningInfo, IndexDocumentData indexDocumentData, CancellationToken cancellationToken)
        {
            return CreateActivityAndExecuteAsync(IndexingActivityType.UpdateDocument, state.OriginalPath, state.Node.Id, state.Node.VersionId, state.Node.VersionTimestamp, versioningInfo, indexDocumentData, cancellationToken);
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
        private static STT.Task CreateActivityAndExecuteAsync(IndexingActivityType type, string path, int nodeId, int versionId, long versionTimestamp, VersioningInfo versioningInfo, IndexDocumentData indexDocumentData, CancellationToken cancellationToken)
        {
            return ExecuteActivityAsync(CreateActivity(type, path, nodeId, versionId, versionTimestamp, versioningInfo, indexDocumentData), cancellationToken);
        }
        private static STT.Task CreateTreeActivityAndExecuteAsync(IndexingActivityType type, string path, int nodeId, IndexDocumentData indexDocumentData, CancellationToken cancellationToken)
        {
            return ExecuteActivityAsync(CreateTreeActivity(type, path, nodeId, indexDocumentData), cancellationToken);
        }
        private static async STT.Task ExecuteActivityAsync(IndexingActivityBase activity, CancellationToken cancellationToken)
        {
            await IndexManager.RegisterActivityAsync(activity, cancellationToken).ConfigureAwait(false);
            await IndexManager.ExecuteActivityAsync(activity, cancellationToken).ConfigureAwait(false);
        }
    }
}
