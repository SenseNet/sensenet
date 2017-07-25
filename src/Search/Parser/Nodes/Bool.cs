using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser.Nodes
{
    internal enum Occurence { Default, Should, Must, MustNot }

    internal class Bool : SnQueryNode
    {
        public SnQueryNode Node { get; }
        public Occurence Occur { get; set; }

        public Bool(SnQueryNode node, Occurence occur)
        {
            Node = node;
            Occur = occur;
        }
    }
}
