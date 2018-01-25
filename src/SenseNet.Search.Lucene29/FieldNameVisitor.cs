using System.Collections.Generic;

namespace SenseNet.Search.Lucene29
{
    internal class FieldNameVisitor : LucQueryVisitor
    {
        private readonly List<string> _fieldNames = new List<string>();
        public IEnumerable<string> FieldNames => _fieldNames;

        public override string VisitField(string field)
        {
            var visitedField = base.VisitField(field);
            if(!_fieldNames.Contains(visitedField))
                _fieldNames.Add(visitedField);
            return visitedField;
        }
    }
}
