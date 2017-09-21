using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Linq
{
    public class LinqChildrenEnumerator<T> : IEnumerator<T>
    {
        private ChildrenContentSet<T> _queryable;
        private IEnumerable<T> _result;
        private IEnumerator<T> _resultEnumerator;
        private LucQuery _query;
        private bool _isContent;

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
            _resultEnumerator.Reset();
        }
        public bool MoveNext()
        {
            if (_result == null)
            {
                Compile();
                var qresult = _query.Execute(); //UNDONE:!!! LINQ: Use SnQuery instead of LucQuery
                if (_isContent)
                    _result = (IEnumerable<T>)qresult.Select(x => Content.Load(x.NodeId));
                else
                    _result = (IEnumerable<T>)qresult.Select(x => SenseNet.ContentRepository.Storage.Node.LoadNode(x.NodeId));
                _resultEnumerator = _result.GetEnumerator();
            }
            return _resultEnumerator.MoveNext();
        }
        private void Compile()
        {
            if (_query == null)
            {
                var q = LucQuery.Create(SnExpression.GetPathQuery(_queryable.ContextPath, _queryable.SubTree));
                var query = SnExpression.BuildQuery(_queryable.Expression, typeof(T), _queryable.ContextPath, _queryable.ChildrenDefinition);
                query.AddAndClause(q);
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
