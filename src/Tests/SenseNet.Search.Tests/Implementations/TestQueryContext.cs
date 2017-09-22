using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search.Parser;

namespace SenseNet.Search.Tests.Implementations
{
    public class TestQueryContext : IQueryContext
    {
        private IDictionary<string, IPerFieldIndexingInfo> _indexingInfo;

        public QuerySettings Settings { get; }
        public int UserId { get; }
        public IQueryEngine QueryEngine { get; }
        public IMetaQueryEngine MetaQueryEngine { get; }
        public bool AllVersions { get; set; } //UNDONE:!!!!! tusmester API: TEST: AllVersions: Move to QuerySettings.

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
