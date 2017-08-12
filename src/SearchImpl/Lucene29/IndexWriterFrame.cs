using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using System.Threading;
using SenseNet.Search.Indexing;
using SenseNet.Search.Lucene29;

namespace SenseNet.Search.Lucene29
{
    internal class IndexWriterFrame : IDisposable
    {
        // ============================================================================== public static part

        public static IndexWriterFrame Get(bool safe)
        {
            return IndexWriterUsage.GetWriterFrame(safe);
        }

        // ============================================================================== nonpublic instance part

        private bool _safe;
        private IndexWriterUsage _usage;
        private IndexWriter _writer;
        public IndexWriter IndexWriter { get { return _writer; } }

        internal IndexWriterFrame(IndexWriter writer, IndexWriterUsage usage, bool safe)
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

    internal abstract class IndexWriterUsage
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
            return ((Lucene29IndexingEngine)IndexManager.IndexingEngine)._writer; //UNDONE: refactor: do not use member of another class
        }

        protected ReaderWriterLockSlim GetWriterRestartLock()
        {
            return ((Lucene29IndexingEngine)IndexManager.IndexingEngine)._writerRestartLock; //UNDONE: refactor: do not use member of another class
        }
    }
    internal class FastIndexWriterUsage : IndexWriterUsage
    {
        internal override IndexWriterFrame CreateWriterFrame(bool safe)
        {
#pragma warning disable 420
            Interlocked.Increment(ref _refCount);
#pragma warning restore 420
            return new IndexWriterFrame(GetWriter(), this, safe); //UNDONE: do not use member of amother class
        }
        internal override void FinalizeFrame(bool safe)
        {
#pragma warning disable 420
            Interlocked.Decrement(ref _refCount);
#pragma warning restore 420
            _signal.Set();
        }
    }
    internal class SafeIndexWriterUsage : IndexWriterUsage
    {
        internal override IndexWriterFrame CreateWriterFrame(bool safe)
        {
            GetWriterRestartLock().EnterReadLock();
            return new IndexWriterFrame(GetWriter(), this, safe);
        }
        internal override void FinalizeFrame(bool safe)
        {
            GetWriterRestartLock().ExitReadLock();
            if(safe)
                ChangeToFast();
        }
    }
}
