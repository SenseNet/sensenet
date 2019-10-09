using System.Collections.Generic;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.InMemory
{
    public class InMemorySearchEngine : ISearchEngine
    {
        private IDictionary<string, IndexFieldAnalyzer> _analyzers = new Dictionary<string, IndexFieldAnalyzer>();
        private readonly InMemoryIndexingEngine _indexingEngine;
        private readonly InMemoryQueryEngine _queryEngine;

        public IIndexingEngine IndexingEngine => _indexingEngine;

        public IQueryEngine QueryEngine => _queryEngine;

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
                    analyzerTypes.Add(fieldName, fieldInfo.Analyzer);
            }

            _analyzers = analyzerTypes;
        }

        public InMemoryIndex Index { get; set; }
        public InMemorySearchEngine(InMemoryIndex index)
        {
            Index = index;
            _indexingEngine = new InMemoryIndexingEngine(this);
            _queryEngine = new InMemoryQueryEngine(this);
        }
    }
}
