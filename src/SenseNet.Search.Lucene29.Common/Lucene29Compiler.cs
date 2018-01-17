using Lucene.Net.Analysis;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Lucene29
{
    internal class Lucene29Compiler
    {
        private readonly Analyzer _masterAnalyzer;

        public Lucene29Compiler(Analyzer masterAnalyzer = null)
        {
            _masterAnalyzer = masterAnalyzer ?? ((ILuceneIndexingEngine)IndexManager.IndexingEngine).GetAnalyzer();
        }

        public LucQuery Compile(SnQuery snQuery, IQueryContext context)
        {
            var visitor = new SnQueryToLucQueryVisitor(_masterAnalyzer, context);
            visitor.Visit(snQuery.QueryTree);

            var searchManager = ((ILuceneIndexingEngine) IndexManager.IndexingEngine).LuceneSearchManager;

            return LucQuery.Create(visitor.Result, searchManager);
        }
    }
}
