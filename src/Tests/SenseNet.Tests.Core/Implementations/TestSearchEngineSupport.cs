using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search;
using SenseNet.Search.Indexing;

namespace SenseNet.Tests.Core.Implementations
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
            return _indexingInfos.TryGetValue(fieldName, out var indexingInfo) ? indexingInfo : null;
        }

        public QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters)
        {
            throw new NotSupportedException();
        }

        public IIndexPopulator GetIndexPopulator()
        {
            return new DocumentPopulator();
        }

        public IndexDocument CompleteIndexDocument(IndexDocumentData indexDocumentData)
        {
            return IndexManager.CompleteIndexDocument(indexDocumentData);
        }
    }
}
