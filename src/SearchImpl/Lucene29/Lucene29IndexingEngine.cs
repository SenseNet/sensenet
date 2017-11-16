using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.LuceneSearch;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Lucene29
{
    internal class Lucene29IndexingEngine : IIndexingEngine
    {
        internal IndexDirectory IndexDirectory => LuceneSearchManager.IndexDirectory;

        private LuceneSearchManager LuceneSearchManager { get; }

        //===================================================================================== Constructors

        public Lucene29IndexingEngine(IndexDirectory indexDirectory = null)
        {
            //UNDONE: initialize/startup LuceneSearchManager
            LuceneSearchManager = new LuceneSearchManager(indexDirectory);
            LuceneSearchManager.OnStarted += Startup;
            LuceneSearchManager.OnLockFileRemoved += StartMessaging;
        }
        public Lucene29IndexingEngine(TimeSpan forceReopenFrequency)
        {
            //UNDONE: maybe set force reopen sequency in the constructor
            LuceneSearchManager = new LuceneSearchManager {ForceReopenFrequency = forceReopenFrequency};
            LuceneSearchManager.OnStarted += Startup;
            LuceneSearchManager.OnLockFileRemoved += StartMessaging;
        }

        //===================================================================================== IIndexingEngine implementation

        public bool IndexIsCentralized => false;
        public bool Running
        {
            get => LuceneSearchManager.Running;
            internal set => LuceneSearchManager.Running = value;
        }

        public void Start(TextWriter consoleOut)
        {
            //UNDONE: search engine-level start operations? Start messaging?
            LuceneSearchManager.Start(consoleOut);
        }

        protected virtual void Startup(TextWriter consoleOut) { }

        public void ShutDown()
        {
            LuceneSearchManager.ShutDown();
        }

        public void ClearIndex()
        {
            LuceneSearchManager.ClearIndex();
        }

        public IndexingActivityStatus ReadActivityStatusFromIndex()
        {
            return LuceneSearchManager.ReadActivityStatusFromIndex();
        }

        public void WriteActivityStatusToIndex(IndexingActivityStatus state)
        {
            LuceneSearchManager.WriteActivityStatusToIndex(state);
        }

        public void WriteIndex(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, IEnumerable<IndexDocument> addition)
        {
            LuceneSearchManager.WriteIndex(deletions, updates, addition);
        }

        //===================================================================================== IndexReader

        public IndexReaderFrame GetIndexReaderFrame(bool dirty)
        {
            return LuceneSearchManager.GetIndexReaderFrame(dirty);
        }
        public static IndexReaderFrame GetReaderFrame(bool dirty = false)
        {
            return ((Lucene29IndexingEngine)IndexManager.IndexingEngine).GetIndexReaderFrame(dirty);
        }

        public static Analyzer GetAnalyzer()
        {
            return LuceneSearchManager.GetAnalyzer();
        }

        //===================================================================================== Helper methods

        private void StartMessaging()
        {
            // we have to start the message cluster here
            var dummy = DistributedApplication.Cache.Count;
            var dummy2 = DistributedApplication.ClusterChannel;
        }
    }
}
