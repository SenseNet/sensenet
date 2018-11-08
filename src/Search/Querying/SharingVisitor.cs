using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.Search.Querying
{
    internal class SharingVisitor : SnQueryVisitor
    {
        private class AssortedPredicates
        {
            public List<LogicalClause> GeneralClauses { get; } = new List<LogicalClause>();
            public List<SimplePredicate>   SimpleShould { get; } = new List<SimplePredicate>();
            public List<SimplePredicate>   SimpleMust { get; } = new List<SimplePredicate>();
            public List<LogicalPredicate> LogicalShould { get; } = new List<LogicalPredicate>();
            public List<LogicalPredicate> LogicalMust { get; } = new List<LogicalPredicate>();
        }

        internal class SharingRelatedPredicateFinderVisitor : SnQueryVisitor
        {
            public List<SimplePredicate> SharingRelatedPredicates { get; } = new List<SimplePredicate>();

            public override SnQueryPredicate VisitSimplePredicate(SimplePredicate simplePredicate)
            {
                var visited = (SimplePredicate)base.VisitSimplePredicate(simplePredicate);
                if (simplePredicate.FieldName == "Sharing")
                    SharingRelatedPredicates.Add(visited);
                return visited;
            }
        }

        /// <summary>
        /// Decides whether to normalize the predicate tree.
        /// Also provides information about negative clause existence.
        /// </summary>
        internal class SharingScannerVisitor : SnQueryVisitor
        {
            private static readonly string[] SharingRelatedFieldNames =
                {"Sharing", "SharedWith", "SharedBy", "SharingMode", "SharingLevel"};

            public bool HasSharingTerm { get; private set; }
            public bool HasMixedOccurences { get; private set; }
            public bool HasUnnecessaryParentheses { get; private set; }
            public bool HasNegativeTerm { get; private set; }

            public bool NeedToBeNormalized => HasSharingTerm && (HasMixedOccurences || HasUnnecessaryParentheses);

            public override SnQueryPredicate VisitSimplePredicate(SimplePredicate simplePredicate)
            {
                var visited = (SimplePredicate) base.VisitSimplePredicate(simplePredicate);
                if (SharingRelatedFieldNames.Contains(simplePredicate.FieldName))
                    HasSharingTerm = true;
                return visited;
            }

            public override LogicalClause VisitLogicalClause(LogicalClause clause)
            {
                var visited = (LogicalClause) base.VisitLogicalClause(clause);

                var must = 0;
                var mustNot = 0;
                var should = 0;

                // Negative clause is skipped in this level
                if (visited.Occur == Occurence.MustNot)
                {
                    HasNegativeTerm = true;
                    return visited;
                }

                if (!(visited.Predicate is LogicalPredicate logical))
                    return visited;

                // Count the occurences of the sub-clauses
                foreach (var subClause in logical.Clauses)
                {
                    switch (subClause.Occur)
                    {
                        case Occurence.Default:
                        case Occurence.Should: should++; break;
                        case Occurence.Must: must++; break;
                        case Occurence.MustNot: mustNot++; break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }

                if (mustNot > 0)
                    HasNegativeTerm = true;

                if (must * should != 0)
                    HasMixedOccurences = true;

                // If the own occurence and occurences of the sub-level are equal, the sub level be combined
                var thisOccurence = visited.Occur == Occurence.Default || visited.Occur == Occurence.Should ? Occurence.Should : Occurence.Must;
                var subOccurence = should > 0 ? Occurence.Should : Occurence.Must;

                if (logical.Clauses.Count == 1)
                    HasUnnecessaryParentheses = true;
                if (thisOccurence == subOccurence)
                    HasUnnecessaryParentheses = true;

                return visited;
            }
        }

        /// <summary>
        /// Normalizes the predicate tree. Removes irrelevant terma and unnecessary parentheses.
        /// Only works correctly if the predicate tree does not contain negative logical clause.
        /// </summary>
        internal class NormalizerVisitor : SnQueryVisitor
        {
            public override LogicalClause VisitLogicalClause(LogicalClause clause)
            {
                // Eliminate a sub-level if it contains only one clause.

                var visited = base.VisitLogicalClause(clause);
                if (!(clause.Predicate is LogicalPredicate logical))
                    return visited;

                if (logical.Clauses.Count > 1)
                    return visited;

                var theClause = logical.Clauses[0];
                return new LogicalClause(theClause.Predicate, visited.Occur);
            }
            public override List<LogicalClause> VisitLogicalClauses(List<LogicalClause> clauses)
            {
                // Remove all SHOULD clauses if there are any MUST clause.

                var visited = base.VisitLogicalClauses(clauses);

                var should = 0;
                var must = 0;

                foreach (var subClause in visited)
                {
                    switch (subClause.Occur)
                    {
                        case Occurence.Default:
                        case Occurence.Should: should++; break;
                        case Occurence.Must: must++; break;
                        case Occurence.MustNot: throw new InvalidOperationException(); //UNDONE:<? write human readable exception message.
                        default: throw new ArgumentOutOfRangeException();
                    }
                }

                return must == 0 || should == 0
                    ? visited
                    : visited.Where(x => x.Occur == Occurence.Must).ToList();
            }
            public override SnQueryPredicate VisitLogicalPredicate(LogicalPredicate logic)
            {
                // Merge clauses with sub levels if the occurences are equal.

                var visited = (LogicalPredicate)base.VisitLogicalPredicate(logic);

                var occur = visited.Clauses[0].Occur;
                var subClausesToMerge = visited.Clauses
                    .Where(x => x.Predicate is LogicalPredicate y && OccursAreEqual(y.Clauses[0].Occur, occur))
                    .ToArray();
                if (subClausesToMerge.Length == 0)
                    return visited;

                var mergedclauses = visited.Clauses.ToList();
                foreach (var subClause in subClausesToMerge)
                {
                    var subLevel = (LogicalPredicate)subClause.Predicate;
                    mergedclauses.Remove(subClause);
                    mergedclauses.AddRange(subLevel.Clauses);
                }

                return new LogicalPredicate(mergedclauses);
            }

            private bool OccursAreEqual(Occurence occur1, Occurence occur2)
            {
                // the most common case returns first
                if (occur1 == occur2)
                    return true;

                // pay attention to the default
                if (occur1 == Occurence.Default || occur1 == Occurence.Should)
                    return occur2 == Occurence.Default || occur2 == Occurence.Should;

                return false;
            }
        }

        public override SnQueryPredicate VisitSimplePredicate(SimplePredicate simplePredicate)
        {
            var visited = (SimplePredicate)base.VisitSimplePredicate(simplePredicate);
            if (visited.FieldName == "SharedWith" || visited.FieldName == "SharedBy" ||
                visited.FieldName == "SharingMode" || visited.FieldName == "SharingLevel")
                return new SimplePredicate("Sharing", visited.Value);
            return visited;
        }

        //UNDONE:<? Finalize SharingVisitor

        public override SnQueryPredicate VisitLogicalPredicate(LogicalPredicate logic)
        {
            var visited = (LogicalPredicate)base.VisitLogicalPredicate(logic);

            var assortedLevel = AssortPredicates(visited);
            var shouldClauseCount = assortedLevel.SimpleShould.Count + assortedLevel.LogicalShould.Count;
            var mustClauseCount = assortedLevel.SimpleMust.Count + assortedLevel.LogicalMust.Count;

            // 1 - If there are only "SHOULD" clauses or there is only one "MUST" clause, it is not necessary to do any transformation.
            // Note that the query tree is normalized that means "MUST" clause existence excludes any "SHOULD" clause
            // so "SHOULD" clause can only exist if the count of "MUST" clauses is 0.
            if (mustClauseCount < 2)
                return visited;

            // 2 - If there are only "MUST" simple clauses but two or more: combine them and not sharing clauses be unchanged
            // Consider that the values may already be combined.
            if (assortedLevel.SimpleMust.Count >= 2 && assortedLevel.LogicalMust.Count == 0)
            {
                var newClauses = assortedLevel.GeneralClauses.ToList();

                var values = assortedLevel.SimpleMust
                    .Select(x => x.Value.StringValue) //UNDONE:<? what about combined values?
                    .OrderBy(x => "TICML".IndexOf(x[0]))
                    .ToArray();

                newClauses.Add(
                    new LogicalClause(
                        new SimplePredicate("Sharing",
                            new IndexValue(string.Join(",", values))), Occurence.Must));

                return new LogicalPredicate(newClauses);
            }

            // 3 - if there are any "MUST" simple clauses and logical clauses: combine everything
            if (assortedLevel.SimpleMust.Count >= 0 && assortedLevel.LogicalMust.Count >= 0)
            {
throw new NotImplementedException();
                // Combine all clauses for example: +a +b +(c d) +(e f) --> +(abce abcf abde abdf)
                // In the temporary storage the inner collection stores the items of the combined values and the
                //    outer collection contains the future terms (List<List<string>>).

                //UNDONE:<? NOT SHARING TERMS

                // 1 - Combine all simple values (with breaking the already combined values)
                var combinedSimpleValues = assortedLevel.SimpleMust
                    .SelectMany(x => x.Value.StringValue.Split(',')).ToList();

                // 2 - create a temporary storage from the combined simple values.
                var combinedValues = new List<List<string>> { combinedSimpleValues };

                // 3 - Combine the current list with each values of each logical predicate
                foreach(var logical in assortedLevel.LogicalMust)
                {
                    //UNDONE:<? test this tightening
                    //if (logical.Clauses.Any(x => x.Occur != Occurence.Default && x.Occur != Occurence.Should))
                    //    throw new SnNotSupportedException("Only sequence of SHOULD elements allowed in a MUST logical predicate.");

                    //UNDONE:<? throw not supported ex if this level contains any not-sharing element.
                    //UNDONE:<? throw not supported ex if this level contains more recursion

                    var values = logical.Clauses
                        .Where(x => x.Predicate is SimplePredicate)
                        .Select(x => (SimplePredicate)x.Predicate)
                        .Where(x => x.FieldName == "Sharing")
                        .Select(x => x.Value.StringValue.Split(',').ToList())
                        .ToList();

                    combinedValues = CombineValues(values, combinedValues);
                }

                // 4 - Create a new logical clause from the combined values
                var newPredicate = CreateSharingLogicalPredicate(combinedValues);
                var finalSharingClause = new LogicalClause(newPredicate, Occurence.Must);

                // 5 - Create a list of output logical clauses.
                var newClauses = assortedLevel.GeneralClauses.ToList();
                newClauses.Add(finalSharingClause);

                // 6 - Return with the rewritten level
                return new LogicalPredicate(newClauses);
            }

            throw new NotImplementedException(); //UNDONE:<? Rewriting for "Should" and "NustNot" clauses are not implemented.
        }

        private static AssortedPredicates AssortPredicates(LogicalPredicate visited)
        {
            var result = new AssortedPredicates();

            foreach (var clause in visited.Clauses)
            {
                if (clause.Predicate is SimplePredicate simplePredicate)
                {
                    if(simplePredicate.FieldName == "Sharing")
                    {
                        switch (clause.Occur)
                        {
                            case Occurence.Default:
                            case Occurence.Should:
                                result.SimpleShould.Add(simplePredicate);
                                break;
                            case Occurence.Must:
                                result.SimpleMust.Add(simplePredicate);
                                break;
                            case Occurence.MustNot:
                                throw new InvalidOperationException(); //UNDONE:<? write human readable exception message.
                            default:
                                throw new SnNotSupportedException("Unknown occurence: " + clause.Occur);
                        }
                    }
                    else
                    {
                        result.GeneralClauses.Add(clause);
                    }
                }
                else if(clause.Predicate is LogicalPredicate logicalPredicate)
                {
                    if (HasSharingRelatedClause(logicalPredicate))
                    {
                        switch (clause.Occur)
                        {
                            case Occurence.Default:
                            case Occurence.Should:
                                result.LogicalShould.Add(logicalPredicate);
                                break;
                            case Occurence.Must:
                                result.LogicalMust.Add(logicalPredicate);
                                break;
                            case Occurence.MustNot:
                                throw new InvalidOperationException(); //UNDONE:<? write human readable exception message.
                            default:
                                throw new SnNotSupportedException("Unknown occurence: " + clause.Occur);
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
            foreach(var item1 in values)
            {
                foreach(var item2 in combinedValues)
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
            foreach(var termValue in values)
            {
                var combinedTermValue = string.Join(",", termValue.Distinct().OrderBy(x => "TICML".IndexOf(x[0])));

                clauses.Add(
                    new LogicalClause(
                        new SimplePredicate("Sharing",
                            new IndexValue(string.Join(",", combinedTermValue))), Occurence.Should));
            }
            return new LogicalPredicate(clauses);
        }
    }
}
