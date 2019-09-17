namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Defines query operations for increasing performance purposes.
    /// </summary>
    public interface IMetaQueryEngine
    {
        /// <summary>
        /// Returns with the permitted hit collection defined in the query.
        /// If there is any problem or the query is not executable in this compinent, returns with null.
        /// </summary>
        /// <param name="query">Defines the query.</param>
        /// <param name="filter">Supports mermission check methods.</param>
        /// <param name="context">Contains additional data required to execution.</param>
        /// <returns>
        /// Contains two properties:
        /// Hits: contains content identifier collection in the desired order defined in the query.
        /// TotalCount: if the CountAllPages of the query is false, the TotalCount need to be the count of Hits
        /// otherwise the count of hits without skip and top restrictions.
        /// </returns>
        QueryResult<int> TryExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context);
        /// <summary>
        /// Returns with the permitted hit collection defined in the query.
        /// Every hit is the matched content's field defined in the query's Projection property.
        /// If there is any problem or the query is not executable in this compinent, returns with null.
        /// </summary>
        /// <param name="query">Defines the query.</param>
        /// <param name="filter">Supports mermission check methods.</param>
        /// <param name="context">Contains additional data required to execution.</param>
        /// <returns>
        /// Contains two properties:
        /// Hits: string value collection of the content property values.
        /// Field name is defined in the query.Projection property.
        /// Order of hits is defined in the query.
        /// TotalCount: if the CountAllPages of the query is false, the TotalCount need to be the count of Hits
        /// otherwise the count of hits without skip and top restrictions.
        /// </returns>
        QueryResult<string> TryExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context);
    }

    /// <inheritdoc />
    /// <summary>
    /// Defines a class for a placeholder object that implements the IMetaQueryEngine interface.
    /// </summary>
    public class NullMetaQueryEngine : IMetaQueryEngine
    {
        /// <inheritdoc />
        public QueryResult<int> TryExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            return null;
        }
        /// <inheritdoc />
        public QueryResult<string> TryExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            return null;
        }
    }
}
