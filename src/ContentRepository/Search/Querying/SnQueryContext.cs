using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search.Indexing;

// ReSharper disable once CheckNamespace
namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Implements a context for query execution.
    /// </summary>
    public class SnQueryContext : IQueryContext
    {
        /// <inheritdoc />
        public QuerySettings Settings { get; }
        /// <inheritdoc />
        public int UserId { get; }
        /// <inheritdoc />
        public IQueryEngine QueryEngine => SearchManager.SearchEngine.QueryEngine;
        /// <inheritdoc />
        public IMetaQueryEngine MetaQueryEngine => DataProvider.Current.MetaQueryEngine; //DB:??

        /// <summary>
        /// Initializes a new instance of the SnQueryContext.
        /// </summary>
        public SnQueryContext(QuerySettings settings, int userId)
        {
            Settings = settings ?? QuerySettings.Default;
            UserId = userId;
        }

        /// <inheritdoc />
        public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return SearchManager.GetPerFieldIndexingInfo(fieldName);
        }

        /// <summary>
        /// Creates a default context for the content query with the currently logged-in user.
        /// </summary>
        /// <returns></returns>
        public static IQueryContext CreateDefault()
        {
            return new SnQueryContext(QuerySettings.Default, AccessProvider.Current.GetCurrentUser().Id);
        }
    }
}
