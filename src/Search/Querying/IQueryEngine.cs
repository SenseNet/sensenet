namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Defines query operations for general purposes.
    /// </summary>
    public interface IQueryEngine
    {
        /// <summary>
        /// Returns with the permitted hit collection defined in the query.
        /// If there is any problem, throws an exception.
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
        IQueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context);
        /// <summary>
        /// Returns with the permitted hit collection defined in the query.
        /// Every hit is the matched content's field defined in the query's Projection property.
        /// If there is any problem, throws an exception.
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
        IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context);
    }
}
