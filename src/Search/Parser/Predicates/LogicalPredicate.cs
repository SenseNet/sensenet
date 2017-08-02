using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser.Predicates
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
