using Lucene.Net.Documents;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using SenseNet.Configuration;

namespace SenseNet.Search
{
    internal class QueryExecutor20131012 : LuceneQueryExecutor //UNDONE: move to Luc29 implementation
    {
        protected override SearchResult DoExecute(SearchParams p)
        {
            p.skip = this.LucQuery.Skip;

            var maxtop = p.numDocs - p.skip;
            if (maxtop < 1)
                return SearchResult.Empty;

            SearchResult r = null;
            SearchResult r1 = null;

            var howManyList = new List<int>(Querying.DefaultTopAndGrowth);
            if (howManyList[howManyList.Count - 1] == 0)
                howManyList[howManyList.Count - 1] = int.MaxValue;

            if (p.top < int.MaxValue)
            {
                var howMany = p.top;
                if ((long)howMany > maxtop)
                    howMany = maxtop - p.skip;
                while (howManyList.Count > 0)
                {
                    if (howMany < howManyList[0])
                        break;
                    howManyList.RemoveAt(0);
                }
                howManyList.Insert(0, howMany);
            }

            var top0 = p.top;
            for (var i = 0; i < howManyList.Count; i++)
            {
                var defaultTop = howManyList[i];
                if (defaultTop == 0)
                    defaultTop = p.numDocs;

                p.howMany = defaultTop;
                p.useHowMany = i < howManyList.Count - 1;
                var maxSize = i == 0 ? p.numDocs : r.totalCount;
                p.collectorSize = Math.Min(defaultTop, maxSize - p.skip) + p.skip;

                r1 = this.Search(p);

                if (i == 0)
                    r = r1;
                else
                    r.Add(r1);
                p.skip += r.nextIndex;
                p.top = top0 - r.result.Count;

                if (r.result.Count == 0 || r.result.Count >= top0 || r.result.Count >= r.totalCount)
                    break;
            }
            return r;
        }
        protected override void GetResultPage(ScoreDoc[] hits, SearchParams p, SearchResult r)
        {
            var result = new List<LucObject>();
            if (hits.Length == 0)
            {
                r.result = result;
                return;
            }

            var upperBound = hits.Length;
            var index = 0;
            while (true)
            {
                Document doc = p.searcher.Doc(hits[index].Doc);
                result.Add(new LucObject(doc));
                if (result.Count == p.top)
                {
                    index++;
                    break;
                }
                if (++index >= upperBound)
                    break;
            }
            r.nextIndex = index;
            r.result = result;
        }
    }
}
