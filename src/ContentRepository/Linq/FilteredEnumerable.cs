using System;
using System.Collections;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage;
using System.Linq.Expressions;

namespace SenseNet.ContentRepository.Linq
{
    public class FilteredEnumerable : IEnumerable<Node>
    {
        private readonly IEnumerable _enumerable;
        private readonly int _top;
        private readonly int _skip;

        private readonly Func<Content, bool> _isOk;

        public int AllCount { get; private set; }

        public FilteredEnumerable(IEnumerable enumerable, LambdaExpression filterExpression, int top, int skip)
        {
            _enumerable = enumerable;
            _top = top;
            _skip = skip;

            var func = filterExpression.Compile();
            _isOk = func as Func<Content, bool>;
            if (_isOk == null)
                throw new InvalidOperationException("Invalid filterExpression (LambdaExpression): return value must be bool, parameter must be " + typeof(Node).FullName);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public IEnumerator<Node> GetEnumerator()
        {
            var skipped = 0;
            var count = 0;
            //UNDONE: Missing $orderby
            foreach (Node item in _enumerable)
            {
                AllCount++;

                if (!_isOk(Content.Create(item)))
                    continue;
                if (skipped++ < _skip)
                    continue;
                if (_top == 0 || count++ < _top)
                    yield return item;
                else
                    yield break;
            }
        }
    }
}
