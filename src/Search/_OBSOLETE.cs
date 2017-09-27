using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search.Parser;

namespace SenseNet.Search
{
    public enum IndexableDataType { String, Int, Long, Float, Double }

    public class QueryCompilerValue
    {
        public string StringValue { get; private set; }

        public IndexableDataType Datatype { get; private set; }
        public int IntValue { get; private set; }
        public long LongValue { get; private set; }
        public float SingleValue { get; private set; }
        public double DoubleValue { get; private set; }

        public QueryCompilerValue(string text)
        {
            Datatype = IndexableDataType.String;
            StringValue = text;
        }

        public void Set(int value)
        {
            Datatype = IndexableDataType.Int;
            IntValue = value;
        }
        public void Set(long value)
        {
            Datatype = IndexableDataType.Long;
            LongValue = value;
        }
        public void Set(float value)
        {
            Datatype = IndexableDataType.Float;
            SingleValue = value;
        }
        public void Set(double value)
        {
            Datatype = IndexableDataType.Double;
            DoubleValue = value;
        }
        public void Set(string value)
        {
            Datatype = IndexableDataType.String;
            StringValue = value;
        }

        public override string ToString()
        {
            return $"{StringValue} ({Datatype})";
        }
    }

}
