using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace SenseNet.ContentRepository.Linq
{
    public class ChildrenContentSet<T> : ContentSet<T>
    {
        public bool SubTree { get; private set; }

        public ChildrenContentSet(string path, bool headersOnly, ChildrenDefinition childrenDef) : base(false, headersOnly, childrenDef, path)
        {
            SubTree = childrenDef.PathUsage == PathUsageMode.InTreeAnd;
        }
        public ChildrenContentSet(Expression expression, string path, bool subTree, ChildrenDefinition childrenDef) : base(expression, childrenDef, path)
        {
            SubTree = subTree;
        }

        public override IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
        {
            var contents = new ChildrenContentSet<TElement>(expression, ContextPath, SubTree, ChildrenDefinition)
            {
                CountOnlyEnabled = this.CountOnlyEnabled,
                HeadersOnlyEnabled = this.HeadersOnlyEnabled
            };
            return (IQueryable<TElement>)contents;
        }

        public override IEnumerator<T> GetEnumerator()
        {
            return new LinqChildrenEnumerator<T>(this);
        }

    }
}
