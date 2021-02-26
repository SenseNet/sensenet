using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository
{
    internal class SearchEngineSupport : ISearchEngineSupport
    {
        public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return ContentTypeManager.GetPerFieldIndexingInfo(fieldName);
        }

        public QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters)
        {
            return ContentQuery.Query(text, settings, parameters);
        }

        public IIndexPopulator GetIndexPopulator()
        {
            return SearchManager.IsOuterEngineEnabled
                ? (IIndexPopulator) new DocumentPopulator()
                : NullPopulator.Instance;
        }

        public IndexDocument CompleteIndexDocument(IndexDocumentData indexDocumentData)
        {
            return IndexManager.CompleteIndexDocument(indexDocumentData);
        }
    }
}
