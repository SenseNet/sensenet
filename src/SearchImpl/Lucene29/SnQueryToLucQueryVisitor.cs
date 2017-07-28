using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Index;
using Lucene.Net.Search;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.Search.Lucene29
{
    internal class SnQueryToLucQueryVisitor : SnQueryVisitor
    {
        public Query Result { get; private set; }

        public override SnQueryPredicate Visit(SnQueryPredicate predicate)
        {
            var textPred = predicate as TextPredicate;
            if (textPred != null && textPred.Value == "asdf")
            {
                // only a mock
                Result = new TermQuery(new Term("_Text", "asdf"));
                return predicate;
            }
            throw new NotImplementedException(); //UNDONE:!!!!! implement visitor and delete this override
        }
    }
}
