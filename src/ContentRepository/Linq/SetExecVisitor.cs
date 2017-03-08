using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace SenseNet.ContentRepository.Linq
{
    internal class SetExecVisitor : ExpressionVisitor
    {
        private Dictionary<Expression, SnExpr> _expressions = new Dictionary<Expression, SnExpr>();
        private Stack<SnExpr> _parentChain = new Stack<SnExpr>();
        private SnExpr _rootExpr;
        private SnExpr _currentExpr;

        internal bool HasParameter { get; private set; }
        internal Dictionary<Expression, SnExpr> GetExpressions()
        {
            return _expressions;
        }

        public override Expression Visit(Expression node)
        {
            if (node != null)
            {
                _currentExpr = new SnExpr { Expression = node, IsExecutable = true };
                _expressions[node] = _currentExpr;
                var parent = _parentChain.Count == 0 ? null : _parentChain.Peek();
                _currentExpr.Parent = parent;
                if (parent == null)
                    _rootExpr = _currentExpr;
                else
                    parent.Children.Add(_currentExpr);
                _parentChain.Push(_currentExpr);
            }

            var visited = base.Visit(node);

            if (node != null)
                _parentChain.Pop();

            return visited;
        }
        protected override Expression VisitParameter(ParameterExpression node)
        {
            this.HasParameter = true;
            _currentExpr.SetExecutable(false);
            return base.VisitParameter(node);
        }
    }
}
