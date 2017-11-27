namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Defines query executor performance tuning options.
    /// </summary>
    public enum QueryExecutionMode
    {
        /// <summary>Default value of the filter. Interpretation is context-dependent.</summary>
        Default,
        /// <summary>The query result cannot contain dirty hits. The query execution can be slower.</summary>
        Strict,
        /// <summary>The query execution should be fast but the query result can contain dirty hits.</summary>
        Quick
    }
}
