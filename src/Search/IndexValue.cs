using System;
using System.Diagnostics;
using System.Globalization;

namespace SenseNet.Search
{
    public enum IndexValueType { String, StringArray, Bool, Int, Long, Float, Double, DateTime }

    [Serializable]
    [DebuggerDisplay("{ValueAsString}:{Type}")]
    public class IndexValue
    {
        public const string Yes = "yes";
        public const string No = "no";

        public IndexValue(string value) { Type = IndexValueType.String; StringValue = value; ValueAsString = value; }
        public IndexValue(string[] value) { Type = IndexValueType.StringArray; StringArrayValue = value; ValueAsString = string.Join(",", value); }
        public IndexValue(bool value) { Type = IndexValueType.Bool; BooleanValue = value; ValueAsString = value ? Yes : No; }
        public IndexValue(int value) { Type = IndexValueType.Int; IntegerValue = value; ValueAsString = value.ToString(CultureInfo.InvariantCulture); }
        public IndexValue(long value) { Type = IndexValueType.Long; LongValue = value; ValueAsString = value.ToString(CultureInfo.InvariantCulture); }
        public IndexValue(float value) { Type = IndexValueType.Float; SingleValue = value; ValueAsString = value.ToString(CultureInfo.InvariantCulture); }
        public IndexValue(double value) { Type = IndexValueType.Double; DoubleValue = value; ValueAsString = value.ToString(CultureInfo.InvariantCulture); }
        public IndexValue(DateTime value) { Type = IndexValueType.DateTime; DateTimeValue = value; ValueAsString = value.ToString("yyyy-MM-dd HH:mm:ss.ffff"); }

        public IndexValueType Type { get; }

        public virtual string StringValue { get; }
        public virtual string[] StringArrayValue { get; }
        public virtual bool BooleanValue { get; }
        public virtual int IntegerValue { get; }
        public virtual long LongValue { get; }
        public virtual float SingleValue { get; }
        public virtual double DoubleValue { get; }
        public virtual DateTime DateTimeValue { get; }

        public string ValueAsString { get; }
    }

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
