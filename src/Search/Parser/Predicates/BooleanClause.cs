using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser.Predicates
{
    public enum Occurence { Default, Should, Must, MustNot }

    public class BooleanClause : SnQueryNode
    {
        public SnQueryNode Node { get; }
        public Occurence Occur { get; set; }

        public BooleanClause(SnQueryNode node, Occurence occur)
        {
            Node = node;
            Occur = occur;
        }
    }
}
