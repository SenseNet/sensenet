using SenseNet.ContentRepository.Storage;
using SenseNet.Search.Parser;

namespace SenseNet.Search
{
    public class SnQueryContext : IQueryContext
    {
        public QuerySettings Settings { get; }
        public int UserId { get; }
        public IQueryEngine QueryEngine => StorageContext.Search.SearchEngine.QueryEngine;

        public SnQueryContext(QuerySettings settings, int userId)
        {
            Settings = settings;
            UserId = userId;
        }

        public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(fieldName);
        }
    }
}
