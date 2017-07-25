using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser.Nodes
{
    public abstract class Range : SnQueryNode
    {
        public string FieldName { get; }
        public bool MinExclusive { get; }
        public bool MaxExclusive { get; }

        protected Range(string fieldName, bool minExclusive, bool maxExclusive)
        {
            FieldName = fieldName;
            MinExclusive = minExclusive;
            MaxExclusive = maxExclusive;
        }
    }

    public class TextRange : Range
    {
        public string Min { get; }
        public string Max { get; }

        public TextRange(string fieldName, string min, string max, bool minExclusive, bool maxExclusive) : base(fieldName, minExclusive, maxExclusive)
        {
            Min = min;
            Max = max;
        }
    }

    public class IntegerRange : Range
    {
        public int Min { get; }
        public int Max { get; }

        public IntegerRange(string fieldName, int min, int max, bool minExclusive, bool maxExclusive) : base(fieldName, minExclusive, maxExclusive)
        {
            Min = min;
            Max = max;
        }
    }
    public class LongRange : Range
    {
        public long Min { get; }
        public long Max { get; }

        public LongRange(string fieldName, long min, long max, bool minExclusive, bool maxExclusive) : base(fieldName, minExclusive, maxExclusive)
        {
            Min = min;
            Max = max;
        }
    }
    public class SingleRange : Range
    {
        public float Min { get; }
        public float Max { get; }

        public SingleRange(string fieldName, float min, float max, bool minExclusive, bool maxExclusive) : base(fieldName, minExclusive, maxExclusive)
        {
            Min = min;
            Max = max;
        }
    }
    public class DoubleRange : Range
    {
        public double Min { get; }
        public double Max { get; }

        public DoubleRange(string fieldName, double min, double max, bool minExclusive, bool maxExclusive) : base(fieldName, minExclusive, maxExclusive)
        {
            Min = min;
            Max = max;
        }
    }
}

