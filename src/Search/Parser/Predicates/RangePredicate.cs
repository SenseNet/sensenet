using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser.Predicates
{
    public class RangePredicate : SnQueryPredicate
    {
        public string FieldName { get; }
        public string Min { get; }
        public string Max { get; }
        public bool MinExclusive { get; }
        public bool MaxExclusive { get; }

        public RangePredicate(string fieldName, string min, string max, bool minExclusive, bool maxExclusive)
        {
            FieldName = fieldName;
            Min = min;
            Max = max;
            MinExclusive = minExclusive;
            MaxExclusive = maxExclusive;
        }
    }
}
