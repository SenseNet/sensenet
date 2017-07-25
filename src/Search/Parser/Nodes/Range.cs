using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser.Nodes
{
    internal class Range<T> : SnQueryNode
    {
        public string FieldName { get; }
        public T Min { get; }
        public T Max { get; }
        public bool MinExclusive { get; }
        public bool MaxExclusive { get; }

        public Range(string fieldName, T min, T max, bool minExclusive, bool maxExclusive)
        {
            FieldName = fieldName;
            Min = min;
            Max = max;
            MinExclusive = minExclusive;
            MaxExclusive = maxExclusive;
        }
    }
}
