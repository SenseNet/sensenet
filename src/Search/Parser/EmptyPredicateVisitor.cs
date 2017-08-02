using System.Collections.Generic;
using System.Linq;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.Search.Parser
{
    internal class EmptyPredicateVisitor : SnQueryVisitor
    {
        public override SnQueryPredicate VisitLogicalPredicate(LogicalPredicate logic)
        {
            var clauses = logic.Clauses;
            var visitedClauses = VisitLogicalClauses(clauses);
            if (visitedClauses == clauses)
                return logic;
            if (visitedClauses == null)
                return null;
            var newList = new LogicalPredicate();
            newList.Clauses.AddRange(visitedClauses);
            return newList;
        }
        public override List<LogicalClause> VisitLogicalClauses(List<LogicalClause> clauses)
        {
            var visitedClauses = clauses.Select(VisitLogicalClause).Where(c => c != null).ToList();
            if (visitedClauses.Count == 0)
                return null;
            if (clauses.Count == visitedClauses.Count && clauses.Intersect(visitedClauses).Count() == clauses.Count)
                return clauses;
            return visitedClauses;
        }
        public override LogicalClause VisitLogicalClause(LogicalClause clause)
        {
            var predicate = clause.Predicate;
            var visited = base.Visit(predicate);
            if (visited == null)
                return null;
            if (predicate == visited)
                return clause;
            return new LogicalClause(visited, clause.Occur);
        }
        public override SnQueryPredicate VisitTextPredicate(TextPredicate text)
        {
            return text.Value.Equals(SnQuery.EmptyText) ? null : base.VisitTextPredicate(text);
        }

    }
}
