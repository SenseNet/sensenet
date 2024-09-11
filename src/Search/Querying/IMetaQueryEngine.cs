using System;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Defines query operations for increasing performance purposes.
    /// </summary>
    public interface IMetaQueryEngine
    {
        /// <summary>
        /// Executes the query and returns the permitted hit collection.
        /// If there is any problem or the query is not executable in this component, returns null.
        /// </summary>
        /// <param name="query">Defines the query.</param>
        /// <param name="filter">Supports permission check methods.</param>
        /// <param name="context">Contains additional data required to execution.</param>
        /// <returns>
        /// Contains two properties:
        /// Hits: contains content identifier collection in the desired order defined in the query.
        /// TotalCount: if the CountAllPages of the query is false, the TotalCount need to be the count of Hits
        /// otherwise the count of hits without skip and top restrictions.
        /// </returns>
        [Obsolete("Use async version instead", true)]
        QueryResult<int> TryExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context);
        /// <summary>
        /// Returns the permitted hit collection defined in the query.
        /// Every hit is the matched content's field defined in the query's Projection property.
        /// If there is any problem or the query is not executable in this component, returns null.
        /// </summary>
        /// <param name="query">Defines the query.</param>
        /// <param name="filter">Supports permission check methods.</param>
        /// <param name="context">Contains additional data required to execution.</param>
        /// <returns>
        /// Contains two properties:
        /// Hits: string value collection of the content property values.
        /// Field name is defined in the query.Projection property.
        /// Order of hits is defined in the query.
        /// TotalCount: if the CountAllPages of the query is false, the TotalCount need to be the count of Hits
        /// otherwise the count of hits without skip and top restrictions.
        /// </returns>
        [Obsolete("Use async version instead", true)]
        QueryResult<string> TryExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context);

        /// <summary>
        /// Asynchronously executes the query and returns the permitted hit collection.
        /// If there is any problem or the query is not executable in this component, returns null.
        /// </summary>
        /// <param name="query">Defines the query.</param>
        /// <param name="filter">Supports permission check methods.</param>
        /// <param name="context">Contains additional data required to execution.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// A Task that represents the asynchronous operation and wraps the query result.
        /// The query result is null or contains two properties:
        /// Hits: contains content identifier collection in the desired order defined in the query.
        /// TotalCount: if the CountAllPages of the query is false, the TotalCount need to be the count of Hits
        /// otherwise the count of hits without skip and top restrictions.
        /// </returns>
        Task<QueryResult<int>> TryExecuteQueryAsync(SnQuery query, IPermissionFilter filter, IQueryContext context, CancellationToken cancel);
        /// <summary>
        /// Asynchronously executes the query and returns the permitted hit collection.
        /// Every hit is the matched content's field defined in the query's Projection property.
        /// If there is any problem or the query is not executable in this component, returns null.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="filter"></param>
        /// <param name="context"></param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// A Task that represents the asynchronous operation and wraps the query result.
        /// The query result is null or contains two properties:
        /// Hits: string value collection of the content property values.
        /// Field name is defined in the query.Projection property.
        /// Order of hits is defined in the query.
        /// TotalCount: if the CountAllPages of the query is false, the TotalCount need to be the count of Hits
        /// otherwise the count of hits without skip and top restrictions.
        /// </returns>
        Task<QueryResult<string>> TryExecuteQueryAndProjectAsync(SnQuery query, IPermissionFilter filter, IQueryContext context, CancellationToken cancel);

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
        /// <inheritdoc />
        public Task<QueryResult<int>> TryExecuteQueryAsync(SnQuery query, IPermissionFilter filter, IQueryContext context, CancellationToken cancel)
        {
            return Task.FromResult((QueryResult<int>)null);
        }
        /// <inheritdoc />
        public Task<QueryResult<string>> TryExecuteQueryAndProjectAsync(SnQuery query, IPermissionFilter filter, IQueryContext context,
            CancellationToken cancel)
        {
            return Task.FromResult((QueryResult<string>)null);
        }
    }
}
