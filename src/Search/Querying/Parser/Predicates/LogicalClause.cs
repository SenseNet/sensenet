namespace SenseNet.Search.Querying.Parser.Predicates
{
    /// <summary>
    /// Defines clause occurence options in CQL queries.
    /// </summary>
    public enum Occurence
    {
        /// <summary>
        /// Means: Should
        /// </summary>
        Default,
        /// <summary>
        /// The clause SHOULD appear in the matching document.
        /// </summary>
        Should,
        /// <summary>
        /// The clause MUST appear in the matching document.
        /// </summary>
        Must,
        /// <summary>
        /// The clause MUST NOT appear in the matching document.
        /// </summary>
        MustNot
    }

    /// <summary>
    /// Defines a logical clause inspired by Lucene query syntax.
    /// This clause is any clause expanded by an occurence.
    /// </summary>
    public class LogicalClause
    {
        /// <summary>
        /// Gets the base predicate of the clause.
        /// </summary>
        public SnQueryPredicate Predicate { get; }
        /// <summary>
        /// Gets or sets the occurence of the predicate.
        /// </summary>
        public Occurence Occur { get; set; }

        /// <summary>
        /// Initializes a new LogicalClause instance.
        /// </summary>
        /// <param name="predicate">Any predicate.</param>
        /// <param name="occur">Occurence</param>
        public LogicalClause(SnQueryPredicate predicate, Occurence occur)
        {
            Predicate = predicate;
            Occur = occur;
        }
    }
}
