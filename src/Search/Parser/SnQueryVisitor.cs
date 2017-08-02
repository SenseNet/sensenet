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

            var text           = predicate as TextPredicate;     if (text != null)           return VisitTextPredicate     (text);
            var textRange      = predicate as RangePredicate;    if (textRange != null)      return VisitRangePredicate    (textRange);
            var boolClauseList = predicate as BooleanClauseList; if (boolClauseList != null) return VisitBooleanClauseList (boolClauseList);

            throw new NotSupportedException("Unknown predicate type: " + predicate.GetType().FullName);
        }

        public virtual SnQueryPredicate VisitTextPredicate(TextPredicate textPredicate)
        {
            return textPredicate;
        }

        public virtual SnQueryPredicate VisitRangePredicate(RangePredicate rangePredicate)
        {
            return rangePredicate;
        }

        public virtual SnQueryPredicate VisitBooleanClauseList(BooleanClauseList boolClauseList)
        {
            var clauses = boolClauseList.Clauses;
            var visitedClauses = VisitBooleanClauses(clauses);
            BooleanClauseList newList = null;
            if (visitedClauses != clauses)
            {
                newList = new BooleanClauseList();
                newList.Clauses.AddRange(visitedClauses);
            }
            return newList ?? boolClauseList;
        }
        public virtual List<BooleanClause> VisitBooleanClauses(List<BooleanClause> clauses)
        {
            List<BooleanClause> newList = null;
            var index = 0;
            var count = clauses.Count;
            while (index < count)
            {
                var visitedClause = VisitBooleanClause(clauses[index]);
                if (newList != null)
                {
                    newList.Add(visitedClause);
                }
                else if (visitedClause != clauses[index])
                {
                    newList = new List<BooleanClause>();
                    for (int i = 0; i < index; i++)
                        newList.Add(clauses[i]);
                    newList.Add(visitedClause);
                }
                index++;
            }
            return newList ?? clauses;
        }
        public virtual BooleanClause VisitBooleanClause(BooleanClause clause)
        {
            var occur = clause.Occur;
            var predicate = clause.Predicate;
            var visited = Visit(predicate);
            if (predicate == visited)
                return clause;
            return new BooleanClause(visited, occur);
        }
    }

}
