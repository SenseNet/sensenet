using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Search.Parser
{
    internal class FieldNameVisitor : LucQueryVisitor
    {
        private List<string> _fieldNames = new List<string>();
        public IEnumerable<string> FieldNames { get { return _fieldNames; } }

        public override string VisitField(string field)
        {
            var visitedField = base.VisitField(field);
            if(!_fieldNames.Contains(visitedField))
                _fieldNames.Add(visitedField);
            return visitedField;
        }
    }
}
