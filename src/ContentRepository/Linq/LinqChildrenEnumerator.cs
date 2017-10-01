using System.Collections.Generic;
using System.Linq;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Linq
{
    public class LinqChildrenEnumerator<T> : IEnumerator<T>
    {
        private ChildrenContentSet<T> _queryable;
        private IEnumerable<T> _result;
        private IEnumerator<T> _resultEnumerator;
        private SnQuery _query;
        private IQueryContext _queryContext;
        private bool _isContent;

        private IQueryContext QueryContext => _queryContext ?? (_queryContext = SnQueryContext.CreateDefault());

        public LinqChildrenEnumerator(ChildrenContentSet<T> queryable)
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
                var qresult = _query.Execute(QueryContext);
                if (_isContent)
                    _result = (IEnumerable<T>)qresult.Hits.Select(Content.Load);
                else
                    _result = (IEnumerable<T>)qresult.Hits.Select(Storage.Node.LoadNode);
                _resultEnumerator = _result.GetEnumerator();
            }
            return _resultEnumerator.MoveNext();
        }
        private void Compile()
        {
            if (_query == null)
            {
                var query = SnExpression.BuildQuery(_queryable.Expression, typeof(T), _queryable.ContextPath, _queryable.ChildrenDefinition);
                query.AddAndClause(SnExpression.GetPathPredicate(_queryable.ContextPath, _queryable.SubTree));
                _query = query;
            }
        }
        public string GetQueryText()
        {
            Compile();
            return _query.ToString();
        }

    }
}
