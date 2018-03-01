using System.Collections.Generic;

namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Represens a result of the SnQuery execution.
    /// </summary>
    /// <typeparam name="T">Can be int or string.</typeparam>
    public class QueryResult<T>
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
        /// Gets the total count of permitted items without top and skip restrictions.
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
