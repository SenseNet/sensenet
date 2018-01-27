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
    /// <summary>
    /// Lucene29 indexing engine for a local environment. Works with a Lucene index stored in the file system.
    /// </summary>
    public class Lucene29LocalIndexingEngine : ILuceneIndexingEngine
    {
        internal IndexDirectory IndexDirectory => LuceneSearchManager.IndexDirectory;

        /// <summary>
        /// Gets the Lucene search manager instance that is responsible for indexing operations.
        /// </summary>
        public LuceneSearchManager LuceneSearchManager { get; }

        //===================================================================================== Constructors

        /// <summary>
        /// Initializes an instance of the Lucene29LocalIndexingEngine class. Needed for automatic type loading.
        /// </summary>
        public Lucene29LocalIndexingEngine() : this(null)
        {
            // default constructor is needed for automatic type loading
        }
        /// <summary>
        /// Initializes an instance of the Lucene29LocalIndexingEngine class.
        /// </summary>
        /// <param name="indexDirectory">File system directory for storing the index. 
        /// If not provided, <see cref="SearchManager.IndexDirectoryPath"/> will be used.</param>
        public Lucene29LocalIndexingEngine(IndexDirectory indexDirectory)
        {
            var indexDir = indexDirectory ?? new IndexDirectory(null, SearchManager.IndexDirectoryPath);

            LuceneSearchManager = new LuceneSearchManager(indexDir, Notification.NotificationSender); 

            SetEventhandlers();
        }

        private void SetEventhandlers()
        {
            // set up event handlers
            LuceneSearchManager.OnStarted += Startup;
            LuceneSearchManager.OnLockFileRemoved += StartMessaging;
        }

        //===================================================================================== IIndexingEngine implementation

        /// <summary>
        /// Returns false, because this is a local indexing engine.
        /// </summary>
        public bool IndexIsCentralized => false;
        /// <summary>
        /// Gets a value indicating whether the underlying Lucene search manager is running.
        /// </summary>
        public bool Running
        {
            get => LuceneSearchManager.Running;
            internal set => LuceneSearchManager.Running = value;
        }

        /// <inheritdoc />
        /// <summary>
        /// Starts the underlying Lucene search manager.
        /// </summary>
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

        /// <inheritdoc />
        /// <summary>
        /// Stops the underlying Lucene search manager.
        /// </summary>
        public void ShutDown()
        {
            //TODO: CommitState: maybe need to write the final state in the distributed environment.
            // IndexManager.GetCurrentIndexingActivityStatus()
            // WriteActivityStatusToIndex
            LuceneSearchManager.ShutDown();
        }

        /// <inheritdoc />
        public void ClearIndex()
        {
            LuceneSearchManager.ClearIndex();
        }

        /// <inheritdoc />
        public IndexingActivityStatus ReadActivityStatusFromIndex()
        {
            return LuceneSearchManager.ReadActivityStatusFromIndex();
        }

        /// <inheritdoc />
        public void WriteActivityStatusToIndex(IndexingActivityStatus state)
        {
            LuceneSearchManager.WriteActivityStatusToIndex(state);
        }

        /// <inheritdoc />
        public void WriteIndex(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, IEnumerable<IndexDocument> additions)
        {
            LuceneSearchManager.WriteIndex(deletions, updates, additions);
        }

        //===================================================================================== IndexReader

        private IndexReaderFrame GetIndexReaderFrame(bool dirty)
        {
            return LuceneSearchManager.GetIndexReaderFrame(dirty);
        }
        /// <summary>
        /// Gets an <see cref="IndexReaderFrame"/> from the indexing engine.
        /// </summary>
        /// <param name="dirty">Whether the reader should be reopened from the writer. Default is false.</param>
        public static IndexReaderFrame GetReaderFrame(bool dirty = false)
        {
            return ((Lucene29LocalIndexingEngine)IndexManager.IndexingEngine).GetIndexReaderFrame(dirty);
        }

        //===================================================================================== ILuceneIndexingEngine implementation

        /// <inheritdoc />
        public Analyzer GetAnalyzer()
        {
            return LuceneSearchManager.GetAnalyzer();
        }

        /// <inheritdoc />
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
