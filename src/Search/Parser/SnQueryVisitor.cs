using System;
using System.Collections.Generic;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.Search.Parser
{
    public abstract class SnQueryVisitor
    {
        public virtual SnQueryNode Visit(SnQueryNode node)
        {
            if (node == null)
                return null;

            var text         = node as Text;                   if (text != null)         return VisitText          (text);
            var intNumber    = node as IntegerNumber;          if (intNumber != null)    return VisitIntegerNumber (intNumber);
            var longNumber   = node as LongNumber;             if (longNumber != null)   return VisitLongNumber    (longNumber);
            var singleNumber = node as SingleNumber;           if (singleNumber != null) return VisitSingleNumber  (singleNumber);
            var doubleNumber = node as DoubleNumber;           if (doubleNumber != null) return VisitDoubleNumber  (doubleNumber);
            var textRange    = node as TextRange;              if (textRange != null)    return VisitTextRange     (textRange);
            var intRrange    = node as IntegerRange;           if (intRrange != null)    return VisitIntegerRange  (intRrange);
            var longRange    = node as LongRange;              if (longRange != null)    return VisitLongRange     (longRange);
            var singleRange  = node as SingleRange;            if (singleRange != null)  return VisitSingleRange   (singleRange);
            var doubleRange  = node as DoubleRange;            if (doubleRange != null)  return VisitDoubleRange   (doubleRange);
            var boolList     = node as BooleanClauseList;   if (boolList != null)     return VisitBoolList      (boolList);

            throw new NotSupportedException("Unknown query type: " + node.GetType().FullName);
        }

        public virtual SnQueryNode VisitText(Text predicate)
        {
            return predicate;
        }

        public virtual SnQueryNode VisitIntegerNumber(IntegerNumber predicate)
        {
            return predicate;
        }
        public virtual SnQueryNode VisitLongNumber(LongNumber predicate)
        {
            return predicate;
        }
        public virtual SnQueryNode VisitSingleNumber(SingleNumber predicate)
        {
            return predicate;
        }
        public virtual SnQueryNode VisitDoubleNumber(DoubleNumber predicate)
        {
            return predicate;
        }
        public virtual SnQueryNode VisitTextRange(TextRange predicate)
        {
            return predicate;
        }
        public virtual SnQueryNode VisitIntegerRange(IntegerRange predicate)
        {
            return predicate;
        }
        public virtual SnQueryNode VisitLongRange(LongRange predicate)
        {
            return predicate;
        }
        public virtual SnQueryNode VisitSingleRange(SingleRange predicate)
        {
            return predicate;
        }
        public virtual SnQueryNode VisitDoubleRange(DoubleRange predicate)
        {
            return predicate;
        }

        public virtual SnQueryNode VisitBoolList(BooleanClauseList boolList)
        {
            var clauses = boolList.Clauses;
            var visitedClauses = VisitBoolClauses(clauses);
            BooleanClauseList newQuery = null;
            if (visitedClauses != clauses)
            {
                newQuery = new BooleanClauseList();
                newQuery.Clauses.AddRange(visitedClauses);
            }
            return newQuery ?? boolList;

        }
        public virtual List<BooleanClause> VisitBoolClauses(List<BooleanClause> clauses)
        {
            List<BooleanClause> newList = null;
            var index = 0;
            var count = clauses.Count;
            while (index < count)
            {
                var visitedClause = VisitBool(clauses[index]);
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
        public virtual BooleanClause VisitBool(BooleanClause clause)
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
