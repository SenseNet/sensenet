using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Linq
{
    internal class CaseInsensitiveFilterVisitor : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var visited = (BinaryExpression)base.VisitBinary(node);

            var method = visited.Method;
            if (method == null || method.DeclaringType != typeof(string))
                return visited;

            string newMethodName;
            switch (method.Name)
            {
                default:
                    return visited;
                case "op_Equality":
                    newMethodName = "CaseInsensitiveEqual";
                    break;
                case "op_Inequality":
                    newMethodName = "CaseInsensitiveNotEqual";
                    break;
            }

            var newMethod = typeof(CaseInsensitiveFilterVisitor).GetMethod(newMethodName, BindingFlags.NonPublic | BindingFlags.Static);
            var rewritten = Expression.MakeBinary(visited.NodeType, visited.Left, visited.Right, visited.IsLiftedToNull, newMethod);
            return rewritten;
        }

        private static bool CaseInsensitiveEqual(string op1, string op2)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(op1, op2) == 0;
        }
        private static bool CaseInsensitiveNotEqual(string op1, string op2)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(op1, op2) != 0;
        }

    }
}
