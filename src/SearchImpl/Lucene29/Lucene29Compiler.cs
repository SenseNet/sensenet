using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Parser;

namespace SenseNet.Search.Lucene29
{
    internal class Lucene29Compiler
    {
        public LucQuery Compile(SnQuery snQuery, IQueryContext context)
        {
            //var visitor = new SnQueryToLucQueryVisitor(new KeywordAnalyzer(), context); //UNDONE:!!! this is not the MasterAnalyzer
            var masterAnalyzer = new SnPerFieldAnalyzerWrapper(new KeywordAnalyzer());
            var visitor = new SnQueryToLucQueryVisitor(masterAnalyzer, context);
            visitor.Visit(snQuery.QueryTree);
            return LucQuery.Create(visitor.Result);
        }
    }
}
