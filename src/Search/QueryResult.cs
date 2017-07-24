using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search
{
    public class QueryResult<T> : IQueryResult<T>
    {
        public IEnumerable<T> Hits { get; }
        public int TotalCount { get; }

        public QueryResult(IEnumerable<T> identifiers, int totalCount)
        {
            Hits = identifiers;
            TotalCount = totalCount;
        }
    }
}
