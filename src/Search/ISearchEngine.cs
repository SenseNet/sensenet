using System.Collections.Generic;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Search
{
    /// <summary>
    /// Describes the top level interface of the searching functionality.
    /// Searching is union of querying and indexing in this component.
    /// </summary>
    public interface ISearchEngine
    {
        /// <summary>
        /// Gets an IIndexingEngine implementation. The instance is not changed during the repository's lifetime. 
        /// </summary>
        IIndexingEngine IndexingEngine { get; }

        /// <summary>
        /// Gets an IQueryEngine implementation. The instance is not changed during the repository's lifetime.
        /// </summary>
        IQueryEngine QueryEngine { get; }

        /// <summary>
        /// Gets a key-value pairs of the fields with analyzer that is different from the default.
        /// </summary>
        /// <returns>Dictionary of FieldName-IndexFieldAnalyzer pairs.</returns>
        IDictionary<string, IndexFieldAnalyzer> GetAnalyzers();

        /// <summary>
        /// Method to keep index descriptors up to date.
        /// Called by ContentTypeManager from its initialization method when the content type tree is changed.
        /// </summary>
        /// <param name="indexingInfo">Key value pairs of the indexing descriptors of all fields.</param>
        void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo);
    }
}
