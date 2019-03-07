using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

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
        /// Gets the ids of the not indexed <see cref="NodeType"/>s.
        /// </summary>
        public static int[] GetNotIndexedNodeTypes()
        {
            return new AllContentTypes()
                .Where(c => !c.IndexingEnabled)
                .Select(c => NodeType.GetByName(c.Name).Id)
                .ToArray();
        }

        /// <summary>
        /// Initializes the indexing feature: starts the IndexingEngine, CommitManager and indexing activity organizator.
        /// If "consoleOut" is not null, writes progress and debug messages into it.
        /// </summary>
        /// <param name="consoleOut">A <see cref="TextWriter"/> instance or null.</param>
        public static void Start(TextWriter consoleOut)
        {
            IndexingEngine.Start(consoleOut);

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
        /// Shuts down the indexing feature: stops CommitManager, indexing activity organizator and IndexingEngine.
        /// </summary>
        public static void ShutDown()
        {
            CommitManager?.ShutDown();

            if (IndexingEngine == null)
                return;

            if (IndexingEngine.IndexIsCentralized)
                CentralizedIndexingActivityQueue.ShutDown();
            else
                DistributedIndexingActivityQueue.ShutDown();

            IndexingEngine.ShutDown();
            SnLog.WriteInformation("Indexing engine has stopped. Max task id and exceptions: " + DistributedIndexingActivityQueue.GetCurrentCompletionState());
        }

        /// <summary>
        /// Deletes the existing index. Called before making a brand new index.
        /// </summary>
        public static void ClearIndex()
        {
            IndexingEngine.ClearIndex();
        }

        /* ========================================================================================== Activity */

        /// <summary>
        /// Registers an indexing aztivity in the database.
        /// </summary>
        public static void RegisterActivity(IndexingActivityBase activity)
        {
            DataProvider.Current.RegisterIndexingActivity(activity);
        }

        /// <summary>
        /// Executes an indexing activity taking into account the dependencies.
        /// The execution is immediately (ie parallelized) when possible but the
        /// dependent activities are executed in the order of registration.
        /// Dependent activity execution starts after the blocker activity is completed.
        /// </summary>
        public static void ExecuteActivity(IndexingActivityBase activity)
        {
            if (SearchManager.SearchEngine.IndexingEngine.IndexIsCentralized)
                ExecuteCentralizedActivity(activity);
            else
                ExecuteDistributedActivity(activity);
        }
        private static void ExecuteCentralizedActivity(IndexingActivityBase activity)
        {
            SnTrace.Index.Write("ExecuteCentralizedActivity: #{0}", activity.Id);
            CentralizedIndexingActivityQueue.ExecuteActivity(activity);

            activity.WaitForComplete();
        }
        private static void ExecuteDistributedActivity(IndexingActivityBase activity)
        {
            SnTrace.Index.Write("ExecuteDistributedActivity: #{0}", activity.Id);
            activity.Distribute();

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

            activity.WaitForComplete();
        }

        /// <summary>
        /// Returns with the Id of the last registered indexing activity.
        /// </summary>
        public static int GetLastStoredIndexingActivityId()
        {
            return DataProvider.Current.GetLastIndexingActivityId();
        }

        internal static void DeleteAllIndexingActivities()
        {
            DataProvider.Current.DeleteAllIndexingActivities();
        }

        /// <summary>
        /// Returns with the current <see cref="IndexingActivityStatus"/> instance
        /// containing the last executed indexing activity id and ids if missing indexing activities.
        /// </summary>
        /// <returns></returns>
        public static IndexingActivityStatus GetCurrentIndexingActivityStatus()
        {
            return DistributedIndexingActivityQueue.GetCurrentCompletionState();
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
            CommitManager.ActivityFinished();
        }

        internal static void Commit()
        {
            var state = GetCurrentIndexingActivityStatus();
            SnTrace.Index.Write("LM: WriteActivityStatusToIndex: {0}", state);
            IndexingEngine.WriteActivityStatusToIndex(state);
        }

        #endregion

        #region /* ==================================================================== Document operations */

        /* ClearAndPopulateAll */
        internal static void AddDocuments(IEnumerable<IndexDocument> documents)
        {
            IndexingEngine.WriteIndex(null, null, documents);
        }

        /* AddDocumentActivity, RebuildActivity */
        internal static bool AddDocument(IndexDocument document, VersioningInfo versioning)
        {
            var delTerms = versioning.Delete.Select(i => new SnTerm(IndexFieldName.VersionId, i)).ToArray();
            var updates = GetUpdates(versioning);
            if (document != null)
                SetDocumentFlags(document, versioning);

            IndexingEngine.WriteIndex(delTerms, updates, new[] {document});

            return true;
        }

        // UpdateDocumentActivity
        internal static bool UpdateDocument(IndexDocument document, VersioningInfo versioning)
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

            IndexingEngine.WriteIndex(delTerms, updates, null);

            return true;
        }
        // RemoveTreeActivity, RebuildActivity
        internal static bool DeleteDocuments(IEnumerable<SnTerm> deleteTerms, VersioningInfo versioning)
        {
            IndexingEngine.WriteIndex(deleteTerms, null, null);

            // don't need to check if indexing interfered here. If it did, change is detected in overlapped adddocument/updatedocument, and refresh (re-delete) is called there.
            // deletedocuments will never detect change in index, since it sets timestamp in indexhistory to maxvalue.

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
        internal static bool AddTree(string treeRoot, int activityId, bool executingUnprocessedActivities)
        {
            var delTerms = executingUnprocessedActivities ? new [] { new SnTerm(IndexFieldName.InTree, treeRoot) } : null;
            var excludedNodeTypes = GetNotIndexedNodeTypes();
            var docs = SearchManager.LoadIndexDocumentsByPath(treeRoot, excludedNodeTypes).Select(CreateIndexDocument);
            IndexingEngine.WriteIndex(delTerms, null, docs);
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
