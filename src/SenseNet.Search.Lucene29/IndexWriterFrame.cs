using System;
using System.Threading;
using Lucene.Net.Index;

namespace SenseNet.Search.Lucene29
{
    public class IndexWriterFrame : IDisposable
    {
        private abstract class IndexWriterUsage
        {
            private static IndexWriterUsage _instance = new FastIndexWriterUsage();
            protected static readonly AutoResetEvent Signal = new AutoResetEvent(false);
            protected static volatile int RefCount;

            internal static IndexWriterFrame GetWriterFrame(IndexWriter writer, ReaderWriterLockSlim writerRestartLock, bool safe)
            {
                if (safe)
                {
                    ChangeToSafe();
                    _instance.WaitForAllReleases();
                }
                return _instance.CreateWriterFrame(writer, writerRestartLock, safe);
            }
            // ReSharper disable once MemberHidesStaticFromOuterClass
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
            private static void ChangeToSafe()
            {
                if (_instance is SafeIndexWriterUsage)
                    return;
                _instance = new SafeIndexWriterUsage();
            }

            internal abstract IndexWriterFrame CreateWriterFrame(IndexWriter writer, ReaderWriterLockSlim writerRestartLock, bool safe);
            internal abstract void FinalizeFrame(ReaderWriterLockSlim writerRestartLock, bool safe);
            
            private void WaitForAllReleases()
            {
                while (RefCount > 0)
                    Signal.WaitOne();
            }
        }
        private class FastIndexWriterUsage : IndexWriterUsage
        {
            internal override IndexWriterFrame CreateWriterFrame(IndexWriter writer, ReaderWriterLockSlim writerRestartLock, bool safe)
            {
#pragma warning disable 420
                Interlocked.Increment(ref RefCount);
#pragma warning restore 420
                return new IndexWriterFrame(writer, writerRestartLock, this, safe);
            }
            internal override void FinalizeFrame(ReaderWriterLockSlim writerRestartLock, bool safe)
            {
#pragma warning disable 420
                Interlocked.Decrement(ref RefCount);
#pragma warning restore 420
                Signal.Set();
            }
        }
        private class SafeIndexWriterUsage : IndexWriterUsage
        {
            internal override IndexWriterFrame CreateWriterFrame(IndexWriter writer, ReaderWriterLockSlim writerRestartLock, bool safe)
            {
                writerRestartLock.EnterReadLock();
                return new IndexWriterFrame(writer, writerRestartLock, this, safe);
            }
            internal override void FinalizeFrame(ReaderWriterLockSlim writerRestartLock, bool safe)
            {
                writerRestartLock.ExitReadLock();
                if (safe)
                    ChangeToFast();
            }
        }

        // ============================================================================== public static part

        public static IndexWriterFrame Get(IndexWriter writer, ReaderWriterLockSlim writerRestartLock, bool safe)
        {
            return IndexWriterUsage.GetWriterFrame(writer, writerRestartLock, safe);
        }

        public static void WaitForRunOutAllWriters()
        {
            IndexWriterUsage.WaitForRunOutAllWriters();
        }

        // ============================================================================== nonpublic instance part

        private readonly bool _safe;
        private readonly IndexWriterUsage _usage;
        private readonly ReaderWriterLockSlim _writerRestartLock;
        public IndexWriter IndexWriter { get; }

        private IndexWriterFrame(IndexWriter writer, ReaderWriterLockSlim writerRestartLock, IndexWriterUsage usage, bool safe)
        {
            IndexWriter = writer;
            _writerRestartLock = writerRestartLock;
            _usage = usage;
            _safe = safe;
        }
        public void Dispose()
        {
            _usage.FinalizeFrame(_writerRestartLock, _safe);
        }
    }
}
