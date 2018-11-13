using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Search.Querying;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.ContentRepository.Sharing
{
    /// <summary>
    /// Normalizes the predicate tree. Removes irrelevant terma and unnecessary parentheses.
    /// Only works correctly if the predicate tree does not contain negative logical clause.
    /// </summary>
    internal class SharingNormalizerVisitor : SnQueryVisitor
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
                    case Occurence.MustNot: throw new InvalidContentSharingQueryException("Sharing related query clause cannot be negation.");
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
}
