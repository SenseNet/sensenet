using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;

namespace SenseNet.Search.Lucene29
{
    internal class Lucene29IndexingEngine : IIndexingEngine
    {
        public bool Running { get; private set; }
        public bool Paused { get; private set; }

        public void Pause()
        {
            _indexingSemaphore.Reset();

            Commit();
            IndexWriterUsage.WaitForRunOutAllWriters();
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

        public void Commit()
        {
            Commit(false);
        }
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

        internal IndexWriter _writer; //UNDONE: refactor: change internal to private
        internal IndexReader _reader; //UNDONE: refactor: change internal to private

        internal ReaderWriterLockSlim _writerRestartLock = new ReaderWriterLockSlim(); //UNDONE: refactor: change internal to private

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

            _writer = new IndexWriter(directory, IndexManager.GetAnalyzer(), false, IndexWriter.MaxFieldLength.LIMITED);

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

    }
}
