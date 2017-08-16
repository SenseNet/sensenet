using System.Linq;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;
using SenseNet.Search.Parser;

namespace SenseNet.ContentRepository
{
    internal class SearchEngineSupport : ISearchEngineSupport
    {
        public bool RestoreIndexOnstartup()
        {
            return RepositoryInstance.RestoreIndexOnStartup();
        }

        public int[] GetNotIndexedNodeTypeIds()
        {
            return new AllContentTypes()
                .Where(c => !c.IndexingEnabled)
                .Select(c => Storage.Schema.NodeType.GetByName(c.Name).Id)
                .ToArray();
        }

        public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return ContentTypeManager.GetPerFieldIndexingInfo(fieldName);
        }

        public bool IsContentTypeIndexed(string contentTypeName)
        {
            var ct = ContentType.GetByName(contentTypeName);
            if (ct == null)
                return true;
            return ct.IndexingEnabled;
        }

        public bool TextExtractingWillBePotentiallySlow(IIndexableField field)
        {
            return TextExtractor.TextExtractingWillBePotentiallySlow((BinaryData) ((BinaryField) field).GetData());
        }

        public string ReplaceQueryTemplates(string queryText)
        {
            return TemplateManager.Replace(typeof(ContentQueryTemplateReplacer), queryText);
        }

        public T GetSettingsValue<T>(string key, T defaultValue)
        {
            return Settings.GetValue<T>(IndexingSettings.SETTINGSNAME, key, null, defaultValue);
        }

        public QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters)
        {
            return ContentQuery_NEW.Query(text, settings, parameters);
        }
    }
}
