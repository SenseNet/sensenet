using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Store;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage;
using System.Threading;
using Lucene.Net.Documents;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing.Activities;
using Lucene.Net.Util;
//using SenseNet.Search.Lucene29;
using Field = Lucene.Net.Documents.Field;

namespace SenseNet.Search.Indexing
{
    public enum CommitHint { AddNew, AddNewVersion, Update, Rename, Move, Delete } ;

    public static class IndexManager // alias LuceneManager
    {
        private class DocumentUpdate
        {
            public Term UpdateTerm;
            public Document Document;
        }

        internal static IIndexingEngine _indexingEngine = new Lucene29IndexingEngine(); //UNDONE:!!! This should be a little bit better :)


        public static int[] GetNotIndexedNodeTypes()
        {
            return StorageContext.Search.ContentRepository.GetNotIndexedNodeTypeIds();
        }

        /**/public static bool Running => _indexingEngine.Running;
        /**/internal static bool Paused => _indexingEngine.Paused;

        /**/internal static void PauseIndexing()
        {
            _indexingEngine.Pause();
        }
        /**/internal static void ContinueIndexing()
        {
            _indexingEngine.Continue();
            throw new NotSupportedException("Continue indexing is not supported in this version.");
        }

        /**/internal static void WaitIfIndexingPaused()
        {
            _indexingEngine.WaitIfIndexingPaused();
        }

        /**/public static void Start(TextWriter consoleOut)
        {
            _indexingEngine.Start(consoleOut);
        }


        /* ========================================================================================== Register Activity */

        /**/public static void RegisterActivity(LuceneIndexingActivity activity)
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
            if (IndexingActivityQueue.IsOverloaded()) //UNDONE:!!!!!!!!! Decision: execution without queue
            {
                SnTrace.Index.Write("IAQ OVERLOAD drop activity FromPopulator A:" + activity.Id);
                activity.IndexDocumentData = null;
            }

            // all activities must be executed through the activity queue's API
            IndexingActivityQueue.ExecuteActivity(activity); //UNDONE:!!!!!!!!! Decision: execution without queue

            if (waitForComplete)
                activity.WaitForComplete();
        }

        // ========================================================================================== Start, Restart, Shutdown, Warmup

        /**/internal static void Restart()
        {
            _indexingEngine.Restart();
        }
        /**/public static void ShutDown()
        {
            _indexingEngine.ShutDown();
            SnLog.WriteInformation("Indexing engine has stopped. Max task id and exceptions: " + IndexingActivityQueue.GetCurrentCompletionState());
        }

        public static void Backup()
        {
            BackupTools.SynchronousBackupIndex();
        }
        public static void BackupAndShutDown()
        {
            ShutDown();
            BackupTools.BackupIndexImmediatelly();
        }

        /**/public static int GetLastStoredIndexingActivityId()
        {
            return DataProvider.Current.GetLastActivityId();
        }

        /**/internal static void DeleteAllIndexingActivities()
        {
            DataProvider.Current.DeleteAllIndexingActivities();
        }

        /*========================================================================================== Commit */

        /**/internal static void ActivityFinished(int activityId, bool executingUnprocessedActivities)
        {
            if (!IsActivityExecutable(executingUnprocessedActivities))
                return;

            _indexingEngine.ActivityFinished();
        }
        /**/internal static void Commit()
        {
            _indexingEngine.Commit();
        }


        #region /* ==================================================================== Document operations */

        // AddDocumentActivity
        internal static bool AddCompleteDocument(Document document, int activityId, bool executingUnprocessedActivities, VersioningInfo versioning)
        {
            if (!IsActivityExecutable(executingUnprocessedActivities))
            {
                SnTrace.Index.Write("LM: AddCompleteDocument skipped #1. ActivityId:{0}, ExecutingUnprocessedActivities:{1}", activityId, executingUnprocessedActivities);
                return false;
            }

            var nodeId = GetNodeIdFromDocument(document);
            var versionId = GetVersionIdFromDocument(document);
            SnTrace.Index.Write("LM: AddCompleteDocument. ActivityId:{0}, ExecutingUnprocessedActivities:{1}, NodeId:{2}, VersionId:{3}", activityId, executingUnprocessedActivities, nodeId, versionId);

            var updates = GetUpdates(versioning);
            SetDocumentFlags(document, versionId, versioning);
            using (var wrFrame = IndexWriterFrame.Get(false)) // // AddCompleteDocument
            {
                wrFrame.IndexWriter.DeleteDocuments(versioning.Delete.Select(i => GetVersionIdTerm(i)).ToArray());
                foreach (var update in updates)
                    wrFrame.IndexWriter.UpdateDocument(update.UpdateTerm, update.Document);

                // pessimistic approach: delete document before adding it to avoid duplicate index documents
                wrFrame.IndexWriter.DeleteDocuments(GetVersionIdTerm(versionId));
                wrFrame.IndexWriter.AddDocument(document);
            }

            return true;
        }
        // AddDocumentActivity, RebuildActivity
        internal static bool AddDocument(Document document, int activityId, bool executingUnprocessedActivities, VersioningInfo versioning)
        {
            if (!IsActivityExecutable(executingUnprocessedActivities))
            {
                SnTrace.Index.Write("LM: AddDocument skipped #1. ActivityId:{0}, ExecutingUnprocessedActivities:{1}", activityId, executingUnprocessedActivities);
                return false;
            }

            var nodeId = GetNodeIdFromDocument(document);
            var versionId = GetVersionIdFromDocument(document);
            SnTrace.Index.Write("LM: AddDocument. ActivityId:{0}, ExecutingUnprocessedActivities:{1}, NodeId:{2}, VersionId:{3}", activityId, executingUnprocessedActivities, nodeId, versionId);

            var updates = GetUpdates(versioning);
            SetDocumentFlags(document, versionId, versioning);
            using (var wrFrame = IndexWriterFrame.Get(false)) // // AddDocument
            {
                wrFrame.IndexWriter.DeleteDocuments(versioning.Delete.Select(i => GetVersionIdTerm(i)).ToArray());
                foreach (var update in updates)
                    wrFrame.IndexWriter.UpdateDocument(update.UpdateTerm, update.Document);

                // pessimistic approach: delete document before adding it to avoid duplicate index documents
                wrFrame.IndexWriter.DeleteDocuments(GetVersionIdTerm(versionId));
                wrFrame.IndexWriter.AddDocument(document);
            }

            return true;
        }
        // AddDocumentActivity
        internal static bool AddDocument(int activityId, bool executingUnprocessedActivities, VersioningInfo versioning)
        {
            if (!IsActivityExecutable(executingUnprocessedActivities))
            {
                SnTrace.Index.Write("LM: AddDocument skipped #1. ActivityId:{0}, ExecutingUnprocessedActivities:{1}", activityId, executingUnprocessedActivities);
                return false;
            }

            SnTrace.Index.Write("LM: AddDocument without document. ActivityId:{0}, ExecutingUnprocessedActivities:{1}", activityId, executingUnprocessedActivities);

            var updates = GetUpdates(versioning);
            using (var wrFrame = IndexWriterFrame.Get(false)) // // AddDocument
            {
                wrFrame.IndexWriter.DeleteDocuments(versioning.Delete.Select(i => GetVersionIdTerm(i)).ToArray());
                foreach (var update in updates)
                    wrFrame.IndexWriter.UpdateDocument(update.UpdateTerm, update.Document);
            }

            return true;
        }
        // UpdateDocumentActivity
        internal static bool UpdateDocument(Document document, int activityId, bool executingUnprocessedActivities, VersioningInfo versioning)
        {
            if (!IsActivityExecutable(executingUnprocessedActivities))
            {
                SnTrace.Index.Write("LM: UpdateDocument skipped #1. ActivityId:{0}, ExecutingUnprocessedActivities:{1}", activityId, executingUnprocessedActivities);
                return false;
            }

            var nodeId = GetNodeIdFromDocument(document);
            var versionId = GetVersionIdFromDocument(document);
            SnTrace.Index.Write("LM: UpdateDocument. ActivityId:{0}, ExecutingUnprocessedActivities:{1}, NodeId:{2}, VersionId:{3}", activityId, executingUnprocessedActivities, nodeId, versionId);

            var updates = GetUpdates(versioning);
            SetDocumentFlags(document, versionId, versioning);
            using (var wrFrame = IndexWriterFrame.Get(false)) // // UpdateDocument
            {
                wrFrame.IndexWriter.DeleteDocuments(versioning.Delete.Select(i => GetVersionIdTerm(i)).ToArray());
                foreach (var update in updates)
                    wrFrame.IndexWriter.UpdateDocument(update.UpdateTerm, update.Document);

                wrFrame.IndexWriter.UpdateDocument(GetVersionIdTerm(versionId), document);
            }

            return true;
        }
        // UpdateDocumentActivity
        internal static bool UpdateDocument(int activityId, bool executingUnprocessedActivities, VersioningInfo versioning)
        {
            if (!IsActivityExecutable(executingUnprocessedActivities))
            {
                SnTrace.Index.Write("LM: UpdateDocument skipped #1. ActivityId:{0}, ExecutingUnprocessedActivities:{1}", activityId, executingUnprocessedActivities);
                return false;
            }

            SnTrace.Index.Write("LM: AddDocument without document. ActivityId:{0}, ExecutingUnprocessedActivities:{1}", activityId, executingUnprocessedActivities);

            var updates = GetUpdates(versioning);
            using (var wrFrame = IndexWriterFrame.Get(false)) // // UpdateDocument
            {
                wrFrame.IndexWriter.DeleteDocuments(versioning.Delete.Select(i => GetVersionIdTerm(i)).ToArray());
                foreach (var update in updates)
                    wrFrame.IndexWriter.UpdateDocument(update.UpdateTerm, update.Document);
            }

            return true;
        }
        // RemoveDocumentActivity
        internal static bool DeleteDocument(int nodeId, int versionId, bool moveOrRename, int activityId, bool executingUnprocessedActivities, VersioningInfo versioning)
        {
            if (!IsActivityExecutable(executingUnprocessedActivities))
            {
                SnTrace.Index.Write("LM: DeleteDocument skipped #1. ActivityId:{0}, ExecutingUnprocessedActivities:{1}, MoveOrRename:{2}", activityId, executingUnprocessedActivities, moveOrRename);
                return false;
            }

            SnTrace.Index.Write("LM: DeleteDocument. ActivityId:{0}, ExecutingUnprocessedActivities:{1}, NodeId:{2}, VersionId:{3}", activityId, executingUnprocessedActivities, nodeId, versionId);

            var deleteTerm = GetVersionIdTerm(versionId);

            var updates = GetUpdates(versioning);
            using (var wrFrame = IndexWriterFrame.Get(false)) // // DeleteDocuments
            {
                wrFrame.IndexWriter.DeleteDocuments(versioning.Delete.Select(i => GetVersionIdTerm(i)).ToArray());
                foreach (var update in updates)
                    wrFrame.IndexWriter.UpdateDocument(update.UpdateTerm, update.Document);
                wrFrame.IndexWriter.DeleteDocuments(deleteTerm);
            }

            return true;
        }
        // RemoveTreeActivity, RebuildActivity
        internal static bool DeleteDocuments(Term[] deleteTerms, bool moveOrRename, int activityId, bool executingUnprocessedActivities, VersioningInfo versioning)
        {
            if (!IsActivityExecutable(executingUnprocessedActivities))
            {
                SnTrace.Index.Write("LM: DeleteDocuments skipped #1. ActivityId:{0}, ExecutingUnprocessedActivities:{1}, MoveOrRename:{2}", activityId, executingUnprocessedActivities, moveOrRename);
                return false;
            }

            if (SnTrace.Index.Enabled)
                SnTrace.Index.Write("LM: DeleteDocuments. ActivityId:{0}, ExecutingUnprocessedActivities:{1}, terms:{2}", activityId,
                    executingUnprocessedActivities,
                    string.Join(", ", deleteTerms.Select(t =>
                    {
                        var name = t.Field();
                        var value = t.Text();
                        if (name == "VersionId")
                            value = GetIntFromPrefixCode(value).ToString();
                        return name + ":" + value;
                    }))
                    );

            using (var wrFrame = IndexWriterFrame.Get(false)) // // DeleteDocuments
            {
                wrFrame.IndexWriter.DeleteDocuments(deleteTerms);
            }

            // don't need to check if indexing interfered here. If it did, change is detected in overlapped adddocument/updatedocument, and refresh (re-delete) is called there.
            // deletedocuments will never detect change in index, since it sets timestamp in indexhistory to maxvalue.

            return true;
        }

        private static IEnumerable<DocumentUpdate> GetUpdates(VersioningInfo versioning)
        {
            var result = new List<DocumentUpdate>(versioning.Reindex.Length);

            var updates = IndexDocumentInfo.GetDocuments(versioning.Reindex);
            foreach (var doc in updates)
            {
                var verId = GetVersionIdFromDocument(doc);
                SetDocumentFlags(doc, verId, versioning);
                result.Add(new DocumentUpdate { UpdateTerm = GetVersionIdTerm(verId), Document = doc });
            }

            return result;
        }
        private static void SetDocumentFlags(Document doc, int versionId, VersioningInfo versioning)
        {
            var version = GetVersionFromDocument(doc);

            var isMajor = version.IsMajor;
            var isPublic = version.Status == VersionStatus.Approved;
            var isLastPublic = versionId == versioning.LastPublicVersionId;
            var isLastDraft = versionId == versioning.LastDraftVersionId;

            // set flags
            doc.RemoveField(IndexFieldName.IsMajor);
            doc.RemoveField(IndexFieldName.IsPublic);
            doc.RemoveField(IndexFieldName.IsLastPublic);
            doc.RemoveField(IndexFieldName.IsLastDraft);
            SetDocumentFlag(doc, IndexFieldName.IsMajor, isMajor);
            SetDocumentFlag(doc, IndexFieldName.IsPublic, isPublic);
            SetDocumentFlag(doc, IndexFieldName.IsLastPublic, isLastPublic);
            SetDocumentFlag(doc, IndexFieldName.IsLastDraft, isLastDraft);
        }
        internal static void SetDocumentFlag(Document doc, string fieldName, bool value)
        {
            doc.Add(new Field(fieldName, value ? StorageContext.Search.Yes : StorageContext.Search.No, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
        }


        // AddTreeActivity
        internal static bool AddTree(string treeRoot, bool moveOrRename, int activityId, bool executingUnprocessedActivities)
        {
            if (!IsActivityExecutable(executingUnprocessedActivities))
            {
                SnTrace.Index.Write("LM: AddTree skipped #1. ActivityId:{0}, ExecutingUnprocessedActivities:{1}, TreeRoot:{2}", activityId, executingUnprocessedActivities, treeRoot);
                return false;
            }

            SnTrace.Index.Write("LM: AddTree. ActivityId:{0}, ExecutingUnprocessedActivities:{1}, TreeRoot:{2}", activityId, executingUnprocessedActivities, treeRoot);

            using (var wrFrame = IndexWriterFrame.Get(false)) // // AddTree
            {
                if (executingUnprocessedActivities) // pessimistic compensation
                    wrFrame.IndexWriter.DeleteDocuments(new Term("InTree", treeRoot), new Term("Path", treeRoot));

                var excludedNodeTypes = GetNotIndexedNodeTypes();
                foreach (var docData in StorageContext.Search.LoadIndexDocumentsByPath(treeRoot, excludedNodeTypes))
                {
                    Document document;
                    int versionId;
                    try
                    {
                        document = IndexDocumentInfo.GetDocument(docData);
                        if (document == null) // indexing disabled
                            continue;
                        versionId = GetVersionIdFromDocument(document);
                    }
                    catch (Exception e)
                    {
                        var path = docData == null ? string.Empty : docData.Path ?? string.Empty;
                        SnLog.WriteException(e, "Error during indexing: the document data loaded from the database or the generated Lucene Document is invalid. Please save the content to regenerate the index for it. Path: " + path);

                        SnTrace.Index.WriteError("LM: Error during indexing: the document data loaded from the database or the generated Lucene Document is invalid. Please save the content to regenerate the index for it. Path: " + path);
                        SnTrace.Index.WriteError("LM: Error during indexing: " + e);

                        throw;
                    }

                    // pessimistic approach: delete document before adding it to avoid duplicate index documents
                    wrFrame.IndexWriter.DeleteDocuments(GetVersionIdTerm(versionId));
                    wrFrame.IndexWriter.AddDocument(document);
                }
            }
            return true;
        }

        internal static Term GetVersionIdTerm(int versionId)
        {
            return new Term(IndexFieldName.VersionId, Lucene.Net.Util.NumericUtils.IntToPrefixCoded(versionId));
        }
        internal static Term GetNodeIdTerm(int nodeId)
        {
            return new Term(IndexFieldName.NodeId, Lucene.Net.Util.NumericUtils.IntToPrefixCoded(nodeId));
        }

        private static bool IsActivityExecutable(bool executingUnprocessedActivities)
        {
            // if not running or paused, skip execution except executing unprocessed activities
            if (executingUnprocessedActivities)
                return true;
            if (Running && !Paused)
                return true;
            return false;
        }

        private static VersionNumber GetVersionFromDocument(Document doc)
        {
            return VersionNumber.Parse(doc.Get(IndexFieldName.Version));
        }
        private static int GetVersionIdFromDocument(Document doc)
        {
            return Int32.Parse(doc.Get(IndexFieldName.VersionId));
        }
        private static int GetNodeIdFromDocument(Document doc)
        {
            return Int32.Parse(doc.Get(IndexFieldName.NodeId));
        }

        /**/public static List<IIndexDocument> GetDocumentsByNodeId(int nodeId)
        {
            return _indexingEngine.GetDocumentsByNodeId(nodeId).ToList();
        }

        private static int GetIntFromPrefixCode(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            try
            {
                return NumericUtils.PrefixCodedToInt(text);
            }
            catch
            {
                // we cannot do much here
            }

            return -1;
        }

        #endregion
    }
}
