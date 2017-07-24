using System;

namespace SenseNet.Search
{
    public interface IQueryEngine
    {
        bool CanCheckPermission { get; }
        SnQueryResult Execute(SnQuery query, QuerySettings settings, int userId);
    }

    public class SnQuery
    {
        public static SnQueryResult Query(string queryText, QuerySettings settings, int userId)
        {
            var query = Create(queryText, settings);
            var executor = ChooseQueryExecutor(query, settings);
            return executor.Execute(query, settings, userId);
        }

        public static SnQuery Create(string queryText, QuerySettings settings)
        {
            throw new NotImplementedException(); //UNDONE: implement SnQuery.Create(string queryText, QuerySettings settings)
        }

        internal static IQueryEngine ChooseQueryExecutor(SnQuery query, QuerySettings settings)
        {
            throw new NotImplementedException(); //UNDONE: implement SnQuery.ChooseQueryExecutor(string queryText, QuerySettings settings)
        }
    }
}