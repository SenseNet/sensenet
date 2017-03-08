using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace SenseNet.ContentRepository.Linq
{
    internal class ExecutorVisitor : ExpressionVisitor
    {
        private Dictionary<Expression, SnExpr> _expressions = new Dictionary<Expression, SnExpr>();
        public ExecutorVisitor(Dictionary<Expression, SnExpr> expressions)
        {
            _expressions = expressions;
        }

        public override Expression Visit(Expression node)
        {
            if (node != null)
            {
                var snExpr = _expressions[node];
                if (snExpr.IsExecutable)
                {
                    if (node.NodeType == ExpressionType.Constant)
                        return node;

                    var x = DynamicExpression.Lambda(node);
                    var y = x.Compile();
                    var z = y.DynamicInvoke();
                    return Expression.Constant(z);
                }
            }
            return base.Visit(node);
        }
    }
}
