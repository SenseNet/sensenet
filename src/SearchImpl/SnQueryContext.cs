using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Search
{
    public class SnQueryContext : IQueryContext
    {
        public QuerySettings Settings { get; }
        public int UserId { get; }
        public IQueryEngine QueryEngine => StorageContext.Search.SearchEngine.QueryEngine;
        public IMetaQueryEngine MetaQueryEngine => DataProvider.Current.MetaQueryEngine;
        public bool AllVersions { get; set; } //UNDONE:!!!!! tusmester API: TEST: AllVersions: Move to QuerySettings.

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
