using System.Linq;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Parser;

namespace SenseNet.ContentRepository
{
    internal class SearchEngineSupport : ISearchEngineSupport
    {
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
            return ContentType.GetByName(contentTypeName)?.IndexingEnabled ?? true;
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
            return Settings.GetValue(IndexingSettings.SETTINGSNAME, key, null, defaultValue);
        }

        public QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters)
        {
            return ContentQuery.Query(text, settings, parameters);
        }

        public IIndexPopulator GetIndexPopulator()
        {
            return StorageContext.Search.IsOuterEngineEnabled
                ? (IIndexPopulator) new DocumentPopulator()
                : NullPopulator.Instance;
        }
    }
}
