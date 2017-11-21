using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search.Indexing;

namespace SenseNet.Search.Querying
{
    //UNDONE:!!!! XMLDOC ContentRepository
    public class SnQueryContext : IQueryContext
    {
        //UNDONE:!!!! XMLDOC ContentRepository
        public QuerySettings Settings { get; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public int UserId { get; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public IQueryEngine QueryEngine => SearchManager.SearchEngine.QueryEngine;
        //UNDONE:!!!! XMLDOC ContentRepository
        public IMetaQueryEngine MetaQueryEngine => DataProvider.Current.MetaQueryEngine;

        //UNDONE:!!!! XMLDOC ContentRepository
        public SnQueryContext(QuerySettings settings, int userId)
        {
            Settings = settings;
            UserId = userId;
        }

        //UNDONE:!!!! XMLDOC ContentRepository
        public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return SearchManager.GetPerFieldIndexingInfo(fieldName);
        }

        //UNDONE:!!!! XMLDOC ContentRepository
        public static IQueryContext CreateDefault()
        {
            return new SnQueryContext(QuerySettings.Default, AccessProvider.Current.GetCurrentUser().Id);
        }
    }
}
