using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.Search;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Search
{
    public interface ISearchEngineSupport
    {
        IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName);
        QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters);
        IIndexPopulator GetIndexPopulator();
    }
}
