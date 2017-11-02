using System;
using System.Collections.Generic;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.Search
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

        public static readonly List<string> YesList = new List<string>(new[] { "1", "true", "y", IndexValue.Yes });
        public static readonly List<string> NoList = new List<string>(new[] { "0", "false", "n", IndexValue.No });

        public static ISearchEngine SearchEngine => !Configuration.Indexing.IsOuterSearchEngineEnabled
            ? InternalSearchEngine.Instance
            : Providers.Instance.SearchEngine;

        public static ISearchEngineSupport ContentRepository { get; set; }

        public static bool ContentQueryIsAllowed => Configuration.Indexing.IsOuterSearchEngineEnabled &&
                                                    SearchEngine != InternalSearchEngine.Instance;

        public static bool IsOuterEngineEnabled => Configuration.Indexing.IsOuterSearchEngineEnabled;
        public static string IndexDirectoryPath => Instance.IndexDirectoryPathPrivate;

        public static void EnableOuterEngine()
        {
            if (false == Configuration.Indexing.IsOuterSearchEngineEnabled)
                throw new InvalidOperationException("Indexing is not allowed in the configuration");
            Configuration.Indexing.IsOuterSearchEngineEnabled = true;
        }
        public static void DisableOuterEngine()
        {
            Configuration.Indexing.IsOuterSearchEngineEnabled = false;
        }

        public static void SetIndexDirectoryPath(string path)
        {
            Instance.IndexDirectoryPathPrivate = path;
        }

        public static IndexDocumentData LoadIndexDocumentByVersionId(int versionId)
        {
            return DataProvider.LoadIndexDocument(versionId);
        }
        public static IEnumerable<IndexDocumentData> LoadIndexDocumentByVersionId(IEnumerable<int> versionId)
        {
            return DataProvider.LoadIndexDocument(versionId);
        }
        public static IEnumerable<IndexDocumentData> LoadIndexDocumentsByPath(string path, int[] excludedNodeTypes)
        {
            return DataProvider.LoadIndexDocument(path, excludedNodeTypes);
        }

        public static readonly FilterStatus EnableAutofiltersDefaultValue = FilterStatus.Enabled;
        public static readonly FilterStatus EnableLifespanFilterDefaultValue = FilterStatus.Disabled;

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
