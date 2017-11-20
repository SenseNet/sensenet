using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
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
            var indexDir = indexDirectory ?? new IndexDirectory(null, SearchManager.IndexDirectoryPath);

            LuceneSearchManager = new LuceneSearchManager(indexDir);

            SetEventhandlers();
        }

        //UNDONE: find usages
        public Lucene29IndexingEngine(TimeSpan forceReopenFrequency)
        {
            var indexDirectory = new IndexDirectory(null, SearchManager.IndexDirectoryPath);

            //UNDONE: maybe set force reopen sequency in the constructor
            LuceneSearchManager = new LuceneSearchManager(indexDirectory)
            {
                ForceReopenFrequency = forceReopenFrequency
            };

            SetEventhandlers();
        }

        private void SetEventhandlers()
        {
            // set up event handlers
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
            LuceneSearchManager.Start(consoleOut);
        }

        /// <summary>
        /// Derived classes may add custom logic here that will be executed at the end
        /// of the start process, but before the Running switch is set to True.
        /// </summary>
        /// <param name="consoleOut"></param>
        protected virtual void Startup(TextWriter consoleOut) { }

        public void ShutDown()
        {
            //UNDONE: CommitState: write the indexing status before shutdown?
            // IndexManager.GetCurrentIndexingActivityStatus()
            // WriteActivityStatusToIndex
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

        private IndexReaderFrame GetIndexReaderFrame(bool dirty)
        {
            return LuceneSearchManager.GetIndexReaderFrame(dirty);
        }
        public static IndexReaderFrame GetReaderFrame(bool dirty = false)
        {
            return ((Lucene29IndexingEngine)IndexManager.IndexingEngine).GetIndexReaderFrame(dirty);
        }

        public Analyzer GetAnalyzer()
        {
            return LuceneSearchManager.GetAnalyzer();
        }

        public void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo)
        {
            LuceneSearchManager.IndexingInfo = indexingInfo;
        }

        //===================================================================================== Helper methods

        private static void StartMessaging()
        {
            // we have to start the message cluster here
            var dummy = DistributedApplication.Cache.Count;
            var dummy2 = DistributedApplication.ClusterChannel;
        }
    }
}
