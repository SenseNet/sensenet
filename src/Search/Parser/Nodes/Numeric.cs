using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser.Nodes
{
    internal class Numeric<T> : SnQueryNode
    {
        public string FieldName { get; }
        public T Value { get; }

        public Numeric(string fieldName, T value)
        {
            FieldName = fieldName;
            Value = value;
        }
    }
}
