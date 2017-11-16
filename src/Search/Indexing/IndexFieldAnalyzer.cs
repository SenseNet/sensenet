namespace SenseNet.Search.Indexing
{
    /// <summary>
    /// Specifies a text analyzer category assigned to a field.
    /// The actually used underlying analyzer depends from the implementation.
    /// </summary>
    public enum IndexFieldAnalyzer
    {
        /// <summary>
        /// Means: Keyword
        /// </summary>
        Default,
        /// <summary>
        /// Defines a text analyzer that uses the whole field value as one token.
        /// </summary>
        Keyword,
        /// <summary>
        /// Defines a text analyzer that applies lexical text analysis for stripping text to words and filters stop-words.
        /// </summary>
        Standard,
        /// <summary>
        /// Defines a text analyzer that can split more terms along the whitespaces.
        /// </summary>
        Whitespace
    }
}
