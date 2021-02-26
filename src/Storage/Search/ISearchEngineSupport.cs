using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search;
using SenseNet.Search.Indexing;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Search
{
    /// <summary>
    /// Defines methods that helps acces to functionality in the higher service level.
    /// </summary>
    public interface ISearchEngineSupport
    {
        /// <summary>
        /// Gets indexing metadata descriptor instance by fieldName
        /// </summary>
        IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName);

        /// <summary>
        /// Returns with the <see cref="QueryResult"/> of the given CQL query.
        /// </summary>
        /// <param name="text">CQL query text.</param>
        /// <param name="settings"><see cref="QuerySettings"/> that extends the query.</param>
        /// <param name="parameters">Values to substitute the parameters of the CQL query text.</param>
        QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters);

        /// <summary>
        /// Returns an <see cref="IIndexPopulator"/> implementation instance.
        /// </summary>
        IIndexPopulator GetIndexPopulator();

        /// <summary>
        /// Returns complete index document that contains all postponed field.
        /// </summary>
        /// <param name="indexDocumentData">Preloaded structure.</param>
        /// <returns></returns>
        IndexDocument CompleteIndexDocument(IndexDocumentData indexDocumentData);
    }
}
