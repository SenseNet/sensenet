﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace SenseNet.Search
{
    /// <summary>
    /// Specifies the value types in the indexing and querying
    /// </summary>
    public enum IndexValueType
    {
        /// <summary>Represents a System.String value.</summary>
        String,
        /// <summary>Represents an array of System.String values.</summary>
        StringArray,
        /// <summary>Represents a System.Boolean value.</summary>
        Bool,
        /// <summary>Represents a System.Int32 value.</summary>
        Int,
        /// <summary>Represents an array of System.Int32 values.</summary>
        IntArray,
        /// <summary>Represents a System.Int64 value.</summary>
        Long,
        /// <summary>Represents a System.Single value.</summary>
        Float,
        /// <summary>Represents a System.Double value.</summary>
        Double,
        /// <summary>Represents a System.DateTime value.</summary>
        DateTime
    }

    /// <summary>
    /// Defines a universal atomic data in the indexing and querying.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{ValueAsString}:{Type}")]
    public class IndexValue : IComparable<IndexValue>, IComparable
    {
        /// <summary>
        /// Contains all values that mean "true". These are: "1", "true", "y" and "yes"
        /// </summary>
        public static IReadOnlyList<string> YesList =
            new List<string>(new[] { "1", "true", "y", IndexValue.Yes }).AsReadOnly();
        /// <summary>
        /// Contains all values that mean "false". These are: "0", "false", "n" and "no"
        /// </summary>
        public static IReadOnlyList<string> NoList =
            new List<string>(new[] { "0", "false", "n", IndexValue.No }).AsReadOnly();

        /// <summary>
        /// Generalized value of the "true" used in indexing and querying.
        /// </summary>
        public const string Yes = "yes";
        /// <summary>
        /// Generalized value of the "false" used in indexing and querying.
        /// </summary>
        public const string No = "no";


        /// <summary>
        /// Initializes an instance of the IndexValue with a System.String value.
        /// </summary>
        /// <param name="value">System.String value</param>
        public IndexValue(string value) { Type = IndexValueType.String; StringValue = value; ValueAsString = value; }

        /// <summary>
        /// Initializes an instance of the IndexValue with an array of System.String value.
        /// </summary>
        /// <param name="value">Array of System.String value</param>
        public IndexValue(string[] value) { Type = IndexValueType.StringArray; StringArrayValue = value; ValueAsString = string.Join(",", value); }

        /// <summary>
        /// Initializes an instance of the IndexValue with a System.Boolean value.
        /// </summary>
        /// <param name="value">System.Boolean value</param>
        public IndexValue(bool value) { Type = IndexValueType.Bool; BooleanValue = value; ValueAsString = value ? Yes : No; }

        /// <summary>
        /// Initializes an instance of the IndexValue with a System.Int32 value.
        /// </summary>
        /// <param name="value">System.Int32 value</param>
        public IndexValue(int value) { Type = IndexValueType.Int; IntegerValue = value; ValueAsString = value.ToString(CultureInfo.InvariantCulture); }

        /// <summary>
        /// Initializes an instance of the IndexValue with an array of System.Int32.
        /// </summary>
        /// <param name="value">System.Int32 value</param>
        public IndexValue(int[] value) { Type = IndexValueType.IntArray; IntegerArrayValue = value; ValueAsString = string.Join(",", value.Select(x=>x.ToString())); }

        /// <summary>
        /// Initializes an instance of the IndexValue with a System.Int64 value.
        /// </summary>
        /// <param name="value">System.Int64 value</param>
        public IndexValue(long value) { Type = IndexValueType.Long; LongValue = value; ValueAsString = value.ToString(CultureInfo.InvariantCulture); }

        /// <summary>
        /// Initializes an instance of the IndexValue with a System.Single value.
        /// </summary>
        /// <param name="value">System.Single value</param>
        public IndexValue(float value) { Type = IndexValueType.Float; SingleValue = value; ValueAsString = value.ToString(CultureInfo.InvariantCulture); }

        /// <summary>
        /// Initializes an instance of the IndexValue with a System.Double value.
        /// </summary>
        /// <param name="value">System.Double value</param>
        public IndexValue(double value) { Type = IndexValueType.Double; DoubleValue = value; ValueAsString = value.ToString(CultureInfo.InvariantCulture); }

        /// <summary>
        /// Initializes an instance of the IndexValue with a System.DateTime value.
        /// </summary>
        /// <param name="value">System.DateTime value</param>
        public IndexValue(DateTime value) { Type = IndexValueType.DateTime; DateTimeValue = value; ValueAsString = value.ToString("yyyy-MM-dd HH:mm:ss.ffff"); }


        /// <summary>
        /// Gets the type of the value.
        /// </summary>
        public IndexValueType Type { get; }

        /// <summary>
        /// Gets the System.String value of the instance if the Type is IndexValueType.String, otherwise null.
        /// </summary>
        public virtual string StringValue { get; }
        /// <summary>
        /// Gets the array of System.String value of the instance if the Type is IndexValueType.StringArray, otherwise null.
        /// </summary>
        public virtual string[] StringArrayValue { get; }
        /// <summary>
        /// Gets the System.Boolean value of the instance if the Type is IndexValueType.Bool, otherwise false.
        /// </summary>
        public virtual bool BooleanValue { get; }
        /// <summary>
        /// Gets the System.Int32 value of the instance if the Type is IndexValueType.Int, otherwise 0.
        /// </summary>
        public virtual int IntegerValue { get; }
        /// <summary>
        /// Gets an array of System.Int32 values of the instance if the Type is IndexValueType.IntArray, otherwise null.
        /// </summary>
        public virtual int[] IntegerArrayValue { get; }
        /// <summary>
        /// Gets the System.Int64 value of the instance if the Type is IndexValueType.Long, otherwise 0l.
        /// </summary>
        public virtual long LongValue { get; }
        /// <summary>
        /// Gets the System.Single value of the instance if the Type is IndexValueType.Float, otherwise 0f.
        /// </summary>
        public virtual float SingleValue { get; }
        /// <summary>
        /// Gets the System.Double value of the instance if the Type is IndexValueType.Double, otherwise 0d.
        /// </summary>
        public virtual double DoubleValue { get; }
        /// <summary>
        /// Gets the System.DateTime value of the instance if the Type is IndexValueType.DateTime, otherwise DateTime.MinValue.
        /// </summary>
        public virtual DateTime DateTimeValue { get; }

        /// <summary>
        /// Gets the type-independent string representation of the value.
        /// </summary>
        public string ValueAsString { get; }

        private static string[] _typesStrings = {"S", "S[]", "B", "I", "I[]", "L", "F", "D", "T"};
        public override string ToString()
        {
            var type = _typesStrings[(int)Type];
            string value;
            switch (Type)
            {
                case IndexValueType.String:
                    value = StringValue;
                    break;
                case IndexValueType.StringArray:
                    throw new NotSupportedException();
                case IndexValueType.Bool:
                    value = BooleanValue ? IndexValue.Yes : IndexValue.No;
                    break;
                case IndexValueType.Int:
                    value = IntegerValue.ToString(CultureInfo.InvariantCulture);
                    break;
                case IndexValueType.IntArray:
                    //throw new NotSupportedException();
                    //UNDONE: Investigate this modification because it can cause deserialization- or parsing errors.
                    value = $"[{string.Join(",", IntegerArrayValue.Select(x => x.ToString()))}]";
                    break;
                case IndexValueType.Long:
                    value = LongValue.ToString(CultureInfo.InvariantCulture);
                    break;
                case IndexValueType.Float:
                    value = SingleValue.ToString(CultureInfo.InvariantCulture);
                    break;
                case IndexValueType.Double:
                    value = DoubleValue.ToString(CultureInfo.InvariantCulture);
                    break;
                case IndexValueType.DateTime:
                    value = DateTimeValue.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return $"{value}:{type}";
        }

        public int CompareTo(IndexValue other)
        {
            if (ReferenceEquals(this, other))
                return 0;
            if (ReferenceEquals(null, other))
                return 1;
            var typeComparison = Type.CompareTo(other.Type);
            if (typeComparison != 0)
                return typeComparison;
            switch (Type)
            {
                case IndexValueType.String:
                    return string.Compare(StringValue, other.StringValue, StringComparison.Ordinal);
                case IndexValueType.StringArray:
                    return string.Compare(ValueAsString, other.ValueAsString, StringComparison.Ordinal);
                case IndexValueType.Bool:
                    return BooleanValue.CompareTo(other.BooleanValue);
                case IndexValueType.Int:
                    return IntegerValue.CompareTo(other.IntegerValue);
                case IndexValueType.IntArray:
                    return string.Compare(ValueAsString, other.ValueAsString, StringComparison.Ordinal);
                case IndexValueType.Long:
                    return LongValue.CompareTo(other.LongValue);
                case IndexValueType.Float:
                    return SingleValue.CompareTo(other.SingleValue);
                case IndexValueType.Double:
                    return DoubleValue.CompareTo(other.DoubleValue);
                case IndexValueType.DateTime:
                    return DateTimeValue.CompareTo(other.DateTimeValue);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is IndexValue other
                ? CompareTo(other)
                : throw new ArgumentException($"Object must be of type {nameof(IndexValue)}");
        }

        public static bool operator <(IndexValue left, IndexValue right)
        {
            return Comparer<IndexValue>.Default.Compare(left, right) < 0;
        }

        public static bool operator >(IndexValue left, IndexValue right)
        {
            return Comparer<IndexValue>.Default.Compare(left, right) > 0;
        }

        public static bool operator <=(IndexValue left, IndexValue right)
        {
            return Comparer<IndexValue>.Default.Compare(left, right) <= 0;
        }

        public static bool operator >=(IndexValue left, IndexValue right)
        {
            return Comparer<IndexValue>.Default.Compare(left, right) >= 0;
        }
    }
}
