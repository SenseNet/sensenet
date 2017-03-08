using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Search.Internal;
using SenseNet.ContentRepository.Storage.Schema;
using Lucene.Net.Search;
using Lucene.Net.Index;

namespace SenseNet.Search
{
    internal class SnLucCompiler : INodeQueryCompiler
    {
        private enum LikeOperator { None, StartsWith, Contains, EndsWith }
        private SearchExpression _searchExpression;
        private bool _hasFullTextSearch;
        private IDictionary<string, Type> _analyzers;

        private NodeQuery _nodeQuery;
        public Query CompiledQuery { get; private set; }

        public SnLucCompiler()
        {
            _analyzers = SenseNet.ContentRepository.Storage.StorageContext.Search.SearchEngine.GetAnalyzers();
        }

        // ====================================================================================== INodeQueryCompiler Members

        public string Compile(NodeQuery query, out NodeQueryParameter[] parameters)
        {
            _nodeQuery = query;

            CompiledQuery = TreeWalker(query);

            parameters = new NodeQueryParameter[0];
            return CompiledQuery.ToString();
        }

        // ====================================================================================== 

        private Query TreeWalker(Expression exp)
        {
            return CompileExpressionNode(exp);
        }
        private Query CompileExpressionNode(Expression expression)
        {
            ExpressionList expList;
            NotExpression notExp;
            ReferenceExpression refExp;
            SearchExpression textExp;
            TypeExpression typeExp;
            IBinaryExpressionWrapper binExp;

            if ((expList = expression as ExpressionList) != null)
                return CompileExpressionListNode(expList);
            else if ((notExp = expression as NotExpression) != null)
                return CompileNotExpressionNode(notExp);
            else if ((refExp = expression as ReferenceExpression) != null)
                return CompileReferenceExpressionNode(refExp);
            else if ((textExp = expression as SearchExpression) != null)
                return CompileSearchExpressionNode(textExp);
            else if ((typeExp = expression as TypeExpression) != null)
                return CompileTypeExpressionNode(typeExp);
            else if ((binExp = expression as IBinaryExpressionWrapper) != null)
                return CompileBinaryExpressionNode(binExp);
            throw new SenseNet.ContentRepository.Storage.SnNotSupportedException("Unknown expression type: " + expression.GetType().FullName);
        }

        private Query CompileExpressionListNode(ExpressionList expression)
        {
            int expCount = expression.Expressions.Count;
            if (expCount == 0)
                throw new NotSupportedException("Do not use empty ExpressionList");

            if (expression.Expressions.Count == 1)
                return CompileExpressionNode(expression.Expressions[0]);

            var result = new BooleanQuery();
            var occur = (expression.OperatorType == ChainOperator.And) ? BooleanClause.Occur.MUST : BooleanClause.Occur.SHOULD;
            foreach (Expression expr in expression.Expressions)
            {
                Query q;
                BooleanClause clause;
                var notExp = expr as NotExpression;
                if (notExp != null)
                {
                    q = CompileExpressionNode(notExp.Expression);
                    clause = new BooleanClause(q, BooleanClause.Occur.MUST_NOT);
                }
                else
                {
                    var binwrapper = expr as IBinaryExpressionWrapper;
                    if (binwrapper != null && binwrapper.BinExp.Operator == Operator.NotEqual)
                    {
                        q = CompileBinaryExpression(binwrapper.BinExp.LeftValue, Operator.Equal, binwrapper.BinExp.RightValue);
                        clause = new BooleanClause(q, BooleanClause.Occur.MUST_NOT);
                    }
                    else
                    {
                        q = CompileExpressionNode(expr);
                        clause = new BooleanClause(q, occur);
                    }
                }
                result.Add(clause);
            }
            return result;
        }
        private Query CompileNotExpressionNode(NotExpression expression)
        {
            var q = CompileExpressionNode(expression.Expression);
            var result = new BooleanQuery();
            var clause = new BooleanClause(q, BooleanClause.Occur.MUST_NOT);
            result.Add(clause);
            return result;
        }
        private Query CompileSearchExpressionNode(SearchExpression expression)
        {
            if (!(expression.Parent is NodeQuery))
                throw new NotSupportedException();
            if (_searchExpression != null)
                throw new NotSupportedException();
            _searchExpression = expression;
            if (_hasFullTextSearch)
                throw new NotSupportedException("More than one fulltext expression!");
            _hasFullTextSearch = true;

            var text = expression.FullTextExpression.Trim('"').TrimEnd('*');

            var phrases = TextSplitter.SplitText(null, text, _analyzers);
            if (phrases.Length > 1)
            {
                // _Text:"text text text"
                var phq = new PhraseQuery();

                var validPhrases = phrases.Except(Lucene.Net.Analysis.Standard.StandardAnalyzer.STOP_WORDS_SET);

                foreach (var phrase in validPhrases)
                    phq.Add(new Term(LucObject.FieldName.AllText, phrase));
                return phq;
            }
            // _Text:text
            var term = new Term(LucObject.FieldName.AllText, text);
            var result = new TermQuery(term);
            return result;
        }
        private Query CompileTypeExpressionNode(TypeExpression expression)
        {
            if (expression.ExactMatch)
                return new TermQuery(new Term(LucObject.FieldName.Type, expression.NodeType.Name));
            return new TermQuery(new Term(LucObject.FieldName.TypeIs, expression.NodeType.Name));
        }
        private Query CompileReferenceExpressionNode(ReferenceExpression expression)
        {
            if (expression.Expression != null)
                throw new SenseNet.ContentRepository.Storage.SnNotSupportedException();

            var leftName = CompileLeftValue(expression.ReferrerProperty);

            if (expression.ExistenceOnly)
            {
                // not null --> not empty --> -(name:)
                var boolQuery = new BooleanQuery();
                boolQuery.Add(new BooleanClause(new TermQuery(new Term(leftName)), BooleanClause.Occur.MUST_NOT));
                return boolQuery;
            }
            return new TermQuery(new Term(leftName, expression.ReferencedNode.Id.ToString()));
        }
        private Query CompileBinaryExpressionNode(IBinaryExpressionWrapper expression)
        {
            return CompileBinaryExpression(expression.BinExp.LeftValue, expression.BinExp.Operator, expression.BinExp.RightValue);
        }
        private Query CompileBinaryExpression(PropertyLiteral left, Operator op, Literal right)
        {
            if (!right.IsValue)
                throw new NotSupportedException();

            var formatterName = left.DataType.ToString();
            if (left.Name == "Path" && op == Operator.StartsWith)
                return CompileInTreeQuery((string)right.Value);

            var leftName = CompileLeftValue(left);
            var rightValue = CompileLiteralValue(right.Value, formatterName);
            Term rightTerm = CreateRightTerm(leftName, op, rightValue);
            switch (op)
            {
                case Operator.StartsWith:                                  // left:right*
                    return new PrefixQuery(rightTerm);
                case Operator.EndsWith:                                    // left:*right
                case Operator.Contains:                                    // left:*right*
                    return new WildcardQuery(rightTerm);
                case Operator.Equal:                                       // left:right
                    return new TermQuery(rightTerm);
                case Operator.NotEqual:                                    // -(left:right)
                    throw new NotSupportedException("##Wrong optimizer");
                case Operator.LessThan:                                    // left:{minValue TO right}
                    return new TermRangeQuery(leftName, CompileMinValue(left), rightValue, false, false);
                case Operator.GreaterThan:                                 // left:{right TO maxValue}
                    return new TermRangeQuery(leftName, rightValue, CompileMaxValue(left), false, false);
                case Operator.LessThanOrEqual:                             // left:[minValue TO right]
                    return new TermRangeQuery(leftName, CompileMinValue(left), rightValue, true, true);
                case Operator.GreaterThanOrEqual:                          // left:[right TO maxValue]
                    return new TermRangeQuery(leftName, rightValue, CompileMaxValue(left), true, true);
                default:
                    throw new SenseNet.ContentRepository.Storage.SnNotSupportedException();
            }
        }

        private Query CompileInTreeQuery(string path)
        {
            if (!path.EndsWith("/"))
                return new TermQuery(new Term(LucObject.FieldName.InTree, "\"" + path.ToLowerInvariant() + "\""));

            var trimmedWrapped = "\"" + path.ToLowerInvariant().TrimEnd('/') + "\"";
            var q = new BooleanQuery();
            q.Add(new BooleanClause(new TermQuery(new Term(LucObject.FieldName.Path, trimmedWrapped)), BooleanClause.Occur.MUST_NOT));
            q.Add(new BooleanClause(new TermQuery(new Term(LucObject.FieldName.InTree, trimmedWrapped)), BooleanClause.Occur.MUST));
            return q;
        }

        private Term CreateRightTerm(string leftName, Operator op, string text)
        {
            text = text.Trim('"');
            var t = new StringBuilder(text);
            if (op == Operator.EndsWith)
                t.Insert(0, '*');
            else if (op == Operator.Contains)
                t.Insert(0, '*').Append('*');
            if(op!= Operator.StartsWith)
                t.Insert(0, '"').Append('"');
            return new Term(leftName, t.ToString());
        }

        private string CompileLeftValue(PropertyLiteral propLit)
        {
            if (propLit.IsSlot)
                return CompilePropertySlot(propLit.PropertySlot);
            else
                return CompileNodeAttribute(propLit.NodeAttribute);
        }
        private string CompilePropertySlot(PropertyType slot)
        {
            return slot.Name;
        }
        private string CompileNodeAttribute(NodeAttribute attr)
        {
            switch (attr)
            {
                case NodeAttribute.Id: return LucObject.FieldName.NodeId;
                case NodeAttribute.IsDeleted: return LucObject.FieldName.IsDeleted;
                case NodeAttribute.IsInherited: return LucObject.FieldName.IsInherited;
                case NodeAttribute.ParentId: return LucObject.FieldName.ParentId;
                case NodeAttribute.Parent: return LucObject.FieldName.ParentId;
                case NodeAttribute.Name: return LucObject.FieldName.Name;
                case NodeAttribute.Path: return LucObject.FieldName.Path;
                case NodeAttribute.Index: return LucObject.FieldName.Index;
                case NodeAttribute.Locked: return LucObject.FieldName.Locked;
                case NodeAttribute.LockedById: return LucObject.FieldName.LockedById;
                case NodeAttribute.LockedBy: return LucObject.FieldName.LockedById;
                case NodeAttribute.ETag: return LucObject.FieldName.ETag;
                case NodeAttribute.LockType: return LucObject.FieldName.LockType;
                case NodeAttribute.LockTimeout: return LucObject.FieldName.LockTimeout;
                case NodeAttribute.LockDate: return LucObject.FieldName.LockDate;
                case NodeAttribute.LockToken: return LucObject.FieldName.LockToken;
                case NodeAttribute.LastLockUpdate: return LucObject.FieldName.LastLockUpdate;
                case NodeAttribute.MajorVersion: return LucObject.FieldName.MajorNumber;
                case NodeAttribute.MinorVersion: return LucObject.FieldName.MinorNumber;
                case NodeAttribute.CreationDate: return LucObject.FieldName.CreationDate;
                case NodeAttribute.CreatedById: return LucObject.FieldName.CreatedById;
                case NodeAttribute.CreatedBy: return LucObject.FieldName.CreatedById;
                case NodeAttribute.ModificationDate: return LucObject.FieldName.ModificationDate;
                case NodeAttribute.ModifiedById: return LucObject.FieldName.ModifiedById;
                case NodeAttribute.ModifiedBy: return LucObject.FieldName.ModifiedById;
                case NodeAttribute.IsSystem: return LucObject.FieldName.IsSystem;
                case NodeAttribute.OwnerId: return LucObject.FieldName.OwnerId;
                case NodeAttribute.SavingState: return LucObject.FieldName.SavingState;
                default:
                    throw new NotSupportedException(String.Concat("NodeAttribute attr = ", attr, ")"));
            }
        }
        private string CompileLiteralValue(object value, string formatterName)
        {
            return String.Concat("\"", value, "\"");
        }
        private string CompileMinValue(PropertyLiteral literal)
        {
            return null;
        }
        private string CompileMaxValue(PropertyLiteral literal)
        {
            return null;
        }
    }
}
