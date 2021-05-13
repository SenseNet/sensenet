using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Search
{
    /// <summary>
    /// Provides indexing and querying related management elements for all service layers. 
    /// </summary>
    public class SearchManager
    {
        private static IDataStore DataStore => Providers.Instance.DataStore;

        /* ========================================================================== Singleton model */

        private static SearchManager Instance = new SearchManager();

        private SearchManager() { }

        /* ========================================================================== Private instance interface */

        // ReSharper disable once InconsistentNaming
        private string __indexDirectoryPath;
        private string IndexDirectoryPathPrivate
        {
            get => __indexDirectoryPath ?? (__indexDirectoryPath = Configuration.Indexing.IndexDirectoryFullPath);
            set => __indexDirectoryPath = value;
        }

        // ReSharper disable once InconsistentNaming
        private bool? __isOuterSearchEngineEnabled;
        private bool IsOuterSearchEngineEnabled
        {
            get
            {
                if(__isOuterSearchEngineEnabled == null)
                    return Configuration.Indexing.IsOuterSearchEngineEnabled;
                return __isOuterSearchEngineEnabled.Value;
            }
            set
            {
                if (false == Configuration.Indexing.IsOuterSearchEngineEnabled)
                    throw new InvalidOperationException("Indexing is not allowed in the configuration");
                __isOuterSearchEngineEnabled = value; 
            }
        }

        /* ========================================================================== Public static interface */

        /// <summary>
        /// Contains all values that mean "true". These are: "1", "true", "y" and "yes"
        /// </summary>
        public static readonly List<string> YesList = new List<string>(new[] { "1", "true", "y", IndexValue.Yes });
        /// <summary>
        /// Contains all values that mean "false". These are: "0", "false", "n" and "no"
        /// </summary>
        public static readonly List<string> NoList = new List<string>(new[] { "0", "false", "n", IndexValue.No });

        /// <summary>
        /// Gets the implementation instance of the current <see cref="ISearchEngine"/>.
        /// The value depends on the value of the Configuration.Indexing.IsOuterSearchEngineEnabled setting.
        /// If this value is true, returns Providers.Instance.SearchEngine, otherwise the InternalSearchEngine.Instance.
        /// </summary>
        public static ISearchEngine SearchEngine => !Configuration.Indexing.IsOuterSearchEngineEnabled
            ? InternalSearchEngine.Instance
            : Providers.Instance.SearchEngine;

        private static ISearchEngineSupport _searchEngineSupport;
        /// <summary>
        /// Stores the given reference of the <see cref="ISearchEngineSupport"/> implementation instance
        /// that allows access to methods implemented in the higher service level.
        /// </summary>
        /// <param name="searchEngineSupport"></param>
        public static void SetSearchEngineSupport(ISearchEngineSupport searchEngineSupport)
        {
            _searchEngineSupport = searchEngineSupport;
        }
        /// <summary>
        /// Returns with the <see cref="QueryResult"/> of the given CQL query.
        /// </summary>
        /// <param name="text">CQL query text.</param>
        /// <param name="settings"><see cref="QuerySettings"/> that extends the query.</param>
        /// <param name="parameters">Values to substitute the parameters of the CQL query text.</param>
        public static QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters)
        {
            return _searchEngineSupport.ExecuteContentQuery(text, settings, parameters);
        }
        /// <summary>
        /// Returns an <see cref="IIndexPopulator"/> implementation instance.
        /// </summary>
        public static IIndexPopulator GetIndexPopulator()
        {
            return _searchEngineSupport.GetIndexPopulator();
        }
        /// <summary>
        /// Gets indexing metadata descriptor instance by fieldName
        /// </summary>
        public static IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return _searchEngineSupport.GetPerFieldIndexingInfo(fieldName);
        }

        public static IndexDocument CompleteIndexDocument(IndexDocumentData indexDocumentData)
        {
            return _searchEngineSupport.CompleteIndexDocument(indexDocumentData);
        }

        /// <summary>
        /// Gets a value that is true if the content query can run in the configured outer query engine.
        /// </summary>
        public static bool ContentQueryIsAllowed => Instance.IsOuterSearchEngineEnabled &&
                                                    SearchEngine != InternalSearchEngine.Instance &&
                                                    (SearchEngine?.IndexingEngine?.Running ?? false);

        /// <summary>
        /// Gets a value that is true if the outer search engine is enabled.
        /// </summary>
        public static bool IsOuterEngineEnabled => Instance.IsOuterSearchEngineEnabled;
        /// <summary>
        /// Gets the path of the local index in the file system in case of local indexing engines.
        /// The value can be configured in the Indexing configuration class or set directly
        /// using the <see cref="SetIndexDirectoryPath"/> method.
        /// </summary>
        public static string IndexDirectoryPath => Instance.IndexDirectoryPathPrivate;

        /// <summary>
        /// Enables the outer search engine.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the outer indexing engine is disabled in the configuration.
        /// The examined value: SenseNet.Configuration.Indexing.IsOuterSearchEngineEnabled.
        /// </exception>
        public static void EnableOuterEngine()
        {
            Instance.IsOuterSearchEngineEnabled = true;
        }
        /// <summary>
        /// Disables the outer search engine.
        /// </summary>
        public static void DisableOuterEngine()
        {
            Instance.IsOuterSearchEngineEnabled = false;
        }

        /// <summary>
        /// Sets the path of the local index in the file system in case of local indexing engines.
        /// </summary>
        public static void SetIndexDirectoryPath(string path)
        {
            Instance.IndexDirectoryPathPrivate = path;
        }

        /// <summary>
        /// Returns with the <see cref="IndexDocumentData"/> of the version identified by the given versionId.
        /// </summary>
        public static IndexDocumentData LoadIndexDocumentByVersionId(int versionId)
        {
            return DataStore.LoadIndexDocumentsAsync(new[] {versionId}, CancellationToken.None).GetAwaiter().GetResult()
                .FirstOrDefault();
        }
        /// <summary>
        /// Returns with the <see cref="IEnumerable&lt;IndexDocumentData&gt;"/> of the versions identified by the given versionIds.
        /// </summary>
        public static IEnumerable<IndexDocumentData> LoadIndexDocumentByVersionId(IEnumerable<int> versionId)
        {
            return DataStore.LoadIndexDocumentsAsync(versionId, CancellationToken.None).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Returns with the <see cref="IEnumerable&lt;IndexDocumentData&gt;"/> of all version of the node identified by the given path.
        /// </summary>
        public static IEnumerable<IndexDocumentData> LoadIndexDocumentsByPath(string path, int[] excludedNodeTypes)
        {
            return DataStore.LoadIndexDocumentsAsync(path, excludedNodeTypes);
        }

        /// <summary>
        /// Constant value of the default auto filter status. The value is FilterStatus.Enabled.
        /// </summary>
        public static FilterStatus EnableAutofiltersDefaultValue => SnQuery.EnableAutofiltersDefaultValue;
        /// <summary>
        /// Constant value of the default lifespan filter status. The value is FilterStatus.Disabled.
        /// </summary>
        public static FilterStatus EnableLifespanFilterDefaultValue = SnQuery.EnableLifespanFilterDefaultValue;

        /// <summary>
        /// Returns with true id the value is "Enabled".
        /// Takes into account the EnableAutofiltersDefaultValue actual value.
        /// </summary>
        public static bool IsAutofilterEnabled(FilterStatus value)
        {
            switch (value)
            {
                case FilterStatus.Default:
                    return EnableAutofiltersDefaultValue == FilterStatus.Enabled;
                case FilterStatus.Enabled:
                    return true;
                case FilterStatus.Disabled:
                    return false;
                default:
                    throw new SnNotSupportedException("Unknown FilterStatus: " + value);
            }
        }
        /// <summary>
        /// Returns with true id the value is "Enabled".
        /// Takes into account the EnableLifespanFilterDefaultValue actual value.
        /// </summary>
        public static bool IsLifespanFilterEnabled(FilterStatus value)
        {
            switch (value)
            {
                case FilterStatus.Default:
                    return EnableLifespanFilterDefaultValue == FilterStatus.Enabled;
                case FilterStatus.Enabled:
                    return true;
                case FilterStatus.Disabled:
                    return false;
                default:
                    throw new SnNotSupportedException("Unknown FilterStatus: " + value);
            }
        }

    }
}
