using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.ContentRepository;
using SenseNet.Tools;

namespace SenseNet.Portal.OData.Parser
{
    internal class ExpressionBuilder
    {
        internal class AspectAccessor
        {
            public string AspectName;
            public string FieldName;
        }


        internal ODataParser Parser { get; private set; }

        public ExpressionBuilder(ODataParser parser)
        {
            this.Parser = parser;
        }

        public ParameterExpression x = Expression.Parameter(typeof(SenseNet.ContentRepository.Content), "x");


        internal Expression BuildSimpleBinary(Expression expr, Expression right, string @operator)
        {
            ExpressionType expressionType;
            switch (@operator)
            {
                case "eq": expressionType = ExpressionType.Equal; break;
                case "ne": expressionType = ExpressionType.NotEqual; break;
                case "lt": expressionType = ExpressionType.LessThan; break;
                case "gt": expressionType = ExpressionType.GreaterThan; break;
                case "le": expressionType = ExpressionType.LessThanOrEqual; break;
                case "ge": expressionType = ExpressionType.GreaterThanOrEqual; break;
                case "add": expressionType = ExpressionType.Add; break;
                case "sub": expressionType = ExpressionType.Subtract; break;
                case "mul": expressionType = ExpressionType.Multiply; break;
                case "div": expressionType = ExpressionType.Divide; break;
                case "mod": expressionType = ExpressionType.Modulo; break;
                case "and": expressionType = ExpressionType.AndAlso; break;
                case "or": expressionType = ExpressionType.OrElse; break;
                default: throw new SnNotSupportedException("Unknown operator: " + @operator);
            }
            return Expression.MakeBinary(expressionType, expr, right);
        }
        internal Expression BuildUnary(Expression expr, string type)
        {
            switch (type)
            {
                case "not": return Expression.Not(expr);
                case "minus": return Expression.MakeUnary(ExpressionType.Negate, expr, expr.Type);
                case "plus": return expr;
                default: throw new SnNotSupportedException("Unknown type: " + type);
            }
        }
        internal Expression BuildConstant(object expr)
        {
            return Expression.Constant(expr);
        }
        internal Expression BuildPointConstant(Expression n1, Expression n2, Expression n3)
        {
            MethodInfo method;
            if (n3 == null)
            {
                method = typeof(Globals).GetMethod("Point", new[] { typeof(double), typeof(double) });
                return Expression.Call(method, n1, n2);
            }
            method = typeof(Globals).GetMethod("Point", new[] { typeof(double), typeof(double), typeof(double) });
            return Expression.Call(method, n1, n2, n3);
        }
        internal Expression BuildGlobalCall(string name, Expression[] args)
        {
            var argTypes = args.Select(a => a.Type).ToArray();
            var methodInfo = typeof(Globals).GetMethod(name, argTypes);
            if (methodInfo == null)
                throw ODataParser.SyntaxError(this.Parser.Lexer, String.Format("Unknown function: {0} ({1})", name, string.Join(", ", argTypes.Select(x => x.Name))));
            return Expression.Call(methodInfo, args);
        }
        internal Expression BuildMemberPath(List<string> steps)
        {
            Type type;
            int k;
            if (steps[0].Contains('.'))
            {
                type = TypeResolver.GetType(steps[0], false);
                if (type == null)
                    throw ODataParser.SyntaxError(this.Parser.Lexer, "Unknown type: " + steps[0]);
                k = 1;
            }
            else
            {
                if (steps.Count == 1 && steps[0] == "ContentType")
                    steps.Add("Name");
                type = typeof(SenseNet.ContentRepository.Content);
                k = 0;
            }

            Expression expression = this.x; // Expression.Parameter(type, "x");
            Expression lastExpr = null;
            Expression lastLastExpr = null;
            string lastAspectName = null;
            for (var i = k; i < steps.Count; i++)
            {
                lastLastExpr = lastExpr;
                lastExpr = expression;

                if (lastAspectName == null)
                {
                    var member = type.GetProperty(steps[i]);
                    expression = member == null ? GetDynamicMember(type, steps[i]) : Expression.MakeMemberAccess(member.GetGetMethod().IsStatic ? null : expression, member);
                    if (expression == null)
                        lastAspectName = steps[i];
                }
                else
                {
                    var aspect = Aspect.LoadAspectByName(lastAspectName);
                    var fieldSetting = aspect.FieldSettings.Where(f => f.Name == steps[i]).FirstOrDefault();
                    if (fieldSetting == null)
                        throw new InvalidOperationException("Field not found: " + lastAspectName + "." + steps[i]);

                    var fieldName = lastAspectName + "." + steps[i];
                    var fieldType = fieldSetting.FieldDataType;

                    expression = CreateFieldOfContentAccessExpression(lastLastExpr, fieldName, fieldType);
                    lastAspectName = null;
                }
                type = expression == null ? null : expression.Type;
            }

            if(expression==null)
                throw new InvalidOperationException("Field not found: " + lastAspectName);

            return expression;
        }

        private Expression GetDynamicMember(Type type, string fieldName)
        {
            var fieldType = GetGenericParamType(fieldName);
            if (fieldType == null)
            {
                if (type == typeof(Content))
                {
                    // maybe an aspect
                    var aspect = Aspect.LoadAspectByName(fieldName);
                    if (aspect == null)
                        throw new InvalidOperationException("Field not found: " + fieldName);
                    return null; // means: aspectName
                }
                throw new InvalidOperationException("Field not found: " + fieldName);
            }
            return CreateFieldOfContentAccessExpression(this.x, fieldName, fieldType);
        }
        private Expression CreateFieldOfContentAccessExpression(Expression contentInstance, string fieldName, Type fieldType)
        {
            // model of this: (fieldType)content[fieldName]
            var method = typeof(Content).GetMethod("get_Item", new Type[] { typeof(string) });
            var indexerExpr = Expression.Call(contentInstance, method, Expression.Constant(fieldName));
            var convertExpr = Expression.Convert(indexerExpr, fieldType);
            return convertExpr;
        }

        private static MethodInfo FindGenericMethod(Type type, string methodName, Type[] genericArgumentTypes, Type[] methodParameterTypes)
        {
            MethodInfo methodInfo = null;

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.Name != methodName)
                    continue;
                if (!method.IsGenericMethod)
                    continue;

                bool paramsOk = false;
                if (method.GetParameters().Length == methodParameterTypes.Length)
                {
                    paramsOk = true;
                    for (int i = 0; i < method.GetParameters().Length; i++)
                    {
                        if (method.GetParameters()[i].ParameterType != methodParameterTypes[i])
                        {
                            paramsOk = false;
                            break;
                        }
                    }
                }
                if (!paramsOk)
                    continue;

                if (method.GetGenericArguments().Length != genericArgumentTypes.Length)
                    continue;

                methodInfo = method.MakeGenericMethod(genericArgumentTypes);
                break;
            }

            return methodInfo;
        }
        private static Type GetGenericParamType(string fieldName)
        {
            var fieldIndexingInfo = SenseNet.ContentRepository.Schema.ContentType.GetPerfieldIndexingInfo(fieldName);
            if (fieldIndexingInfo != null)
                return fieldIndexingInfo.FieldDataType;
            return null;
        }
    }
}
