using System;
using System.Collections.Generic;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.Search.Parser
{
    public abstract class SnQueryVisitor
    {
        public virtual SnQueryPredicate Visit(SnQueryPredicate predicate)
        {
            if (predicate == null)
                return null;

            var text  = predicate as TextPredicate;    if (text != null)  return VisitTextPredicate    (text);
            var range = predicate as RangePredicate;   if (range != null) return VisitRangePredicate   (range);
            var logic = predicate as LogicalPredicate; if (logic != null) return VisitLogicalPredicate (logic);

            throw new NotSupportedException("Unknown predicate type: " + predicate.GetType().FullName);
        }

        public virtual SnQueryPredicate VisitTextPredicate(TextPredicate text)
        {
            return text;
        }

        public virtual SnQueryPredicate VisitRangePredicate(RangePredicate range)
        {
            return range;
        }

        public virtual SnQueryPredicate VisitLogicalPredicate(LogicalPredicate logic)
        {
            var clauses = logic.Clauses;
            var visitedClauses = VisitLogicalClauses(clauses);
            LogicalPredicate rewritten = null;
            if (visitedClauses != clauses)
                rewritten = new LogicalPredicate(visitedClauses);
            return rewritten ?? logic;
        }
        public virtual List<LogicalClause> VisitLogicalClauses(List<LogicalClause> clauses)
        {
            List<LogicalClause> rewritten = null;
            var index = 0;
            var count = clauses.Count;
            while (index < count)
            {
                var visitedClause = VisitLogicalClause(clauses[index]);
                if (rewritten != null)
                {
                    rewritten.Add(visitedClause);
                }
                else if (visitedClause != clauses[index])
                {
                    rewritten = new List<LogicalClause>();
                    for (int i = 0; i < index; i++)
                        rewritten.Add(clauses[i]);
                    rewritten.Add(visitedClause);
                }
                index++;
            }
            return rewritten ?? clauses;
        }
        public virtual LogicalClause VisitLogicalClause(LogicalClause clause)
        {
            var occur = clause.Occur;
            var predicate = clause.Predicate;
            var visited = Visit(predicate);
            if (predicate == visited)
                return clause;
            return new LogicalClause(visited, occur);
        }
    }

}
