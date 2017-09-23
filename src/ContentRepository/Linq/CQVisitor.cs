using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Lucene.Net.Search;
using System.Diagnostics;
using SenseNet.Search;
using Lucene.Net.Index;
using SenseNet.Search.Parser;
using Lucene.Net.Util;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;
using SenseNet.Search.Lucene29;
using Expression = System.Linq.Expressions.Expression;

namespace SenseNet.ContentRepository.Linq
{
    internal class CQVisitor : ExpressionVisitor
    {
        private class ContentHandlerGetTypeQuery : Query
        {
            public override string ToString(string field)
            {
                throw new SnNotSupportedException("SnLinq: ContentHandlerGetTypeQuery.ToString");
            }
        }
        [DebuggerDisplay("BMQ: {FieldName} = {Value}")]
        internal class BooleanMemberQuery : Query
        {
            public bool FromVisitMember { get; set; }
            public bool FromVisitUnary { get; set; }
            public string FieldName { get; set; }
            public bool Value { get; set; }
            public override string ToString(string field)
            {
                throw new SnNotSupportedException("SnLinq: BooleanMemberQuery.ToString");
            }
        }

        internal Query GetQuery(Type sourceCollectionItemType, ChildrenDefinition childrenDef)
        {
            if (sourceCollectionItemType != null && sourceCollectionItemType != typeof(Content))
            {
                _queries.Push(CreateTypeIsQuery(sourceCollectionItemType));
                if (_queries.Count > 1)
                    CombineTwoQueriesOnStack();
            }
            if (_queries.Count == 0)
                return null;
            if (_queries.Count > 1)
                CombineAllQueriesOnStack();
            var bmq = _queries.Peek() as BooleanMemberQuery;
            if (bmq != null)
            {
                _queries.Pop();
                _queries.Push(CreateTermQuery(bmq.FieldName, bmq.Value));
            }
            return _queries.Peek();
        }

        private void Trace(MethodBase methodBase, object node)
        {
            return;
        }

        private Expression _root;
        public int Top { get; private set; }
        public int Skip { get; private set; }
        public bool CountOnly { get; private set; }
        public List<SortField> SortFields { get; private set; }
        public bool ThrowIfEmpty { get; private set; }
        public bool ExistenceOnly { get; private set; }

        public override Expression Visit(Expression node)
        {
            if (_root == null)
            {
                _root = node;
                SortFields = new List<SortField>();
            }

            if (node != _root)
                return base.Visit(node);

            return base.Visit(node); // first visit
        }
        protected override Expression VisitBinary(BinaryExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
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
                default: throw new SnNotSupportedException("SnLinq: VisitBinary");
            }
            return visited;
        }
        protected override Expression VisitBlock(BlockExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitBlock(node);
        }
        protected override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitCatchBlock(node);
        }
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);

            var testConstant = node.Test as ConstantExpression;
            if (testConstant != null)
            {
                // If this is a constant expression, evaluate it and visit the appropriate expression

                if (testConstant.Value is bool && ((bool)testConstant.Value) == true)
                    return base.Visit(node.IfTrue);
                else
                    return base.Visit(node.IfFalse);
            }
            else
            {
                // Transform the node into a binary expression like this:
                // A ? B : C = (A && B) || (!A && C)

                Expression transformed;

                try
                {
                    var left = Expression.MakeBinary(ExpressionType.AndAlso, node.Test, node.IfTrue);
                    var right = Expression.MakeBinary(ExpressionType.AndAlso, Expression.MakeUnary(ExpressionType.Not, node.Test, null), node.IfFalse);
                    transformed = Expression.MakeBinary(ExpressionType.OrElse, left, right);
                }
                catch (Exception exc)
                {
                    throw new Exception("Couldn't transform conditional expression into binary expressions. See inner exception for details.", exc);
                }

                return base.Visit(transformed);
            }
        }
        protected override Expression VisitConstant(ConstantExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitConstant(node);
        }
        protected override Expression VisitDebugInfo(DebugInfoExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitDebugInfo(node);
        }
        protected override Expression VisitDefault(DefaultExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitDefault(node);
        }
        protected override Expression VisitDynamic(DynamicExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitDynamic(node);
        }
        protected override ElementInit VisitElementInit(ElementInit node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitElementInit(node);
        }
        protected override Expression VisitExtension(Expression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitExtension(node);
        }
        protected override Expression VisitGoto(GotoExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitGoto(node);
        }
        protected override Expression VisitIndex(IndexExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitIndex(node);
        }
        protected override Expression VisitInvocation(InvocationExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitInvocation(node);
        }
        protected override Expression VisitLabel(LabelExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitLabel(node);
        }
        protected override LabelTarget VisitLabelTarget(LabelTarget node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitLabelTarget(node);
        }
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitLambda<T>(node);
        }
        protected override Expression VisitListInit(ListInitExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitListInit(node);
        }
        protected override Expression VisitLoop(LoopExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitLoop(node);
        }
        protected override Expression VisitMember(MemberExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            var memberExpr = (MemberExpression)base.VisitMember(node);

            if (memberExpr.Type == typeof(bool))
                _queries.Push(new BooleanMemberQuery
                {
                    FieldName = memberExpr.Member.Name,
                    Value = true,
                    FromVisitMember = true
                });

            return memberExpr;
        }
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitMemberAssignment(node);
        }
        protected override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitMemberBinding(node);
        }
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitMemberInit(node);
        }
        protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitMemberListBinding(node);
        }
        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitMemberMemberBinding(node);
        }
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            var visited = base.VisitMethodCall(node);
            var methodCallExpr = visited as MethodCallExpression;
            if (methodCallExpr == null)
                throw new NotSupportedException("#VisitMethodCall if visited is not null");
            switch (methodCallExpr.Method.Name)
            {
                case "OfType":
                    // Do nothing. Type of expression has been changed so a TypeIs query will be created.
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
                case "Count":
                    if (node.Arguments.Count == 2)
                    {
                        if (_queries.Count > 1) // There is Where in the main expression
                            CombineTwoQueriesOnStack();
                    }
                    this.CountOnly = true;
                    break;
                case "ThenBy":
                case "OrderBy":
                    SortFields.Add(CreateSortFieldFromExpr(node, false));
                    break;
                case "ThenByDescending":
                case "OrderByDescending":
                    SortFields.Add(CreateSortFieldFromExpr(node, true));
                    break;
                case "StartsWith":
                    var startsWithExpr = GetArgumentAsConstant(methodCallExpr, 0);
                    var startsWithArg = (string)startsWithExpr.Value;
                    BuildWildcardQuery(GetPropertyName(methodCallExpr.Object), WildcardPosition.AtEnd, startsWithArg);
                    break;
                case "EndsWith":
                    var endsWithExpr = GetArgumentAsConstant(methodCallExpr, 0);
                    var endsWithArg = (string)endsWithExpr.Value;
                    BuildWildcardQuery(GetPropertyName(methodCallExpr.Object), WildcardPosition.AtStart, endsWithArg);
                    break;
                case "Contains":
                    var arg0 = methodCallExpr.Arguments[0];
                    var constantExpr = arg0 as ConstantExpression;
                    if (constantExpr != null)
                    {
                        if (constantExpr.Type == typeof(string))
                        {
                            var containsArg = (string)constantExpr.Value;
                            BuildWildcardQuery(GetPropertyName(methodCallExpr.Object), WildcardPosition.AtStartAndEnd, containsArg);
                            break;
                        }
                        throw new NotSupportedException(String.Format("Calling Contains on an instance of type {0} is not supported. Allowed types: string, IEnumerable<Node>.", constantExpr.Type));
                    }
                    var memberExpr = arg0 as MemberExpression;
                    if (memberExpr != null)
                    {
                        if (memberExpr.Type == typeof(IEnumerable<Node>))
                        {
                            var rightConstant = methodCallExpr.Arguments[1] as ConstantExpression;
                            if (rightConstant != null)
                            {
                                var nodeValue = (Node)rightConstant.Value;
                                BuildTermQuery(memberExpr.Member.Name, nodeValue);
                                break;
                            }
                            throw NotSupportedException(node, "#1");
                        }
                        throw NotSupportedException(node, "#2");
                    }
                    throw NotSupportedException(node, "#3");
                case "First":
                    this.Top = 1;
                    this.ThrowIfEmpty = true;
                    if (methodCallExpr.Arguments.Count == 2)
                        if (_queries.Count > 1)
                            CombineTwoQueriesOnStack();
                    break;
                case "FirstOrDefault":
                    this.ThrowIfEmpty = false;
                    this.Top = 1;
                    if (methodCallExpr.Arguments.Count == 2)
                        if (_queries.Count > 1) // There is Where in the main expression
                            CombineTwoQueriesOnStack();
                    break;
                case "Any":
                    this.CountOnly = true;
                    this.ExistenceOnly = true;
                    this.Top = 1;
                    if (methodCallExpr.Arguments.Count == 2)
                        if (_queries.Count > 1)
                            CombineTwoQueriesOnStack();
                    break;
                case "Type":
                    var typeExpr = GetArgumentAsConstant(methodCallExpr, 0);
                    BuildTermQuery("Type", (string)typeExpr.Value);
                    break;
                case "TypeIs":
                    var typeIsExpr = GetArgumentAsConstant(methodCallExpr, 0);
                    BuildTermQuery("TypeIs", (string)(typeIsExpr).Value);
                    break;
                case "get_Item":
                    var typedParamExpr = methodCallExpr.Object as ParameterExpression;
                    if (typedParamExpr == null)
                        throw new NotSupportedException("#get_Item");
                    break;
                case "startswith":
                    {
                        var fieldName = GetPropertyName(methodCallExpr.Arguments[0]);
                        var startswithExpr = GetArgumentAsConstant(methodCallExpr, 1);
                        var arg = (string)startswithExpr.Value;
                        BuildWildcardQuery(fieldName, WildcardPosition.AtEnd, arg);
                        break;
                    }
                case "endswith":
                    {
                        var fieldName = GetPropertyName(methodCallExpr.Arguments[0]);
                        var endswithExpr = GetArgumentAsConstant(methodCallExpr, 1);
                        var arg = (string)endswithExpr.Value;
                        BuildWildcardQuery(fieldName, WildcardPosition.AtStart, arg);
                        break;
                    }
                case "substringof":
                    {
                        var fieldName = GetPropertyName(methodCallExpr.Arguments[1]);
                        var substringofExpr = GetArgumentAsConstant(methodCallExpr, 0);
                        var arg = (string)substringofExpr.Value;
                        BuildWildcardQuery(fieldName, WildcardPosition.AtStartAndEnd, arg);
                        break;
                    }
                case "isof":
                    {
                        var isofExpr = GetArgumentAsConstant(methodCallExpr, 1);
                        BuildTermQuery("TypeIs", (string)(isofExpr).Value);
                        break;
                    }
                case "InFolder":
                    {
                        var infolderexpr = GetArgumentAsConstant(methodCallExpr, 0);
                        var folder = infolderexpr.Value;
                        BuildTermQuery("InFolder", GetPath(folder, "InFolder"));
                        break;
                    }
                case "InTree":
                    {
                        var intreeexpr = GetArgumentAsConstant(methodCallExpr, 0);
                        var folder = intreeexpr.Value;
                        BuildTermQuery("InTree", GetPath(folder, "InTree"));
                        break;
                    }
                case "GetType":
                    {
                        var member = methodCallExpr.Object as MemberExpression;
                        if (member != null && member.Member == typeof(Content).GetProperty("ContentHandler"))
                            _queries.Push(new ContentHandlerGetTypeQuery());
                        else
                            throw new NotSupportedException("GetType method is not supported: " + node);
                        break;
                    }
                case "IsAssignableFrom":
                    {
                        var member = methodCallExpr.Object as ConstantExpression;
                        if (member == null)
                            throw new NotSupportedException("IsAssignableFrom method is not supported: " + node);
                        var type = member.Value as Type;
                        if (type == null)
                            throw new NotSupportedException("IsAssignableFrom method is not supported" + node);
                        if (_queries.Count == 0)
                            throw new NotSupportedException("IsAssignableFrom method is not supported" + node);
                        var q = _queries.Pop() as ContentHandlerGetTypeQuery;
                        if (q == null)
                            throw new NotSupportedException("IsAssignableFrom method is not supported" + node);
                        _queries.Push(CreateTypeIsQuery(type));

                        break;
                    }
                default:
                    throw new SnNotSupportedException("Unknown method: " + methodCallExpr.Method.Name);
            }

            return visited;
        }
        protected override Expression VisitNew(NewExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitNew(node);
        }
        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitNewArray(node);
        }
        protected override Expression VisitParameter(ParameterExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitParameter(node);
        }
        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitRuntimeVariables(node);
        }
        protected override Expression VisitSwitch(SwitchExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitSwitch(node);
        }
        protected override SwitchCase VisitSwitchCase(SwitchCase node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitSwitchCase(node);
        }
        protected override Expression VisitTry(TryExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            return base.VisitTry(node);
        }
        protected override Expression VisitUnary(UnaryExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            var unary = (UnaryExpression)base.VisitUnary(node);
            if (unary.NodeType == ExpressionType.Convert)
            {
                if (unary.Type == typeof(bool))
                {
                    var visited = Visit(unary.Operand);
                    string name = null;
                    var callExpr = visited as MethodCallExpression;
                    if (callExpr != null)
                    {
                        if (callExpr.Method.Name == "get_Item" && callExpr.Arguments.Count==1&&callExpr.Arguments[0].Type==typeof(string))
                        {
                            var constExpr = callExpr.Arguments[0] as ConstantExpression;
                            if(constExpr == null)
                                throw NotSupportedException(node, "#4");
                            name = constExpr.Value as string;
                            if(string.IsNullOrEmpty(name))
                                throw new NotSupportedException("Value must be an existing field name: " + constExpr.Value);
                        }
                    }
                    else
                    {
                        var memberExpr = visited as MemberExpression;
                        if (memberExpr != null)
                            name = memberExpr.Member.Name;
                        else
                            throw NotSupportedException(node, "#5");
                    }
                    _queries.Push(new BooleanMemberQuery { FieldName = name, Value = true, FromVisitUnary = true});
                }
            }
            else if (unary.NodeType == ExpressionType.Not)
            {
                var query = _queries.Peek();
                var bmq = query as BooleanMemberQuery;
                if (bmq != null)
                {
                    bmq.Value = !bmq.Value;
                }
                else
                {
                    query = _queries.Pop();
                    var bq = new BooleanQuery();
                    bq.Add(new BooleanClause(query, BooleanClause.Occur.MUST_NOT));
                    _queries.Push(bq);
                }
            }
            return unary;
        }
        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            Trace(MethodInfo.GetCurrentMethod(), node);
            var visited = (TypeBinaryExpression)base.VisitTypeBinary(node);
            _queries.Push(CreateTypeIsQuery(visited.TypeOperand));
            return visited;
        }

        private Stack<Query> _queries = new Stack<Query>();

        private void CombineAllQueriesOnStack()
        {
            CombineQueriesOnStack(_queries.Count);
        }
        private void CombineTwoQueriesOnStack()
        {
            CombineQueriesOnStack(2);
        }
        private void CombineQueriesOnStack(int count)
        {
            Query query;
            BooleanMemberQuery bmq;
            var bq = new BooleanQuery();

            for (int i = 0; i < count; i++)
            {
                query = _queries.Pop();
                bmq = query as BooleanMemberQuery;
                if (bmq != null)
                    query = CreateTermQuery(bmq.FieldName, bmq.Value);
                bq.Add(query, BooleanClause.Occur.MUST);
            }

            _queries.Push(bq);
        }

        private Query CreateTypeIsQuery(Type targetType)
        {
            var contentTypeName = ContentTypeManager.GetContentTypeNameByType(targetType);
            if (contentTypeName == null)
                throw new ApplicationException(String.Format("Unknown Content Type: ", targetType.FullName));
            var term = CreateTerm("TypeIs", ConvertValue("TypeIs", contentTypeName));
            return new TermQuery(term);
        }
        private bool TopLevelQueryIsTypeQuery()
        {
            if (_queries.Count == 0)
                return false;

            var topQuery = _queries.Peek();
            var topTermQuery = topQuery as TermQuery;
            if (topTermQuery == null)
                return false;

            var field = topTermQuery.GetTerm().Field();
            return field == "TypeIs" || field == "Type";
        }
        private bool RemoveDuplicatedTopLevelBooleanMemberQuery(Query query)
        {
            if (_queries.Count == 0)
                return false;

            var bmq = query as BooleanMemberQuery;
            if (bmq == null)
                return false;

            var topBmq = _queries.Peek() as BooleanMemberQuery;
            if (topBmq == null)
                return false;

            if (topBmq.FieldName != bmq.FieldName)
                return false;
            if(!topBmq.FromVisitMember && !topBmq.FromVisitUnary && topBmq.Value != bmq.Value)
                return false;
            return true;
        }

        private string[] _enabledCanonicalFunctionNames = new[] { "startswith", "endswith", "substringof", "isof" };
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
            string name;
            string fieldName = null;
            Type fieldType = null;
            if (IsIndexerAccess(left, out name))
            {
                fieldType = typeof(object);
                fieldName = name;
            }
            else if (left.NodeType == ExpressionType.MemberAccess && right.NodeType == ExpressionType.Constant)
            {
                var leftAsMemberExpr = left as MemberExpression;
                if (leftAsMemberExpr == null)
                    throw new NotSupportedException();

                var parentExpr = leftAsMemberExpr.Expression;
                var parentAsParameter = parentExpr as ParameterExpression;
                if (parentAsParameter != null)
                {
                    if (IsValidType(parentAsParameter.Type))
                    {
                        fieldName = leftAsMemberExpr.Member.Name;
                        fieldType = leftAsMemberExpr.Member.DeclaringType;
                    }
                    else
                    {
                        throw new NotSupportedException("#BuildFieldExpr-1#");
                    }
                }
                else
                {
                    var parentAsMemberExpr = parentExpr as MemberExpression;
                    if (parentAsMemberExpr != null)
                    {
                        var parentParentAsParameter = parentAsMemberExpr.Expression as ParameterExpression;
                        if (parentParentAsParameter != null && IsValidType(parentParentAsParameter.Type) && parentAsMemberExpr.Member.Name == "ContentType" && leftAsMemberExpr.Member.Name == "Name")
                        {
                            fieldName = "Type";
                            fieldType = typeof(ContentType);
                        }
                        else
                        {
                            throw new NotSupportedException("Cannot parse an expression #1: " + parentAsMemberExpr);
                        }
                    }
                    else
                    {
                        if (parentExpr.NodeType == ExpressionType.Convert)
                        {
                            var unary = (UnaryExpression)parentExpr;
                            var targetType = unary.Type;
                            if (typeof(Node).IsAssignableFrom(unary.Type))
                            {
                                fieldName = leftAsMemberExpr.Member.Name;
                                fieldType = leftAsMemberExpr.Member.DeclaringType;
                            }
                            else
                            {
                                throw new NotSupportedException("Cannot a member expression where object is: " + unary.Type.FullName);
                            }
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
            var contentTypeValue = value as ContentType;
            if (contentTypeValue != null && fieldName == "ContentType")
            {
                fieldName = "Type";
                value = contentTypeValue.Name;
            }

            // converting
            Type fieldDatatype;
            var queryFieldValue = ConvertValue(fieldName, value, out fieldDatatype);

            // creating product
            Query query = null;
            switch (node.NodeType)
            {
                case ExpressionType.Equal:               // term
                    if (fieldDatatype == typeof(bool))
                        query = new BooleanMemberQuery { FieldName = fieldName, Value = (bool)value };
                    else
                        query = new TermQuery(CreateTerm(fieldName, queryFieldValue));
                    break;
                case ExpressionType.NotEqual:            // -term
                    if (fieldDatatype == typeof(bool))
                    {
                        var boolValue = !(bool)value;
                        query = new BooleanMemberQuery { FieldName = fieldName, Value = boolValue };
                    }
                    else
                    {
                        var bq = new BooleanQuery();
                        bq.Add(new BooleanClause(new TermQuery(CreateTerm(fieldName, queryFieldValue)), BooleanClause.Occur.MUST_NOT));
                        query = bq;
                    }
                    break;
                case ExpressionType.GreaterThan:         // field:{value TO ]
                    query = CreateRangeQuery(fieldName, queryFieldValue, null, false, true);
                    break;
                case ExpressionType.GreaterThanOrEqual:  // field:[value TO ]
                    query = CreateRangeQuery(fieldName, queryFieldValue, null, true, true);
                    break;
                case ExpressionType.LessThan:            // field:[ to value}
                    query = CreateRangeQuery(fieldName, null, queryFieldValue, true, false);
                    break;
                case ExpressionType.LessThanOrEqual:     // field:[ to value]
                    query = CreateRangeQuery(fieldName, null, queryFieldValue, true, true);
                    break;
                default:
                    throw new SnNotSupportedException(string.Format("NodeType {0} isn't implemented", node.NodeType));
            }
            if (node.Type == typeof(bool))
                if (RemoveDuplicatedTopLevelBooleanMemberQuery(query))
                    _queries.Pop();
            _queries.Push(query);
        }
        internal static TermQuery CreateTermQuery(string fieldName, object value)
        {
            var queryFieldValue = ConvertValue(fieldName, value);
            return new TermQuery(CreateTerm(fieldName, queryFieldValue));
        }
        private void BuildTermQuery(string fieldName, object value)
        {
            _queries.Push(CreateTermQuery(fieldName, value));
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
            BooleanQuery bq;
            Query query = null;
            switch (node.NodeType)
            {
                case ExpressionType.Or: // |
                    throw new NotSupportedException("#BuildBooleanExpr/ExpressionType.Or");
                case ExpressionType.And: // &
                    throw new NotSupportedException("#BuildBooleanExpr/ExpressionType.And");
                case ExpressionType.AndAlso: // &&
                    bq = new BooleanQuery();
                    bq.Add(_queries.Pop(), BooleanClause.Occur.MUST);
                    bq.Add(_queries.Pop(), BooleanClause.Occur.MUST);
                    query = bq;
                    break;
                case ExpressionType.OrElse: // ||
                    bq = new BooleanQuery();
                    bq.Add(_queries.Pop(), BooleanClause.Occur.SHOULD);
                    bq.Add(_queries.Pop(), BooleanClause.Occur.SHOULD);
                    query = bq;
                    break;
                default:
                    throw new SnNotSupportedException(string.Format("NodeType {0} isn't implemented", node.NodeType));
            }
            _queries.Push(query);
        }

        private static Term CreateTerm(string name, ContentType contentType)
        {
            if (name != "ContentType")
                throw new ApplicationException("Value of the ContentType field must be ContentType.");
            name = "TypeIs";
            var value = contentType.Name;
            var queryFieldValue = ConvertValue(name, value);

            return new Term(name, queryFieldValue.StringValue);
        }
        private static Term CreateTerm(string name, QueryFieldValue queryFieldValue)
        {
            string numval;
            switch (queryFieldValue.Datatype)
            {
                case IndexableDataType.String:
                    return new Term(name, queryFieldValue.StringValue);
                case IndexableDataType.Int: numval = NumericUtils.IntToPrefixCoded(queryFieldValue.IntValue); break;
                case IndexableDataType.Long: numval = NumericUtils.LongToPrefixCoded(queryFieldValue.LongValue); break;
                case IndexableDataType.Float: numval = NumericUtils.FloatToPrefixCoded(queryFieldValue.SingleValue); break;
                case IndexableDataType.Double: numval = NumericUtils.DoubleToPrefixCoded(queryFieldValue.DoubleValue); break;
                default:
                    throw new SnNotSupportedException("Unknown IndexableDataType enum value: " + queryFieldValue.Datatype);
            }

            var numterm = new Term(name, numval);
            return numterm;
        }
        private static QueryFieldValue ConvertValue(string name, object value)
        {
            Type fieldDataType;
            return ConvertValue(name, value, out fieldDataType);
        }
        private static QueryFieldValue ConvertValue(string name, object value, out Type fieldDataType)
        {
            var contentTypeValue = value as ContentType;
            if (contentTypeValue != null && name == "ContentType")
            {
                name = "TypeIs";
                value = contentTypeValue.Name;
            }

            var fieldInfo = SenseNet.ContentRepository.Schema.ContentTypeManager.GetPerFieldIndexingInfo(name);
            if (fieldInfo == null)
                throw new InvalidOperationException("Unknown field: " + name);
            var converter = fieldInfo.IndexFieldHandler;

            var queryFieldValue = new QueryFieldValue(value);
            converter.ConvertToTermValue(queryFieldValue);

            fieldDataType = fieldInfo.FieldDataType;
            return queryFieldValue;
        }

        private Query CreateRangeQuery(string fieldName, QueryFieldValue minValue, QueryFieldValue maxValue, bool includeLower, bool includeUpper)
        {
            QueryFieldValue value = minValue == null ? maxValue : minValue;
            var dataType = value.Datatype;
            switch (dataType)
            {
                case IndexableDataType.String:
                    var lowerTerm = minValue == null ? null : minValue.StringValue;
                    var upperTerm = maxValue == null ? null : maxValue.StringValue;
                    return new TermRangeQuery(fieldName, lowerTerm, upperTerm, includeLower, includeUpper);
                case IndexableDataType.Int:
                    var lowerInt = minValue == null ? null : (Int32?)minValue.IntValue;
                    var upperInt = maxValue == null ? null : (Int32?)maxValue.IntValue;
                    return NumericRangeQuery.NewIntRange(fieldName, lowerInt, upperInt, includeLower, includeUpper);
                case IndexableDataType.Long:
                    var lowerLong = minValue == null ? null : (Int64?)minValue.LongValue;
                    var upperLong = maxValue == null ? null : (Int64?)maxValue.LongValue;
                    return NumericRangeQuery.NewLongRange(fieldName, 8, lowerLong, upperLong, includeLower, includeUpper);
                case IndexableDataType.Float:
                    var lowerFloat = minValue == null ? Single.MinValue : minValue.SingleValue;
                    var upperFloat = maxValue == null ? Single.MaxValue : maxValue.SingleValue;
                    return NumericRangeQuery.NewFloatRange(fieldName, lowerFloat, upperFloat, includeLower, includeUpper);
                case IndexableDataType.Double:
                    var lowerDouble = minValue == null ? Double.MinValue : minValue.DoubleValue;
                    var upperDouble = maxValue == null ? Double.MaxValue : maxValue.DoubleValue;
                    return NumericRangeQuery.NewDoubleRange(fieldName, 8, lowerDouble, upperDouble, includeLower, includeUpper);
                default:
                    throw new SnNotSupportedException("Unknown IndexableDataType: " + dataType);
            }
        }

        private static IndexableDataType? GetIndexableDataType(object value)
        {
            if (value == null)
                return null;
            if (value is string)
                return IndexableDataType.String;
            if (value is Int32)
                return IndexableDataType.Int;
            if (value is Int64)
                return IndexableDataType.Long;
            if (value is float)
                return IndexableDataType.Float;
            if (value is double)
                return IndexableDataType.Double;
            throw new NotSupportedException("Datatype is not supported: " + value.GetType().FullName);
        }

        private SortField CreateSortFieldFromExpr(MethodCallExpression node, bool reverse)
        {
            string name;

            var unaryExpr = node.Arguments[1] as UnaryExpression;
            if (unaryExpr == null)
                throw new Exception("Invalid sort: " + node.ToString());

            var operand = unaryExpr.Operand as LambdaExpression;
            if (operand == null)
                throw new Exception("Invalid sort: " + node.ToString());

            var memberExpr = operand.Body as MemberExpression;
            if (memberExpr != null)
            {
                name = memberExpr.Member.Name;
                return LucQuery.CreateSortField(name, reverse);
            }

            var methodCallExpr = operand.Body as MethodCallExpression;
            if (methodCallExpr == null)
                throw new Exception("Invalid sort: " + node.ToString());

            if (methodCallExpr.Method.Name != "get_Item")
                throw new Exception("Invalid sort: " + node.ToString());

            if (methodCallExpr.Arguments.Count != 1)
                throw new Exception("Invalid sort: " + node.ToString());

            var nameExpr = methodCallExpr.Arguments[0] as ConstantExpression;
            if (nameExpr == null)
                throw new Exception("Invalid sort: " + node.ToString());

            name = (string)nameExpr.Value;
            return LucQuery.CreateSortField(name, reverse);
        }

        private enum WildcardPosition { AtStart, AtEnd, AtStartAndEnd }
        private void BuildWildcardQuery(string field, WildcardPosition type, string arg)
        {
            var fieldInfo = SenseNet.ContentRepository.Schema.ContentTypeManager.GetPerFieldIndexingInfo(field);
            if (fieldInfo == null)
                throw new InvalidOperationException("Unknown field: " + field);
            var queryFieldValue = new QueryFieldValue(arg);
            fieldInfo.IndexFieldHandler.ConvertToTermValue(queryFieldValue);
            if (queryFieldValue.Datatype != IndexableDataType.String)
                throw new NotSupportedException("Wildcard query only can be built on string based Field.");

            string text;
            switch (type)
            {
                case WildcardPosition.AtStart: text = "*" + queryFieldValue.StringValue; break;
                case WildcardPosition.AtEnd: text = queryFieldValue.StringValue + "*"; break;
                case WildcardPosition.AtStartAndEnd: text = string.Concat("*", queryFieldValue.StringValue, "*"); break;
                default: throw new SnNotSupportedException("WildcardType is not supported: " + type);
            }
            var wq = new WildcardQuery(new Term(field, text));
            _queries.Push(wq);
        }

        private string GetPropertyName(Expression expr)
        {
            var memberExpr = expr as MemberExpression;
            if (memberExpr != null)
                return memberExpr.Member.Name;

            var unaryExpr = expr as UnaryExpression;
            if (unaryExpr != null)
                return GetPropertyName(unaryExpr.Operand);

            var methodCallExpr = expr as MethodCallExpression;
            if (methodCallExpr != null)
            {
                if (methodCallExpr.Method.Name == "get_Item" && methodCallExpr.Object.Type == typeof(Content))
                {
                    // content indexer --> return with field name
                    var constantArgumentExpr = methodCallExpr.Arguments[0] as ConstantExpression;
                    if (constantArgumentExpr != null)
                        return (string)constantArgumentExpr.Value;
                    else
                        throw new SnNotSupportedException("SnLinq: GetPropertyName from Content indexer expression");
                }
                else
                {
                    throw new SnNotSupportedException("SnLinq: GetPropertyName from MethodCallExpression");
                }
            }
            throw new SnNotSupportedException("SnLinq: GetPropertyName from Expression");
        }

        // --------------------------------------------------------------------------- helpers
        private bool IsValidType(Type type)
        {
            return type == typeof(Content) || typeof(Node).IsAssignableFrom(type);
        }
        private static string GetPath(object folder, string methodName)
        {
            string path = null;
            var str = folder as string;
            if (str != null)
            {
                path = str;
            }
            else
            {
                var content = folder as Content;
                if (content != null)
                {
                    path = content.Path;
                }
                else
                {
                    var fldr = folder as Node;
                    if (fldr != null)
                    {
                        path = fldr.Path;
                    }
                    else
                    {
                        throw new NotSupportedException(methodName + " method is not supported on the following object: " + folder.ToString());
                    }
                }
            }
            return path;
        }
        private Exception NotSupportedException(Expression node, string id)
        {
            return new NotSupportedException(String.Format("The expression ({1}) cannot be executed: {0}", node, id));
        }
        private ConstantExpression GetArgumentAsConstant(MethodCallExpression methodCallExpr, int argumentIndex)
        {
            var constantExpr = methodCallExpr.Arguments[argumentIndex] as ConstantExpression;
            if (constantExpr == null)
                throw new NotSupportedException(String.Format("Argument {0} is invalid in a method call. Argument must be constant or transformable to constant. {1}", argumentIndex, methodCallExpr));
            return constantExpr;
        }

    }
}
