using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Represens a result of the SnQuery execution.
    /// </summary>
    /// <typeparam name="T">Can be int or string.</typeparam>
    [DataContract]
    [KnownType(typeof(QueryResult<int>))]
    [KnownType(typeof(QueryResult<string>))]
    public class QueryResult<T>
    {
        /// <summary>
        /// Represents the empty result.
        /// </summary>
        public static QueryResult<T> Empty = new QueryResult<T>(new T[0], 0);

        /// <summary>
        /// Gets the resulted items.
        /// </summary>
        [DataMember]
        public IEnumerable<T> Hits { get; private set; }

        /// <summary>
        /// Gets the total count of permitted items without top and skip restrictions.
        /// </summary>
        [DataMember]
        public int TotalCount { get; private set; }

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
