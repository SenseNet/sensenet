using System.Collections.Generic;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.InMemory
{
    public class InMemorySearchEngine : ISearchEngine
    {
        private IDictionary<string, IndexFieldAnalyzer> _analyzers = new Dictionary<string, IndexFieldAnalyzer>();

        public IIndexingEngine IndexingEngine { get; protected set; }

        public IQueryEngine QueryEngine { get; }

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
            IndexingEngine = new InMemoryIndexingEngine(this);
            QueryEngine = new InMemoryQueryEngine(this);
        }
    }
}
