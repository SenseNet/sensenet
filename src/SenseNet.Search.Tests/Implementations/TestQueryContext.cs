using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search.Parser;

namespace SenseNet.Search.Tests.Implementations
{
    internal class TestQueryContext : IQueryContext
    {
        private IDictionary<string, IPerFieldIndexingInfo> _indexingInfo;

        public QuerySettings Settings { get; }
        public int UserId { get; }
        public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return _indexingInfo[fieldName];
        }

        public TestQueryContext(QuerySettings settings, int userId, IDictionary<string, IPerFieldIndexingInfo> indexingInfo)
        {
            Settings = settings;
            UserId = userId;
            _indexingInfo = indexingInfo;
        }
    }
}
