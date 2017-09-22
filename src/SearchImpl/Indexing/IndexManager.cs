using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing.Activities;

namespace SenseNet.Search.Indexing
{
    public enum CommitHint { AddNew, AddNewVersion, Update, Rename, Move, Delete } ;

    public static class IndexManager // alias LuceneManager
    {
        #region /* ==================================================================== Managing index */

        internal static IIndexingEngine IndexingEngine => StorageContext.Search.SearchEngine.IndexingEngine;
        internal static ICommitManager CommitManager { get; } = new NoDelayCommitManager();

        public static bool Running => IndexingEngine.Running;

        public static int[] GetNotIndexedNodeTypes()
        {
            return StorageContext.Search.ContentRepository.GetNotIndexedNodeTypeIds();
        }

        public static void Start(TextWriter consoleOut)
        {
            IndexingEngine.Start(consoleOut);
            CommitManager.Start();
        }

        public static void ShutDown()
        {
            CommitManager.ShutDown();
            IndexingEngine.ShutDown();
            SnLog.WriteInformation("Indexing engine has stopped. Max task id and exceptions: " + IndexingActivityQueue.GetCurrentCompletionState());
        }

        public static void ClearIndex()
        {
            IndexingEngine.ClearIndex();
        }

        /* ========================================================================================== Activity */

        public static void RegisterActivity(IndexingActivityBase activity)
        {
            DataProvider.Current.RegisterIndexingActivity(activity);
        }

        public static void ExecuteActivity(IndexingActivityBase activity, bool waitForComplete, bool distribute)
        {
            if (distribute)
                activity.Distribute();

            // If there are too many activities in the queue, we have to drop at least the inner
            // data of the activity to prevent memory overflow. We still have to wait for the 
            // activity to finish, but the inner data can (and will) be loaded from the db when 
            // the time comes for this activity to be executed.
            if (IndexingActivityQueue.IsOverloaded())
            {
                SnTrace.Index.Write("IAQ OVERLOAD drop activity FromPopulator A:" + activity.Id);
                activity.IndexDocumentData = null;
            }

            // all activities must be executed through the activity queue's API
            IndexingActivityQueue.ExecuteActivity(activity);

            if (waitForComplete)
                activity.WaitForComplete();
        }

        public static int GetLastStoredIndexingActivityId()
        {
            return DataProvider.Current.GetLastActivityId();
        }

        internal static void DeleteAllIndexingActivities()
        {
            DataProvider.Current.DeleteAllIndexingActivities();
        }

        /*========================================================================================== Commit */

        internal static void ActivityFinished(int activityId, bool executingUnprocessedActivities)
        {
            CommitManager.ActivityFinished();
        }
        internal static void Commit()
        {
            IndexingEngine.WriteActivityStatusToIndex(CompletionState.GetCurrent());
        }

        #endregion

        #region /* ==================================================================== Document operations */

        /* ClearAndPopulateAll */
        internal static void AddDocuments(IEnumerable<IndexDocument> documents)
        {
            IndexingEngine.WriteIndex(null, documents);
        }

        /* AddDocumentActivity, RebuildActivity */
        internal static bool AddDocument(IndexDocument document, VersioningInfo versioning)
        {
            var delTerms = versioning.Delete.Select(i => new SnTerm(IndexFieldName.VersionId, i)).ToArray();
            var updates = GetUpdates(versioning);
            if(document != null)
                SetDocumentFlags(document, versioning);

            IndexingEngine.WriteIndex(delTerms, document, updates);

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

            IndexingEngine.WriteIndex(delTerms, null, updates);

            return true;
        }
        // RemoveDocumentActivity
        internal static bool DeleteDocument(int versionId, VersioningInfo versioning) //UNDONE:!!!!!!!! RemoveDocumentActivity: Unused method
        {
            var delTerms = versioning.Delete.Select(i => new SnTerm(IndexFieldName.VersionId, i)).ToList();
            delTerms.Add(new SnTerm(IndexFieldName.VersionId, versionId));
            var updates = GetUpdates(versioning).ToList();

            IndexingEngine.WriteIndex(delTerms, null, updates);

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
        internal static bool AddTree(string treeRoot, bool moveOrRename, int activityId, bool executingUnprocessedActivities)
        {
            var delTerm = executingUnprocessedActivities ? new [] { new SnTerm(IndexFieldName.InTree, treeRoot) } : null;
            var excludedNodeTypes = GetNotIndexedNodeTypes();
            var docs =
                StorageContext.Search.LoadIndexDocumentsByPath(treeRoot, excludedNodeTypes)
                    .Select(d => CreateIndexDocument(d));
            IndexingEngine.WriteIndex(delTerm, docs);

            return true;
        }

        #endregion

        #region /* ==================================================================== IndexDocument management */

        private static IPerFieldIndexingInfo __nameFieldIndexingInfo;
        private static IPerFieldIndexingInfo __pathFieldIndexingInfo;
        private static IPerFieldIndexingInfo __inTreeFieldIndexingInfo;
        private static IPerFieldIndexingInfo __inFolderFieldIndexingInfo;

        internal static IPerFieldIndexingInfo NameFieldIndexingInfo
        {
            get
            {
                if (__nameFieldIndexingInfo == null)
                    __nameFieldIndexingInfo = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(IndexFieldName.Name);
                return __nameFieldIndexingInfo;
            }
        }
        internal static IPerFieldIndexingInfo PathFieldIndexingInfo
        {
            get
            {
                if (__pathFieldIndexingInfo == null)
                    __pathFieldIndexingInfo = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(IndexFieldName.Path);
                return __pathFieldIndexingInfo;
            }
        }
        internal static IPerFieldIndexingInfo InTreeFieldIndexingInfo
        {
            get
            {
                if (__inTreeFieldIndexingInfo == null)
                    __inTreeFieldIndexingInfo = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(IndexFieldName.InTree);
                return __inTreeFieldIndexingInfo;
            }
        }
        internal static IPerFieldIndexingInfo InFolderFieldIndexingInfo
        {
            get
            {
                if (__inFolderFieldIndexingInfo == null)
                    __inFolderFieldIndexingInfo = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(IndexFieldName.InFolder);
                return __inFolderFieldIndexingInfo;
            }
        }

        internal static IndexDocument LoadIndexDocumentByVersionId(int versionId)
        {
            return CreateIndexDocument(StorageContext.Search.LoadIndexDocumentByVersionId(versionId));
        }
        internal static IEnumerable<IndexDocument> LoadIndexDocumentsByVersionId(int[] versionIds)
        {
            return versionIds.Length == 0
                ? new IndexDocument[0]
                : StorageContext.Search.LoadIndexDocumentByVersionId(versionIds)
                    .Select(CreateIndexDocument)
                    .ToArray();
        }
        private static IndexDocument CreateIndexDocument(IndexDocumentData data)
        {
            return CompleteIndexDocument(data);
        }
        internal static IndexDocument CompleteIndexDocument(IndexDocumentData docData)
        {
            var doc = docData.IndexDocument;

            if (doc == null)
                return null;
            if (doc is NotIndexedIndexDocument)
                return null;

            var path = docData.Path.ToLowerInvariant();

            doc.Add(new IndexField(IndexFieldName.Name, RepositoryPath.GetFileName(path), NameFieldIndexingInfo.IndexingMode, NameFieldIndexingInfo.IndexStoringMode, NameFieldIndexingInfo.TermVectorStoringMode));
            doc.Add(new IndexField(IndexFieldName.Path, path, PathFieldIndexingInfo.IndexingMode, PathFieldIndexingInfo.IndexStoringMode, PathFieldIndexingInfo.TermVectorStoringMode));

            doc.Add(new IndexField(IndexFieldName.Depth, Node.GetDepth(path), IndexingMode.AnalyzedNoNorms, IndexStoringMode.Yes, IndexTermVector.No));

            doc.Add(new IndexField(IndexFieldName.InTree, GetParentPaths(path), InTreeFieldIndexingInfo.IndexingMode, InTreeFieldIndexingInfo.IndexStoringMode, InTreeFieldIndexingInfo.TermVectorStoringMode));
            doc.Add(new IndexField(IndexFieldName.InFolder, path, InFolderFieldIndexingInfo.IndexingMode, InFolderFieldIndexingInfo.IndexStoringMode, InFolderFieldIndexingInfo.TermVectorStoringMode));

            doc.Add(new IndexField(IndexFieldName.ParentId, docData.ParentId, IndexingMode.AnalyzedNoNorms, IndexStoringMode.No, IndexTermVector.No));

            doc.Add(new IndexField(IndexFieldName.IsSystem, docData.IsSystem, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.No));

            // flags
            doc.Add(new IndexField(IndexFieldName.IsLastPublic, docData.IsLastPublic, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.No));
            doc.Add(new IndexField(IndexFieldName.IsLastDraft, docData.IsLastDraft, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.No));

            // timestamps
            doc.Add(new IndexField(IndexFieldName.NodeTimestamp, docData.NodeTimestamp, IndexingMode.AnalyzedNoNorms, IndexStoringMode.Yes, IndexTermVector.No));
            doc.Add(new IndexField(IndexFieldName.VersionTimestamp, docData.VersionTimestamp, IndexingMode.AnalyzedNoNorms, IndexStoringMode.Yes, IndexTermVector.No));

            // custom fields
            if (doc.HasCustomField)
            {
                var customFields = CustomIndexFieldManager.GetFields(doc, docData);
                if (customFields != null)
                    foreach (var field in customFields)
                        doc.Add(field);
            }

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
