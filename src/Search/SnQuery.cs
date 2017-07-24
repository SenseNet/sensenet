using System;

namespace SenseNet.Search
{

    public class SnQuery
    {
        private static IPermissionFilterFactory PermissionFilterFactory = new DefaultPermissionFilterFactory();
        private static IQueryEngineSelector QueryEngineSelector = new DefaultQueryEngineSelector();

        private static IQueryResult<int> Query(string queryText, QuerySettings settings, int userId)
        {
            var query = Create(queryText, settings);
            var engine = QueryEngineSelector.Select(query, settings);
            var permissionFilter = PermissionFilterFactory.Create(userId);
            return engine.ExecuteQuery(query, permissionFilter);
        }
        private static IQueryResult<string> Query(string queryText, QuerySettings settings, int userId, string projection)
        {
            var query = Create(queryText, settings);
            var engine = QueryEngineSelector.Select(query, settings);
            var permissionFilter = PermissionFilterFactory.Create(userId);
            return engine.ExecuteQuery(query, permissionFilter, projection);
        }

        public static SnQuery Create(string queryText, QuerySettings settings)
        {
            throw new NotImplementedException(); //UNDONE: implement SnQuery.Create(string queryText, QuerySettings settings)
        }
    }
}