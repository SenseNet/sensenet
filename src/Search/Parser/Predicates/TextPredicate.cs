using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser.Predicates
{
    public class TextPredicate : SnQueryPredicate
    {
        public string FieldName { get; }
        public string Value { get; }
        public double? FuzzyValue { get; }

        public TextPredicate(string fieldName, string value, double? fuzzyValue = null)
        {
            FieldName = fieldName;
            Value = value;
            FuzzyValue = fuzzyValue;
        }
    }
}
