using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Search.Querying.Parser;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.ContentRepository.Search
{
    public class PredicationEngine : SnQueryVisitor
    {
        //private readonly Content _content;
        private readonly IndexDocument _indexDoc;
        private readonly IQueryContext _queryContext;
        private readonly Stack<bool> _hitStack = new Stack<bool>();

        public PredicationEngine(Content content)
        {
            //_content = content;
            _indexDoc = GetIndexDocument(content.ContentHandler);
            _queryContext = new SnQueryContext(QuerySettings.Default, User.Current.Id);
        }
        public IndexDocument GetIndexDocument(Node node)
        {
            //UNDONE:<?predication: Somehow store the index document after saving and get the stored object here, instead of recreating it.
            // Problem: the index doc finalization doing in an async indexing task and it maybe not ready yet.
            var docProvider = Providers.Instance.IndexDocumentProvider;
            var doc = docProvider.GetIndexDocument(node, false, node.Id == 0, out var _);
            var docData = DataStore.CreateIndexDocumentData(node, doc, null);
            IndexManager.CompleteIndexDocument(docData);
            return doc;
        }

        public bool IsTrue(string predication)
        {
            return IsTrue(SnQuery.Parse(predication, _queryContext));
        }
        public bool IsTrue(SnQuery predication)
        {
            Visit(predication.QueryTree);

            if (_hitStack.Count == 0)
                throw new CompilerException("Compiler error: The stack does not contain any elements.");
            if (_hitStack.Count != 1)
                throw new CompilerException($"Compiler error: The stack contains more than one elements ({_hitStack.Count}).");

            var result = _hitStack.Pop();

            return result;
        }

        // ========================================================================================

        public override SnQueryPredicate VisitSimplePredicate(SimplePredicate simplePredicate)
        {
            var result = false;

            var value = simplePredicate.Value;
            if (_indexDoc.Fields.TryGetValue(simplePredicate.FieldName, out var field))
            {
                if (IsWildcardPredicate(field, value))
                    result = GetResultByWildcard(field.StringValue, value.StringValue);
                else if (field.Type == IndexValueType.StringArray && value.Type == IndexValueType.String)
                    result = field.StringArrayValue.Contains(value.StringValue);
                else if (field.Type == value.Type && field.CompareTo(value) == 0)
                    result = true;
            }
            _hitStack.Push(result);

            return simplePredicate;
        }
        private bool IsWildcardPredicate(IndexField field, IndexValue predicationValue)
        {
            return predicationValue.Type == IndexValueType.String && field.Type == IndexValueType.String &&
                    (predicationValue.StringValue.Contains("*") || predicationValue.StringValue.Contains("?"));
        }
        private bool GetResultByWildcard(string fieldValue, string value)
        {
            if (value.Contains("?"))
                throw new NotSupportedException("Wildcard '?' not supported.");
            if (value.Replace("*", "").Length == 0)
                throw new NotSupportedException($"Query is not supported for this field value: {value}");

            string prefix, suffix;

            if (value.StartsWith("*") && value.EndsWith("*"))
            {
                var middle = value.Trim('*');
                return fieldValue.Contains(middle);
            }

            if (value.StartsWith("*"))
            {
                suffix = value.Trim('*');
                return fieldValue.EndsWith(suffix);
            }

            if (value.EndsWith("*"))
            {
                prefix = value.Trim('*');
                return fieldValue.StartsWith(prefix);
            }

            // if (value.Contains("*"))
            var sa = value.Split("*".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            prefix = sa[0];
            suffix = sa[1];
            return fieldValue.StartsWith(prefix) && fieldValue.EndsWith(suffix);
        }

        public override SnQueryPredicate VisitRangePredicate(RangePredicate range)
        {
            var result = false;

            if (_indexDoc.Fields.TryGetValue(range.FieldName, out var field))
            {
                var min = range.Min;
                var max = range.Max;

                // play permutation of min, max and exclusiveness
                if (min != null && max != null)
                {
                    if (min.Type == field.Type && max.Type == field.Type)
                    {
                        if (!range.MinExclusive && !range.MaxExclusive)
                            result = field >= min && field <= max;
                        else if (!range.MinExclusive && range.MaxExclusive)
                            result = field >= min && field < max;
                        else if (range.MinExclusive && !range.MaxExclusive)
                            result = field > min && field <= max;
                        else
                            result = field > min && field < max;
                    }
                }
                else if (min != null)
                {
                    if (min.Type == field.Type)
                        result = range.MinExclusive ? field > min : field >= min;
                }
                else
                {
                    if (max.Type == field.Type)
                        result = range.MaxExclusive ? field < max : field <= max;
                }
            }

            _hitStack.Push(result);

            return range;
        }

        public override List<LogicalClause> VisitLogicalClauses(List<LogicalClause> clauses)
        {
            // interpret every clause in deep
            var visitedClauses = base.VisitLogicalClauses(clauses);

            // clause categories
            var shouldSubset = false;
            var mustSubset = true;
            var notSubset = true;

            // pop every subset belonging to clauses and categorize them
            for (int i = visitedClauses.Count - 1; i >= 0; i--)
            {
                var current = _hitStack.Pop();

                var occur = visitedClauses[i].Occur;
                if (occur == Occurence.Default)
                    occur = Occurence.Should;

                switch (occur)
                {
                    case Occurence.Should:
                        shouldSubset |= current;
                        break;
                    case Occurence.Must: 
                        mustSubset &= current;
                        break;
                    case Occurence.MustNot:
                        notSubset &= !current;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // combine categories
            var result = (mustSubset & notSubset) | shouldSubset;

            // push result to the hit stack
            _hitStack.Push(result);

            // return the original parameter
            return clauses;
        }
    }
}
