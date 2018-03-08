using SenseNet.Search.Indexing;

namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Defines a context for query execution.
    /// </summary>
    public interface IQueryContext
    {
        /// <summary>
        /// Gets the current query extension values (top, skip, sort etc.).
        /// </summary>
        QuerySettings Settings { get; }

        /// <summary>
        /// Gets the logged in user's id.
        /// </summary>
        int UserId { get; }

        /// <summary>
        /// Gets the current IQueryEngine instance.
        /// </summary>
        IQueryEngine QueryEngine { get; }

        /// <summary>
        /// Gets the current IMetaQueryEngine instance.
        /// Not used in this release.
        /// </summary>
        IMetaQueryEngine MetaQueryEngine { get; }

        /// <summary>
        /// Returns a field indexing metadata by given fieldName.
        /// </summary>
        IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName);
    }
}
