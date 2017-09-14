using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SenseNet.Search;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.ContentRepository.Tests.Implementations
{
    internal class InMemoryQueryEngine : IQueryEngine
    {
        private class Hit
        {
            public int NodeId;
            public int VersionId;
            public bool IsLastPublic;
            public bool IsLastDraft;
            public string ValueForProject;
            public string[] ValuesForSort;
        }

        private class HitComparer : IComparer<Hit>
        {
            private SortInfo[] _sort;

            public HitComparer(SortInfo[] sort)
            {
                _sort = sort;
            }

            public int Compare(Hit x, Hit y)
            {
                for (var i = 0; i < _sort.Length; i++)
                {
                    var vx = x.ValuesForSort[i];
                    var vy = y.ValuesForSort[i];
                    var c = _sort[i].Reverse
                        ? string.Compare(vy, vx, StringComparison.InvariantCultureIgnoreCase)
                        : string.Compare(vx, vy, StringComparison.InvariantCultureIgnoreCase);
                    if (c != 0)
                        return c;
                }
                return 0;
            }
        }

        private readonly InMemoryIndex _index;

        public InMemoryQueryEngine(InMemoryIndex index)
        {
            _index = index;
        }

        public IQueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            var interpreter = new SnQueryInterpreter(_index);
            int totalCount;
            var result = interpreter.Execute(query, filter, out totalCount);

            var nodeIds = result.Select(h => h.NodeId).ToArray();
            var queryResult = new QueryResult<int>(nodeIds, totalCount);
            return queryResult;
        }

        public IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            var interpreter = new SnQueryInterpreter(_index);
            int totalCount;
            var result = interpreter.Execute(query, filter, out totalCount);

            var projectedValues = result.Select(h => h.ValueForProject).Distinct().ToArray();
            var queryResult = new QueryResult<string>(projectedValues, totalCount);
            return queryResult;
        }

        private class SnQueryInterpreter : SnQueryVisitor
        {
            private readonly InMemoryIndex _index;
            private readonly Stack<List<int>> _hitStack = new Stack<List<int>>();

            public SnQueryInterpreter(InMemoryIndex index)
            {
                _index = index;
            }

            public IEnumerable<Hit> Execute(SnQuery query, IPermissionFilter filter, out int totalCount)
            {
                Visit(query.QueryTree);

                if (_hitStack.Count == 0)
                    throw new CompilerException($"Compiler error: The stack does not contain any elements.");
                if (_hitStack.Count != 1)
                    throw new CompilerException($"Compiler error: The stack contains more than one elements ({_hitStack.Count}).");

                var foundVersionIds = _hitStack.Pop();

                IEnumerable<Hit> permittedHits = foundVersionIds.Select(v=>GetHitByVersionId(v, query.Projection, query.Sort)).Where(h=>filter.IsPermitted(h.NodeId, h.IsLastPublic, h.IsLastDraft));
                var sortedHits = GetSortedResult(permittedHits, query.Sort).ToArray();

                totalCount = sortedHits.Length;

                var result = sortedHits.Skip(query.Skip).Take(query.Top > 0 ? query.Top : totalCount).ToArray();
                return result;
            }

            private IEnumerable<Hit> GetSortedResult(IEnumerable<Hit> hits, SortInfo[] sort)
            {
                var hitList = hits.ToList();
                hitList.Sort(new HitComparer(sort));
                return hitList;
            }

            private Hit GetHitByVersionId(int versionId, string projection, SortInfo[] sort)
            {
                // VersionId, IndexFields
                // List<Tuple<int, List<IndexField>>>
                var storedFields = _index.StoredData.First(x => x.Item1 == versionId).Item2;

                var hit = new Hit
                {
                    NodeId = storedFields.First(f=>f.Name == IndexFieldName.NodeId).IntegerValue,
                    VersionId = versionId,
                    IsLastDraft = storedFields.First(f => f.Name == IndexFieldName.IsLastDraft).BooleanValue,
                    IsLastPublic = storedFields.First(f => f.Name == IndexFieldName.IsLastPublic).BooleanValue,
                };

                if (projection != null)
                    hit.ValueForProject = GetFieldValueAsString(storedFields.FirstOrDefault(f => f.Name == projection));

                if (sort != null)
                    hit.ValuesForSort = sort.Select(s => FindSortFieldValue(versionId, s.FieldName)).ToArray();

                return hit;
            }

            private string FindSortFieldValue(int versionId, string fieldName)
            {
                // FieldName => FieldValue => VersionId
                // Dictionary<string, Dictionary<string, List<int>>>
                Dictionary<string, List<int>> fieldValues;
                if (!_index.IndexData.TryGetValue(fieldName, out fieldValues))
                    return null;
                var values = fieldValues.Where(v => v.Value.Contains(versionId)).Select(v => v.Key).ToArray();
                return values.FirstOrDefault();
            }

            private string GetFieldValueAsString(IndexField field)
            {
                if (field == null)
                    return null;

                switch (field.Type)
                {
                    case SnTermType.String:
                        return field.StringValue;
                    case SnTermType.StringArray:
                        throw new NotImplementedException();
                    case SnTermType.Bool:
                        return field.BooleanValue ? SnTerm.Yes : SnTerm.No;
                    case SnTermType.Int:
                        return field.IntegerValue.ToString(CultureInfo.InvariantCulture);
                    case SnTermType.Long:
                        return field.LongValue.ToString(CultureInfo.InvariantCulture);
                    case SnTermType.Float:
                        return field.SingleValue.ToString(CultureInfo.InvariantCulture);
                    case SnTermType.Double:
                        return field.DoubleValue.ToString(CultureInfo.InvariantCulture);
                    case SnTermType.DateTime:
                        return field.DateTimeValue.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // ========================================================================================

            public override SnQueryPredicate VisitTextPredicate(TextPredicate text)
            {
                var result = new List<int>();

                var value = text.Value.ToLowerInvariant();
                Dictionary<string, List<int>> fieldValues;
                if (_index.IndexData.TryGetValue(text.FieldName, out fieldValues))
                {
                    if (!value.Contains("*"))
                    {
                        List<int> versionIds;
                        if (fieldValues.TryGetValue(value, out versionIds))
                            //UNDONE: call perfield indexing info
                            result.AddRange(versionIds);
                    }
                    else
                    {
                        result.AddRange(GetVersionIdsByWildcard(fieldValues, value));
                    }
                }
                _hitStack.Push(result);
                return text;
            }
            private IEnumerable<int> GetVersionIdsByWildcard(Dictionary<string, List<int>> fieldValues, string value)
            {
                if (value.Contains("?"))
                    throw new NotSupportedException($"Wildcard '?' not supported.");
                if(value.Replace("*", "").Length == 0)
                    throw new NotSupportedException($"Query is not supported for this field value: {value}");

                List<int>[] versionIds;
                if (value.StartsWith("*") && value.EndsWith("*"))
                {
                    var middle = value.Trim('*');
                    versionIds = fieldValues.Keys.Where(k => k.Contains(middle)).Select(k => fieldValues[k]).ToArray();
                }
                else if (value.StartsWith("*"))
                {
                    var suffix = value.Trim('*');
                    versionIds = fieldValues.Keys.Where(k => k.EndsWith(suffix)).Select(k => fieldValues[k]).ToArray();
                }
                else if (value.EndsWith("*"))
                {
                    var prefix = value.Trim('*');
                    versionIds = fieldValues.Keys.Where(k => k.StartsWith(prefix)).Select(k => fieldValues[k]).ToArray();
                }
                else // if (value.Contains("*"))
                {
                    var sa = value.Split("*".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    var prefix = sa[0];
                    var suffix = sa[1];
                    versionIds = fieldValues.Keys.Where(k => k.StartsWith(prefix) && k.EndsWith(suffix)).Select(k => fieldValues[k]).ToArray();
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

                Dictionary<string, List<int>> fieldValues;
                if (_index.IndexData.TryGetValue(range.FieldName, out fieldValues))
                {
                    var min = range.Min?.ToLowerInvariant();
                    var max = range.Max?.ToLowerInvariant();
                    IEnumerable<KeyValuePair<string, List<int>>> expression;

                    // play permutation of min, max and exclusiveness
                    if (min != null && max != null)
                    {
                        if (!range.MinExclusive && !range.MaxExclusive)
                            expression = fieldValues.Where(x => (string.Compare(x.Key, min, StringComparison.Ordinal) >= 0) &&
                                                                (string.Compare(x.Key, max, StringComparison.Ordinal) <= 0));
                        else if (!range.MinExclusive && range.MaxExclusive)
                            expression = fieldValues.Where(x => (string.Compare(x.Key, min, StringComparison.Ordinal) >= 0) &&
                                                                (string.Compare(x.Key, max, StringComparison.Ordinal) < 0));
                        else if (range.MinExclusive && !range.MaxExclusive)
                            expression = fieldValues.Where(x => (string.Compare(x.Key, min, StringComparison.Ordinal) > 0) &&
                                                                (string.Compare(x.Key, max, StringComparison.Ordinal) <= 0));
                        else
                            expression = fieldValues.Where(x => (string.Compare(x.Key, min, StringComparison.Ordinal) > 0) &&
                                                                (string.Compare(x.Key, max, StringComparison.Ordinal) < 0));
                    }
                    else if (min != null)
                    {
                        if (!range.MinExclusive)
                            expression = fieldValues.Where(x => string.Compare(x.Key, min, StringComparison.Ordinal) >= 0);
                        else
                            expression = fieldValues.Where(x => string.Compare(x.Key, min, StringComparison.Ordinal) > 0);
                    }
                    else
                    {
                        if (!range.MaxExclusive)
                            expression = fieldValues.Where(x => string.Compare(x.Key, max, StringComparison.Ordinal) <= 0);
                        else
                            expression = fieldValues.Where(x => string.Compare(x.Key, max, StringComparison.Ordinal) < 0);
                    }

                    var lists = expression.Select(x => x.Value).ToArray();

                    // aggregate
                    var aggregation = new int[0].AsEnumerable();
                    foreach (var item in lists)
                        aggregation = aggregation.Union(item);
                    result = aggregation.Distinct().ToList();
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
                            notSubset = notSubset.Union(currentSubset).Distinct().ToList();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                // combine the subsets (if there is any "must", the "should" is irrelevant)
                var result = (mustSubset.Count > 0 ? mustSubset : shouldSubset).Except(notSubset).ToList();

                // push result to the hit stack
                _hitStack.Push(result);

                // return with the original parameter
                return clauses;
            }
        }
    }
}
