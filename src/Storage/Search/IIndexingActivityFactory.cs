namespace SenseNet.ContentRepository.Search.Indexing
{
    public interface IIndexingActivityFactory
    {
        IIndexingActivity CreateActivity(IndexingActivityType activityType);
    }
}
