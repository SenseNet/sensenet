using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Tests.Implementations
{
    public class InMemorySearchEngine : ISearchEngine
    {
        private IDictionary<string, Type> _analyzers = new Dictionary<string, Type>();
        private readonly InMemoryIndexingEngine _indexingEngine;
        private readonly InMemoryQueryEngine _queryEngine;

        public IIndexingEngine IndexingEngine => _indexingEngine;

        public IQueryEngine QueryEngine => _queryEngine;

        public IIndexPopulator GetPopulator()
        {
            return new DocumentPopulator();
        }

        public IDictionary<string, Type> GetAnalyzers()
        {
            return _analyzers;
        }

        public void SetIndexingInfo(object indexingInfo)
        {
            var allInfo = (Dictionary<string, PerFieldIndexingInfo>)indexingInfo;
            var analyzerTypes = new Dictionary<string, Type>();

            foreach (var item in allInfo)
            {
                var fieldName = item.Key;
                var fieldInfo = item.Value;
                if (fieldInfo.Analyzer != null)
                {
                    var analyzerType = TypeResolver.GetType(fieldInfo.Analyzer);
                    if (analyzerType == null)
                        throw new InvalidOperationException(String.Concat("Unknown analyzer: ", fieldInfo.Analyzer, ". Field: ", fieldName));
                    analyzerTypes.Add(fieldName, analyzerType);
                }
                _analyzers = analyzerTypes;
            }
        }


        public InMemorySearchEngine()
        {
            _indexingEngine = new InMemoryIndexingEngine();
            _queryEngine = new InMemoryQueryEngine(_indexingEngine.Index);
        }
    }
}
