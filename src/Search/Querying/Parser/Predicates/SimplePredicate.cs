namespace SenseNet.Search.Querying.Parser.Predicates
{
    public class SimplePredicate : SnQueryPredicate
    {
        public string FieldName { get; }
        public IndexValue Value { get; }
        public double? FuzzyValue { get; }

        public SimplePredicate(string fieldName, IndexValue value, double? fuzzyValue = null)
        {
            FieldName = fieldName;
            Value = value;
            FuzzyValue = fuzzyValue;
        }
    }
}
