using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Defines a method that analyzes a SnQuery.
    /// For future usage only. This class is not used in this release.
    /// </summary>
    public class SnQueryClassifier : SnQueryVisitor
    {
        /// <summary>
        /// Analyzes a parsed SnQuery.
        /// </summary>
        /// <param name="query">The input query object</param>
        /// <returns>An SnQueryInfo instance as a result of the analysis.</returns>
        public static SnQueryInfo Classify(SnQuery query)
        {
            var sortfieldNames = query.Sort?.Select(x => x.FieldName).ToList() ?? new List<string>();
            var queryInfo = new SnQueryInfo
            {
                Query = query,
                SortFields = query.Sort,
                Top = query.Top,
                Skip = query.Skip,
                SortFieldNames = sortfieldNames,
                CountAllPages = query.CountAllPages,
                CountOnly = query.CountOnly,
                AllVersions = query.AllVersions
            };

            var visitor = new QueryClassifierVisitor(queryInfo);
            visitor.Visit(query.QueryTree);

            return queryInfo;
        }

        private class QueryClassifierVisitor : SnQueryVisitor
        {
            private readonly SnQueryInfo _queryInfo;

            public QueryClassifierVisitor(SnQueryInfo queryInfo)
            {
                _queryInfo = queryInfo;
            }

            public override SnQueryPredicate VisitTextPredicate(SimplePredicate simplePredicate)
            {
                if (!_queryInfo.QueryFieldNames.Contains(simplePredicate.FieldName))
                    _queryInfo.QueryFieldNames.Add(simplePredicate.FieldName);

                var stringValue = simplePredicate.Value.ValueAsString;
                var asterisks = stringValue.Count(c => c == '*');
                var questionMarks = stringValue.Count(c => c == '?');

                if (asterisks + questionMarks > 0)
                {
                    if (asterisks == 1 && questionMarks == 0 && stringValue.EndsWith("*"))
                        _queryInfo.PrefixQueries++;
                    else
                        _queryInfo.WildcardQueries++;

                    _queryInfo.AsteriskWildcards += asterisks;
                    _queryInfo.QuestionMarkWildcards += questionMarks;
                }
                else if (simplePredicate.FuzzyValue != null)
                {
                    _queryInfo.FuzzyQueries++;
                }
                else
                {
                    _queryInfo.TermQueries++;
                }

                return base.VisitTextPredicate(simplePredicate);
            }

            public override SnQueryPredicate VisitRangePredicate(RangePredicate range)
            {
                if (!_queryInfo.QueryFieldNames.Contains(range.FieldName))
                    _queryInfo.QueryFieldNames.Add(range.FieldName);

                _queryInfo.RangeQueries++;
                if (range.Min != null && range.Max != null)
                    _queryInfo.FullRangeQueries++;

                return base.VisitRangePredicate(range);
            }

            public override SnQueryPredicate VisitLogicalPredicate(LogicalPredicate logic)
            {
                _queryInfo.BooleanQueries++;
                return base.VisitLogicalPredicate(logic);
            }

            public override LogicalClause VisitLogicalClause(LogicalClause clause)
            {
                switch (clause.Occur)
                {
                    case Occurence.Default:
                    case Occurence.Should:
                        _queryInfo.ShouldClauses++;
                        break;
                    case Occurence.Must:
                        _queryInfo.MustClauses++;
                        break;
                    case Occurence.MustNot:
                        _queryInfo.MustNotClauses++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return base.VisitLogicalClause(clause);
            }
        }
    }
}
