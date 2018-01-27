using Lucene.Net.Analysis;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Lucene29
{
    /// <summary>
    /// <see cref="SnQuery"/> to <see cref="LucQuery"/> compiler.
    /// </summary>
    internal class Lucene29Compiler
    {
        private readonly Analyzer _masterAnalyzer;

        public Lucene29Compiler(Analyzer masterAnalyzer = null)
        {
            _masterAnalyzer = masterAnalyzer ?? ((ILuceneIndexingEngine)IndexManager.IndexingEngine).GetAnalyzer();
        }

        /// <summary>
        /// Compiles an <see cref="SnQuery"/> to <see cref="LucQuery"/>.
        /// </summary>
        public LucQuery Compile(SnQuery snQuery, IQueryContext context)
        {
            var visitor = new SnQueryToLucQueryVisitor(_masterAnalyzer, context);
            visitor.Visit(snQuery.QueryTree);

            var searchManager = ((ILuceneIndexingEngine) IndexManager.IndexingEngine).LuceneSearchManager;

            return LucQuery.Create(visitor.Result, searchManager);
        }
    }
}
