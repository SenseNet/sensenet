using SenseNet.ContentRepository.Search.Indexing;

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// The activity log entry without the associated index document.
    /// </summary>
    public sealed record IndexingActivityMetadata(
        int Id, IndexingActivityType ActivityType, int NodeId, int VersionId, string Path, string Extension);
}
