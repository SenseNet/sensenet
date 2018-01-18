using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using ContentType = SenseNet.ContentRepository.Schema.ContentType;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.Search.Parser;

namespace SenseNet.Tests.Implementations
{
    public class TestSearchEngineSupport : ISearchEngineSupport
    {
        private readonly IDictionary<string, IPerFieldIndexingInfo> _indexingInfos;

        public TestSearchEngineSupport(IDictionary<string, IPerFieldIndexingInfo> indexingInfos)
        {
            _indexingInfos = indexingInfos;
        }

        public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            IPerFieldIndexingInfo indexingInfo;
            return _indexingInfos.TryGetValue(fieldName, out indexingInfo) ? indexingInfo : null;
        }

        public QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters)
        {
            throw new NotSupportedException();
        }

        public IIndexPopulator GetIndexPopulator()
        {
            return new DocumentPopulator();
        }
    }
}
