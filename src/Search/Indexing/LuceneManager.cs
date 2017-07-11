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
using Field = Lucene.Net.Documents.Field;

namespace SenseNet.Search.Indexing
{
    public enum CommitHint { AddNew, AddNewVersion, Update, Rename, Move, Delete } ;

    public static class LuceneManager
    {
        private class DocumentUpdate
        {
            public Term UpdateTerm;
            public Document Document;
        }

        private class DocumentVersionComparer : IComparer<Document>
        {
            public int Compare(Document x, Document y)
            {
                var vx = x.Get("Version").Substring(1);
                var vxa = vx.Split('.');
                var vy = y.Get("Version").Substring(1);
                var vya = vy.Split('.');

                var vxma = Int32.Parse(vxa[0]);
                var vyma = Int32.Parse(vya[0]);
                var dxma = vxma.CompareTo(vyma);
                if (dxma != 0)
                    return dxma;

                var vxmi = Int32.Parse(vxa[1]);
                var vymi = Int32.Parse(vya[1]);
                var dxmi = vxmi.CompareTo(vymi);
                if (dxmi != 0)
                    return dxmi;
                return vxa[2].CompareTo(vya[2]);
            }
        }

        public static readonly Lucene.Net.Util.Version LuceneVersion = Lucene.Net.Util.Version.LUCENE_29;

        internal static IndexWriter _writer;
        internal static IndexReader _reader;

        public static int IndexCount { get { return 1; } }
        public static int IndexedDocumentCount
        {
            get
            {
                using (var readerFrame = LuceneManager.GetIndexReaderFrame())
                {
                    var idxReader = readerFrame.IndexReader;
                    return idxReader.NumDocs();
                }
            }
        }

        public static int[] GetNotIndexedNodeTypes()
        {
            return StorageContext.Search.ContentRepository.GetNotIndexedNodeTypeIds();
        }

        public static TimeSpan ForceReopenFrequency => SearchEngineSettings.Instance.ForceReopenFrequency;

        public static DateTime IndexReopenedAt { get; private set; }
        private static volatile int _recentlyUsedReaderFrames;

        internal static ReaderWriterLockSlim _writerRestartLock = new ReaderWriterLockSlim();
        private const int REOPENRETRYMAX = 2;
        public static IndexReaderFrame GetIndexReaderFrame(bool dirty = false)
        {
            var needReopen = (DateTime.UtcNow - IndexReopenedAt) > ForceReopenFrequency;
            if (!dirty || needReopen)
                if (!_reader.IsCurrent())
                    using (var wrFrame = IndexWriterFrame.Get(false)) // // IndexReader getter
                        ReopenReader();
            Interlocked.Increment(ref _recentlyUsedReaderFrames);
            return new IndexReaderFrame(_reader);
        }

        private static object _startSync = new object();
        private static bool _running;
        public static bool Running
        {
            get { return _running; }
        }

        private static bool _paused;
        internal static bool Paused
        {
            get { return _paused; }
        }

        internal static void PauseIndexing()
        {
            _indexingSemaphore.Reset();

            Commit();
            IndexWriterUsage.WaitForRunOutAllWriters();
            _paused = true;
        }
        internal static void ContinueIndexing()
        {
            throw new NotSupportedException("Continue indexing is not supported in this version.");
        }
        internal static Exception GetPausedException(string msg = null)
        {
            if (msg == null)
                msg = "Cannot use the IndexReader if the indexing is paused (i.e. LuceneManager.Paused = true)";
            return new InvalidOperationException(msg);
        }

        internal static ManualResetEventSlim _indexingSemaphore = new ManualResetEventSlim(true);

        internal static void WaitIfIndexingPaused()
        {
            if (!_indexingSemaphore.Wait(SenseNet.Configuration.Indexing.IndexingPausedTimeout * 1000))
                throw new TimeoutException("Operation timed out, indexing is paused.");
        }

        [Obsolete("Use Start(System.IO.TextWriter) instead.")]
        public static void Start()
        {
            Start(null);
        }

        internal static bool StartingUp { get; private set; }

        public static void Start(System.IO.TextWriter consoleOut)
        {
            if (!_running)
            {
                lock (_startSync)
                {
                    if (!_running)
                    {
                        StartingUp = true;
                        Startup(consoleOut);
                        StartingUp = false;
                        _running = true;
                    }
                }
            }
        }
        private static void Startup(System.IO.TextWriter consoleOut)
        {
            // we positively start the message cluster
            int dummy = SenseNet.ContentRepository.DistributedApplication.Cache.Count;
            var dummy2 = SenseNet.ContentRepository.DistributedApplication.ClusterChannel;

            if (StorageContext.Search.ContentRepository.RestoreIndexOnstartup())
                BackupTools.RestoreIndex(false, consoleOut);

            CreateWriterAndReader();

            IndexingActivityQueue.Startup(consoleOut);

            Warmup();

            var commitStart = new ThreadStart(CommitWorker);
            var t = new Thread(commitStart);
            t.Start();

            SnTrace.Index.Write("LM: 'CommitWorker' thread started. ManagedThreadId: {0}", t.ManagedThreadId);

            IndexHealthMonitor.Start(consoleOut);
        }

        private static void CreateWriterAndReader()
        {
            var directory = FSDirectory.Open(new System.IO.DirectoryInfo(IndexDirectory.CurrentDirectory));

            _writer = new IndexWriter(directory, IndexManager.GetAnalyzer(), false, IndexWriter.MaxFieldLength.LIMITED);

            _writer.SetMaxMergeDocs(SenseNet.Configuration.Indexing.LuceneMaxMergeDocs);
            _writer.SetMergeFactor(SenseNet.Configuration.Indexing.LuceneMergeFactor);
            _writer.SetRAMBufferSizeMB(SenseNet.Configuration.Indexing.LuceneRAMBufferSizeMB);
            _reader = _writer.GetReader();
        }

        internal static string[] PauseIndexingAndGetIndexFilePaths()
        {
            PauseIndexing();

            return GetIndexFilePathsInternal();
        }

        private static string[] GetIndexFilePathsInternal()
        {
            var filePaths = new List<string>();

            // in case of the index does not exist
            if (!IndexDirectory.Exists)
                return filePaths.ToArray();

            try
            {
                var di = new DirectoryInfo(IndexDirectory.CurrentDirectory);
                var files = di.GetFiles();

                filePaths.AddRange(files.Where(f => f.Name != IndexWriter.WRITE_LOCK_NAME).Select(f => f.FullName));
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
            }

            return filePaths.ToArray();
        }

        // ========================================================================================== Register Activity

        public static void RegisterActivity(LuceneIndexingActivity activity)
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

        // ========================================================================================== Start, Restart, Shutdown, Warmup

        internal static void Restart()
        {
            if (Paused)
            {
                SnTrace.Index.Write("LM: LUCENEMANAGER RESTART called but it is not executed because indexing is paused.");
                return;
            }
            SnTrace.Index.Write("LM: LUCENEMANAGER RESTART");

            using (var wrFrame = IndexWriterFrame.Get(true)) // // Restart
            {
                wrFrame.IndexWriter.Close();
                CreateWriterAndReader();
            }
        }
        public static void ShutDown()
        {
            ShutDown(true);
        }
        private static void ShutDown(bool log)
        {
            if (!_running)
                return;
            if (Paused)
                throw GetPausedException();

            using (var op = SnTrace.Index.StartOperation("LUCENEMANAGER SHUTDOWN"))
            {

                if (_writer != null)
                {
                    _stopCommitWorker = true;

                    lock (_commitLock)
                        Commit(false);

                    using (var op2 = SnTrace.Index.StartOperation("LM.CloseReaders"))
                    {
                        using (var wrFrame = IndexWriterFrame.Get(true)) // // ShutDown
                        {
                            if (_reader != null)
                                _reader.Close();
                            if (_writer != null)
                                _writer.Close();
                            _running = false;
                        }
                        op2.Successful = true;
                    }
                    op.Successful = true;
                }
            }


            if (log)
                SnLog.WriteInformation("LuceneManager has stopped. Max task id and exceptions: " + IndexingActivityQueue.GetCurrentCompletionState());
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

        private const string COMMITFIELDNAME = "$#COMMIT";
        private const string COMMITDATAFIELDNAME = "$#DATA";

        private static Document GetFakeDocument()
        {
            var value = Guid.NewGuid().ToString();
            var doc = new Document();
            doc.Add(new Field(COMMITFIELDNAME, COMMITFIELDNAME, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
            doc.Add(new Field(COMMITDATAFIELDNAME, value, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));

            return doc;
        }

        internal static void Warmup()
        {
            var idList = ((LuceneSearchEngine)StorageContext.Search.SearchEngine).Execute("+Id:1");
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
            if (!IsActivityExecutable(executingUnprocessedActivities))
                return;

            // compiler warning here is not a problem, Interlocked 
            // class can work with a volatile variable
#pragma warning disable 420
            Interlocked.Increment(ref _activities);
#pragma warning restore 420
        }

        internal static void CommitOrDelay()
        {
            if (Paused)
                return;

            var act = _activities;
            if (act == 0 && _delayCycle == 0)
                return;

            if (act < 2)
            {
                Commit();
            }
            else
            {
                _delayCycle++;
                if (_delayCycle > SenseNet.Configuration.Indexing.DelayedCommitCycleMaxCount)
                {
                    Commit();
                }
            }

#pragma warning disable 420
            Interlocked.Exchange(ref _activities, 0);
#pragma warning restore 420
        }

        internal static void Commit(bool reopenReader = true)
        {
            CompletionState commitState;
            using (var op = SnTrace.Index.StartOperation("LM: Commit. reopenReader:{0}", reopenReader))
            {
                using (var wrFrame = IndexWriterFrame.Get(!reopenReader)) // // Commit
                {
                    commitState = CompletionState.GetCurrent();
                    var commitStateMessage = commitState.ToString();

                    SnTrace.Index.Write("LM: Committing_writer. commitState: " + commitStateMessage);

                    // Write a fake document to make sure that the index changes are written to the file system.
                    wrFrame.IndexWriter.UpdateDocument(new Term(COMMITFIELDNAME, COMMITFIELDNAME), GetFakeDocument());

                    wrFrame.IndexWriter.Commit(CompletionState.GetCommitUserData(commitState));

                    if (reopenReader)
                        ReopenReader();
                }

#pragma warning disable 420
                Interlocked.Exchange(ref _activities, 0);
#pragma warning restore 420
                _delayCycle = 0;

                op.Successful = true;
            }
        }
        internal static void ReopenReader()
        {
            using (var op = SnTrace.Index.StartOperation("LM: ReopenReader"))
            {
                var retry = 0;
                Exception e = null;
                while (retry++ < REOPENRETRYMAX)
                {
                    try
                    {
                        if (retry > 1)
                            SnTrace.Index.Write("LM: REOPEN READER {0}. ATTEMPT", retry);

                        _reader = _writer.GetReader();

                        var recentlyUsedReaderFrames = Interlocked.Exchange(ref _recentlyUsedReaderFrames, 0);

                        op.Successful = true;
                        IndexReopenedAt = DateTime.UtcNow;
                        SnTrace.Index.Write("Recently used reader frames from last reopening reader: {0}", recentlyUsedReaderFrames);
                        return;
                    }
                    catch (AlreadyClosedException ace)
                    {
                        e = ace;
                        Thread.Sleep(100);
                    }
                }
                if (e != null)
                    throw new ApplicationException(String.Concat("Indexwriter is closed after ", retry, " attempt."), e);
            }
        }

        private static volatile int _activities;          // committer thread sets 0 other threads increment
        private static volatile int _delayCycle;          // committer thread uses

        private static bool _stopCommitWorker;
        private static object _commitLock = new object();
        private static void CommitWorker()
        {
            int wait = (int)(SenseNet.Configuration.Indexing.CommitDelayInSeconds * 1000.0);
            for (; ; )
            {
                // check if commit worker instructed to stop
                if (_stopCommitWorker)
                {
                    _stopCommitWorker = false;
                    return;
                }

                try
                {
                    lock (_commitLock)
                    {
                        CommitOrDelay();
                    }
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex);
                }
                Thread.Sleep(wait);
            }
        }

        /*==================================================================== Document operations */

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
            doc.Add(new Field(fieldName, value ? StorageContext.Search.YES : StorageContext.Search.NO, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
        }



        internal static bool AddTree(string treeRoot, bool moveOrRename, int activityId, bool executingUnprocessedActivities)
        {
            if (!IsActivityExecutable(executingUnprocessedActivities))
            {
                SnTrace.Index.Write("LM: AddTree skipped #1. ActivityId:{0}, ExecutingUnprocessedActivities:{1}, TreeRoot:{2}", activityId, executingUnprocessedActivities, treeRoot);
                return false;
            }

            SnTrace.Index.Write("LM: AddTree. ActivityId:{0}, ExecutingUnprocessedActivities:{1}, TreeRoot:{2}", activityId, executingUnprocessedActivities, treeRoot);

            using (var wrFrame = IndexWriterFrame.Get(false))
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

        public static List<Document> GetDocumentsByNodeId(int nodeId)
        {
            using (var readerFrame = LuceneManager.GetIndexReaderFrame())
            {
                var termDocs = readerFrame.IndexReader.TermDocs(new Term(IndexFieldName.NodeId, Lucene.Net.Util.NumericUtils.IntToPrefixCoded(nodeId)));
                return GetDocumentsFromTermDocs(termDocs, readerFrame);
            }
        }
        private static List<Document> GetDocumentsFromTermDocs(TermDocs termDocs, IndexReaderFrame readerFrame)
        {
            var docs = new List<Document>();
            while (termDocs.Next())
                docs.Add(readerFrame.IndexReader.Document(termDocs.Doc()));
            docs.Sort(new DocumentVersionComparer());
            return docs;
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
    }
}
