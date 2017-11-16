using System.Collections.Generic;

namespace SenseNet.Search.Querying.Parser.Predicates
{
    /// <summary>
    /// Defines a logical predicate that represents one level of the parenthesis in the CQL queries.
    /// </summary>
    public class LogicalPredicate : SnQueryPredicate
    {
        /// <summary>
        /// Gets the list of the logical clauses.
        /// </summary>
        public List<LogicalClause> Clauses { get; } = new List<LogicalClause>();

        /// <summary>
        /// Initializes a new instance of the LogicalPredicate with empty clause list.
        /// </summary>
        public LogicalPredicate() { }

        /// <summary>
        /// Initializes a new instance of the LogicalPredicate with an initial clause list.
        /// </summary>
        public LogicalPredicate(IEnumerable<LogicalClause> clauses)
        {
            Clauses.AddRange(clauses);
        }
    }
}
