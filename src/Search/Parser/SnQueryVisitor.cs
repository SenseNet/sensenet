using System;
using System.Collections.Generic;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.Search.Parser
{
    public abstract class SnQueryVisitor
    {
        public virtual SnQueryPredicate Visit(SnQueryPredicate node)
        {
            if (node == null)
                return null;

            var text           = node as TextPredicate;                   if (text != null)            return VisitText              (text);
            var intNumber      = node as IntegerNumberPredicate;          if (intNumber != null)       return VisitIntegerNumber     (intNumber);
            var longNumber     = node as LongNumberPredicate;             if (longNumber != null)      return VisitLongNumber        (longNumber);
            var singleNumber   = node as SingleNumberPredicate;           if (singleNumber != null)    return VisitSingleNumber      (singleNumber);
            var doubleNumber   = node as DoubleNumberPredicate;           if (doubleNumber != null)    return VisitDoubleNumber      (doubleNumber);
            var textRange      = node as TextRange;              if (textRange != null)       return VisitTextRange         (textRange);
            var intRrange      = node as IntegerRange;           if (intRrange != null)       return VisitIntegerRange      (intRrange);
            var longRange      = node as LongRange;              if (longRange != null)       return VisitLongRange         (longRange);
            var singleRange    = node as SingleRange;            if (singleRange != null)     return VisitSingleRange       (singleRange);
            var doubleRange    = node as DoubleRange;            if (doubleRange != null)     return VisitDoubleRange       (doubleRange);
            var boolClauseList = node as BooleanClauseList;      if (boolClauseList != null)  return VisitBooleanClauseList (boolClauseList);

            throw new NotSupportedException("Unknown query type: " + node.GetType().FullName);
        }

        public virtual SnQueryPredicate VisitText(TextPredicate predicate)
        {
            return predicate;
        }

        public virtual SnQueryPredicate VisitIntegerNumber(IntegerNumberPredicate predicate)
        {
            return predicate;
        }
        public virtual SnQueryPredicate VisitLongNumber(LongNumberPredicate predicate)
        {
            return predicate;
        }
        public virtual SnQueryPredicate VisitSingleNumber(SingleNumberPredicate predicate)
        {
            return predicate;
        }
        public virtual SnQueryPredicate VisitDoubleNumber(DoubleNumberPredicate predicate)
        {
            return predicate;
        }
        public virtual SnQueryPredicate VisitTextRange(TextRange range)
        {
            return range;
        }
        public virtual SnQueryPredicate VisitIntegerRange(IntegerRange range)
        {
            return range;
        }
        public virtual SnQueryPredicate VisitLongRange(LongRange range)
        {
            return range;
        }
        public virtual SnQueryPredicate VisitSingleRange(SingleRange range)
        {
            return range;
        }
        public virtual SnQueryPredicate VisitDoubleRange(DoubleRange range)
        {
            return range;
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
            var query = clause.Node;
            var visited = Visit(query);
            if (query == visited)
                return clause;
            return new BooleanClause(visited, occur);
        }
    }

}
