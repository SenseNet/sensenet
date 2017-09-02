using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using SenseNet.BackgroundOperations;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal class TestSearchEngineSupport : ISearchEngineSupport
    {
        private IDictionary<string, IPerFieldIndexingInfo> _indexingInfos;

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
            return true; //UNDONE: Partial solution
        }

        public bool TextExtractingWillBePotentiallySlow(IIndexableField field)
        {
            throw new NotSupportedException();
        }

        public string ReplaceQueryTemplates(string queryText)
        {
            throw new NotSupportedException();
        }

        public T GetSettingsValue<T>(string key, T defaultValue)
        {
            throw new NotSupportedException();
        }

        public QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters)
        {
            throw new NotSupportedException();
        }
    }
}
