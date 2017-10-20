using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser.Predicates
{
    public enum Occurence { Default, Should, Must, MustNot }

    public class LogicalClause
    {
        public SnQueryPredicate Predicate { get; }
        public Occurence Occur { get; set; }

        public LogicalClause(SnQueryPredicate predicate, Occurence occur)
        {
            Predicate = predicate;
            Occur = occur;
        }
    }
}
