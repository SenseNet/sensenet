using System;
using System.Collections.Generic;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Represents a visitor or rewriter for SnQueryPredicate trees.
    /// </summary>
    public abstract class SnQueryVisitor
    {
        /// <summary>
        /// Dispatches the predicate to one of the more specialized visit methods in this class.
        /// </summary>
        /// <param name="predicate">The predicate to visit.</param>
        /// <returns>The modified predicate, if it or any child was modified; otherwise, returns the original predicate.</returns>
        public virtual SnQueryPredicate Visit(SnQueryPredicate predicate)
        {
            switch (predicate)
            {
                case null:
                    return null;
                case SimplePredicate simple:
                    return VisitSimplePredicate(simple);
                case RangePredicate range:
                    return VisitRangePredicate(range);
                case LogicalPredicate logic:
                    return VisitLogicalPredicate(logic);
            }

            throw new NotSupportedException("Unknown predicate type: " + predicate.GetType().FullName);
        }

        /// <summary>
        /// Visits the given SimplePredicate.
        /// </summary>
        /// <param name="simplePredicate">The predicate to visit.</param>
        /// <returns>The modified predicate, if it was modified; otherwise, returns the original predicate.</returns>
        public virtual SnQueryPredicate VisitSimplePredicate(SimplePredicate simplePredicate)
        {
            return simplePredicate;
        }

        /// <summary>
        /// Visits the given RangePredicate.
        /// </summary>
        /// <param name="range">The predicate to visit.</param>
        /// <returns>The modified predicate, if it was modified; otherwise, returns the original predicate.</returns>
        public virtual SnQueryPredicate VisitRangePredicate(RangePredicate range)
        {
            return range;
        }

        /// <summary>
        /// Visits the given LogicalPredicate and it's children.
        /// </summary>
        /// <param name="logic">The predicate to visit.</param>
        /// <returns>The modified predicate, if it or any child was modified; otherwise, returns the original predicate.</returns>
        public virtual SnQueryPredicate VisitLogicalPredicate(LogicalPredicate logic)
        {
            var clauses = logic.Clauses;
            var visitedClauses = VisitLogicalClauses(clauses);
            LogicalPredicate rewritten = null;
            if (visitedClauses != clauses)
                rewritten = new LogicalPredicate(visitedClauses);
            return rewritten ?? logic;
        }
        /// <summary>
        /// Visits the given list of LogicalClauses and all list items.
        /// </summary>
        /// <param name="clauses">The list to visit.</param>
        /// <returns>The modified list, if it was modified or any item was changed; otherwise, returns the original list.</returns>
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
                    for (var i = 0; i < index; i++)
                        rewritten.Add(clauses[i]);
                    rewritten.Add(visitedClause);
                }
                index++;
            }
            return rewritten ?? clauses;
        }
        /// <summary>
        /// Visits the given LogicalClause and it's child.
        /// </summary>
        /// <param name="clause">The clause to visit.</param>
        /// <returns>The modified clause, if it or it's child was modified; otherwise, returns the original clause.</returns>
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
