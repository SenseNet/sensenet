using System;
using System.Diagnostics;
using System.Globalization;

namespace SenseNet.Search
{
    [Serializable]
    [DebuggerDisplay("{ValueAsString}:{Type}")]
    public class IndexFieldValue
    {
        public const string Yes = "yes";
        public const string No = "no";

        public IndexFieldValue(string value) { Type = SnTermType.String; StringValue = value; ValueAsString = value; }
        public IndexFieldValue(string[] value) { Type = SnTermType.StringArray; StringArrayValue = value; ValueAsString = string.Join(",", value); }
        public IndexFieldValue(bool value) { Type = SnTermType.Bool; BooleanValue = value; ValueAsString = value ? Yes : No; }
        public IndexFieldValue(int value) { Type = SnTermType.Int; IntegerValue = value; ValueAsString = value.ToString(CultureInfo.InvariantCulture); }
        public IndexFieldValue(long value) { Type = SnTermType.Long; LongValue = value; ValueAsString = value.ToString(CultureInfo.InvariantCulture); }
        public IndexFieldValue(float value) { Type = SnTermType.Float; SingleValue = value; ValueAsString = value.ToString(CultureInfo.InvariantCulture); }
        public IndexFieldValue(double value) { Type = SnTermType.Double; DoubleValue = value; ValueAsString = value.ToString(CultureInfo.InvariantCulture); }
        public IndexFieldValue(DateTime value) { Type = SnTermType.DateTime; DateTimeValue = value; ValueAsString = value.ToString("yyyy-MM-dd HH:mm:ss.ffff"); }

        public SnTermType Type { get; }

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
}
