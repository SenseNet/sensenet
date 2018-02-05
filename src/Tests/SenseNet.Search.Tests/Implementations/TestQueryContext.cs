using System.Collections.Generic;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Tests.Implementations
{
    internal class TestQueryContext : IQueryContext
    {
        private readonly IDictionary<string, IPerFieldIndexingInfo> _indexingInfo;

        public QuerySettings Settings { get; }
        public int UserId { get; }
        public IQueryEngine QueryEngine { get; }
        public IMetaQueryEngine MetaQueryEngine { get; }

        public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return _indexingInfo[fieldName];
        }

        public TestQueryContext(QuerySettings settings, int userId, IDictionary<string, IPerFieldIndexingInfo> indexingInfo, IQueryEngine queryEngine = null, IMetaQueryEngine metaQueryEngine = null)
        {
            Settings = settings;
            UserId = userId;
            _indexingInfo = indexingInfo;
            QueryEngine = queryEngine;
            MetaQueryEngine = metaQueryEngine;
        }
    }
}
