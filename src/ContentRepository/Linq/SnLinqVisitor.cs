﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using System.Diagnostics;
using SenseNet.Search;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using SenseNet.Search.Querying.Parser.Predicates;
using Expression = System.Linq.Expressions.Expression;

namespace SenseNet.ContentRepository.Linq
{
    internal class SnLinqVisitor : ExpressionVisitor
    {
        #region ContentHandlerGetTypePredicate, BooleanMemberPredicate
        private class ContentHandlerGetTypePredicate : SnQueryPredicate
        {
            public override string ToString()
            {
                throw new SnNotSupportedException("SnLinq: ContentHandlerGetTypePredicate.ToString");
            }
        }
        [DebuggerDisplay("BMQ: {FieldName} = {Value}")]
        internal class BooleanMemberPredicate : SnQueryPredicate
        {
            public bool FromVisitMember { get; set; }
            public bool FromVisitUnary { get; set; }
            public string FieldName { get; set; }
            public bool Value { get; set; }
            public override string ToString()
            {
                throw new SnNotSupportedException("SnLinq: BooleanMemberPredicate.ToString");
            }
        }
        #endregion

        private readonly Stack<SnQueryPredicate> _predicates = new Stack<SnQueryPredicate>();

        internal SnQueryPredicate GetPredicate(Type sourceCollectionItemType, ChildrenDefinition childrenDef)
        {
            if (sourceCollectionItemType != null && sourceCollectionItemType != typeof(Content))
            {
                _predicates.Push(CreateTypeIsPredicate(sourceCollectionItemType));
                if (_predicates.Count > 1)
                    CombineTwoPredicatesOnStack();
            }
            if (_predicates.Count == 0)
                return null;
            if (_predicates.Count > 1)
                CombineAllPredicatesOnStack();
            if (_predicates.Peek() is BooleanMemberPredicate bmq)
            {
                _predicates.Pop();
                _predicates.Push(CreateTextPredicate(bmq.FieldName, bmq.Value));
            }
            return _predicates.Peek();
        }

        [Conditional("DEBUG")]
        private static void Trace(MethodBase methodBase, object node)
        {
        }

        private Expression _root;
        public int Top { get; private set; }
        public int Skip { get; private set; }
        public bool CountOnly { get; private set; }
        public List<SortInfo> Sort { get; private set; }
        public bool ThrowIfEmpty { get; private set; }
        public bool ExistenceOnly { get; private set; }
        public string ElementSelection { get; private set; }

        public override Expression Visit(Expression node)
        {
            if (_root == null)
            {
                _root = node;
                Sort = new List<SortInfo>();
            }

            return base.Visit(node);
        }
        protected override Expression VisitBinary(BinaryExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            var visited = (BinaryExpression)base.VisitBinary(node);

            switch (visited.NodeType)
            {
                case ExpressionType.Equal:                // term
                case ExpressionType.NotEqual:             // -term
                case ExpressionType.GreaterThan:          // field:{value TO ]
                case ExpressionType.GreaterThanOrEqual:   // field:[value TO ]
                case ExpressionType.LessThan:             // field:[ to value}
                case ExpressionType.LessThanOrEqual:      // field:[ to value]
                    BuildFieldExpr(visited);
                    break;

                case ExpressionType.Not:
                case ExpressionType.Or:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                    BuildBooleanExpr(visited);
                    break;

                // All other cases are not supported here (~70)
                default: throw new SnNotSupportedException("VisitBinary: " + node);
            }
            return visited;
        }
        protected override Expression VisitBlock(BlockExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitBlock(node);
        }
        protected override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitCatchBlock(node);
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);

            var testConstant = node.Test as ConstantExpression;
            if (testConstant != null)
            {
                // If this is a constant expression, evaluate it and visit the appropriate expression
                if (testConstant.Value is bool && (bool)testConstant.Value)
                    return base.Visit(node.IfTrue);
                return base.Visit(node.IfFalse);
            }


            // Transform the node into a binary expression like this:
            // A ? B : C = (A && B) || (!A && C)
            Expression transformed;
            try
            {
                var left = Expression.MakeBinary(ExpressionType.AndAlso, node.Test, node.IfTrue);
                var right = Expression.MakeBinary(ExpressionType.AndAlso,
                    Expression.MakeUnary(ExpressionType.Not, node.Test, null), node.IfFalse);
                transformed = Expression.MakeBinary(ExpressionType.OrElse, left, right);
            }
            catch (Exception exc)
            {
                throw new Exception(
                    "Couldn't transform conditional expression into binary expressions. See inner exception for details.",
                    exc);
            }

            return base.Visit(transformed);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitConstant(node);
        }
        protected override Expression VisitDebugInfo(DebugInfoExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitDebugInfo(node);
        }
        protected override Expression VisitDefault(DefaultExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitDefault(node);
        }
        protected override Expression VisitDynamic(DynamicExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitDynamic(node);
        }
        protected override ElementInit VisitElementInit(ElementInit node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitElementInit(node);
        }
        protected override Expression VisitExtension(Expression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitExtension(node);
        }
        protected override Expression VisitGoto(GotoExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitGoto(node);
        }
        protected override Expression VisitIndex(IndexExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitIndex(node);
        }
        protected override Expression VisitInvocation(InvocationExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitInvocation(node);
        }
        protected override Expression VisitLabel(LabelExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitLabel(node);
        }
        protected override LabelTarget VisitLabelTarget(LabelTarget node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitLabelTarget(node);
        }
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitLambda(node);
        }
        protected override Expression VisitListInit(ListInitExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitListInit(node);
        }
        protected override Expression VisitLoop(LoopExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitLoop(node);
        }
        protected override Expression VisitMember(MemberExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            var memberExpr = (MemberExpression)base.VisitMember(node);

            if (memberExpr.Type == typeof(bool))
                _predicates.Push(new BooleanMemberPredicate
                {
                    FieldName = memberExpr.Member.Name,
                    Value = true,
                    FromVisitMember = true
                });

            return memberExpr;
        }
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitMemberAssignment(node);
        }
        protected override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitMemberBinding(node);
        }
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitMemberInit(node);
        }
        protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitMemberListBinding(node);
        }
        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitMemberMemberBinding(node);
        }
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            Expression visited = null;
            try
            {
                visited = base.VisitMethodCall(node);
            }
            catch(SnNotSupportedException e)
            {
                throw SnExpression.CallingAsEnunerableExpectedError(node.Method.Name, e);
            }

            if (!(visited is MethodCallExpression methodCallExpr))
                throw new NotSupportedException("#VisitMethodCall if visited is not null");

            var methodName = methodCallExpr.Method.Name;
            switch (methodName)
            {
                case "OfType":
                // Do nothing. Type of expression has been changed so a TypeIs predicate will be created.
                case "Where":
                    // do nothing
                    break;
                case "Take":
                    var topExpr = GetArgumentAsConstant(methodCallExpr, 1);
                    this.Top = (int)topExpr.Value;
                    break;
                case "Skip":
                    var skipExpr = GetArgumentAsConstant(methodCallExpr, 1);
                    this.Skip = (int)skipExpr.Value;
                    break;
                case "LongCount":
                case "Count":
                    if (methodCallExpr.Arguments.Count == 2)
                        if (_predicates.Count > 1) // There is Where in the main expression
                            CombineTwoPredicatesOnStack();
                    this.CountOnly = true;
                    break;
                case "ThenBy":
                case "OrderBy":
                    Sort.Add(CreateSortInfoFromExpr(node, false));
                    break;
                case "ThenByDescending":
                case "OrderByDescending":
                    Sort.Add(CreateSortInfoFromExpr(node, true));
                    break;
                case "StartsWith":
                    var startsWithExpr = GetArgumentAsConstant(methodCallExpr, 0);
                    var startsWithArg = (string)startsWithExpr.Value;
                    BuildWildcardPredicate(GetPropertyName(methodCallExpr.Object), WildcardPosition.AtEnd, startsWithArg);
                    break;
                case "EndsWith":
                    var endsWithExpr = GetArgumentAsConstant(methodCallExpr, 0);
                    var endsWithArg = (string)endsWithExpr.Value;
                    BuildWildcardPredicate(GetPropertyName(methodCallExpr.Object), WildcardPosition.AtStart, endsWithArg);
                    break;
                case "Contains":
                    var arg0 = methodCallExpr.Arguments[0];
                    if (arg0 is ConstantExpression constantExpr)
                    {
                        if (constantExpr.Type != typeof(string))
                            throw new NotSupportedException(
                                $"Calling Contains on an instance of type {constantExpr.Type} is not supported. Allowed types: string, IEnumerable<Node>.");
                        var containsArg = (string)constantExpr.Value;
                        BuildWildcardPredicate(GetPropertyName(methodCallExpr.Object), WildcardPosition.AtStartAndEnd, containsArg);
                        break;
                    }
                    if (arg0 is MemberExpression memberExpr)
                    {
                        if (memberExpr.Type != typeof(IEnumerable<Node>))
                            throw NotSupportedException(node, "#2");
                        if (!(methodCallExpr.Arguments[1] is ConstantExpression rightConstant))
                            throw NotSupportedException(node, "#1");
                        var nodeValue = (Node)rightConstant.Value;
                        BuildTextPredicate(memberExpr.Member.Name, nodeValue);
                        break;
                    }
                    throw NotSupportedException(node, "#3");
                case "FirstOrDefault":
                case "First":
                    ElementSelection = "first";
                    this.Top = 1;
                    this.ThrowIfEmpty = methodName == "First";
                    if (methodCallExpr.Arguments.Count == 2)
                        if (_predicates.Count > 1)
                            CombineTwoPredicatesOnStack();
                    break;
                case "SingleOrDefault":
                case "Single":
                    ElementSelection = "single";
                    this.ThrowIfEmpty = methodName == "Single";
                    if (methodCallExpr.Arguments.Count == 2)
                        if (_predicates.Count > 1)
                            CombineTwoPredicatesOnStack();
                    break;
                case "LastOrDefault":
                case "Last":
                    ElementSelection = "last";
                    this.ThrowIfEmpty = methodName == "Last";
                    if (methodCallExpr.Arguments.Count == 2)
                        if (_predicates.Count > 1)
                            CombineTwoPredicatesOnStack();
                    break;
                case "ElementAtOrDefault":
                case "ElementAt":
                    ElementSelection = "elementat";
                    this.ThrowIfEmpty = methodName == "ElementAt";
                    var constExpr = GetArgumentAsConstant(methodCallExpr, 1);
                    var index = Convert.ToInt32(constExpr.Value);
                    this.Skip = index;
                    this.Top = 1;
                    break;
                case "Any":
                    ElementSelection = "first";
                    this.CountOnly = true;
                    this.ExistenceOnly = true;
                    this.Top = 1;
                    if (methodCallExpr.Arguments.Count == 2)
                        if (_predicates.Count > 1)
                            CombineTwoPredicatesOnStack();
                    break;
                case "Type":
                    var typeExpr = GetArgumentAsConstant(methodCallExpr, 0);
                    BuildTextPredicate("Type", (string)typeExpr.Value);
                    break;
                case "TypeIs":
                    var typeIsExpr = GetArgumentAsConstant(methodCallExpr, 0);
                    BuildTextPredicate("TypeIs", (string)(typeIsExpr).Value);
                    break;
                case "get_Item":
                    if (!(methodCallExpr.Object is ParameterExpression))
                        throw new NotSupportedException("#get_Item");
                    break;
                case "startswith":
                    {
                        var fieldName = GetPropertyName(methodCallExpr.Arguments[0]);
                        var startswithExpr = GetArgumentAsConstant(methodCallExpr, 1);
                        var arg = (string)startswithExpr.Value;
                        BuildWildcardPredicate(fieldName, WildcardPosition.AtEnd, arg);
                        break;
                    }
                case "endswith":
                    {
                        var fieldName = GetPropertyName(methodCallExpr.Arguments[0]);
                        var endswithExpr = GetArgumentAsConstant(methodCallExpr, 1);
                        var arg = (string)endswithExpr.Value;
                        BuildWildcardPredicate(fieldName, WildcardPosition.AtStart, arg);
                        break;
                    }
                case "substringof":
                    {
                        var fieldName = GetPropertyName(methodCallExpr.Arguments[1]);
                        var substringofExpr = GetArgumentAsConstant(methodCallExpr, 0);
                        var arg = (string)substringofExpr.Value;
                        BuildWildcardPredicate(fieldName, WildcardPosition.AtStartAndEnd, arg);
                        break;
                    }
                case "isof":
                    {
                        var isofExpr = GetArgumentAsConstant(methodCallExpr, 1);
                        BuildTextPredicate("TypeIs", (string)(isofExpr).Value);
                        break;
                    }
                case "InFolder":
                    {
                        var infolderexpr = GetArgumentAsConstant(methodCallExpr, 0);
                        var folder = infolderexpr.Value;
                        BuildTextPredicate("InFolder", GetPath(folder, "InFolder"));
                        break;
                    }
                case "InTree":
                    {
                        var intreeexpr = GetArgumentAsConstant(methodCallExpr, 0);
                        var folder = intreeexpr.Value;
                        BuildTextPredicate("InTree", GetPath(folder, "InTree"));
                        break;
                    }
                case "GetType":
                    {
                        if (methodCallExpr.Object is MemberExpression member && member.Member == typeof(Content).GetProperty("ContentHandler"))
                            _predicates.Push(new ContentHandlerGetTypePredicate());
                        else
                            throw new NotSupportedException("GetType method is not supported: " + node);
                        break;
                    }
                case "IsAssignableFrom":
                    {
                        if (!(methodCallExpr.Object is ConstantExpression member))
                            throw new NotSupportedException("IsAssignableFrom method is not supported: " + node);
                        var type = member.Value as Type;
                        if (type == null)
                            throw new NotSupportedException("IsAssignableFrom method is not supported" + node);
                        if (_predicates.Count == 0)
                            throw new NotSupportedException("IsAssignableFrom method is not supported" + node);
                        if (!(_predicates.Pop() is ContentHandlerGetTypePredicate))
                            throw new NotSupportedException("IsAssignableFrom method is not supported" + node);
                        _predicates.Push(CreateTypeIsPredicate(type));

                        break;
                    }
                default:
                    throw SnExpression.CallingAsEnunerableExpectedError(methodCallExpr.Method.Name);
            }

            return visited;
        }
        protected override Expression VisitNew(NewExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitNew(node);
        }
        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitNewArray(node);
        }
        protected override Expression VisitParameter(ParameterExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitParameter(node);
        }
        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitRuntimeVariables(node);
        }
        protected override Expression VisitSwitch(SwitchExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitSwitch(node);
        }
        protected override SwitchCase VisitSwitchCase(SwitchCase node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitSwitchCase(node);
        }
        protected override Expression VisitTry(TryExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            return base.VisitTry(node);
        }
        protected override Expression VisitUnary(UnaryExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            var unary = (UnaryExpression)base.VisitUnary(node);
            if (unary.NodeType == ExpressionType.Convert)
            {
                if (unary.Type == typeof(bool))
                {
                    var visited = Visit(unary.Operand);
                    string name = null;
                    if (visited is MethodCallExpression callExpr)
                    {
                        if (callExpr.Method.Name == "get_Item" && callExpr.Arguments.Count == 1 && callExpr.Arguments[0].Type == typeof(string))
                        {
                            if (!(callExpr.Arguments[0] is ConstantExpression constExpr))
                                throw NotSupportedException(node, "#4");
                            name = constExpr.Value as string;
                            if (string.IsNullOrEmpty(name))
                                throw new NotSupportedException("Value must be an existing field name: " + constExpr.Value);
                        }
                    }
                    else
                    {
                        if (visited is MemberExpression memberExpr)
                            name = memberExpr.Member.Name;
                        else
                            throw NotSupportedException(node, "#5");
                    }
                    _predicates.Push(new BooleanMemberPredicate { FieldName = name, Value = true, FromVisitUnary = true });
                }
            }
            else if (unary.NodeType == ExpressionType.Not)
            {
                var predicate = _predicates.Peek();
                if (predicate is BooleanMemberPredicate bmq)
                {
                    bmq.Value = !bmq.Value;
                }
                else
                {
                    predicate = _predicates.Pop();
                    var clauses = new List<LogicalClause> { new LogicalClause(predicate, Occurence.MustNot) };
                    _predicates.Push(new LogicalPredicate(clauses));
                }
            }
            return unary;
        }
        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            Trace(MethodBase.GetCurrentMethod(), node);
            var visited = (TypeBinaryExpression)base.VisitTypeBinary(node);
            _predicates.Push(CreateTypeIsPredicate(visited.TypeOperand));
            return visited;
        }

        private void CombineAllPredicatesOnStack()
        {
            CombinePredicatesOnStack(_predicates.Count);
        }
        private void CombineTwoPredicatesOnStack()
        {
            CombinePredicatesOnStack(2);
        }
        private void CombinePredicatesOnStack(int count)
        {
            var clauses = new List<LogicalClause>();

            for (var i = 0; i < count; i++)
            {
                var predicate = _predicates.Pop();
                if (predicate is BooleanMemberPredicate bmp)
                    predicate = CreateTextPredicate(bmp.FieldName, bmp.Value);
                clauses.Add(new LogicalClause(predicate, Occurence.Must));
            }

            _predicates.Push(new LogicalPredicate(clauses));
        }

        private SnQueryPredicate CreateTypeIsPredicate(Type targetType)
        {
            var contentTypeName = ContentTypeManager.GetContentTypeNameByType(targetType);
            if (contentTypeName == null)
                throw new ApplicationException($"Unknown Content Type: {targetType.FullName}");
            return new SimplePredicate(IndexFieldName.TypeIs, ConvertValue(IndexFieldName.TypeIs, contentTypeName));
        }
        private bool RemoveDuplicatedTopLevelBooleanMemberPredicate(SnQueryPredicate predicate)
        {
            if (_predicates.Count == 0)
                return false;

            if (!(predicate is BooleanMemberPredicate bmq))
                return false;

            if (!(_predicates.Peek() is BooleanMemberPredicate topBmq))
                return false;

            if (topBmq.FieldName != bmq.FieldName)
                return false;
            if (!topBmq.FromVisitMember && !topBmq.FromVisitUnary && topBmq.Value != bmq.Value)
                return false;
            return true;
        }

        private readonly string[] _enabledCanonicalFunctionNames = { "startswith", "endswith", "substringof", "isof" };
        private void BuildFieldExpr(BinaryExpression node)
        {
            // normalizing sides: constant must be on the right side
            Expression left, right;
            if (node.Left.NodeType == ExpressionType.Constant)
            {
                left = node.Right;
                right = node.Left;
            }
            else
            {
                left = node.Left;
                right = node.Right;
            }

            // getting field name
            string fieldName;
            if (IsIndexerAccess(left, out var name))
            {
                fieldName = name;
            }
            else if (left.NodeType == ExpressionType.MemberAccess && right.NodeType == ExpressionType.Constant)
            {
                if (!(left is MemberExpression leftAsMemberExpr))
                    throw new NotSupportedException();

                var parentExpr = leftAsMemberExpr.Expression;
                if (parentExpr is ParameterExpression parentAsParameter)
                {
                    if (IsValidType(parentAsParameter.Type))
                        fieldName = leftAsMemberExpr.Member.Name;
                    else
                        throw new NotSupportedException("#BuildFieldExpr-1#");
                }
                else
                {
                    if (parentExpr is MemberExpression parentAsMemberExpr)
                    {
                        if (parentAsMemberExpr.Expression is ParameterExpression parentParentAsParameter 
                            && IsValidType(parentParentAsParameter.Type) 
                            && parentAsMemberExpr.Member.Name == "ContentType" 
                            && leftAsMemberExpr.Member.Name == "Name")
                            fieldName = "Type";
                        else
                            throw new NotSupportedException("Cannot parse an expression #1: " + parentAsMemberExpr);
                    }
                    else
                    {
                        if (parentExpr.NodeType == ExpressionType.Convert)
                        {
                            var unary = (UnaryExpression)parentExpr;
                            if (typeof(Node).IsAssignableFrom(unary.Type))
                                fieldName = leftAsMemberExpr.Member.Name;
                            else
                                throw new NotSupportedException(
                                    "Cannot a member expression where object is: " + unary.Type.FullName);
                        }
                        else
                        {
                            throw new NotSupportedException("Cannot parse an expression #2: " + parentExpr);
                        }
                    }
                }
            }
            else if (left.NodeType == ExpressionType.Call && right.NodeType == ExpressionType.Constant)
            {
                var callName = ((MethodCallExpression)left).Method.Name;
                if (node.NodeType == ExpressionType.Equal)
                {
                    if (_enabledCanonicalFunctionNames.Contains(callName, StringComparer.OrdinalIgnoreCase))
                    {
                        var constant = (ConstantExpression)right;
                        if (constant.Type == typeof(bool) && (bool)constant.Value)
                            return;
                    }
                }
                throw new NotSupportedException("#BuildFieldExpr: only the following form allowed: canonicalFunction() eq true. Canonical functions: startswith, endswith, substringof, isof.");
            }
            else
            {
                throw new NotSupportedException("#BuildFieldExpr");
            }

            // getting value
            var value = ((ConstantExpression)right).Value;

            // content type hack
            if (value is ContentType contentTypeValue && fieldName == "ContentType")
            {
                fieldName = "Type";
                value = contentTypeValue.Name;
            }

            // converting
            var indexValue = ConvertValue(fieldName, value, out var fieldDatatype);

            // creating product
            SnQueryPredicate predicate;
            switch (node.NodeType)
            {
                case ExpressionType.Equal:               // term
                    if (fieldDatatype == typeof(bool))
                        predicate = new BooleanMemberPredicate { FieldName = fieldName, Value = (bool)value };
                    else
                        predicate = CreateTextPredicate(fieldName, indexValue);
                    break;
                case ExpressionType.NotEqual:            // -term
                    if (fieldDatatype == typeof(bool))
                        predicate = new BooleanMemberPredicate { FieldName = fieldName, Value = !(bool)value };
                    else
                        predicate = new LogicalPredicate(new List<LogicalClause> { new LogicalClause(CreateTextPredicate(fieldName, indexValue), Occurence.MustNot) });
                    break;
                case ExpressionType.GreaterThan:         // field:{value TO ]
                    predicate = CreateRangePredicate(fieldName, indexValue, null, false, true);
                    break;
                case ExpressionType.GreaterThanOrEqual:  // field:[value TO ]
                    predicate = CreateRangePredicate(fieldName, indexValue, null, true, true);
                    break;
                case ExpressionType.LessThan:            // field:[ to value}
                    predicate = CreateRangePredicate(fieldName, null, indexValue, true, false);
                    break;
                case ExpressionType.LessThanOrEqual:     // field:[ to value]
                    predicate = CreateRangePredicate(fieldName, null, indexValue, true, true);
                    break;
                default:
                    throw new SnNotSupportedException($"NodeType {node.NodeType} isn't implemented");
            }
            if (node.Type == typeof(bool))
                if (RemoveDuplicatedTopLevelBooleanMemberPredicate(predicate))
                    _predicates.Pop();
            _predicates.Push(predicate);
        }
        internal static SimplePredicate CreateTextPredicate(string fieldName, object value)
        {
            if (!(value is IndexValue indexValue))
                indexValue = ConvertValue(fieldName, value);
            return new SimplePredicate(fieldName, indexValue);
        }
        private void BuildTextPredicate(string fieldName, object value)
        {
            _predicates.Push(CreateTextPredicate(fieldName, value));
        }

        private bool IsIndexerAccess(Expression expr, out string name)
        {
            name = null;
            if (expr.NodeType != ExpressionType.Convert)
                return false;
            expr = ((UnaryExpression)expr).Operand;
            if (expr.NodeType != ExpressionType.Call)
                return false;
            var method = ((MethodCallExpression)expr).Method;
            if (method.Name != "get_Item")
                return false;
            var args = ((MethodCallExpression)expr).Arguments;
            if (args.Count != 1)
                return false;
            var constExpr = (ConstantExpression)args[0];
            if (constExpr.Type != typeof(string))
                return false;
            name = (string)constExpr.Value;
            return true;
        }
        private void BuildBooleanExpr(BinaryExpression node)
        {
            var clauses = new List<LogicalClause>();
            switch (node.NodeType)
            {
                case ExpressionType.Or: // |
                    throw new NotSupportedException("#BuildBooleanExpr/ExpressionType.Or");
                case ExpressionType.And: // &
                    throw new NotSupportedException("#BuildBooleanExpr/ExpressionType.And");
                case ExpressionType.AndAlso: // &&
                    clauses.Add(new LogicalClause(_predicates.Pop(), Occurence.Must));
                    clauses.Add(new LogicalClause(_predicates.Pop(), Occurence.Must));
                    break;
                case ExpressionType.OrElse: // ||
                    clauses.Add(new LogicalClause(_predicates.Pop(), Occurence.Should));
                    clauses.Add(new LogicalClause(_predicates.Pop(), Occurence.Should));
                    break;
                default:
                    throw new SnNotSupportedException($"NodeType {node.NodeType} isn't implemented");
            }
            _predicates.Push(new LogicalPredicate(clauses));
        }

        private static IndexValue ConvertValue(string name, object value)
        {
            return ConvertValue(name, value, out _);
        }
        private static IndexValue ConvertValue(string name, object value, out Type fieldDataType)
        {
            if (value is ContentType contentTypeValue && name == "ContentType")
            {
                name = "TypeIs";
                value = contentTypeValue.Name.ToLowerInvariant();
            }

            var fieldInfo = ContentTypeManager.GetPerFieldIndexingInfo(name);
            if (fieldInfo == null)
                throw new InvalidOperationException("Unknown field: " + name);
            var converter = fieldInfo.IndexFieldHandler;

            var indexValue = converter.ConvertToTermValue(value);

            fieldDataType = fieldInfo.FieldDataType;
            return indexValue;
        }

        private SnQueryPredicate CreateRangePredicate(string fieldName, IndexValue minValue, IndexValue maxValue, bool includeLower, bool includeUpper)
        {
            return new RangePredicate(fieldName, minValue, maxValue, !includeLower, !includeUpper);
        }

        private SortInfo CreateSortInfoFromExpr(MethodCallExpression node, bool reverse)
        {
            if (!(node.Arguments[1] is UnaryExpression unaryExpr))
                throw new Exception("Invalid sort: " + node);

            if (!(unaryExpr.Operand is LambdaExpression operand))
                throw new Exception("Invalid sort: " + node);

            if (operand.Body is MemberExpression memberExpr)
                return new SortInfo(memberExpr.Member.Name, reverse);

            if (!(operand.Body is MethodCallExpression methodCallExpr))
                throw new Exception("Invalid sort: " + node);

            if (methodCallExpr.Method.Name != "get_Item")
                throw new Exception("Invalid sort: " + node);

            if (methodCallExpr.Arguments.Count != 1)
                throw new Exception("Invalid sort: " + node);

            if (!(methodCallExpr.Arguments[0] is ConstantExpression nameExpr))
                throw new Exception("Invalid sort: " + node);

            return new SortInfo((string)nameExpr.Value, reverse);
        }

        private enum WildcardPosition { AtStart, AtEnd, AtStartAndEnd }
        private void BuildWildcardPredicate(string field, WildcardPosition type, string argument)
        {
            var fieldInfo = ContentTypeManager.GetPerFieldIndexingInfo(field);
            if (fieldInfo == null)
                throw new InvalidOperationException("Unknown field: " + field);

            var arg = ConvertValue(field, argument).StringValue;
            string text;
            switch (type)
            {
                case WildcardPosition.AtStart: text = "*" + arg; break;
                case WildcardPosition.AtEnd: text = arg + "*"; break;
                case WildcardPosition.AtStartAndEnd: text = string.Concat("*", arg, "*"); break;
                default: throw new SnNotSupportedException("WildcardType is not supported: " + type);
            }
            var wq = new SimplePredicate(field, new IndexValue(text));
            _predicates.Push(wq);
        }

        private string GetPropertyName(Expression expr)
        {
            if (expr is MemberExpression memberExpr)
                return memberExpr.Member.Name;

            if (expr is UnaryExpression unaryExpr)
                return GetPropertyName(unaryExpr.Operand);

            if (!(expr is MethodCallExpression methodCallExpr))
                throw new SnNotSupportedException("SnLinq: GetPropertyName from Expression");

            if (methodCallExpr.Method.Name != "get_Item" || methodCallExpr.Object?.Type != typeof(Content))
                throw new SnNotSupportedException("SnLinq: GetPropertyName from MethodCallExpression");
            // content indexer --> return with field name

            if (!(methodCallExpr.Arguments[0] is ConstantExpression constantArgumentExpr))
                throw new SnNotSupportedException("SnLinq: GetPropertyName from Content indexer expression");
            return (string)constantArgumentExpr.Value;
        }

        // --------------------------------------------------------------------------- helpers

        private bool IsValidType(Type type)
        {
            return type == typeof(Content) || typeof(Node).IsAssignableFrom(type);
        }
        private static string GetPath(object folder, string methodName)
        {
            string path;
            if (folder is string str)
            {
                path = str;
            }
            else
            {
                if (folder is Content content)
                {
                    path = content.Path;
                }
                else
                {
                    if (folder is Node fldr)
                    {
                        path = fldr.Path;
                    }
                    else
                    {
                        throw new NotSupportedException(methodName + " method is not supported on the following object: " + folder);
                    }
                }
            }
            return path;
        }
        private Exception NotSupportedException(Expression node, string id)
        {
            return new NotSupportedException($"The expression ({id}) cannot be executed: {node}");
        }
        private ConstantExpression GetArgumentAsConstant(MethodCallExpression methodCallExpr, int argumentIndex)
        {
            if (!(methodCallExpr.Arguments[argumentIndex] is ConstantExpression constantExpr))
                throw new NotSupportedException(
                    $"Argument {argumentIndex} is invalid in a method call. Argument must be constant or transformable to constant. {methodCallExpr}");
            return constantExpr;
        }

    }
}
