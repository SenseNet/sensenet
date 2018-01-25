using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using SenseNet.Diagnostics;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Lucene29.QueryExecutors
{
    internal abstract class LuceneQueryExecutor : IQueryExecutor
    {
        public IPermissionFilter PermissionChecker { get; private set; }
        public LucQuery LucQuery { get; private set; }
        private LuceneSearchManager LuceneSearchManager => LucQuery.LuceneSearchManager;

        public void Initialize(LucQuery lucQuery, IPermissionFilter permissionChecker)
        {
            LucQuery = lucQuery;
            PermissionChecker = permissionChecker;
        }

        public string QueryString => LucQuery.ToString();

        public int TotalCount { get; internal set; }


        public IEnumerable<LucObject> Execute()
        {
            using (var op = SnTrace.Query.StartOperation("LuceneQueryExecutor. CQL:{0}", LucQuery))
            {
                SearchResult result;

                var top = LucQuery.Top != 0 ? LucQuery.Top : LucQuery.PageSize;
                if (top == 0)
                    top = int.MaxValue;

                using (var readerFrame = LuceneSearchManager.GetIndexReaderFrame(LucQuery.QueryExecutionMode == QueryExecutionMode.Quick))
                {
                    var idxReader = readerFrame.IndexReader;
                    var searcher = new IndexSearcher(idxReader);

                    var searchParams = new SearchParams
                    {
                        query = LucQuery.Query,
                        top = top,
                        executor = this,
                        searcher = searcher,
                        numDocs = idxReader.NumDocs()
                    };

                    try
                    {
                        result = DoExecute(searchParams);
                    }
                    finally
                    {
                        if (searchParams.searcher != null)
                        {
                            searchParams.searcher.Close();
                            searchParams.searcher = null;
                        }
                    }
                }

                TotalCount = result.totalCount;

                op.Successful = true;
                return result.result;
            }
        }

        protected internal bool IsPermitted(Document doc)
        {
            var nodeId = Convert.ToInt32(doc.Get(IndexFieldName.NodeId));
            var isLastPublic = doc.Get(IndexFieldName.IsLastPublic) == IndexValue.Yes;
            var isLastDraft = doc.Get(IndexFieldName.IsLastDraft) == IndexValue.Yes;

            return PermissionChecker.IsPermitted(nodeId, isLastPublic, isLastDraft);
        }

        protected SearchResult Search(SearchParams p)
        {
            var r = new SearchResult(null);

            var collector = CreateCollector(p.collectorSize, p);
            p.searcher.Search(p.query, collector);

            TopDocs topDocs = GetTopDocs(collector, p);
            r.totalCount = topDocs.TotalHits;
            var hits = topDocs.ScoreDocs;

            GetResultPage(hits, p, r);

            return r;
        }

        protected Collector CreateCollector(int size, SearchParams searchParams)
        {
            if (LucQuery.HasSort)
                return new SnTopFieldCollector(size, searchParams, new Sort(LucQuery.SortFields));
            return new SnTopScoreDocCollector(size, searchParams);
        }
        protected TopDocs GetTopDocs(Collector collector, SearchParams p)
        {
            return ((ISnCollector)collector).TopDocs(p.skip);
        }

        protected abstract void GetResultPage(ScoreDoc[] hits, SearchParams p, SearchResult r);

        protected abstract SearchResult DoExecute(SearchParams p);
    }
}
