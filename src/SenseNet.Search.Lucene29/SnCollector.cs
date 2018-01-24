using Lucene.Net.Index;
using Lucene.Net.Search;

namespace SenseNet.Search.Lucene29
{
    internal interface ISnCollector
    {
        TopDocs TopDocs(int start);
    }

    internal class SnTopScoreDocCollector : TopScoreDocCollector, ISnCollector
    {
        private readonly SearchParams _searchParams;
        private readonly TopScoreDocCollector _wrapped;
        private int _docBase;

        public SnTopScoreDocCollector(int size, SearchParams searchParams)
        {
            _searchParams = searchParams;
            _wrapped = Create(size, false);
        }

        public override bool AcceptsDocsOutOfOrder()
        {
            return _wrapped.AcceptsDocsOutOfOrder();
        }
        public override void Collect(int doc)
        {
            var p = _searchParams;
            var document = p.searcher.Doc(doc + _docBase);
            if (p.executor.IsPermitted(document))
                _wrapped.Collect(doc);
        }
        public override void SetNextReader(IndexReader reader, int docBase)
        {
            _docBase = docBase;
            _wrapped.SetNextReader(reader, docBase);
        }
        public override void SetScorer(Scorer scorer)
        {
            _wrapped.SetScorer(scorer);
        }
        public override TopDocs TopDocs()
        {
            return _wrapped.TopDocs();
        }
        public override TopDocs TopDocs(int start)
        {
            return _wrapped.TopDocs(start);
        }
        public override TopDocs TopDocs(int start, int howMany)
        {
            return _wrapped.TopDocs(start, howMany);
        }
        public override TopDocs NewTopDocs(ScoreDoc[] results, int start)
        {
            return _wrapped.NewTopDocs(results, start);
        }
        public override void PopulateResults(ScoreDoc[] results, int howMany)
        {
            _wrapped.PopulateResults(results, howMany);
        }
    }

    internal class SnTopFieldCollector : TopFieldCollector, ISnCollector
    {
        private readonly SearchParams _searchParams;
        private readonly TopFieldCollector _wrapped;
        private int _docBase;

        public SnTopFieldCollector(int size, SearchParams searchParams, Sort sort)
        {
            _searchParams = searchParams;
            _wrapped = Create(sort, size, false, true, false, false);
        }

        public override void SetScorer(Scorer scorer)
        {
            _wrapped.SetScorer(scorer);
        }
        public override void Collect(int doc)
        {
            var p = _searchParams;
            var document = p.searcher.Doc(doc + _docBase);
            if (p.executor.IsPermitted(document))
                _wrapped.Collect(doc);
        }
        public override void SetNextReader(IndexReader reader, int docBase)
        {
            _docBase = docBase;
            _wrapped.SetNextReader(reader, docBase);
        }
        public override bool AcceptsDocsOutOfOrder()
        {
            return _wrapped.AcceptsDocsOutOfOrder();
        }
        public override int GetTotalHits()
        {
            return _wrapped.GetTotalHits();
        }
        public override TopDocs NewTopDocs(ScoreDoc[] results, int start)
        {
            return _wrapped.NewTopDocs(results, start);
        }
        public override void PopulateResults(ScoreDoc[] results, int howMany)
        {
            _wrapped.PopulateResults(results, howMany);
        }
        public override TopDocs TopDocs()
        {
            return _wrapped.TopDocs();
        }
        public override TopDocs TopDocs(int start)
        {
            return _wrapped.TopDocs(start);
        }
        public override TopDocs TopDocs(int start, int howMany)
        {
            return _wrapped.TopDocs(start, howMany);
        }
    }
}
