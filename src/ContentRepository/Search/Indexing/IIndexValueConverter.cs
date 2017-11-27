namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Defines a generic interface for a converter that can decode the value stored in the index.
    /// </summary>
    public interface IIndexValueConverter<T>
    {
        /// <summary>
        /// Returns withe the decoded value that stored in the index.
        /// </summary>
        T GetBack(string fieldValue);
    }
    /// <summary>
    /// Defines an interface for a converter that can decode the value stored in the index.
    /// </summary>
    public interface IIndexValueConverter
    {
        /// <summary>
        /// Returns withe the decoded value that stored in the index.
        /// </summary>
        object GetBack(string fieldValue);
    }
}
