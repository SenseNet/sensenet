using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.Search.Querying
{
    internal class SharingComposerVisitor : SnQueryVisitor //UNDONE:<? Move to ContentRepository (?)
    {
        private class AssortedPredicates
        {
            public List<LogicalClause> GeneralClauses { get; } = new List<LogicalClause>();
            public List<SimplePredicate> SimpleMust { get; } = new List<SimplePredicate>();
            public List<LogicalPredicate> LogicalMust { get; } = new List<LogicalPredicate>();
        }

        internal class SharingRelatedPredicateFinderVisitor : SnQueryVisitor
        {
            public List<SimplePredicate> SharingRelatedPredicates { get; } = new List<SimplePredicate>();

            public override SnQueryPredicate VisitSimplePredicate(SimplePredicate simplePredicate)
            {
                var visited = (SimplePredicate)base.VisitSimplePredicate(simplePredicate);
                if (simplePredicate.FieldName == SharingVisitor.Sharing)
                    SharingRelatedPredicates.Add(visited);
                return visited;
            }
        }

        public override SnQueryPredicate VisitLogicalPredicate(LogicalPredicate logic)
        {
            var visited = logic;
            var clauses = logic.Clauses;
            var visitedClauses = VisitLogicalClauses(clauses);
            if (visitedClauses != clauses)
                visited = new LogicalPredicate(visitedClauses);


            var assortedLevel = AssortPredicates(visited);
            var mustClauseCount = assortedLevel.SimpleMust.Count + assortedLevel.LogicalMust.Count;
            List<LogicalClause> newClauses;

            // 1 - If there are only "SHOULD" clauses or there is only one "MUST" clause, it is not necessary to do any transformation.
            //     Note that the query tree is normalized that means "MUST" clause existence excludes any "SHOULD" clause
            //     so "SHOULD" clause can only exist if the count of "MUST" clauses is 0.
            if (mustClauseCount < 2)
                return visited;

            // 2 - If there are only "MUST" simple clauses but two or more: combine them and not sharing clauses be unchanged
            //     Consider that the values may already be combined.
            if (assortedLevel.SimpleMust.Count >= 2 && assortedLevel.LogicalMust.Count == 0)
            {
                newClauses = assortedLevel.GeneralClauses.ToList();

                var values = assortedLevel.SimpleMust
                    .SelectMany(x => x.Value.StringArrayValue)
                    .ToArray();

                newClauses.Add(
                    new LogicalClause(
                        CreateSharingSimplePredicate(values),
                        Occurence.Must));

                return new LogicalPredicate(newClauses);
            }

            // 3 - If there are any "MUST" simple clauses and logical clauses: combine everything
            //     Combine all clauses for example: +a +b +(c d) +(e f) --> +(abce abcf abde abdf)
            //     In the temporary storage the inner collection stores the items of the combined values and the
            //     outer collection contains the future terms (List<List<string>>).

            // 3.1 - Combine all simple values (with breaking the already combined values)
            var combinedSimpleValues = assortedLevel.SimpleMust
                .SelectMany(x => x.Value.StringArrayValue).ToList();

            // 3.2 - create a temporary storage from the combined simple values.
            var combinedValues = new List<List<string>> { combinedSimpleValues };

            // 3.3 - Combine the current list with each values of each logical predicate
            foreach (var logical in assortedLevel.LogicalMust)
            {
                var values = logical.Clauses
                    .Where(x => x.Predicate is SimplePredicate)
                    .Select(x => (SimplePredicate)x.Predicate)
                    .Where(x => x.FieldName == SharingVisitor.Sharing)
                    .Select(x => x.Value.StringArrayValue.ToList())
                    .ToList();

                combinedValues = CombineValues(values, combinedValues);
            }

            // 3.4 - Create a new logical clause from the combined values
            var newPredicate = CreateSharingLogicalPredicate(combinedValues);
            var finalSharingClause = new LogicalClause(newPredicate, Occurence.Must);

            // 3.5 - Create a list of output logical clauses.
            newClauses = assortedLevel.GeneralClauses.ToList();
            newClauses.Add(finalSharingClause);

            // 3.6 - Return with the rewritten level
            return new LogicalPredicate(newClauses);
        }

        private static AssortedPredicates AssortPredicates(LogicalPredicate visited)
        {
            var result = new AssortedPredicates();

            foreach (var clause in visited.Clauses)
            {
                if (clause.Predicate is SimplePredicate simplePredicate)
                {
                    if (simplePredicate.FieldName == SharingVisitor.Sharing)
                    {
                        switch (clause.Occur)
                        {
                            case Occurence.Must:
                                result.SimpleMust.Add(simplePredicate);
                                break;
                            case Occurence.MustNot:
                                throw new InvalidOperationException(); //UNDONE:<? write human readable exception message.
                        }
                    }
                    else
                    {
                        result.GeneralClauses.Add(clause);
                    }
                }
                else if (clause.Predicate is LogicalPredicate logicalPredicate)
                {
                    if (HasSharingRelatedClause(logicalPredicate))
                    {
                        switch (clause.Occur)
                        {
                            case Occurence.Must:
                                result.LogicalMust.Add(logicalPredicate);
                                break;
                            case Occurence.MustNot:
                                throw new InvalidOperationException(); //UNDONE:<? write human readable exception message.
                        }
                    }
                    else
                    {
                        result.GeneralClauses.Add(clause);
                    }
                }
                else
                {
                    result.GeneralClauses.Add(clause); //UNDONE:<? What about rangepredicates?
                }
            }

            return result;
        }
        private static bool HasSharingRelatedClause(LogicalPredicate logicalPredicate)
        {
            var visitor = new SharingRelatedPredicateFinderVisitor();
            visitor.Visit(logicalPredicate);
            return visitor.SharingRelatedPredicates.Count > 0;
        }

        private static List<List<string>> CombineValues(List<List<string>> values, List<List<string>> combinedValues)
        {
            var result = new List<List<string>>();
            foreach (var item1 in values)
            {
                foreach (var item2 in combinedValues)
                {
                    var newItems = item1.ToList();
                    newItems.AddRange(item2);
                    result.Add(newItems);
                }
            }
            return result;
        }

        private static LogicalPredicate CreateSharingLogicalPredicate(List<List<string>> values)
        {
            var clauses = new List<LogicalClause>();
            foreach (var termValue in values)
            {
                var combinedTermValue = termValue.Distinct().OrderBy(x => "TICML".IndexOf(x[0])).ToArray();

                clauses.Add(
                    new LogicalClause(
                        CreateSharingSimplePredicate(combinedTermValue), Occurence.Should));
            }
            return new LogicalPredicate(clauses);
        }

        internal static SimplePredicate CreateSharingSimplePredicate(IndexValue value)
        {
            var values = value.Type == IndexValueType.StringArray
                ? value.StringArrayValue
                : value.StringValue.Split(',');
            return CreateSharingSimplePredicate(values);
        }
        internal static SimplePredicate CreateSharingSimplePredicate(string[] values)
        {
            return new SimplePredicate(SharingVisitor.Sharing, new IndexValue(values));
        }
    }
}
