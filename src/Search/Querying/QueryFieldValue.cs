namespace SenseNet.Search.Querying
{
    public class QueryFieldValue
    {
        public string StringValue { get; }
        internal bool IsPhrase { get; private set; }
        internal double? FuzzyValue { get; set; }

        internal QueryFieldValue(string stringValue, bool isPhrase)
        {
            StringValue = stringValue;
            IsPhrase = isPhrase;
        }

        public override string ToString()
        {
            return string.Concat(StringValue, FuzzyValue == null ? "" : ":" + FuzzyValue);
        }

    }
}
