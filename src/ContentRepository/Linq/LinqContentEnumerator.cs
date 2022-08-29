using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.Linq
{
    public class LinqContentEnumerator<T> : IEnumerator<T>
    {
        private readonly ContentSet<T> _queryable;
        private IEnumerable<T> _result;
        private IEnumerator<T> _resultEnumerator;
        private SnQuery _query;
        private IQueryContext _queryContext;
        private readonly bool _isContent;

        private IQueryContext QueryContext => _queryContext ?? (_queryContext = SnQueryContext.CreateDefault());

        public LinqContentEnumerator(ContentSet<T> queryable)
        {
            _isContent = typeof(T) == typeof(Content);
            _queryable = queryable;
        }

        public void Dispose()
        {
        }
        public T Current => _resultEnumerator.Current;

        object System.Collections.IEnumerator.Current => Current;

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
                var queryResult = _query.ExecuteAsync(QueryContext, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                var nodes = new Storage.NodeList<Storage.Node>(queryResult.Hits.ToArray());
                if (_isContent)
                    _result = (IEnumerable<T>)nodes.Where(n => n != null).Select(Content.Create);
                else
                    _result = nodes.Where(n => n != null).Cast<T>();

                _resultEnumerator = _result.GetEnumerator();
            }
            return _resultEnumerator.MoveNext();
        }
        private void Compile()
        {
            if (_query == null)
            {
                _query = SnExpression.BuildQuery(_queryable.Expression, typeof(T), _queryable.ContextPath,
                    _queryable.ChildrenDefinition);
            }
        }
        public string GetQueryText()
        {
            Compile();
            return _query.ToString();
        }
    }
}
