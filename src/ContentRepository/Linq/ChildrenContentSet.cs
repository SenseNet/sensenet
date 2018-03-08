using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SenseNet.ContentRepository.Linq
{
    public class ChildrenContentSet<T> : ContentSet<T>
    {
        public bool SubTree { get; }

        public ChildrenContentSet(string path, bool headersOnly, ChildrenDefinition childrenDef) : base(false, headersOnly, childrenDef, path)
        {
            SubTree = childrenDef.PathUsage == PathUsageMode.InTreeAnd;
        }
        public ChildrenContentSet(Expression expression, string path, bool subTree, ChildrenDefinition childrenDef) : base(expression, childrenDef, path)
        {
            SubTree = subTree;
        }

        public override IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            var contents = new ChildrenContentSet<TElement>(expression, ContextPath, SubTree, ChildrenDefinition)
            {
                CountOnlyEnabled = this.CountOnlyEnabled,
                HeadersOnlyEnabled = this.HeadersOnlyEnabled
            };
            return contents;
        }

        public override IEnumerator<T> GetEnumerator()
        {
            return new LinqChildrenEnumerator<T>(this);
        }
    }
}
