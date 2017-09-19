using Lucene.Net.Search;

namespace SenseNet.Search.Lucene29.QueryExecutors
{
    internal class QueryExecutor20131012CountOnly : LuceneQueryExecutor
    {
        protected override SearchResult DoExecute(SearchParams p)
        {
            p.skip = 0;

            var maxtop = p.numDocs;
            if (maxtop < 1)
                return SearchResult.Empty;

            var defaultTop = p.numDocs;

            p.howMany = defaultTop;
            p.useHowMany = false;
            p.collectorSize = 1;

            var r = Search(p);

            return r;
        }
        protected override void GetResultPage(ScoreDoc[] hits, SearchParams p, SearchResult r)
        {
            // do nothing
        }
    }
}
