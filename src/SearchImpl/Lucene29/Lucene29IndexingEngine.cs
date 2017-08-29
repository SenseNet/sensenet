using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;

namespace SenseNet.Search.Lucene29
{
    internal class Lucene29IndexingEngine : IIndexingEngine
    {
        private class DocumentVersionComparer : IComparer<IIndexDocument>
        {
            public int Compare(IIndexDocument x, IIndexDocument y)
            {
                var vx = x?.Version.Substring(1) ?? "0.0.A";
                var vxa = vx.Split('.');
                var vy = y?.Version.Substring(1) ?? "0.0.A";
                var vya = vy.Split('.');

                var vxma = int.Parse(vxa[0]);
                var vyma = int.Parse(vya[0]);
                var dxma = vxma.CompareTo(vyma);
                if (dxma != 0)
                    return dxma;

                var vxmi = int.Parse(vxa[1]);
                var vymi = int.Parse(vya[1]);
                var dxmi = vxmi.CompareTo(vymi);
                if (dxmi != 0)
                    return dxmi;

                return string.Compare(vxa[2], vya[2], StringComparison.Ordinal);
            }
        }

        private class IndexWriterFrame : IDisposable
        {
            private abstract class IndexWriterUsage
            {
                private static IndexWriterUsage _instance = new FastIndexWriterUsage();
                protected static AutoResetEvent _signal = new AutoResetEvent(false);
                protected static volatile int _refCount;

                internal static IndexWriterFrame GetWriterFrame(bool safe)
                {
                    if (safe)
                    {
                        ChangeToSafe();
                        _instance.WaitForAllReleases();
                    }
                    return _instance.CreateWriterFrame(safe);
                }
                internal static void WaitForRunOutAllWriters()
                {
                    _instance.WaitForAllReleases();
                }
                internal static void ChangeToFast()
                {
                    if (_instance is FastIndexWriterUsage)
                        return;
                    _instance = new FastIndexWriterUsage();
                }
                internal static void ChangeToSafe()
                {
                    if (_instance is SafeIndexWriterUsage)
                        return;
                    _instance = new SafeIndexWriterUsage();
                }

                internal abstract IndexWriterFrame CreateWriterFrame(bool safe);
                internal abstract void FinalizeFrame(bool safe);
                internal bool Waiting;
                internal void WaitForAllReleases()
                {
                    while (_refCount > 0)
                    {
                        Waiting = true;
                        _signal.WaitOne();
                    }
                    Waiting = false;
                }

                protected IndexWriter GetWriter()
                {
                    return ((Lucene29IndexingEngine)IndexManager.IndexingEngine)._writer;
                }

                protected ReaderWriterLockSlim GetWriterRestartLock()
                {
                    return ((Lucene29IndexingEngine)IndexManager.IndexingEngine)._writerRestartLock;
                }
            }
            private class FastIndexWriterUsage : IndexWriterUsage
            {
                internal override IndexWriterFrame CreateWriterFrame(bool safe)
                {
#pragma warning disable 420
                    Interlocked.Increment(ref _refCount);
#pragma warning restore 420
                    return new IndexWriterFrame(GetWriter(), this, safe);
                }
                internal override void FinalizeFrame(bool safe)
                {
#pragma warning disable 420
                    Interlocked.Decrement(ref _refCount);
#pragma warning restore 420
                    _signal.Set();
                }
            }
            private class SafeIndexWriterUsage : IndexWriterUsage
            {
                internal override IndexWriterFrame CreateWriterFrame(bool safe)
                {
                    GetWriterRestartLock().EnterReadLock();
                    return new IndexWriterFrame(GetWriter(), this, safe);
                }
                internal override void FinalizeFrame(bool safe)
                {
                    GetWriterRestartLock().ExitReadLock();
                    if (safe)
                        ChangeToFast();
                }
            }

            // ============================================================================== public static part

            public static IndexWriterFrame Get(bool safe)
            {
                return IndexWriterUsage.GetWriterFrame(safe);
            }

            public static void WaitForRunOutAllWriters()
            {
                IndexWriterFrame.IndexWriterUsage.WaitForRunOutAllWriters();
            }

            // ============================================================================== nonpublic instance part

            private bool _safe;
            private IndexWriterUsage _usage;
            private IndexWriter _writer;
            public IndexWriter IndexWriter { get { return _writer; } }

            private IndexWriterFrame(IndexWriter writer, IndexWriterUsage usage, bool safe)
            {
                _writer = writer;
                _usage = usage;
                _safe = safe;
            }
            public void Dispose()
            {
                _usage.FinalizeFrame(_safe);
            }
        }

        public bool Running { get; private set; }
        public bool Paused { get; private set; }

        public void Pause()
        {
            _indexingSemaphore.Reset();

            Commit();
            IndexWriterFrame.WaitForRunOutAllWriters();
        }
        public void WaitIfIndexingPaused()
        {
            if (!_indexingSemaphore.Wait(SenseNet.Configuration.Indexing.IndexingPausedTimeout * 1000))
                throw new TimeoutException("Operation timed out, indexing is paused.");
        }
        public void Continue()
        {
            throw new NotSupportedException("Continue indexing is not supported in this version.");
        }

        private object _startSync = new object();
        public void Start(System.IO.TextWriter consoleOut)
        {
            if (!Running)
            {
                lock (_startSync)
                {
                    if (!Running)
                    {
                        Startup(consoleOut);
                        Running = true;
                    }
                }
            }
        }
        private void Startup(System.IO.TextWriter consoleOut)
        {
            RemoveIndexWriterLockFile(consoleOut);

            // we positively start the message cluster
            int dummy = SenseNet.ContentRepository.DistributedApplication.Cache.Count;
            var dummy2 = SenseNet.ContentRepository.DistributedApplication.ClusterChannel;

            // Lucene subsystem behaves strangely if the enums are not initialized.
            var x = Lucene.Net.Documents.Field.Index.NO;
            var y = Lucene.Net.Documents.Field.Store.NO;
            var z = Lucene.Net.Documents.Field.TermVector.NO;

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
        private void RemoveIndexWriterLockFile(System.IO.TextWriter consoleOut)
        {
            // delete write.lock if necessary
            var lockFilePath = StorageContext.Search.IndexLockFilePath;
            if (lockFilePath == null)
                return;

            consoleOut.WriteLine($"Index: {IndexDirectory.CurrentOrDefaultDirectory}");

            if (System.IO.File.Exists(lockFilePath))
            {
                var endRetry = DateTime.UtcNow.AddSeconds(Configuration.Indexing.LuceneLockDeleteRetryInterval);
                consoleOut.WriteLine("Index directory is read only.");

                // retry write.lock for a given period of time
                while (true)
                {
                    try
                    {
                        System.IO.File.Delete(lockFilePath);
                        consoleOut.WriteLine("Index directory lock removed.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Thread.Sleep(5000);
                        if (DateTime.UtcNow > endRetry)
                            throw new IOException("Cannot remove the index lock: " + ex.Message, ex);
                    }
                }
            }
            else
            {
                consoleOut.WriteLine("Index directory is read/write.");
            }
        }
        private void Warmup()
        {
            var idList = ((LuceneSearchEngine)StorageContext.Search.SearchEngine).Execute("+Id:1");
        }

        public void Restart()
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

        public void ShutDown()
        {
            if (!Running)
                return;
            if (Paused)
                throw new InvalidOperationException("Cannot use the IndexReader if the indexing is paused (i.e. LuceneManager.Paused = true)");

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
                            Running = false;
                        }
                        op2.Successful = true;
                    }
                    op.Successful = true;
                }
            }
        }

        public void ActivityFinished()
        {
            // compiler warning here is not a problem, Interlocked 
            // class can work with a volatile variable
#pragma warning disable 420
            Interlocked.Increment(ref _activities);
#pragma warning restore 420
        }

        public void Commit(int lastActivityId = 0)
        {
            Commit(false);
        }

        public IIndexingActivityStatus ReadActivityStatusFromIndex()
        {
            using (var readerFrame = GetIndexReaderFrame())
                return CompletionState.ParseFromReader(readerFrame.IndexReader);
        }

        /* ============================================================================================= Document Operations */

        public void Actualize(IEnumerable<SnTerm> deletions, IndexDocument addition, IEnumerable<DocumentUpdate> updates)
        {
            using (var wrFrame = IndexWriterFrame.Get(false)) // // AddDocument
            {
                if (deletions != null)
                    wrFrame.IndexWriter.DeleteDocuments(GetTerms(deletions));

                if(updates != null)
                    foreach (var update in updates)
                        wrFrame.IndexWriter.UpdateDocument(GetTerm(update.UpdateTerm), GetDocument(update.Document));

                if (addition != null)
                {
                    // pessimistic approach: delete document before adding it to avoid duplicate index documents
                    wrFrame.IndexWriter.DeleteDocuments(GetVersionIdTerm(addition.VersionId));
                    wrFrame.IndexWriter.AddDocument(GetDocument(addition));
                }
            }
        }

        public void Actualize(IEnumerable<SnTerm> deletions, IEnumerable<IndexDocument> addition)
        {
            using (var wrFrame = IndexWriterFrame.Get(false)) // // AddTree
            {
                if (deletions != null)
                    wrFrame.IndexWriter.DeleteDocuments(GetTerms(deletions));

                foreach (var snDoc in addition)
                {
                    Document document;
                    int versionId;
                    try
                    {
                        document = GetDocument(snDoc);
                        if (document == null) // indexing disabled
                            continue;
                        versionId = snDoc.VersionId;
                    }
                    catch (Exception e)
                    {
                        var path = snDoc?.GetStringValue(IndexFieldName.Path) ?? string.Empty;
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
        }

        private Term GetTerm(SnTerm snTerm)
        {
            return GetTerms(snTerm).First();
        }
        private Term[] GetTerms(IEnumerable<SnTerm> snTerms)
        {
            List<Term> terms = new List<Term>();
            foreach(var snTerm in snTerms)
                terms.AddRange(GetTerms(snTerm));
            return terms.ToArray();
        }
        private Term[] GetTerms(SnTerm snTerm)
        {
            switch (snTerm.Type)
            {
                case SnTermType.String:
                    return new[] {new Term(snTerm.Name, snTerm.StringValue)};
                case SnTermType.StringArray:
                    return snTerm.StringArrayValue.Select(s=> new Term(snTerm.Name, s) ).ToArray();
                case SnTermType.Bool:
                    return new[] { new Term(snTerm.Name, snTerm.BooleanValue ? StorageContext.Search.Yes : StorageContext.Search.No) };
                case SnTermType.Int:
                    return new[] { new Term(snTerm.Name, NumericUtils.IntToPrefixCoded(snTerm.IntegerValue)) };
                case SnTermType.Long:
                    return new[] { new Term(snTerm.Name, NumericUtils.LongToPrefixCoded(snTerm.LongValue)) };
                case SnTermType.Float:
                    return new[] { new Term(snTerm.Name, NumericUtils.FloatToPrefixCoded(snTerm.SingleValue)) };
                case SnTermType.Double:
                    return new[] { new Term(snTerm.Name, NumericUtils.DoubleToPrefixCoded(snTerm.DoubleValue)) };
                case SnTermType.DateTime:
                    return new[] { new Term(snTerm.Name, NumericUtils.LongToPrefixCoded(snTerm.DateTimeValue.Ticks)) };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private Term GetVersionIdTerm(int versionId)
        {
            return new Term(IndexFieldName.VersionId, Lucene.Net.Util.NumericUtils.IntToPrefixCoded(versionId));
        }

        private Document GetDocument(IIndexDocument snDoc)
        {
            throw new NotImplementedException(); //UNDONE:!!!! implement GetDocument(IndexDocument snDoc):Document
        }

        /* ============================================================================================= */

        private void Commit(bool reopenReader)
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

        private void ReopenReader()
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

        /* ==================================================================================================== */

        private const string COMMITFIELDNAME = "$#COMMIT";
        private const string COMMITDATAFIELDNAME = "$#DATA";
        private const int REOPENRETRYMAX = 2;

        private IndexWriter _writer;
        private IndexReader _reader;

        private ReaderWriterLockSlim _writerRestartLock = new ReaderWriterLockSlim();

        private readonly ManualResetEventSlim _indexingSemaphore = new ManualResetEventSlim(true);
        private volatile int _delayCycle;          // committer thread uses
        private volatile int _activities;          // committer thread sets 0 other threads increment
        private volatile int _recentlyUsedReaderFrames;

        private TimeSpan _forceReopenFrequency;
        public TimeSpan ForceReopenFrequency
        {
            get
            {
                if (_forceReopenFrequency == default(TimeSpan))
                {
                    var settings = StorageContext.Search.ContentRepository.GetSettingsValue<int>("ForceReopenFrequencyInSeconds", 0);
                    _forceReopenFrequency = TimeSpan.FromSeconds(settings == 0 ? 30.0 : settings);
                }
                return _forceReopenFrequency;
            }
        }

        public DateTime IndexReopenedAt { get; private set; }

        public IndexReaderFrame GetIndexReaderFrame(bool dirty = false)
        {
            var needReopen = (DateTime.UtcNow - IndexReopenedAt) > ForceReopenFrequency;
            if (!dirty || needReopen)
                if (!_reader.IsCurrent())
                    using (var wrFrame = IndexWriterFrame.Get(false)) // // IndexReader getter
                        ReopenReader();
            Interlocked.Increment(ref _recentlyUsedReaderFrames);
            return new IndexReaderFrame(_reader);
        }

        private void CreateWriterAndReader()
        {
            var directory = FSDirectory.Open(new System.IO.DirectoryInfo(IndexDirectory.CurrentDirectory));

            _writer = new IndexWriter(directory, Lucene29IndexManager.GetAnalyzer(), false, IndexWriter.MaxFieldLength.LIMITED);

            _writer.SetMaxMergeDocs(SenseNet.Configuration.Indexing.LuceneMaxMergeDocs);
            _writer.SetMergeFactor(SenseNet.Configuration.Indexing.LuceneMergeFactor);
            _writer.SetRAMBufferSizeMB(SenseNet.Configuration.Indexing.LuceneRAMBufferSizeMB);
            _reader = _writer.GetReader();
        }
        private Document GetFakeDocument()
        {
            var value = Guid.NewGuid().ToString();
            var doc = new Document();
            doc.Add(new Field(COMMITFIELDNAME, COMMITFIELDNAME, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
            doc.Add(new Field(COMMITDATAFIELDNAME, value, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));

            return doc;
        }

        internal void CommitOrDelay()
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

        private bool _stopCommitWorker;
        private object _commitLock = new object();
        private void CommitWorker()
        {
            int wait = (int)(SenseNet.Configuration.Indexing.CommitDelayInSeconds * 1000.0);
            for (;;)
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

        /* ================================================================== Tools */

        /// <summary> For test purposes. </summary>
        public IEnumerable<IIndexDocument> GetDocumentsByNodeId(int nodeId)
        {
            using (var readerFrame = GetIndexReaderFrame())
            {
                var termDocs = readerFrame.IndexReader.TermDocs(new Term(IndexFieldName.NodeId, Lucene.Net.Util.NumericUtils.IntToPrefixCoded(nodeId)));
                return GetDocumentsFromTermDocs(termDocs, readerFrame);
            }
        }
        private IEnumerable<IIndexDocument> GetDocumentsFromTermDocs(TermDocs termDocs, IndexReaderFrame readerFrame)
        {
            throw new NotImplementedException();
            //var docs = new List<IIndexDocument>();
            //while (termDocs.Next())
            //    docs.Add(new Lucene29IndexDocument(readerFrame.IndexReader.Document(termDocs.Doc())));
            //docs.Sort(new DocumentVersionComparer());
            //return docs;
        }

    }
}
