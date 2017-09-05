using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Tools;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal class InMemorySearchEngine : ISearchEngine
    {
        public IIndexPopulator GetPopulator()
        {
            return new DocumentPopulator();
        }

        private IDictionary<string, Type> _analyzers = new Dictionary<string, Type>();
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

        public IIndexingEngine GetIndexingEngine() //UNDONE: not tested
        {
            return new InMemoryIndexingEngine();
        }

        public IQueryEngine GetQueryEngine()
        {
            throw new NotImplementedException();
        }
    }
}
