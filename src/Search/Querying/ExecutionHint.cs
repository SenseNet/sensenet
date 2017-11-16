namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Defines options for query execution source recommendation.
    /// Not used in this release.
    /// </summary>
    public enum ExecutionHint
    {
        /// <summary>
        /// There is no recommendation.
        /// </summary>
        None,
        /// <summary>
        /// Use meta search engine if available.
        /// </summary>
        ForceRelationalEngine,
        /// <summary>
        /// Use regular search engine if available.
        /// </summary>
        ForceIndexedEngine
    }
}
