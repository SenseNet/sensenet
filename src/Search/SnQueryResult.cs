using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search
{
    public class SnQueryResult
    {
        public IEnumerable<int> Identifiers { get; }
        public int TotalCount { get; }

        public SnQueryResult(IEnumerable<int> identifiers, int totalCount)
        {
            Identifiers = identifiers;
            TotalCount = totalCount;
        }
    }
}
