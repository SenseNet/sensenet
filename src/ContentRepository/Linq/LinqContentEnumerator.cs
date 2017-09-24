using System.Collections.Generic;
using System.Linq;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Linq
{
    public class LinqContentEnumerator<T> : IEnumerator<T>
    {
        private ContentSet<T> _queryable;
        private IEnumerable<T> _result;
        private IEnumerator<T> _resultEnumerator;
        private SnQuery _query;
        private IQueryContext _queryContext;
        private bool _isContent;

        private IQueryContext QueryContext => _queryContext ?? (_queryContext = SnQueryContext.CreateDefault());

        public LinqContentEnumerator(ContentSet<T> queryable)
        {
            _isContent = typeof(T) == typeof(Content);
            _queryable = queryable;
        }

        public void Dispose()
        {
        }
        public T Current
        {
            get { return _resultEnumerator.Current; }
        }
        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }
        public void Reset()
        {
            _queryContext = null;
            _resultEnumerator.Reset();
        }
        public bool MoveNext()
        {
            if (_result == null)
            {
                Compile();
                var qresult = _query.Execute(QueryContext); //UNDONE:!!! LINQ: Use SnQuery instead of LucQuery

                var nresult = new Storage.NodeList<Storage.Node>(qresult.Hits.ToArray());
                if (_isContent)
                    _result = (IEnumerable<T>)nresult.Where(n => n != null).Select(Content.Create);
                else
                    _result = nresult.Where(n => n != null).Cast<T>();

                _resultEnumerator = _result.GetEnumerator();
            }
            return _resultEnumerator.MoveNext();
        }
        private void Compile()
        {
            if (_query == null)
                _query = SnExpression.BuildQuery(_queryable.Expression, typeof(T), _queryable.ContextPath, _queryable.ChildrenDefinition);
        }
        public string GetQueryText()
        {
            Compile();
            return _query.ToString();
        }

    }
}
