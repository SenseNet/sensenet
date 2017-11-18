using System.Collections.Generic;

namespace SenseNet.Search.Querying
{
    public interface IQueryResult<out T>
    {
        IEnumerable<T> Hits { get; }
        int TotalCount { get; }
    }

    /// <summary>
    /// Represens a result of the SnQuery execution.
    /// </summary>
    /// <typeparam name="T">Can be int or string.</typeparam>
    public class QueryResult<T> : IQueryResult<T>
    {
        /// <summary>
        /// Represents the empty result.
        /// </summary>
        public static QueryResult<T> Empty = new QueryResult<T>(new T[0], 0);

        /// <summary>
        /// Gets the resulted items.
        /// </summary>
        public IEnumerable<T> Hits { get; }

        /// <summary>
        /// Gets the total count if items.
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Initializes a new QueryResult instance.
        /// </summary>
        /// <param name="hits">The resulted items.</param>
        /// <param name="totalCount">Total count of items.</param>
        public QueryResult(IEnumerable<T> hits, int totalCount)
        {
            Hits = hits;
            TotalCount = totalCount;
        }
    }
}
