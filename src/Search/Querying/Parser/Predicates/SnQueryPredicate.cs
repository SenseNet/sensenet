namespace SenseNet.Search.Querying.Parser.Predicates
{
    /// <summary>
    /// The base class of the predicate class family used in SnQuery
    /// </summary>
    public abstract class SnQueryPredicate
    {
        /// <summary>
        /// Gets or sets the weight of the current predicate. The value can be null.
        /// </summary>
        public double? Boost { get; set; }
    }
}
