using System.Collections.Generic;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Storage.Search
{
    public interface ISearchEngineSupport
    {
        IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName);
        QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters);

        IIndexPopulator GetIndexPopulator();
    }
}
