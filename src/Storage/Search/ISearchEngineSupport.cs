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
        string ReplaceQueryTemplates(string luceneQueryText);
        T GetSettingsValue<T>(string key, T defaultValue);
        QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters);
    }
}
