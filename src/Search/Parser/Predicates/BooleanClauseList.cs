using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser.Predicates
{
    public class BooleanClauseList : SnQueryPredicate
    {
        public List<BooleanClause> Clauses { get; } = new List<BooleanClause>();
    }
}
