using SenseNet.Search.Indexing;

namespace SenseNet.Search.Querying
{
    public interface IQueryContext
    {
        QuerySettings Settings { get; }
        int UserId { get; }
        IQueryEngine QueryEngine { get; }
        IMetaQueryEngine MetaQueryEngine { get; }

        IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName);
    }
}
