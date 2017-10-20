using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using SenseNet.Diagnostics;

namespace SenseNet.Search.Lucene29.QueryExecutors
{
    internal abstract class LuceneQueryExecutor : IQueryExecutor
    {
        public IPermissionFilter PermissionChecker { get; private set; }
        public LucQuery LucQuery { get; private set; }

        public void Initialize(LucQuery lucQuery, IPermissionFilter permissionChecker)
        {
            this.LucQuery = lucQuery;
            this.PermissionChecker = permissionChecker;
        }

        public string QueryString
        {
            get { return this.LucQuery.ToString(); }
        }

        public int TotalCount { get; internal set; }


        public IEnumerable<LucObject> Execute()
        {
            using (var op = SnTrace.Query.StartOperation("LuceneQueryExecutor. CQL:{0}", this.LucQuery))
            {
                SearchResult result = null;

                var top = this.LucQuery.Top != 0 ? this.LucQuery.Top : this.LucQuery.PageSize;
                if (top == 0)
                    top = int.MaxValue;

                using (var readerFrame = IndexReaderFrame.GetReaderFrame(this.LucQuery.QueryExecutionMode == QueryExecutionMode.Quick))
                {
                    var idxReader = readerFrame.IndexReader;
                    var searcher = new IndexSearcher(idxReader);

                    var searchParams = new SearchParams
                    {
                        query = this.LucQuery.Query,
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

                this.TotalCount = result.totalCount;

                op.Successful = true;
                return result.result;
            }
        }

        protected internal bool IsPermitted(Document doc)
        {
            var nodeId = Convert.ToInt32(doc.Get(IndexFieldName.NodeId));
            var isLastPublic = doc.Get(IndexFieldName.IsLastPublic) == SnTerm.Yes;
            var isLastDraft = doc.Get(IndexFieldName.IsLastDraft) == SnTerm.Yes;

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
            if (this.LucQuery.HasSort)
                return new SnTopFieldCollector(size, searchParams, new Sort(this.LucQuery.SortFields));
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
