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

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal class TestSearchEngineSupport : ISearchEngineSupport
    {
        private readonly IDictionary<string, IPerFieldIndexingInfo> _indexingInfos;

        public TestSearchEngineSupport(IDictionary<string, IPerFieldIndexingInfo> indexingInfos)
        {
            _indexingInfos = indexingInfos;
        }

        public int[] GetNotIndexedNodeTypeIds()
        {
            throw new NotSupportedException();
        }

        public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            IPerFieldIndexingInfo indexingInfo;
            return _indexingInfos.TryGetValue(fieldName, out indexingInfo) ? indexingInfo : null;
        }

        public bool IsContentTypeIndexed(string contentTypeName)
        {
            return ContentType.GetByName(contentTypeName)?.IndexingEnabled ?? true;
        }

        public bool TextExtractingWillBePotentiallySlow(IIndexableField field)
        {
            throw new NotSupportedException();
        }

        public string ReplaceQueryTemplates(string queryText)
        {
            return TemplateManager.Replace(typeof(ContentQueryTemplateReplacer), queryText);
        }

        public T GetSettingsValue<T>(string key, T defaultValue)
        {
            throw new NotSupportedException();
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
