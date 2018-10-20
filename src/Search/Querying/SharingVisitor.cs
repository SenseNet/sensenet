using System;
using System.Linq;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.Search.Querying
{
    public class SharingVisitor : SnQueryVisitor
    {
        //UNDONE:<? Finalize SharingVisitor

        public override SnQueryPredicate VisitLogicalPredicate(LogicalPredicate logic)
        {
            var visited = (LogicalPredicate)base.VisitLogicalPredicate(logic);

            var sharingRelatedClauses = visited.Clauses
                .Where(x =>
                {
                    if (x.Predicate is SimplePredicate pred) //UNDONE:<? Only SimplePredicates are rewritten
                        if (pred.FieldName == "Sharing")
                            return true;
                    return false;
                })
                .ToArray();

            if (sharingRelatedClauses.Length < 2)
                return visited;

            var newClauses = visited.Clauses.ToList();
            foreach (var clause in sharingRelatedClauses)
                newClauses.Remove(clause);

            var shouldClauses = sharingRelatedClauses.Where(x => x.Occur == Occurence.Should).ToArray();
            var mustClauses = sharingRelatedClauses.Where(x => x.Occur == Occurence.Must).ToArray();
            var mustNotClauses = sharingRelatedClauses.Where(x => x.Occur == Occurence.MustNot).ToArray();

            if (mustNotClauses.Length + shouldClauses.Length > 0)
                throw new NotImplementedException(); //UNDONE:<? Rewriting for "Should" and "NustNot" clauses are not implemented.

            // Get values from the clauses in right order.
            var values = mustClauses
                .Select(x => ((SimplePredicate)x.Predicate).Value.StringValue)
                .OrderBy(x => "TICML".IndexOf(x[0]))
                .ToArray();

            newClauses.Add(
                new LogicalClause(
                    new SimplePredicate("Sharing",
                        new IndexValue(string.Join(",", values))), Occurence.Must));

            return new LogicalPredicate(newClauses);
        }

        public override SnQueryPredicate VisitSimplePredicate(SimplePredicate simplePredicate)
        {
            var visited = (SimplePredicate)base.VisitSimplePredicate(simplePredicate);
            if (visited.FieldName == "SharedWith" || visited.FieldName == "SharedBy" ||
                visited.FieldName == "SharingMode" || visited.FieldName == "SharingLevel")
                return new SimplePredicate("Sharing", visited.Value);
            return visited;
        }
    }
}
