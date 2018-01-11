using System;
using System.Collections.Generic;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Tools;

namespace SenseNet.Search.Lucene29
{
    internal class Lucene29SearchEngine : ISearchEngine
    {
        private Lazy<IIndexingEngine> _indexingEngine = new Lazy<IIndexingEngine>(() =>
        {
            //UNDONE: get Lucene indexing engine (local or centralized) from configuration.
            return TypeResolver.CreateInstance("SenseNet.Search.Lucene29.Lucene29IndexingEngine") as IIndexingEngine;
        });
        
        public IIndexingEngine IndexingEngine
        {
            get { return _indexingEngine.Value; }
            internal set
            {
                _indexingEngine = new Lazy<IIndexingEngine>(() => value);
            }
        }

        private Lazy<IQueryEngine> _queryEngine = new Lazy<IQueryEngine>(() =>
        {
            //UNDONE: get Lucene query engine (local or centralized) from configuration.
            return TypeResolver.CreateInstance("SenseNet.Search.Lucene29.Lucene29QueryEngine") as IQueryEngine;
        });

        public IQueryEngine QueryEngine
        {
            get { return _queryEngine.Value; }
            internal set
            {
                _queryEngine = new Lazy<IQueryEngine>(() => value);
            }
        }

        static Lucene29SearchEngine()
        {
            Lucene.Net.Search.BooleanQuery.SetMaxClauseCount(100000);
        }

        private IDictionary<string, IndexFieldAnalyzer> _analyzers = new Dictionary<string, IndexFieldAnalyzer>();
        public IDictionary<string, IndexFieldAnalyzer> GetAnalyzers()
        {
            return _analyzers;
        }

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
