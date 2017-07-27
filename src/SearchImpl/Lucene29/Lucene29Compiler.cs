using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Search;
using SenseNet.Search.Parser;

namespace SenseNet.Search.Lucene29
{
    internal class Lucene29Compiler
    {
        public LucQuery Compile(SnQuery snQuery)
        {
            var visitor = new SnQueryToLucQueryVisitor();
            visitor.Visit(snQuery.QueryTree);
            return LucQuery.Create(visitor.Result);
        }
    }

    internal class SnQueryToLucQueryVisitor : SnQueryVisitor
    {
        public Query Result { get; private set; }

        public override SnQueryPredicate Visit(SnQueryPredicate predicate)
        {
            throw new NotImplementedException(); //UNDONE:!!!!! implement visitor and delete this override
        }
    }
}
