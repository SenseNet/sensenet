using System.Collections.Generic;

namespace SenseNet.Search.Querying.Parser.Predicates
{
    public class LogicalPredicate : SnQueryPredicate
    {
        public List<LogicalClause> Clauses { get; } = new List<LogicalClause>();

        public LogicalPredicate() { }
        public LogicalPredicate(IEnumerable<LogicalClause> clauses)
        {
            Clauses.AddRange(clauses);
        }
    }
}
