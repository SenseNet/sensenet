using System.Collections.Generic;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Lucene29
{
    internal class Lucene29SearchEngine : ISearchEngine
    {
        public IIndexingEngine IndexingEngine { get; internal set; } = new Lucene29IndexingEngine();

        public IQueryEngine QueryEngine { get; } = new Lucene29QueryEngine();

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
        }
    }
}
