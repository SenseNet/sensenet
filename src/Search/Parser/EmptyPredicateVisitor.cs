using System.Collections.Generic;
using System.Linq;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.Search.Parser
{
    internal class EmptyPredicateVisitor : SnQueryVisitor
    {
        public override SnQueryPredicate VisitBooleanClauseList(BooleanClauseList boolClauseList)
        {
            var clauses = boolClauseList.Clauses;
            var visitedClauses = VisitBooleanClauses(clauses);
            if (visitedClauses == clauses)
                return boolClauseList;
            if (visitedClauses == null)
                return null;
            var newList = new BooleanClauseList();
            newList.Clauses.AddRange(visitedClauses);
            return newList;
        }
        public override List<BooleanClause> VisitBooleanClauses(List<BooleanClause> clauses)
        {
            var visitedClauses = clauses.Select(VisitBooleanClause).Where(c => c != null).ToList();
            if (visitedClauses.Count == 0)
                return null;
            if (clauses.Count == visitedClauses.Count && clauses.Intersect(visitedClauses).Count() == clauses.Count)
                return clauses;
            return visitedClauses;
        }
        public override BooleanClause VisitBooleanClause(BooleanClause clause)
        {
            var predicate = clause.Predicate;
            var visited = base.Visit(predicate);
            if (visited == null)
                return null;
            if (predicate == visited)
                return clause;
            return new BooleanClause(visited, clause.Occur);
        }
        public override SnQueryPredicate VisitText(TextPredicate predicate)
        {
            return predicate.Value.Equals(SnQuery.EmptyText) ? null : base.VisitText(predicate);
        }

    }
}
