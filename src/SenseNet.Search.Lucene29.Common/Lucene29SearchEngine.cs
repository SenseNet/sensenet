using System;
using System.Collections.Generic;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Tools;

namespace SenseNet.Search.Lucene29
{
    /// <summary>
    /// Lucene29 search engine implementation. Loads the configured Lucene-specific query and indexing engines.
    /// </summary>
    internal class Lucene29SearchEngine : ISearchEngine
    {
        private Lazy<IIndexingEngine> _indexingEngine = new Lazy<IIndexingEngine>(() =>
            TypeResolver.CreateInstance(Configuration.Lucene29.Lucene29IndexingEngineClassName) as IIndexingEngine);
        
        /// <summary>
        /// Gets or sets the current indexing engine. Default value is determined by configuration.
        /// </summary>
        public IIndexingEngine IndexingEngine
        {
            get { return _indexingEngine.Value; }
            internal set { _indexingEngine = new Lazy<IIndexingEngine>(() => value); }
        }

        private Lazy<IQueryEngine> _queryEngine = new Lazy<IQueryEngine>(() =>
            TypeResolver.CreateInstance(Configuration.Lucene29.Lucene29QueryEngineClassName) as IQueryEngine);

        /// <summary>
        /// Gets or sets the current query engine. Default value is determined by configuration.
        /// </summary>
        public IQueryEngine QueryEngine
        {
            get { return _queryEngine.Value; }
            internal set { _queryEngine = new Lazy<IQueryEngine>(() => value); }
        }

        static Lucene29SearchEngine()
        {
            Lucene.Net.Search.BooleanQuery.SetMaxClauseCount(100000);
        }

        private IDictionary<string, IndexFieldAnalyzer> _analyzers = new Dictionary<string, IndexFieldAnalyzer>();

        /// <inheritdoc/>
        /// <summary>
        /// Returns the analyzers that were previously stored by the <see cref="SetIndexingInfo"/> method.
        /// </summary>
        public IDictionary<string, IndexFieldAnalyzer> GetAnalyzers()
        {
            return _analyzers;
        }

        /// <inheritdoc />
        /// <remarks>Passes indexinginfo to the underlying ILuceneIndexingEngine instance.</remarks>
        public void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo)
        {
            var analyzerTypes = new Dictionary<string, IndexFieldAnalyzer>();

            foreach (var item in indexingInfo)
            {
                var fieldName = item.Key;
                var fieldInfo = item.Value;
                if (fieldInfo.Analyzer != IndexFieldAnalyzer.Default)
                {
                    analyzerTypes.Add(fieldName, fieldInfo.Analyzer);
                }
            }

            _analyzers = analyzerTypes;

            // Indexing info is stored in memory in the indexing engine
            // and should be refreshed when the list changes.
            ((ILuceneIndexingEngine)IndexingEngine).SetIndexingInfo(indexingInfo);
        }
    }
}
