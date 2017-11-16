using SenseNet.LuceneSearch;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Lucene29
{
    internal class Lucene29Compiler
    {
        public LucQuery Compile(SnQuery snQuery, IQueryContext context)
        {
            var masterAnalyzer = new SnPerFieldAnalyzerWrapper();
            var visitor = new SnQueryToLucQueryVisitor(masterAnalyzer, context);
            visitor.Visit(snQuery.QueryTree);
            return LucQuery.Create(visitor.Result);
        }
    }
}
