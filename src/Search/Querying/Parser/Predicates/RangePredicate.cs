namespace SenseNet.Search.Querying.Parser.Predicates
{
    public class RangePredicate : SnQueryPredicate
    {
        public string FieldName { get; }
        public IndexValue Min { get; }
        public IndexValue Max { get; }
        public bool MinExclusive { get; }
        public bool MaxExclusive { get; }

        public RangePredicate(string fieldName, IndexValue min, IndexValue max, bool minExclusive, bool maxExclusive)
        {
            FieldName = fieldName;
            Min = min;
            Max = max;
            MinExclusive = minExclusive;
            MaxExclusive = maxExclusive;
        }
    }
}