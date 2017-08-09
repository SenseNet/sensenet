using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search
{
    internal class QueryExecutor20131012CountOnly : LuceneQueryExecutor //UNDONE: move to Luc29 implementation
    {
        protected override SearchResult DoExecute(SearchParams p)
        {
            p.skip = 0;

            var maxtop = p.numDocs;
            if (maxtop < 1)
                return SearchResult.Empty;

            SearchResult r = null;
            var defaultTop = p.numDocs;

            p.howMany = defaultTop;
            p.useHowMany = false;
            var maxSize = p.numDocs;
            p.collectorSize = 1;

            r = Search(p);

            return r;
        }
        protected override void GetResultPage(ScoreDoc[] hits, SearchParams p, SearchResult r)
        {
            // do nothing
        }
    }
}
