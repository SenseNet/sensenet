// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Defines a factory method for creating an IndexingActivity instance by the type constant.
    /// </summary>
    public interface IIndexingActivityFactory
    {
        /// <summary>
        /// Creates a new IndexingActivity instance with default state by the given type constant.
        /// </summary>
        IIndexingActivity CreateActivity(IndexingActivityType activityType);
    }
}