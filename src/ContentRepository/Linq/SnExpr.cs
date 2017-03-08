using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Linq.Expressions;

namespace SenseNet.ContentRepository.Linq
{
    /// <summary>
    /// Represents an <see cref="System.Linq.Expressions.Expression">Expression</see> that knows it parent and children.
    /// </summary>
    [DebuggerDisplay("{this.Expression.NodeType} (Exec={IsExecutable}): {Expression}")]
    internal class SnExpr
    {
        public Expression Expression;
        public SnExpr Parent;
        public List<SnExpr> Children = new List<SnExpr>();
        public bool IsExecutable;

        internal void SetExecutable(bool isExecutable)
        {
            IsExecutable = isExecutable;
            if (Parent != null)
                Parent.SetExecutable(isExecutable);
        }
    }
}
