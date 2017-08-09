using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Lucene.Net.Index;
using Lucene.Net.Analysis;
using Lucene.Net.Store;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;

namespace SenseNet.Search.Lucene29
{
    public static class Lucene29IndexManager //UNDONE:!!!! Merge functionality into the indexing engine (even as static)
    {
        static Lucene29IndexManager()
        {
            IndexWriter.SetDefaultWriteLockTimeout(20 * 60 * 1000); // 20 minutes
        }

        internal static IndexWriter GetIndexWriter(bool createNew)
        {
            Directory dir = FSDirectory.Open(new System.IO.DirectoryInfo(SenseNet.ContentRepository.Storage.IndexDirectory.CurrentOrDefaultDirectory));
            return new IndexWriter(dir, GetAnalyzer(), createNew, IndexWriter.MaxFieldLength.UNLIMITED);
        }

        public static Analyzer GetAnalyzer()
        {
            var defaultAnalyzer = new KeywordAnalyzer();
            var analyzer = new Indexing.SnPerFieldAnalyzerWrapper(defaultAnalyzer);

            return analyzer;
        }

        /* ======================================== Wait for write.lock */

        private const string WAITINGFORLOCKSTR = "write.lock exists, waiting for removal...";
        /// <summary>
        /// Waits for releasing index writer lock file in the configured index directory. Timeout: configured with IndexLockFileWaitForRemovedTimeout key.
        /// Returns true if the lock was released. Returns false if the time has expired.
        /// </summary>
        /// <returns>Returns true if the lock was released. Returns false if the time has expired.</returns>
        public static bool WaitForWriterLockFileIsReleased()
        {
            return WaitForWriterLockFileIsReleased(IndexDirectory.CurrentDirectory);
        }
        /// <summary>
        /// Waits for releasing index writer lock file in the specified directory. Timeout: configured with IndexLockFileWaitForRemovedTimeout key.
        /// Returns true if the lock was released. Returns false if the time has expired.
        /// </summary>
        /// <returns>Returns true if the lock was released. Returns false if the time has expired.</returns>
        public static bool WaitForWriterLockFileIsReleased(string indexDirectory)
        {
            return WaitForWriterLockFileIsReleased(indexDirectory, Configuration.Indexing.IndexLockFileWaitForRemovedTimeout);
        }
        /// <summary>
        /// Waits for releasing index writer lock file in the specified directory and timeout.
        /// Returns true if the lock was released. Returns false if the time has expired.
        /// </summary>
        /// <returns>Returns true if the lock was released. Returns false if the time has expired.</returns>
        public static bool WaitForWriterLockFileIsReleased(string indexDirectory, int timeout)
        {
            if (indexDirectory == null)
            {
                SnTrace.Repository.Write("Index directory not found.");
                return true;
            }

            var lockFilePath = System.IO.Path.Combine(indexDirectory, Lucene.Net.Index.IndexWriter.WRITE_LOCK_NAME);
            var deadline = DateTime.UtcNow.AddSeconds(timeout);

            SnTrace.Repository.Write("Waiting for lock file to disappear: " + lockFilePath);

            while (System.IO.File.Exists(lockFilePath))
            {
                Trace.WriteLine(WAITINGFORLOCKSTR);
                SnTrace.Repository.Write(WAITINGFORLOCKSTR);

                Thread.Sleep(100);
                if (DateTime.UtcNow > deadline)
                    return false;
            }

            SnTrace.Repository.Write("Lock file has gone: " + lockFilePath);

            return true;
        }
    }

}
