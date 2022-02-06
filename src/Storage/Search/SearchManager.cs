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
    //UNDONE:<?xxx: Delete SearchManager and rename SearchManager_INSTANCE to SearchManager
    /// <summary>
    /// Provides indexing and querying related management elements for all service layers. 
    /// </summary>
    public class SearchManager_INSTANCE : ISearchManager
    {
        private ISearchEngineSupport _searchEngineSupport;
        private IDataStore DataStore => Providers.Instance.DataStore;

        public SearchManager_INSTANCE(ISearchEngineSupport searchEngineSupport)
        {
            _searchEngineSupport = searchEngineSupport;
        }

        public ISearchEngine SearchEngine => !Configuration.Indexing.IsOuterSearchEngineEnabled
            ? InternalSearchEngine.Instance
            : Providers.Instance.SearchEngine;

        // ReSharper disable once InconsistentNaming
        private string __indexDirectoryPath;
        public string IndexDirectoryPath
        {
            get => __indexDirectoryPath ??= Configuration.Indexing.IndexDirectoryFullPath;
            set => __indexDirectoryPath = value;
        }

        // ReSharper disable once InconsistentNaming
        private bool? __isOuterSearchEngineEnabled;
        public bool IsOuterEngineEnabled
        {
            get => __isOuterSearchEngineEnabled ?? Configuration.Indexing.IsOuterSearchEngineEnabled;
            set
            {
                if (false == Configuration.Indexing.IsOuterSearchEngineEnabled)
                    throw new InvalidOperationException("Indexing is not allowed in the configuration");
                __isOuterSearchEngineEnabled = value; 
            }
        }

        public FilterStatus EnableAutofiltersDefaultValue => SnQuery.EnableAutofiltersDefaultValue;
        public FilterStatus EnableLifespanFilterDefaultValue => SnQuery.EnableLifespanFilterDefaultValue;

        public  bool IsAutofilterEnabled(FilterStatus value)
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
        public bool IsLifespanFilterEnabled(FilterStatus value)
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

        public bool ContentQueryIsAllowed => IsOuterEngineEnabled &&
                                             SearchEngine != InternalSearchEngine.Instance &&
                                             (SearchEngine?.IndexingEngine?.Running ?? false);

        public QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters)
        {
            return _searchEngineSupport.ExecuteContentQuery(text, settings, parameters);
        }
        public IIndexPopulator GetIndexPopulator()
        {
            return _searchEngineSupport.GetIndexPopulator();
        }
        public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return _searchEngineSupport.GetPerFieldIndexingInfo(fieldName);
        }
        public IndexDocument CompleteIndexDocument(IndexDocumentData indexDocumentData)
        {
            return _searchEngineSupport.CompleteIndexDocument(indexDocumentData);
        }

        public IndexDocumentData LoadIndexDocumentByVersionId(int versionId)
        {
            return DataStore.LoadIndexDocumentsAsync(new[] { versionId }, CancellationToken.None).GetAwaiter().GetResult()
                .FirstOrDefault();
        }
        public IEnumerable<IndexDocumentData> LoadIndexDocumentByVersionId(IEnumerable<int> versionId)
        {
            return DataStore.LoadIndexDocumentsAsync(versionId, CancellationToken.None).GetAwaiter().GetResult();
        }
        public IEnumerable<IndexDocumentData> LoadIndexDocumentsByPath(string path, int[] excludedNodeTypes)
        {
            return DataStore.LoadIndexDocumentsAsync(path, excludedNodeTypes);
        }
    }

    /// <summary>
    /// Provides indexing and querying related management elements for all service layers. 
    /// </summary>
    public class SearchManager
    {
        /// <summary>
        /// Contains all values that mean "true". These are: "1", "true", "y" and "yes"
        /// </summary>
        public static readonly List<string> YesList = new List<string>(new[] { "1", "true", "y", IndexValue.Yes });
        /// <summary>
        /// Contains all values that mean "false". These are: "0", "false", "n" and "no"
        /// </summary>
        public static readonly List<string> NoList = new List<string>(new[] { "0", "false", "n", IndexValue.No });

        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static bool ContentQueryIsAllowed => Providers.Instance.SearchManager.ContentQueryIsAllowed;

        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static ISearchEngine SearchEngine => Providers.Instance.SearchManager.SearchEngine;

        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters)
        {
            return Providers.Instance.SearchManager.ExecuteContentQuery(text, settings, parameters);
        }
        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static IIndexPopulator GetIndexPopulator()
        {
            return Providers.Instance.SearchManager.GetIndexPopulator();
        }
        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return Providers.Instance.SearchManager.GetPerFieldIndexingInfo(fieldName);
        }
        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static IndexDocument CompleteIndexDocument(IndexDocumentData indexDocumentData)
        {
            return Providers.Instance.SearchManager.CompleteIndexDocument(indexDocumentData);
        }

        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static bool IsOuterEngineEnabled => Providers.Instance.SearchManager.IsOuterEngineEnabled;

        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static string IndexDirectoryPath => Providers.Instance.SearchManager.IndexDirectoryPath;

        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static void SetIndexDirectoryPath(string path)
        {
            Providers.Instance.SearchManager.IndexDirectoryPath = path;
        }

        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static IndexDocumentData LoadIndexDocumentByVersionId(int versionId)
        {
            return Providers.Instance.SearchManager.LoadIndexDocumentByVersionId(versionId);
        }
        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static IEnumerable<IndexDocumentData> LoadIndexDocumentByVersionId(IEnumerable<int> versionId)
        {
            return Providers.Instance.SearchManager.LoadIndexDocumentByVersionId(versionId);
        }
        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static IEnumerable<IndexDocumentData> LoadIndexDocumentsByPath(string path, int[] excludedNodeTypes)
        {
            return Providers.Instance.SearchManager.LoadIndexDocumentsByPath(path, excludedNodeTypes);
        }

        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static FilterStatus EnableAutofiltersDefaultValue => SnQuery.EnableAutofiltersDefaultValue;
        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static FilterStatus EnableLifespanFilterDefaultValue => SnQuery.EnableLifespanFilterDefaultValue;

        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static bool IsAutofilterEnabled(FilterStatus value) =>
            Providers.Instance.SearchManager.IsAutofilterEnabled(value);
        //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
        public static bool IsLifespanFilterEnabled(FilterStatus value) =>
            Providers.Instance.SearchManager.IsLifespanFilterEnabled(value);

    }
}
