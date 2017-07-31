//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace SenseNet.Search.Parser.Predicates
//{
//    public abstract class NumericPredicate : SnQueryPredicate
//    {
//        public string FieldName { get; }

//        protected NumericPredicate(string fieldName)
//        {
//            FieldName = fieldName;
//        }
//    }

//    public class IntegerNumberPredicate : NumericPredicate
//    {
//        public int Value { get; }
//        public IntegerNumberPredicate(string fieldName, int value) : base(fieldName)
//        {
//            Value = value;
//        }
//    }
//    public class LongNumberPredicate : NumericPredicate
//    {
//        public long Value { get; }
//        public LongNumberPredicate(string fieldName, long value) : base(fieldName)
//        {
//            Value = value;
//        }
//    }
//    public class SingleNumberPredicate : NumericPredicate
//    {
//        public float Value { get; }
//        public SingleNumberPredicate(string fieldName, float value) : base(fieldName)
//        {
//            Value = value;
//        }
//    }
//    public class DoubleNumberPredicate : NumericPredicate
//    {
//        public double Value { get; }
//        public DoubleNumberPredicate(string fieldName, double value) : base(fieldName)
//        {
//            Value = value;
//        }
//    }
//}
