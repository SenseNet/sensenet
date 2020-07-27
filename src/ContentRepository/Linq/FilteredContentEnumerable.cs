using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Linq
{
    public class FilteredContentEnumerable : IEnumerable<Content>
    {
        private readonly IEnumerable<Node> _nodeEnumerable;
        private readonly IEnumerable<Content> _contentEnumerable;
        private readonly SortInfo[] _sort;
        private readonly int _top;
        private readonly int _skip;
        private readonly Func<Content, bool> _filter;

        public int AllCount { get; private set; }

        public FilteredContentEnumerable(IEnumerable nodeOrContentEnumerable, LambdaExpression filterExpression,
            IEnumerable<SortInfo> sort, int top, int skip)
        {
            _nodeEnumerable = nodeOrContentEnumerable as IEnumerable<Node>;
            _contentEnumerable = nodeOrContentEnumerable as IEnumerable<Content>;
            if(_nodeEnumerable == null && _contentEnumerable == null)
                throw new InvalidOperationException(
                    "The 'nodeOrContentEnumerable' can be IEnumerable<Node> or IEnumerable<Content>.");

            _sort = sort == null ? new SortInfo[0] : sort.ToArray();
            _top = top;
            _skip = skip;

            if(filterExpression != null)
            {
                var func = filterExpression.Compile();
                _filter = func as Func<Content, bool>;
                if (_filter == null)
                    throw new InvalidOperationException(
                        "Invalid filterExpression (LambdaExpression): return value must be bool, " +
                        "parameter need to be " + typeof(Node).FullName);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public IEnumerator<Content> GetEnumerator()
        {
            var enumerable = _contentEnumerable ?? CreateContentEnumerable(_nodeEnumerable);

            if (_filter != null)
                enumerable = enumerable.Where(_filter);
            if (_sort.Length > 0)
                enumerable = CreateSortedEnumerable(enumerable, _sort);
            if (_skip > 0)
                enumerable = enumerable.Skip(_skip);
            if (_top > 0)
                enumerable = enumerable.Take(_top);

            foreach (var item in enumerable)
                yield return item;
        }

        private IEnumerable<Content> CreateContentEnumerable(IEnumerable nodeEnumerable)
        {
            foreach (Node node in nodeEnumerable)
            {
                AllCount++;
                yield return Content.Create(node);
            }
        }

        private IEnumerable<Content> CreateSortedEnumerable(IEnumerable<Content> input, SortInfo[] sort)
        {
            object Sorter(Content c, int i) { return c[sort[i].FieldName]; }

            var result = sort[0].Reverse
                ? input.OrderByDescending(x => Sorter(x, 0))
                : input.OrderBy(x => Sorter(x, 0));

            for (var i = 1; i < sort.Length; i++)
            {
                // Copy the value because "i" is used by reference in the loop block
                // and the result will be executed later(issue: Access to modified closure).
                var a = i;
                result = sort[a].Reverse
                    ? result.ThenByDescending(x => Sorter(x, a))
                    : result.ThenBy(x => Sorter(x, a));
            }

            return result;
        }
    }
}
