using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
    public static class IndexManager // alias LuceneManager
    {
        #region /* ==================================================================== Managing index */

        /// <summary>
        /// Gets the current <see cref="IIndexingEngine"/> implementation.
        /// </summary>
        public static IIndexingEngine IndexingEngine => SearchManager.SearchEngine.IndexingEngine;
        internal static ICommitManager CommitManager { get; private set; }

        /// <summary>
        /// Gets a value that is true if the current indexing engine is running.
        /// </summary>
        public static bool Running => IndexingEngine?.Running ?? false;

        /// <summary>
        /// Gets the ids of not indexed <see cref="NodeType"/>s.
        /// </summary>
        public static int[] GetNotIndexedNodeTypes()
        {
            return new AllContentTypes()
                .Where(c => !c.IndexingEnabled)
                .Select(c => NodeType.GetByName(c.Name).Id)
                .ToArray();
        }

        /// <summary>
        /// Initializes the indexing feature: starts the IndexingEngine, CommitManager and indexing activity organizer.
        /// If "consoleOut" is not null, writes progress and debug messages into it.
        /// </summary>
        /// <param name="consoleOut">A <see cref="TextWriter"/> instance or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static async STT.Task StartAsync(TextWriter consoleOut, CancellationToken cancellationToken)
        {
            await IndexingEngine.StartAsync(consoleOut, cancellationToken).ConfigureAwait(false);

            CommitManager = IndexingEngine.IndexIsCentralized
                ? (ICommitManager) new NoDelayCommitManager()
                : new NearRealTimeCommitManager();

            SnTrace.Index.Write("LM: {0} created.", CommitManager.GetType().Name);

            CommitManager.Start();

            if (IndexingEngine.IndexIsCentralized)
                CentralizedIndexingActivityQueue.Startup(consoleOut);
            else
                DistributedIndexingActivityQueue.Startup(consoleOut);
        }

        /// <summary>
        /// Shuts down the indexing feature: stops CommitManager, indexing activity organizer and IndexingEngine.
        /// </summary>
        public static void ShutDown()
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

        /// <summary>
        /// Deletes the existing index. Called before making a brand new index.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static STT.Task ClearIndexAsync(CancellationToken cancellationToken)
        {
            return IndexingEngine.ClearIndexAsync(cancellationToken);
        }

        /* ========================================================================================== Activity */

        /// <summary>
        /// Registers an indexing activity in the database.
        /// </summary>
        /// <param name="activity">The activity to register.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static STT.Task RegisterActivityAsync(IndexingActivityBase activity, CancellationToken cancellationToken)
        {
            return DataStore.RegisterIndexingActivityAsync(activity, cancellationToken);
        }

        /// <summary>
        /// Executes an indexing activity taking dependencies into account and waits for its completion asynchronously.
        /// Dependent activities are executed in the order of registration.
        /// Dependent activity execution starts after the previously blocker activity is completed.
        /// </summary>
        /// <param name="activity">The activity to execute.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static STT.Task ExecuteActivityAsync(IndexingActivityBase activity, CancellationToken cancellationToken)
        {
            return SearchManager.SearchEngine.IndexingEngine.IndexIsCentralized
                ? ExecuteCentralizedActivityAsync(activity, cancellationToken)
                : ExecuteDistributedActivityAsync(activity, cancellationToken);
        }
        private static STT.Task ExecuteCentralizedActivityAsync(IndexingActivityBase activity, CancellationToken cancellationToken)
        {
            SnTrace.Index.Write("ExecuteCentralizedActivity: #{0}", activity.Id);
            CentralizedIndexingActivityQueue.ExecuteActivity(activity);

            return activity.WaitForCompleteAsync(cancellationToken);
        }
        private static async STT.Task ExecuteDistributedActivityAsync(IndexingActivityBase activity, CancellationToken cancellationToken)
        {
            SnTrace.Index.Write("ExecuteDistributedActivity: #{0}", activity.Id);
            await activity.DistributeAsync(cancellationToken).ConfigureAwait(false);

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
            DistributedIndexingActivityQueue.ExecuteActivity(activity);

            await activity.WaitForCompleteAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns the Id of the last registered indexing activity.
        /// </summary>
        public static int GetLastStoredIndexingActivityId()
        {
            return DataStore.GetLastIndexingActivityIdAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes all activities from the database.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        internal static STT.Task DeleteAllIndexingActivitiesAsync(CancellationToken cancellationToken)
        {
            return DataStore.DeleteAllIndexingActivitiesAsync(cancellationToken);
        }

        /// <summary>
        /// Gets the current <see cref="IndexingActivityStatus"/> instance
        /// containing the last executed indexing activity id and ids of missing indexing activities.
        /// This method is used in the distributed indexing scenario.
        /// The indexing activity status comes from the index.
        /// </summary>
        /// <returns>The current <see cref="IndexingActivityStatus"/> instance.</returns>
        public static IndexingActivityStatus GetCurrentIndexingActivityStatus()
        {
            return DistributedIndexingActivityQueue.GetCurrentCompletionState();
        }
        /// <summary>
        /// Gets the current <see cref="IndexingActivityStatus"/> instance
        /// containing the last executed indexing activity id and ids of missing indexing activities.
        /// This method is used in the centralized indexing scenario.
        /// The indexing activity status comes from the database.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the current
        /// <see cref="IndexingActivityStatus"/> instance.</returns>
        public static STT.Task<IndexingActivityStatus> LoadCurrentIndexingActivityStatusAsync(CancellationToken cancellationToken)
        {
            return DataStore.GetCurrentIndexingActivityStatusAsync(cancellationToken);
        }

        /// <summary>
        /// Restores the indexing activity status.
        /// This method is used in the centralized indexing scenario.
        /// </summary>
        /// <remarks>
        /// To ensure the index and database integrity, this method marks the indexing activities to re-executable
        /// that was executed after the backup-status was queried. In the CentralizedIndexingActivityQueue's
        /// startup-sequence these activities will be executed before any other new indexing-activity.
        /// </remarks>
        /// <param name="status">An <see cref="IndexingActivityStatus"/> instance that contains the latest executed activity id and gaps.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static async STT.Task<IndexingActivityStatusRestoreResult> RestoreIndexingActivityStatusAsync(
            IndexingActivityStatus status,  CancellationToken cancellationToken)
        {
            // Running state of the activity is only used in the centralized indexing scenario. 
            // Additionally, the activity table can be too large in the distributed indexing scenario
            // so it would be blocked for a long time by RestoreIndexingActivityStatusAsync.
            if (!SearchManager.SearchEngine.IndexingEngine.IndexIsCentralized)
                throw new SnNotSupportedException();

            var result = await DataStore.RestoreIndexingActivityStatusAsync(status, cancellationToken);

            await SearchManager.SearchEngine.IndexingEngine.WriteActivityStatusToIndexAsync(
                IndexingActivityStatus.Startup, cancellationToken);

            return result;
        }

        /*========================================================================================== Commit */

        // called from activity
        internal static void ActivityFinished(int activityId, bool executingUnprocessedActivities)
        {
            //SnTrace.Index.Write("LM: ActivityFinished: {0}", activityId);
            //CommitManager.ActivityFinished();
        }
        // called from activity queue
        internal static void ActivityFinished(int activityId)
        {
            SnTrace.Index.Write("LM: ActivityFinished: {0}", activityId);
            CommitManager?.ActivityFinished();
        }

        internal static STT.Task CommitAsync(CancellationToken cancellationToken)
        {
            var state = GetCurrentIndexingActivityStatus();
            SnTrace.Index.Write("LM: WriteActivityStatusToIndex: {0}", state);
            return IndexingEngine.WriteActivityStatusToIndexAsync(state, cancellationToken);
        }

        #endregion

        #region /* ==================================================================== Document operations */

        /* ClearAndPopulateAll */
        internal static STT.Task AddDocumentsAsync(IEnumerable<IndexDocument> documents, CancellationToken cancellationToken)
        {
            return IndexingEngine.WriteIndexAsync(null, null, documents, cancellationToken);
        }

        /* AddDocumentActivity, RebuildActivity */
        internal static async STT.Task<bool> AddDocumentAsync(IndexDocument document, VersioningInfo versioning, CancellationToken cancellationToken)
        {
            var delTerms = versioning.Delete.Select(i => new SnTerm(IndexFieldName.VersionId, i)).ToArray();
            var updates = GetUpdates(versioning);
            if(document != null)
                SetDocumentFlags(document, versioning);

            await IndexingEngine.WriteIndexAsync(delTerms, updates, new[] {document}, cancellationToken).ConfigureAwait(false);

            return true;
        }

        // UpdateDocumentActivity
        internal static async STT.Task<bool> UpdateDocumentAsync(IndexDocument document, VersioningInfo versioning, CancellationToken cancellationToken)
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
        internal static async STT.Task<bool> DeleteDocumentsAsync(IEnumerable<SnTerm> deleteTerms, VersioningInfo versioning, CancellationToken cancellationToken)
        {
            await IndexingEngine.WriteIndexAsync(deleteTerms, null, null, cancellationToken).ConfigureAwait(false);

            // Not necessary to check if indexing interfered here. If it did, change is detected in overlapped AddDocument/UpdateDocument
            // operations and refresh (re-delete) is called there.
            // Delete documents will never detect changes in index, since it sets timestamp in index history to maxvalue.

            return true;
        }

        private static IEnumerable<DocumentUpdate> GetUpdates(VersioningInfo versioning)
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
        private static void SetDocumentFlags(IndexDocument doc, VersioningInfo versioning)
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
        private static void SetDocumentFlag(IndexDocument doc, string fieldName, bool value)
        {
            doc.Add(new IndexField(fieldName, value, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.No));
        }


        // AddTreeActivity
        internal static async  STT.Task<bool> AddTreeAsync(string treeRoot, int activityId, 
            bool executingUnprocessedActivities, CancellationToken cancellationToken)
        {
            var delTerms = executingUnprocessedActivities ? new [] { new SnTerm(IndexFieldName.InTree, treeRoot) } : null;
            var excludedNodeTypes = GetNotIndexedNodeTypes();
            var docs = SearchManager.LoadIndexDocumentsByPath(treeRoot, excludedNodeTypes).Select(CreateIndexDocument);
            await IndexingEngine.WriteIndexAsync(delTerms, null, docs, cancellationToken).ConfigureAwait(false);
            return true;
        }

        #endregion

        #region /* ==================================================================== IndexDocument management */

        // ReSharper disable once InconsistentNaming
        private static IPerFieldIndexingInfo __nameFieldIndexingInfo;
        internal static IPerFieldIndexingInfo NameFieldIndexingInfo => __nameFieldIndexingInfo ??
                                                                       (__nameFieldIndexingInfo = SearchManager.GetPerFieldIndexingInfo(IndexFieldName.Name));

        // ReSharper disable once InconsistentNaming
        private static IPerFieldIndexingInfo __pathFieldIndexingInfo;
        internal static IPerFieldIndexingInfo PathFieldIndexingInfo => __pathFieldIndexingInfo ??
                                                                       (__pathFieldIndexingInfo = SearchManager.GetPerFieldIndexingInfo(IndexFieldName.Path));

        // ReSharper disable once InconsistentNaming
        private static IPerFieldIndexingInfo __inTreeFieldIndexingInfo;
        internal static IPerFieldIndexingInfo InTreeFieldIndexingInfo => __inTreeFieldIndexingInfo ?? (__inTreeFieldIndexingInfo =
                                                                             SearchManager.GetPerFieldIndexingInfo(IndexFieldName.InTree));

        // ReSharper disable once InconsistentNaming
        private static IPerFieldIndexingInfo __inFolderFieldIndexingInfo;
        internal static IPerFieldIndexingInfo InFolderFieldIndexingInfo => __inFolderFieldIndexingInfo ?? (__inFolderFieldIndexingInfo =
                                                                               SearchManager.GetPerFieldIndexingInfo(IndexFieldName.InFolder));

        internal static IndexDocument LoadIndexDocumentByVersionId(int versionId)
        {
            return CreateIndexDocument(SearchManager.LoadIndexDocumentByVersionId(versionId));
        }
        internal static IEnumerable<IndexDocument> LoadIndexDocumentsByVersionId(int[] versionIds)
        {
            return versionIds.Length == 0
                ? new IndexDocument[0]
                : SearchManager.LoadIndexDocumentByVersionId(versionIds)
                    .Select(CreateIndexDocument)
                    .ToArray();
        }
        private static IndexDocument CreateIndexDocument(IndexDocumentData data)
        {
            return data == null ? null : CompleteIndexDocument(data);
        }
        internal static IndexDocument CompleteIndexDocument(IndexDocumentData docData)
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
        private static string[] GetParentPaths(string lowerCasePath)
        {
            var separator = "/";
            string[] fragments = lowerCasePath.Split(separator.ToCharArray(), StringSplitOptions.None);
            string[] pathSteps = new string[fragments.Length];
            for (int i = 0; i < fragments.Length; i++)
                pathSteps[i] = string.Join(separator, fragments, 0, i + 1);
            return pathSteps;
        }

        #endregion

    }
}
