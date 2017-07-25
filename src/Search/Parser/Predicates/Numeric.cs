using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser.Predicates
{
    public abstract class Numeric : SnQueryNode
    {
        public string FieldName { get; }

        protected Numeric(string fieldName)
        {
            FieldName = fieldName;
        }
    }

    public class IntegerNumber : Numeric
    {
        public int Value { get; }
        public IntegerNumber(string fieldName, int value) : base(fieldName)
        {
            Value = value;
        }
    }
    public class LongNumber : Numeric
    {
        public long Value { get; }
        public LongNumber(string fieldName, long value) : base(fieldName)
        {
            Value = value;
        }
    }
    public class SingleNumber : Numeric
    {
        public float Value { get; }
        public SingleNumber(string fieldName, float value) : base(fieldName)
        {
            Value = value;
        }
    }
    public class DoubleNumber : Numeric
    {
        public double Value { get; }
        public DoubleNumber(string fieldName, double value) : base(fieldName)
        {
            Value = value;
        }
    }
}
