using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.Search.Tests
{
    internal class SnQueryToStringVisitor : SnQueryVisitor
    {
        private StringBuilder _output = new StringBuilder();
        public string Output => _output.ToString();

        public override SnQueryNode VisitText(Text text)
        {
            _output.Append($"{text.FieldName}:{text.Value}");
            if (text.Boost.HasValue && text.Boost != 1.0d)
                _output.Append("^").Append(text.Boost);
            if (text.FuzzyValue.HasValue)
                _output.Append("~").Append(text.FuzzyValue);

            return base.VisitText(text);
        }
    }
}
