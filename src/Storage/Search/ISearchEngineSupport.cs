using System.Collections.Generic;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Storage.Search
{
    public interface ISearchEngineSupport
    {
        int[] GetNotIndexedNodeTypeIds();
        IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName);
        bool IsContentTypeIndexed(string contentTypeName);
        bool TextExtractingWillBePotentiallySlow(IIndexableField field);
        string ReplaceQueryTemplates(string queryText);
        T GetSettingsValue<T>(string key, T defaultValue);
        QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters);

        IIndexPopulator GetIndexPopulator();
    }
}
