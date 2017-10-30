using System.Collections.Generic;

namespace SenseNet.Search.Querying
{
    public interface IQueryResult<out T>
    {
        IEnumerable<T> Hits { get; }
        int TotalCount { get; }
    }

    public class QueryResult<T> : IQueryResult<T>
    {
        public static QueryResult<T> Empty = new QueryResult<T>(new T[0], 0);
        public IEnumerable<T> Hits { get; }
        public int TotalCount { get; }

        public QueryResult(IEnumerable<T> hits, int totalCount)
        {
            Hits = hits;
            TotalCount = totalCount;
        }
    }
}
