using System;
using System.Collections.Generic;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

//UNDONE:!!!!! XMLDOC Storage
namespace SenseNet.ContentRepository.Search //UNDONE:! move to Search directory
{
    public class SearchManager
    {
        // ========================================================================== Singleton model

        private static SearchManager Instance = new SearchManager();

        private SearchManager() { }

        // ========================================================================== Private interface

        private string __indexDirectoryPath;
        private string IndexDirectoryPathPrivate
        {
            get => __indexDirectoryPath ?? (__indexDirectoryPath = Configuration.Indexing.IndexDirectoryFullPath);
            set => __indexDirectoryPath = value;
        }

        /// <summary>
        /// Contains all values that mean "true". These are: "1", "true", "y" and "yes"
        /// </summary>
        public static readonly List<string> YesList = new List<string>(new[] { "1", "true", "y", IndexValue.Yes });
        /// <summary>
        /// Contains all values that mean "false". These are: "0", "false", "n" and "no"
        /// </summary>
        public static readonly List<string> NoList = new List<string>(new[] { "0", "false", "n", IndexValue.No });

        //UNDONE:!!!!! XMLDOC Storage
        public static ISearchEngine SearchEngine => !Configuration.Indexing.IsOuterSearchEngineEnabled
            ? InternalSearchEngine.Instance
            : Providers.Instance.SearchEngine;

        private static ISearchEngineSupport _searchEngineSupport;
        //UNDONE:!!!!! XMLDOC Storage
        public static void SetSearchEngineSupport(ISearchEngineSupport searchEngineSupport)
        {
            _searchEngineSupport = searchEngineSupport;
        }
        //UNDONE:!!!!! XMLDOC Storage
        public static QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters)
        {
            return _searchEngineSupport.ExecuteContentQuery(text, settings, parameters);
        }
        //UNDONE:!!!!! XMLDOC Storage
        public static IIndexPopulator GetIndexPopulator()
        {
            return _searchEngineSupport.GetIndexPopulator();
        }
        //UNDONE:!!!!! XMLDOC Storage
        public static IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return _searchEngineSupport.GetPerFieldIndexingInfo(fieldName);
        }


        //UNDONE:!!!!! XMLDOC Storage
        public static bool ContentQueryIsAllowed => Configuration.Indexing.IsOuterSearchEngineEnabled &&
                                                    SearchEngine != InternalSearchEngine.Instance &&
                                                    (SearchEngine?.IndexingEngine?.Running ?? false);

        //UNDONE:!!!!! XMLDOC Storage
        public static bool IsOuterEngineEnabled => Configuration.Indexing.IsOuterSearchEngineEnabled;
        //UNDONE:!!!!! XMLDOC Storage
        public static string IndexDirectoryPath => Instance.IndexDirectoryPathPrivate;

        //UNDONE:!!!!! XMLDOC Storage
        public static void EnableOuterEngine()
        {
            if (false == Configuration.Indexing.IsOuterSearchEngineEnabled)
                throw new InvalidOperationException("Indexing is not allowed in the configuration");
            Configuration.Indexing.IsOuterSearchEngineEnabled = true;
        }
        //UNDONE:!!!!! XMLDOC Storage
        public static void DisableOuterEngine()
        {
            Configuration.Indexing.IsOuterSearchEngineEnabled = false;
        }

        //UNDONE:!!!!! XMLDOC Storage
        public static void SetIndexDirectoryPath(string path)
        {
            Instance.IndexDirectoryPathPrivate = path;
        }

        //UNDONE:!!!!! XMLDOC Storage
        public static IndexDocumentData LoadIndexDocumentByVersionId(int versionId)
        {
            return DataProvider.LoadIndexDocument(versionId);
        }
        //UNDONE:!!!!! XMLDOC Storage
        public static IEnumerable<IndexDocumentData> LoadIndexDocumentByVersionId(IEnumerable<int> versionId)
        {
            return DataProvider.LoadIndexDocument(versionId);
        }
        //UNDONE:!!!!! XMLDOC Storage
        public static IEnumerable<IndexDocumentData> LoadIndexDocumentsByPath(string path, int[] excludedNodeTypes)
        {
            return DataProvider.LoadIndexDocument(path, excludedNodeTypes);
        }

        //UNDONE:!!!!! XMLDOC Storage
        public static readonly FilterStatus EnableAutofiltersDefaultValue = FilterStatus.Enabled;
        //UNDONE:!!!!! XMLDOC Storage
        public static readonly FilterStatus EnableLifespanFilterDefaultValue = FilterStatus.Disabled;

        //UNDONE:!!!!! XMLDOC Storage
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
        //UNDONE:!!!!! XMLDOC Storage
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
