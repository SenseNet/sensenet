using System.Runtime.Serialization;

namespace SenseNet.Search.Querying.Parser.Predicates
{
    /// <summary>
    /// Defines a range predicate inspired by Lucene query syntax.
    /// </summary>
    [DataContract]
    public class RangePredicate : SnQueryPredicate
    {
        /// <summary>
        /// Gets the field name of the predicate.
        /// </summary>
        [DataMember]
        public string FieldName { get; private set; }

        /// <summary>
        /// Gets the minimum value of the range. It can be null.
        /// </summary>
        [DataMember]
        public IndexValue Min { get; private set; }

        /// <summary>
        /// Gets the maximum value of the range. It can be null.
        /// </summary>
        [DataMember]
        public IndexValue Max { get; private set; }

        /// <summary>
        /// Gets the value that is true if the minimum value is in the range.
        /// </summary>
        [DataMember]
        public bool MinExclusive { get; private set; }

        /// <summary>
        /// Gets the value that is true if the maximum value is in the range.
        /// </summary>
        [DataMember]
        public bool MaxExclusive { get; private set; }

        /// <summary>
        /// Initializes a new instance of RangePredicate.
        /// </summary>
        public RangePredicate(string fieldName, IndexValue min, IndexValue max, bool minExclusive, bool maxExclusive)
        {
            FieldName = fieldName;
            Min = min;
            Max = max;
            MinExclusive = minExclusive;
            MaxExclusive = maxExclusive;
        }

        /// <summary>Returns a string that represents the current object.</summary>
        public override string ToString()
        {
            string oneTerm = null;

            string op = null;
            if (Min == null)
            {
                op = !MaxExclusive ? "<=" : "<";
                oneTerm = Max.ValueAsString;
            }
            if (Max == null)
            {
                op = !MinExclusive ? ">=" : ">";
                oneTerm = Min.ValueAsString;
            }

            if (op != null)
                return $"{FieldName}:{op}{oneTerm}";

            var start = !MinExclusive ? '[' : '{';
            var end = !MaxExclusive ? ']' : '}';
            return $"{FieldName}:{start}{Min} TO {Max}{end}";
        }
    }
}
