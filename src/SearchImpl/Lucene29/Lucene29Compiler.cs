using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Lucene29
{
    internal class Lucene29Compiler
    {
        public LucQuery Compile(SnQuery snQuery, IQueryContext context)
        {
            var masterAnalyzer = ((Lucene29IndexingEngine) IndexManager.IndexingEngine).GetAnalyzer();
            var visitor = new SnQueryToLucQueryVisitor(masterAnalyzer, context);
            visitor.Visit(snQuery.QueryTree);
            return LucQuery.Create(visitor.Result);
        }
    }
}
