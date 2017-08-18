using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using Lucene.Net.Index;
//using Lucene.Net.Store;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage;
using System.Threading;
//using Lucene.Net.Documents;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing.Activities;
//using Lucene.Net.Util;
//using SenseNet.Search.Lucene29;
//using Field = Lucene.Net.Documents.Field;

namespace SenseNet.Search.Indexing
{
    public enum CommitHint { AddNew, AddNewVersion, Update, Rename, Move, Delete } ;

    public static class IndexManager // alias LuceneManager
    {
        #region /* ==================================================================== Managing index */

        private static IIndexingEngineFactory _indexingEngineFactory; //UNDONE:!!!!! Inject _indexingEngineFactory
        internal static IIndexingEngine IndexingEngine => _indexingEngineFactory.CreateIndexingEngine();

        /*done*/public static int[] GetNotIndexedNodeTypes()
        {
            return StorageContext.Search.ContentRepository.GetNotIndexedNodeTypeIds();
        }

        /*done*/public static bool Running => IndexingEngine.Running;
        /*done*/internal static bool Paused => IndexingEngine.Paused;

        /*done*/internal static void PauseIndexing()
        {
            IndexingEngine.Pause();
        }
        /*done*/internal static void ContinueIndexing()
        {
            IndexingEngine.Continue();
            throw new NotSupportedException("Continue indexing is not supported in this version.");
        }

        /*done*/internal static void WaitIfIndexingPaused()
        {
            IndexingEngine.WaitIfIndexingPaused();
        }

        /*done*/public static void Start(IIndexingEngineFactory indexingEngineFactory, TextWriter consoleOut)
        {
            _indexingEngineFactory = indexingEngineFactory;
            IndexingEngine.Start(consoleOut);
        }


        /* ========================================================================================== Register Activity */

        /*done*/public static void RegisterActivity(LuceneIndexingActivity activity)
        {
            DataProvider.Current.RegisterIndexingActivity(activity);
        }

        public static void ExecuteActivity(LuceneIndexingActivity activity, bool waitForComplete, bool distribute)
        {
            if (distribute)
                activity.Distribute();

            // If there are too many activities in the queue, we have to drop at least the inner
            // data of the activity to prevent memory overflow. We still have to wait for the 
            // activity to finish, but the inner data can (and will) be loaded from the db when 
            // the time comes for this activity to be executed.
            if (IndexingActivityQueue.IsOverloaded()) //UNDONE:!!!! Decision: execution without queue
            {
                SnTrace.Index.Write("IAQ OVERLOAD drop activity FromPopulator A:" + activity.Id);
                activity.IndexDocumentData = null;
            }

            // all activities must be executed through the activity queue's API
            IndexingActivityQueue.ExecuteActivity(activity); //UNDONE:!!!! Decision: execution without queue

            if (waitForComplete)
                activity.WaitForComplete();
        }

        // ========================================================================================== Start, Restart, Shutdown, Warmup

        /*done*/internal static void Restart()
        {
            IndexingEngine.Restart();
        }
        /*done*/public static void ShutDown()
        {
            IndexingEngine.ShutDown();
            SnLog.WriteInformation("Indexing engine has stopped. Max task id and exceptions: " + IndexingActivityQueue.GetCurrentCompletionState());
        }

        /*done*/public static int GetLastStoredIndexingActivityId()
        {
            return DataProvider.Current.GetLastActivityId();
        }

        /*done*/internal static void DeleteAllIndexingActivities()
        {
            DataProvider.Current.DeleteAllIndexingActivities();
        }

        /*========================================================================================== Commit */

        /*done*/internal static void ActivityFinished(int activityId, bool executingUnprocessedActivities)
        {
            IndexingEngine.ActivityFinished();
        }
        /*done*/internal static void Commit(int lastActivityId = 0)
        {
            IndexingEngine.Commit(lastActivityId);
        }

        #endregion

        #region /* ==================================================================== Document operations */

        /* ClearAndPopulateAll */

        internal static void AddDocument(IndexDocument document)
        {
            IndexingEngine.Actualize(null, document, null);
        }

        /* AddDocumentActivity, RebuildActivity */
        /*done*/
        internal static bool AddDocument(IndexDocument document, VersioningInfo versioning)
        {
            var delTerms = versioning.Delete.Select(i => new SnTerm(IndexFieldName.VersionId, i)).ToArray();
            var updates = GetUpdates(versioning);
            if(document != null)
                SetDocumentFlags(document, versioning);

            IndexingEngine.Actualize(delTerms, document, updates);

            return true;
        }

        // UpdateDocumentActivity
        /*done*/internal static bool UpdateDocument(IndexDocument document, VersioningInfo versioning)
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

            IndexingEngine.Actualize(delTerms, null, updates);

            return true;
        }
        // RemoveDocumentActivity
        internal static bool DeleteDocument(int versionId, VersioningInfo versioning)
        {
            var delTerms = versioning.Delete.Select(i => new SnTerm(IndexFieldName.VersionId, i)).ToList();
            delTerms.Add(new SnTerm(IndexFieldName.VersionId, versionId));
            var updates = GetUpdates(versioning).ToList();

            IndexingEngine.Actualize(delTerms, null, updates);

            return true;
        }
        // RemoveTreeActivity, RebuildActivity
        internal static bool DeleteDocuments(IEnumerable<SnTerm> deleteTerms, VersioningInfo versioning)
        {
            IndexingEngine.Actualize(deleteTerms, null, null);

            // don't need to check if indexing interfered here. If it did, change is detected in overlapped adddocument/updatedocument, and refresh (re-delete) is called there.
            // deletedocuments will never detect change in index, since it sets timestamp in indexhistory to maxvalue.

            return true;
        }

        /*done*/private static IEnumerable<DocumentUpdate> GetUpdates(VersioningInfo versioning)
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
        /*done*/private static void SetDocumentFlags(IndexDocument doc, VersioningInfo versioning)
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
        /*done*/private static void SetDocumentFlag(IndexDocument doc, string fieldName, bool value)
        {
            doc.Add(new IndexField(fieldName, value, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.No));
        }


        // AddTreeActivity
        /*done*/internal static bool AddTree(string treeRoot, bool moveOrRename, int activityId, bool executingUnprocessedActivities)
        {
            var delTerm = executingUnprocessedActivities ? new [] { new SnTerm(IndexFieldName.InTree, treeRoot) } : null;
            var excludedNodeTypes = GetNotIndexedNodeTypes();
            var docs =
                StorageContext.Search.LoadIndexDocumentsByPath(treeRoot, excludedNodeTypes)
                    .Select(d => CreateIndexDocument(d));
            IndexingEngine.Actualize(delTerm, docs);

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
        private static IndexDocument CreateIndexDocument(IndexDocumentData data) //UNDONE: refactor IndexDocumentData --> complete IndexDocument
        {
            var buffer = data.IndexDocumentInfoBytes;

            var docStream = new MemoryStream(data.IndexDocumentInfoBytes);
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            var info = (IndexDocument)formatter.Deserialize(docStream);

            return CreateIndexDocument(info, data); //UNDONE: refactor IndexDocumentData --> complete IndexDocument
        }
        internal static IndexDocument CreateIndexDocument(IndexDocument doc, IndexDocumentData docData)
        {
            if (doc == null)
                return null;
            if (doc is NotIndexedIndexDocument)
                return null;

            //UNDONE:!! Ensure that all fields (except postponed fields) are available in this point.
            //UNDONE:!! Ensure that the "Password" and "PasswordHash" are not in the document.
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
            //UNDONE:!!!!! Reconcept, rewrite and implement ICustomIndexFieldProvider, CustomIndexFieldManager, IHasCustomIndexField
            //if (document.HasCustomField)
            //{
            //    var customFields = CustomIndexFieldManager.GetFields(document, docData);
            //    if (customFields != null)
            //        foreach (var field in customFields)
            //            doc.Add(field);
            //}

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
