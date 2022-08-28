using System;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Defines query operations for general purposes.
    /// </summary>
    public interface IQueryEngine
    {
        /// <summary>
        /// Executes the query and returns the permitted hit collection.
        /// If there is any problem, throws an exception.
        /// </summary>
        /// <param name="query">Defines the query.</param>
        /// <param name="filter">Supports permission check methods.</param>
        /// <param name="context">Contains additional data required to execution.</param>
        /// <returns>
        /// Contains two properties:
        /// - Hits: contains content identifier collection in the desired order defined in the query.
        /// - TotalCount: if the CountAllPages of the query is false, the TotalCount need to be the count of Hits
        /// otherwise the count of hits without skip and top restrictions.
        /// </returns>
[Obsolete("###", true)]
        QueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context);
        /// <summary>
        /// Executes the query and returns the permitted hit collection.
        /// Every hit is the matched content's field defined in the query's Projection property.
        /// If there is any problem, throws an exception.
        /// </summary>
        /// <param name="query">Defines the query.</param>
        /// <param name="filter">Supports permission check methods.</param>
        /// <param name="context">Contains additional data required to execution.</param>
        /// <returns>
        /// Contains two properties:
        /// - Hits: string value collection of the content property values.
        /// - Field name is defined in the query.Projection property.
        /// Order of hits is defined in the query.
        /// TotalCount: if the CountAllPages of the query is false, the TotalCount need to be the count of Hits
        /// otherwise the count of hits without skip and top restrictions.
        /// </returns>
[Obsolete("###", true)]
        QueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context);

        /// <summary>
        /// Asynchronously executes the query and returns the permitted hit collection.
        /// If there is any problem, throws an exception.
        /// </summary>
        /// <param name="query">Defines the query.</param>
        /// <param name="filter">Supports permission check methods.</param>
        /// <param name="context">Contains additional data required to execution.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// A Task that represents the asynchronous operation and wraps the query result.
        /// The query result contains two properties:
        /// - Hits: contains content identifier collection in the desired order defined in the query.
        /// - TotalCount: if the CountAllPages of the query is false, the TotalCount need to be the count of Hits
        /// otherwise the count of hits without skip and top restrictions.
        /// </returns>
        Task<QueryResult<int>> ExecuteQueryAsync(SnQuery query, IPermissionFilter filter, IQueryContext context, CancellationToken cancel);
        /// <summary>
        /// Asynchronously executes the query and returns the permitted hit collection.
        /// Every hit is the matched content's field defined in the query's Projection property.
        /// If there is any problem, throws an exception.
        /// </summary>
        /// <param name="query">Defines the query.</param>
        /// <param name="filter">Supports permission check methods.</param>
        /// <param name="context">Contains additional data required to execution.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// A Task that represents the asynchronous operation and wraps the query result.
        /// The query result contains two properties:
        /// - Hits: string value collection of the content property values.
        /// - Field name is defined in the query.Projection property.
        /// Order of hits is defined in the query.
        /// TotalCount: if the CountAllPages of the query is false, the TotalCount need to be the count of Hits
        /// otherwise the count of hits without skip and top restrictions.
        /// </returns>
        Task<QueryResult<string>> ExecuteQueryAndProjectAsync(SnQuery query, IPermissionFilter filter, IQueryContext context, CancellationToken cancel);

    }
}
