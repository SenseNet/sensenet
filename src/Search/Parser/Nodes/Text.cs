using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser.Nodes
{
    //UNDONE: Can be term, fuzzy, prefix, wildcard, phrase query
    //UNDONE: Fuzzy is converted to Slope if the result is PhraseQuery
    internal class Text : SnQueryNode
    {
        public string FieldName { get; }
        public string Value { get; }
        public double? FuzzyValue { get; }

        public Text(string fieldName, string value, double? fuzzyValue = null)
        {
            FieldName = fieldName;
            Value = value;
            FuzzyValue = fuzzyValue;
        }
    }
}
