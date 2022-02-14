using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search;
using SenseNet.Search.Indexing;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Search
{
    public interface ISearchManager
    {
        /// <summary>
        /// Gets the implementation instance of the current <see cref="ISearchEngine"/>.
        /// The value depends on the value of the Configuration.Indexing.IsOuterSearchEngineEnabled setting.
        /// If this value is true, returns Providers.Instance.SearchEngine, otherwise the InternalSearchEngine.Instance.
        /// </summary>
        public ISearchEngine SearchEngine { get; }

        /// <summary>
        /// Gets or sets the path of the local index in the file system in case of local indexing engines.
        /// The value can be configured in the Indexing configuration class or set directly.
        /// </summary>
        public string IndexDirectoryPath { get; set; }

        /// <summary>
        /// Gets or sets a value that is true if the outer search engine is enabled.
        /// </summary>
        public bool IsOuterEngineEnabled { get; set; }

        /// <summary>
        /// Constant value of the default auto filter status. The value is FilterStatus.Enabled.
        /// </summary>
        public FilterStatus EnableAutofiltersDefaultValue { get; }
        /// <summary>
        /// Constant value of the default lifespan filter status. The value is FilterStatus.Disabled.
        /// </summary>
        public FilterStatus EnableLifespanFilterDefaultValue { get; }

        /// <summary>
        /// Returns with true id the value is "Enabled".
        /// Takes into account the EnableAutofiltersDefaultValue actual value.
        /// </summary>
        public bool IsAutofilterEnabled(FilterStatus value);

        /// <summary>
        /// Returns with true id the value is "Enabled".
        /// Takes into account the EnableLifespanFilterDefaultValue actual value.
        /// </summary>
        public bool IsLifespanFilterEnabled(FilterStatus value);

        /// <summary>
        /// Gets a value that is true if the content query can run in the configured outer query engine.
        /// </summary>
        public bool ContentQueryIsAllowed { get; }

        /// <summary>
        /// Returns with the <see cref="QueryResult"/> of the given CQL query.
        /// </summary>
        /// <param name="text">CQL query text.</param>
        /// <param name="settings"><see cref="QuerySettings"/> that extends the query.</param>
        /// <param name="parameters">Values to substitute the parameters of the CQL query text.</param>
        public QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters);

        /// <summary>
        /// Returns an <see cref="IIndexPopulator"/> implementation instance.
        /// </summary>
        public IIndexPopulator GetIndexPopulator();

        /// <summary>
        /// Gets indexing metadata descriptor instance by fieldName
        /// </summary>
        public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName);
    }
}
