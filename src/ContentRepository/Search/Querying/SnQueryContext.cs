using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search.Indexing;

namespace SenseNet.Search.Querying
{
    public class SnQueryContext : IQueryContext
    {
        public QuerySettings Settings { get; }
        public int UserId { get; }
        public IQueryEngine QueryEngine => SearchManager.SearchEngine.QueryEngine;
        public IMetaQueryEngine MetaQueryEngine => DataProvider.Current.MetaQueryEngine;

        public SnQueryContext(QuerySettings settings, int userId)
        {
            Settings = settings;
            UserId = userId;
        }

        public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return SearchManager.GetPerFieldIndexingInfo(fieldName);
        }

        public static IQueryContext CreateDefault()
        {
            return new SnQueryContext(QuerySettings.Default, AccessProvider.Current.GetCurrentUser().Id);
        }
    }
}
