using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Lucene29
{
    public class Lucene29IndexingEngine : ILuceneIndexingEngine
    {
        internal IndexDirectory IndexDirectory => LuceneSearchManager.IndexDirectory;

        public LuceneSearchManager LuceneSearchManager { get; }

        //===================================================================================== Constructors

        public Lucene29IndexingEngine() : this(null)
        {
            // default constructor is needed for automatic type loading
        }
        public Lucene29IndexingEngine(IndexDirectory indexDirectory)
        {
            var indexDir = indexDirectory ?? new IndexDirectory(null, SearchManager.IndexDirectoryPath);

            LuceneSearchManager = new LuceneSearchManager(indexDir, Notification.NotificationSender); 

            SetEventhandlers();
        }

        //UNDONE: NOREF: find usages: Lucene29IndexingEngine constructor for forceReopenFrequency.
        public Lucene29IndexingEngine(TimeSpan forceReopenFrequency)
        {
            var indexDirectory = new IndexDirectory(null, SearchManager.IndexDirectoryPath);

            LuceneSearchManager = new LuceneSearchManager(indexDirectory, Notification.NotificationSender)
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

            // execute a warmup query
            SnQuery.Query("+Id:1", SnQueryContext.CreateDefault());
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

        public void WriteIndex(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, IEnumerable<IndexDocument> additions)
        {
            LuceneSearchManager.WriteIndex(deletions, updates, additions);
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

        //===================================================================================== ILuceneIndexingEngine implementation

        public Analyzer GetAnalyzer()
        {
            return LuceneSearchManager.GetAnalyzer();
        }
        
        public void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo)
        {
            var analyzers = indexingInfo.ToDictionary(kvp => kvp.Key, kvp => GetAnalyzer(kvp.Value));
            var indexFieldTypes = indexingInfo.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.IndexFieldHandler.IndexFieldType);

            LuceneSearchManager.SetIndexingInfo(analyzers, indexFieldTypes);
        }

        //===================================================================================== Helper methods

        internal static Analyzer GetAnalyzer(IPerFieldIndexingInfo pfii)
        {
            var analyzerToken = pfii.Analyzer == IndexFieldAnalyzer.Default
                ? pfii.IndexFieldHandler.GetDefaultAnalyzer()
                : pfii.Analyzer;

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (analyzerToken)
            {
                case IndexFieldAnalyzer.Keyword: return new KeywordAnalyzer();
                case IndexFieldAnalyzer.Standard: return new StandardAnalyzer(LuceneSearchManager.LuceneVersion);
                case IndexFieldAnalyzer.Whitespace: return new WhitespaceAnalyzer();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void StartMessaging()
        {
            // we have to start the message cluster here
            var dummy = DistributedApplication.Cache.Count;
            var dummy2 = DistributedApplication.ClusterChannel;
        }
    }
}
