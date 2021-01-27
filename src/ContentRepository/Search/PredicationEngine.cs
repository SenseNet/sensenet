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
        private IndexDocument _indexDoc;
        private readonly IQueryContext _context;
        private readonly Stack<List<int>> _hitStack = new Stack<List<int>>();

        public PredicationEngine(Content content, IQueryContext context = null)
        {
            //_content = content;
            _indexDoc = GetIndexDocument(content.ContentHandler);
            _context = context ?? new SnQueryContext(QuerySettings.Default, User.Current.Id);
        }
        public IndexDocument GetIndexDocument(Node node)
        {
            //UNDONE:<?predication: Somehow store the index document after saving and get the stored object here, instead of recreating it.
            // Problem: the index doc finalization doing in an async indexing task and it maybe not ready yet.
            var docProvider = Providers.Instance.IndexDocumentProvider;
            var doc = docProvider.GetIndexDocument(node, false, node.Id == 0, out var hasBinary);
            var docData = DataStore.CreateIndexDocumentData(node, doc, null);
            IndexManager.CompleteIndexDocument(docData);
            return doc;
        }

        public bool IsTrue(string predication)
        {
            return IsTrue(SnQuery.Parse(predication, _context));
        }
        public bool IsTrue(SnQuery predication)
        {
            Visit(predication.QueryTree);

            if (_hitStack.Count == 0)
                throw new CompilerException("Compiler error: The stack does not contain any elements.");
            if (_hitStack.Count != 1)
                throw new CompilerException($"Compiler error: The stack contains more than one elements ({_hitStack.Count}).");

            var foundVersionIds = _hitStack.Pop();

            return foundVersionIds.Count > 0;
        }

        // ========================================================================================

        public override SnQueryPredicate VisitSimplePredicate(SimplePredicate simplePredicate)
        {
            var result = new List<int>();

            var value = simplePredicate.Value;
            if (_indexDoc.Fields.TryGetValue(simplePredicate.FieldName, out var field))
            {
                if (value.Type == IndexValueType.String && field.Type == IndexValueType.String &&
                    (value.StringValue.Contains("*") || value.StringValue.Contains("?")))
                {
                    result.AddRange(GetVersionIdsByWildcard(field.StringValue, value.StringValue));
                }
                else
                {
                    if (field.Type == value.Type && field.CompareTo(value) == 0) //UNDONE:<?predication: ? Operator == overload ?
                        result.AddRange(new[] { 1 }); //UNDONE:<?predication: simplify result
                }
            }
            _hitStack.Push(result);
            return simplePredicate;
        }
        private IEnumerable<int> GetVersionIdsByWildcard(string fieldValue, string value)
        {
            if (value.Contains("?"))
                throw new NotSupportedException("Wildcard '?' not supported.");
            if (value.Replace("*", "").Length == 0)
                throw new NotSupportedException($"Query is not supported for this field value: {value}");

            List<int>[] versionIds;
            if (value.StartsWith("*") && value.EndsWith("*"))
            {
                var middle = value.Trim('*');
                versionIds = fieldValue.Contains(middle) ? new[] { new List<int> { 1 } } : new List<int>[0];
            }
            else if (value.StartsWith("*"))
            {
                var suffix = value.Trim('*');
                versionIds = fieldValue.EndsWith(suffix) ? new[] { new List<int> { 1 } } : new List<int>[0];
            }
            else if (value.EndsWith("*"))
            {
                var prefix = value.Trim('*');
                versionIds = fieldValue.StartsWith(prefix) ? new[] { new List<int> { 1 } } : new List<int>[0];
            }
            else // if (value.Contains("*"))
            {
                var sa = value.Split("*".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                var prefix = sa[0];
                var suffix = sa[1];
                versionIds = fieldValue.StartsWith(prefix) && fieldValue.EndsWith(suffix) ? new[] { new List<int> { 1 } } : new List<int>[0];
            }

            // aggregate
            var result = new int[0].AsEnumerable();
            foreach (var item in versionIds)
                result = result.Union(item);

            return result.Distinct().ToArray();
        }

        public override SnQueryPredicate VisitRangePredicate(RangePredicate range)
        {
            var result = new List<int>();

            if (_indexDoc.Fields.TryGetValue(range.FieldName, out var field))
            {
                var fieldValues = new KeyValuePair<IndexValue, List<int>>[]
                {
                    // FieldValue -> VersionId list
                    new KeyValuePair<IndexValue, List<int>>(field, new List<int>(new[] {1})),
                };

                var min = range.Min;
                var max = range.Max;
                IEnumerable<KeyValuePair<IndexValue, List<int>>> expression = null;

                // play permutation of min, max and exclusiveness
                if (min != null && max != null)
                {
                    if (min.Type == field.Type && max.Type == field.Type)
                    {
                        if (!range.MinExclusive && !range.MaxExclusive)
                            expression = fieldValues.Where(
                                x => x.Key.CompareTo(min) >= 0 && x.Key.CompareTo(max) <= 0);
                        else if (!range.MinExclusive && range.MaxExclusive)
                            expression = fieldValues.Where(
                                x => x.Key.CompareTo(min) >= 0 && x.Key.CompareTo(max) < 0);
                        else if (range.MinExclusive && !range.MaxExclusive)
                            expression = fieldValues.Where(
                                x => x.Key.CompareTo(min) > 0 && x.Key.CompareTo(max) <= 0);
                        else
                            expression = fieldValues.Where(
                                x => x.Key.CompareTo(min) > 0 && x.Key.CompareTo(max) < 0);
                    }
                }
                else if (min != null)
                {
                    if (min.Type == field.Type)
                    {
                        expression = !range.MinExclusive
                            ? fieldValues.Where(x => x.Key.CompareTo(min) >= 0)
                            : fieldValues.Where(x => x.Key.CompareTo(min) > 0);
                    }
                }
                else
                {
                    if (max.Type == field.Type)
                    {
                        expression = !range.MaxExclusive
                            ? fieldValues.Where(x => x.Key.CompareTo(max) <= 0)
                            : fieldValues.Where(x => x.Key.CompareTo(max) < 0);
                    }
                }

                if (expression != null)
                {
                    var lists = expression.Select(x => x.Value).ToArray();

                    // aggregate
                    var aggregation = new int[0].AsEnumerable();
                    foreach (var item in lists)
                        aggregation = aggregation.Union(item);
                    result = aggregation.Distinct().ToList();
                }
            }

            _hitStack.Push(result);

            return range;
        }

        public override List<LogicalClause> VisitLogicalClauses(List<LogicalClause> clauses)
        {
            // interpret every clause in deep
            var visitedClauses = base.VisitLogicalClauses(clauses);

            // pop every subset belonging to clauses and categorize them
            var shouldSubset = new List<int>();
            var mustSubset = new List<int>();
            var notSubset = new List<int>();
            var firstMust = true;
            var firstMustNot = true;

            for (int i = visitedClauses.Count - 1; i >= 0; i--)
            {
                var clause = visitedClauses[i];

                var currentSubset = _hitStack.Pop();
                var occur = clause.Occur == Occurence.Default ? Occurence.Should : clause.Occur;
                switch (occur)
                {
                    case Occurence.Should:
                        shouldSubset = shouldSubset.Union(currentSubset).Distinct().ToList();
                        break;
                    case Occurence.Must:
                        if (firstMust)
                        {
                            mustSubset = currentSubset;
                            firstMust = false;
                        }
                        else
                        {
                            mustSubset = mustSubset.Intersect(currentSubset).ToList();
                        }
                        break;
                    case Occurence.MustNot:
                        if (firstMustNot)
                        {
                            notSubset = currentSubset;
                            firstMustNot = false;
                        }
                        else
                        {
                            notSubset = notSubset.Union(currentSubset).Distinct().ToList();
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // combine the subsets (if there is any "must", the "should" is irrelevant)
            var result = (mustSubset.Count > 0 ? mustSubset : shouldSubset).Except(notSubset).ToList();

            // push result to the hit stack
            _hitStack.Push(result);

            // return the original parameter
            return clauses;
        }
    }
}
