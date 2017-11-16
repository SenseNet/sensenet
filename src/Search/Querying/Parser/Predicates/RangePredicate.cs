namespace SenseNet.Search.Querying.Parser.Predicates
{
    /// <summary>
    /// Defines a range predicate inspired by Lucene query syntax.
    /// </summary>
    public class RangePredicate : SnQueryPredicate
    {
        /// <summary>
        /// Gets the field name of the predicate.
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// Gets the minimum value of the range. It can be null.
        /// </summary>
        public IndexValue Min { get; }

        /// <summary>
        /// Gets the maximum value of the range. It can be null.
        /// </summary>
        public IndexValue Max { get; }

        /// <summary>
        /// Gets the value that is true if the minimum value is in the range.
        /// </summary>
        public bool MinExclusive { get; }

        /// <summary>
        /// Gets the value that is true if the maximum value is in the range.
        /// </summary>
        public bool MaxExclusive { get; }

        /// <summary>
        /// Initializes a new instance of the RangePredicate.
        /// </summary>
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