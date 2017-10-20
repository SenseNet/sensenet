using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search
{
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
