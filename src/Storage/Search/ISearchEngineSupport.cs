using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.Search;
using SenseNet.Search.Indexing;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Search
{
    /// <summary>
    /// Defines methods that helps acces to functionality in the higher component level.
    /// </summary>
    public interface ISearchEngineSupport
    {
        /// <summary>
        /// Gets indexing metadata descriptor instance by fieldName
        /// </summary>
        IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName);

        //UNDONE:!!! XMLDOC: ExecuteContentQuery
        QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters);

        /// <summary>
        /// Returns an IIndexPopulator implementation instance.
        /// </summary>
        /// <returns></returns>
        IIndexPopulator GetIndexPopulator();
    }
}
