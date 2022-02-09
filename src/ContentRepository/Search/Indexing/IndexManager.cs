using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using STT=System.Threading.Tasks;

namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Provides methods for managing indexes.
    /// </summary>
    //UNDONE:<?xxx: Delete IndexManager and rename IndexManager_INSTANCE to IndexManager if all references rewritten in the ecosystem
    public class IndexManager_INSTANCE : IIndexManager // alias LuceneManager
    {
        private IDataStore DataStore => Providers.Instance.DataStore;

        internal DistributedIndexingActivityQueue DistributedIndexingActivityQueue { get; }
        private CentralizedIndexingActivityQueue CentralizedIndexingActivityQueue { get; }

        public IndexManager_INSTANCE()
        {
            DistributedIndexingActivityQueue = new DistributedIndexingActivityQueue(this);
            CentralizedIndexingActivityQueue = new CentralizedIndexingActivityQueue();
        }

        /* ==================================================================== Managing index */

        public IIndexingEngine IndexingEngine => Providers.Instance.SearchManager.SearchEngine.IndexingEngine;
        internal ICommitManager CommitManager { get; private set; }

        public bool Running => IndexingEngine?.Running ?? false;

        public int[] GetNotIndexedNodeTypes()
        {
            return new AllContentTypes()
                .Where(c => !c.IndexingEnabled)
                .Select(c => NodeType.GetByName(c.Name).Id)
                .ToArray();
        }

        public async STT.Task StartAsync(TextWriter consoleOut, CancellationToken cancellationToken)
        {
            await IndexingEngine.StartAsync(consoleOut, cancellationToken).ConfigureAwait(false);

            CommitManager = IndexingEngine.IndexIsCentralized
                ? (ICommitManager)new NoDelayCommitManager(this)
                : new NearRealTimeCommitManager(this);

            SnTrace.Index.Write("LM: {0} created.", CommitManager.GetType().Name);

            CommitManager.Start();

            if (IndexingEngine.IndexIsCentralized)
            {
                RestoreIndexIfNeeded();
                CentralizedIndexingActivityQueue.Startup(consoleOut);
            }
            else
            {
                DistributedIndexingActivityQueue.Startup(consoleOut);
            }
        }

        private void RestoreIndexIfNeeded()
        {
            //TODO: review this method, because currently we do not write status to the service index. That means
            // the last activity id will always be 0 in case of a centralized index and this method will never
            // restore the status of indexing activities. This is a problem in case we restore an older index:
            // newer activities will never be executed.
            SnTrace.Index.Write("Reading IndexingActivityStatus from index:");

            try
            {
                var status = Providers.Instance.SearchManager.SearchEngine.IndexingEngine.ReadActivityStatusFromIndexAsync(CancellationToken.None)
                            .GetAwaiter().GetResult();
                SnTrace.Index.Write($"  Status: {status}");

                if (status.LastActivityId > 0)
                {
                    SnTrace.Index.Write("  Restore indexing activities: ");
                    var result = Providers.Instance.IndexManager.RestoreIndexingActivityStatusAsync(status, CancellationToken.None)
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                    SnTrace.Index.Write($"  Restore result: {result}.");
                }
                else
                {
                    SnTrace.Index.Write("  Restore is not necessary.");
                }
            }
            catch (Exception ex)
            {
                SnTrace.Index.Write($"WARNING: error when reading indexing activity status " +
                                 $"from the centralized index or when restoring activity status: {ex.Message}");
            }
        }

        public void ShutDown()
        {
            CommitManager?.ShutDown();

            if (IndexingEngine == null)
                return;

            //TODO: [async] rewrite this using async APIs.
            if (IndexingEngine.IndexIsCentralized)
                CentralizedIndexingActivityQueue.ShutDown();
            else
                DistributedIndexingActivityQueue.ShutDown();

            //TODO: [async] rewrite this using async APIs.
            IndexingEngine.ShutDownAsync(CancellationToken.None).GetAwaiter().GetResult();
            SnLog.WriteInformation("Indexing engine has stopped. Max task id and exceptions: " + DistributedIndexingActivityQueue.GetCurrentCompletionState());
        }

        public STT.Task ClearIndexAsync(CancellationToken cancellationToken)
        {
            return IndexingEngine.ClearIndexAsync(cancellationToken);
        }

        /* ------------------------------------------------------------------------------------------ Activity */

        public STT.Task RegisterActivityAsync(IIndexingActivity activity, CancellationToken cancellationToken)
        {
            return DataStore.RegisterIndexingActivityAsync(activity, cancellationToken);
        }

        public STT.Task ExecuteActivityAsync(IIndexingActivity activity, CancellationToken cancellationToken)
        {
            return Providers.Instance.SearchManager.SearchEngine.IndexingEngine.IndexIsCentralized
                ? ExecuteCentralizedActivityAsync(activity, cancellationToken)
                : ExecuteDistributedActivityAsync(activity, cancellationToken);
        }
        private STT.Task ExecuteCentralizedActivityAsync(IIndexingActivity activity, CancellationToken cancellationToken)
        {
            var activityBase = (IndexingActivityBase)activity; //UNDONE:<?xxx: wrong parameter interface

            SnTrace.Index.Write("ExecuteCentralizedActivity: #{0}", activity.Id);
            CentralizedIndexingActivityQueue.ExecuteActivity(activityBase);

            return activityBase.WaitForCompleteAsync(cancellationToken);
        }
        private async STT.Task ExecuteDistributedActivityAsync(IIndexingActivity activity, CancellationToken cancellationToken)
        {
            var activityBase = (IndexingActivityBase)activity; //UNDONE:<?xxx: wrong parameter interface
            SnTrace.Index.Write("ExecuteDistributedActivity: #{0}", activity.Id);
            await activityBase.DistributeAsync(cancellationToken).ConfigureAwait(false);

            // If there are too many activities in the queue, we have to drop at least the inner
            // data of the activity to prevent memory overflow. We still have to wait for the 
            // activity to finish, but the inner data can (and will) be loaded from the db when 
            // the time comes for this activity to be executed.
            if (DistributedIndexingActivityQueue.IsOverloaded())
            {
                SnTrace.Index.Write("IAQ OVERLOAD drop activity FromPopulator A:" + activity.Id);
                activity.IndexDocumentData = null;
            }

            // all activities must be executed through the activity queue's API
            DistributedIndexingActivityQueue.ExecuteActivity(activityBase);

            await activityBase.WaitForCompleteAsync(cancellationToken).ConfigureAwait(false);
        }

        public int GetLastStoredIndexingActivityId()
        {
            return DataStore.GetLastIndexingActivityIdAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        internal STT.Task DeleteAllIndexingActivitiesAsync(CancellationToken cancellationToken)
        {
            return DataStore.DeleteAllIndexingActivitiesAsync(cancellationToken);
        }

        public IndexingActivityStatus GetCurrentIndexingActivityStatus()
        {
            return DistributedIndexingActivityQueue.GetCurrentCompletionState();
        }

        public STT.Task DeleteRestorePointsAsync(CancellationToken cancellationToken)
        {
            return DataStore.DeleteRestorePointsAsync(cancellationToken);
        }
        public STT.Task<IndexingActivityStatus> LoadCurrentIndexingActivityStatusAsync(CancellationToken cancellationToken)
        {
            return DataStore.LoadCurrentIndexingActivityStatusAsync(cancellationToken);
        }

        public async STT.Task<IndexingActivityStatusRestoreResult> RestoreIndexingActivityStatusAsync(
            IndexingActivityStatus status, CancellationToken cancellationToken)
        {
            // Running state of the activity is only used in the centralized indexing scenario. 
            // Additionally, the activity table can be too large in the distributed indexing scenario
            // so it would be blocked for a long time by RestoreIndexingActivityStatusAsync.
            if (!Providers.Instance.SearchManager.SearchEngine.IndexingEngine.IndexIsCentralized)
                throw new SnNotSupportedException();

            // No action is required if the status is the default
            if (status.LastActivityId <= 0)
                return IndexingActivityStatusRestoreResult.NotNecessary;

            // Request to restore the running state of the stored activities by the status.
            var result = await DataStore.RestoreIndexingActivityStatusAsync(status, cancellationToken)
                .ConfigureAwait(false);

            // Reset activity status in the index if an actual operation happened.
            if (result == IndexingActivityStatusRestoreResult.Restored)
                await Providers.Instance.SearchManager.SearchEngine.IndexingEngine.WriteActivityStatusToIndexAsync(
                    IndexingActivityStatus.Startup, cancellationToken).ConfigureAwait(false);

            return result;
        }

        /* ------------------------------------------------------------------------------------------ Commit */

        // called from activity
        internal void ActivityFinished(int activityId, bool executingUnprocessedActivities)
        {
            //SnTrace.Index.Write("LM: ActivityFinished: {0}", activityId);
            //CommitManager.ActivityFinished();
        }
        // called from activity queue
        internal void ActivityFinished(int activityId)
        {
            SnTrace.Index.Write("LM: ActivityFinished: {0}", activityId);
            CommitManager?.ActivityFinishedAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        internal STT.Task CommitAsync(CancellationToken cancellationToken)
        {
            var state = GetCurrentIndexingActivityStatus();
            SnTrace.Index.Write("LM: WriteActivityStatusToIndex: {0}", state);
            return IndexingEngine.WriteActivityStatusToIndexAsync(state, cancellationToken);
        }

        /* ==================================================================== Document operations */

        /* ClearAndPopulateAll */
        internal STT.Task AddDocumentsAsync(IEnumerable<IndexDocument> documents, CancellationToken cancellationToken)
        {
            return IndexingEngine.WriteIndexAsync(null, null, documents, cancellationToken);
        }

        /* AddDocumentActivity, RebuildActivity */
        internal async STT.Task<bool> AddDocumentAsync(IndexDocument document, VersioningInfo versioning, CancellationToken cancellationToken)
        {
            var delTerms = versioning.Delete.Select(i => new SnTerm(IndexFieldName.VersionId, i)).ToArray();
            var updates = GetUpdates(versioning);
            if (document != null)
                SetDocumentFlags(document, versioning);

            await IndexingEngine.WriteIndexAsync(delTerms, updates, new[] { document }, cancellationToken).ConfigureAwait(false);

            return true;
        }

        // UpdateDocumentActivity
        internal async STT.Task<bool> UpdateDocumentAsync(IndexDocument document, VersioningInfo versioning, CancellationToken cancellationToken)
        {
            var delTerms = versioning.Delete.Select(i => new SnTerm(IndexFieldName.VersionId, i)).ToArray();
            var updates = GetUpdates(versioning).ToList();
            if (document != null)
            {
                SetDocumentFlags(document, versioning);
                updates.Add(new DocumentUpdate
                {
                    UpdateTerm = new SnTerm(IndexFieldName.VersionId, document.VersionId),
                    Document = document
                });
            }

            await IndexingEngine.WriteIndexAsync(delTerms, updates, null, cancellationToken).ConfigureAwait(false);

            return true;
        }
        // RemoveTreeActivity, RebuildActivity
        internal async STT.Task<bool> DeleteDocumentsAsync(IEnumerable<SnTerm> deleteTerms, VersioningInfo versioning, CancellationToken cancellationToken)
        {
            await IndexingEngine.WriteIndexAsync(deleteTerms, null, null, cancellationToken).ConfigureAwait(false);

            // Not necessary to check if indexing interfered here. If it did, change is detected in overlapped AddDocument/UpdateDocument
            // operations and refresh (re-delete) is called there.
            // Delete documents will never detect changes in index, since it sets timestamp in index history to maxvalue.

            return true;
        }

        private IEnumerable<DocumentUpdate> GetUpdates(VersioningInfo versioning)
        {
            var result = new List<DocumentUpdate>(versioning.Reindex.Length);

            var updates = LoadIndexDocumentsByVersionId(versioning.Reindex);
            foreach (var doc in updates)
            {
                SetDocumentFlags(doc, versioning);
                result.Add(new DocumentUpdate { UpdateTerm = new SnTerm(IndexFieldName.VersionId, doc.VersionId), Document = doc });
            }

            return result;
        }
        private void SetDocumentFlags(IndexDocument doc, VersioningInfo versioning)
        {
            var versionId = doc.VersionId;
            var version = VersionNumber.Parse(doc.Version);

            var isMajor = version.IsMajor;
            var isPublic = version.Status == VersionStatus.Approved;
            var isLastPublic = versionId == versioning.LastPublicVersionId;
            var isLastDraft = versionId == versioning.LastDraftVersionId;

            // set flags
            SetDocumentFlag(doc, IndexFieldName.IsMajor, isMajor);
            SetDocumentFlag(doc, IndexFieldName.IsPublic, isPublic);
            SetDocumentFlag(doc, IndexFieldName.IsLastPublic, isLastPublic);
            SetDocumentFlag(doc, IndexFieldName.IsLastDraft, isLastDraft);
        }
        private void SetDocumentFlag(IndexDocument doc, string fieldName, bool value)
        {
            doc.Add(new IndexField(fieldName, value, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.No));
        }


        // AddTreeActivity
        internal async STT.Task<bool> AddTreeAsync(string treeRoot, int activityId,
            bool executingUnprocessedActivities, CancellationToken cancellationToken)
        {
            var delTerms = executingUnprocessedActivities ? new[] { new SnTerm(IndexFieldName.InTree, treeRoot) } : null;
            var excludedNodeTypes = GetNotIndexedNodeTypes();
            var docs = Providers.Instance.SearchManager.LoadIndexDocumentsByPath(treeRoot, excludedNodeTypes)
                .Select(CreateIndexDocument);
            await IndexingEngine.WriteIndexAsync(delTerms, null, docs, cancellationToken).ConfigureAwait(false);
            return true;
        }
        

        /* ==================================================================== IndexDocument management */

        // ReSharper disable once InconsistentNaming
        private IPerFieldIndexingInfo __nameFieldIndexingInfo;
        internal IPerFieldIndexingInfo NameFieldIndexingInfo =>
            __nameFieldIndexingInfo ??= Providers.Instance.SearchManager.GetPerFieldIndexingInfo(IndexFieldName.Name);

        // ReSharper disable once InconsistentNaming
        private IPerFieldIndexingInfo __pathFieldIndexingInfo;
        internal IPerFieldIndexingInfo PathFieldIndexingInfo =>
            __pathFieldIndexingInfo ??= Providers.Instance.SearchManager.GetPerFieldIndexingInfo(IndexFieldName.Path);

        // ReSharper disable once InconsistentNaming
        private IPerFieldIndexingInfo __inTreeFieldIndexingInfo;
        internal IPerFieldIndexingInfo InTreeFieldIndexingInfo =>
            __inTreeFieldIndexingInfo ??= Providers.Instance.SearchManager.GetPerFieldIndexingInfo(IndexFieldName.InTree);

        // ReSharper disable once InconsistentNaming
        private IPerFieldIndexingInfo __inFolderFieldIndexingInfo;
        internal IPerFieldIndexingInfo InFolderFieldIndexingInfo =>
            __inFolderFieldIndexingInfo ??= Providers.Instance.SearchManager.GetPerFieldIndexingInfo(IndexFieldName.InFolder);

        internal IndexDocument LoadIndexDocumentByVersionId(int versionId)
        {
            return CreateIndexDocument(Providers.Instance.SearchManager.LoadIndexDocumentByVersionId(versionId));
        }
        internal IEnumerable<IndexDocument> LoadIndexDocumentsByVersionId(int[] versionIds)
        {
            return versionIds.Length == 0
                ? new IndexDocument[0]
                : Providers.Instance.SearchManager.LoadIndexDocumentByVersionId(versionIds)
                    .Select(CreateIndexDocument)
                    .ToArray();
        }
        private IndexDocument CreateIndexDocument(IndexDocumentData data)
        {
            return data == null ? null : CompleteIndexDocument(data);
        }

        public IndexDocument CompleteIndexDocument(IndexDocumentData docData)
        {
            var doc = docData?.IndexDocument;

            if (doc == null)
                return null;
            if (doc is NotIndexedIndexDocument)
                return null;

            var path = docData.Path.ToLowerInvariant();
            var parentPath = RepositoryPath.GetParentPath(docData.Path)?.ToLowerInvariant() ?? "/";

            doc.Add(new IndexField(IndexFieldName.Name, RepositoryPath.GetFileName(path), NameFieldIndexingInfo.IndexingMode, NameFieldIndexingInfo.IndexStoringMode, NameFieldIndexingInfo.TermVectorStoringMode));
            doc.Add(new IndexField(IndexFieldName.Path, path, PathFieldIndexingInfo.IndexingMode, PathFieldIndexingInfo.IndexStoringMode, PathFieldIndexingInfo.TermVectorStoringMode));

            doc.Add(new IndexField(IndexFieldName.Depth, Node.GetDepth(path), IndexingMode.AnalyzedNoNorms, IndexStoringMode.Yes, IndexTermVector.No));

            doc.Add(new IndexField(IndexFieldName.InTree, GetParentPaths(path), InTreeFieldIndexingInfo.IndexingMode, InTreeFieldIndexingInfo.IndexStoringMode, InTreeFieldIndexingInfo.TermVectorStoringMode));
            doc.Add(new IndexField(IndexFieldName.InFolder, parentPath, InFolderFieldIndexingInfo.IndexingMode, InFolderFieldIndexingInfo.IndexStoringMode, InFolderFieldIndexingInfo.TermVectorStoringMode));

            doc.Add(new IndexField(IndexFieldName.ParentId, docData.ParentId, IndexingMode.AnalyzedNoNorms, IndexStoringMode.No, IndexTermVector.No));

            doc.Add(new IndexField(IndexFieldName.IsSystem, docData.IsSystem, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.No));

            // flags
            doc.Add(new IndexField(IndexFieldName.IsLastPublic, docData.IsLastPublic, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.No));
            doc.Add(new IndexField(IndexFieldName.IsLastDraft, docData.IsLastDraft, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.No));

            // timestamps
            doc.Add(new IndexField(IndexFieldName.NodeTimestamp, docData.NodeTimestamp, IndexingMode.AnalyzedNoNorms, IndexStoringMode.Yes, IndexTermVector.No));
            doc.Add(new IndexField(IndexFieldName.VersionTimestamp, docData.VersionTimestamp, IndexingMode.AnalyzedNoNorms, IndexStoringMode.Yes, IndexTermVector.No));

            return doc;
        }
        private string[] GetParentPaths(string lowerCasePath)
        {
            var separator = "/";
            string[] fragments = lowerCasePath.Split(separator.ToCharArray(), StringSplitOptions.None);
            string[] pathSteps = new string[fragments.Length];
            for (int i = 0; i < fragments.Length; i++)
                pathSteps[i] = string.Join(separator, fragments, 0, i + 1);
            return pathSteps;
        }

        public void AddTextExtract(int versionId, string textExtract)
        {
            // 1: load indexDocument.
            var docData = Providers.Instance.SearchManager.LoadIndexDocumentByVersionId(versionId);
            var indexDoc = docData.IndexDocument;

            // 2: original and new text extract concatenation.
            textExtract = (indexDoc.GetStringValue(IndexFieldName.AllText) ?? "") + textExtract;

            indexDoc.Add(new IndexField(IndexFieldName.AllText, textExtract, IndexingMode.Analyzed, IndexStoringMode.No,
                IndexTermVector.No));

            // 3: save indexDocument.
            docData.IndexDocumentChanged();
            Providers.Instance.DataStore.SaveIndexDocumentAsync(versionId, indexDoc, CancellationToken.None).GetAwaiter().GetResult();

            // 4: distributed cache invalidation because of version timestamp.
            Providers.Instance.DataStore.RemoveNodeDataFromCacheByVersionId(versionId);

            // 5: index update.
            var node = Node.LoadNodeByVersionId(versionId);
            if (node != null)
                Providers.Instance.SearchManager.GetIndexPopulator()
                    .RebuildIndexAsync(node, CancellationToken.None).GetAwaiter().GetResult();
        }
    }


    //UNDONE:<?xxx: Delete IndexManager and rename IndexManager_INSTANCE to IndexManager if all references rewritten in the ecosystem
    public static class IndexManager // alias LuceneManager
    {
        private static IndexManager_INSTANCE IndexManagerImplementation => (IndexManager_INSTANCE)Providers.Instance.IndexManager;

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static DistributedIndexingActivityQueue DistributedIndexingActivityQueue
            => IndexManagerImplementation.DistributedIndexingActivityQueue;

        /* ==================================================================== Managing index */

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static IIndexingEngine IndexingEngine => Providers.Instance.SearchManager.SearchEngine.IndexingEngine;

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static bool Running => IndexManagerImplementation.Running;

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static int[] GetNotIndexedNodeTypes()
            => IndexManagerImplementation.GetNotIndexedNodeTypes();

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static STT.Task StartAsync(TextWriter consoleOut, CancellationToken cancellationToken)
            => IndexManagerImplementation.StartAsync(consoleOut, cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static void ShutDown()
            => IndexManagerImplementation.ShutDown();

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static STT.Task ClearIndexAsync(CancellationToken cancellationToken)
            => IndexManagerImplementation.ClearIndexAsync(cancellationToken);

        /* ========================================================================================== Activity */

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static STT.Task RegisterActivityAsync(IndexingActivityBase activity, CancellationToken cancellationToken)
            => IndexManagerImplementation.RegisterActivityAsync(activity, cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static STT.Task ExecuteActivityAsync(IndexingActivityBase activity, CancellationToken cancellationToken)
            => IndexManagerImplementation.ExecuteActivityAsync(activity, cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static int GetLastStoredIndexingActivityId()
            => IndexManagerImplementation.GetLastStoredIndexingActivityId();

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static STT.Task DeleteAllIndexingActivitiesAsync(CancellationToken cancellationToken)
            => IndexManagerImplementation.DeleteAllIndexingActivitiesAsync(cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static IndexingActivityStatus GetCurrentIndexingActivityStatus()
            => IndexManagerImplementation.GetCurrentIndexingActivityStatus();

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static STT.Task DeleteRestorePointsAsync(CancellationToken cancellationToken)
            => IndexManagerImplementation.DeleteRestorePointsAsync(cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static STT.Task<IndexingActivityStatus> LoadCurrentIndexingActivityStatusAsync(CancellationToken cancellationToken)
            => IndexManagerImplementation.LoadCurrentIndexingActivityStatusAsync(cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static STT.Task<IndexingActivityStatusRestoreResult> RestoreIndexingActivityStatusAsync(IndexingActivityStatus status, CancellationToken cancellationToken)
            => IndexManagerImplementation.RestoreIndexingActivityStatusAsync(status, cancellationToken);

        /*========================================================================================== Commit */

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static void ActivityFinished(int activityId, bool executingUnprocessedActivities)
            => IndexManagerImplementation.ActivityFinished(activityId, executingUnprocessedActivities);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static void ActivityFinished(int activityId)
            => IndexManagerImplementation.ActivityFinished(activityId);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static STT.Task CommitAsync(CancellationToken cancellationToken)
            => IndexManagerImplementation.CommitAsync(cancellationToken);

        /* ==================================================================== Document operations */

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static STT.Task AddDocumentsAsync(IEnumerable<IndexDocument> documents, CancellationToken cancellationToken)
            => IndexManagerImplementation.AddDocumentsAsync(documents, cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static STT.Task<bool> AddDocumentAsync(IndexDocument document, VersioningInfo versioning, CancellationToken cancellationToken)
            => IndexManagerImplementation.AddDocumentAsync(document, versioning, cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static STT.Task<bool> UpdateDocumentAsync(IndexDocument document, VersioningInfo versioning, CancellationToken cancellationToken)
            => IndexManagerImplementation.UpdateDocumentAsync(document, versioning, cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static STT.Task<bool> DeleteDocumentsAsync(IEnumerable<SnTerm> deleteTerms, VersioningInfo versioning, CancellationToken cancellationToken)
            => IndexManagerImplementation.DeleteDocumentsAsync(deleteTerms, versioning, cancellationToken);

        internal static STT.Task<bool> AddTreeAsync(string treeRoot, int activityId, bool executingUnprocessedActivities, CancellationToken cancellationToken)
            => IndexManagerImplementation.AddTreeAsync(treeRoot, activityId, executingUnprocessedActivities, cancellationToken);

        /* ==================================================================== IndexDocument management */

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static IPerFieldIndexingInfo NameFieldIndexingInfo => IndexManagerImplementation.NameFieldIndexingInfo;
        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static IPerFieldIndexingInfo PathFieldIndexingInfo => IndexManagerImplementation.PathFieldIndexingInfo;
        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static IPerFieldIndexingInfo InTreeFieldIndexingInfo => IndexManagerImplementation.InTreeFieldIndexingInfo;
        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static IPerFieldIndexingInfo InFolderFieldIndexingInfo => IndexManagerImplementation.InFolderFieldIndexingInfo;
        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static IndexDocument LoadIndexDocumentByVersionId(int versionId) => IndexManagerImplementation.LoadIndexDocumentByVersionId(versionId);
        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static IEnumerable<IndexDocument> LoadIndexDocumentsByVersionId(int[] versionIds) => IndexManagerImplementation.LoadIndexDocumentsByVersionId(versionIds);
        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static IndexDocument CompleteIndexDocument(IndexDocumentData docData) => IndexManagerImplementation.CompleteIndexDocument(docData);
    }
}
