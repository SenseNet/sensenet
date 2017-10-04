using System;
using System.Collections.Generic;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;

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
            get
            {
                if (__indexDirectoryPath == null)
                    __indexDirectoryPath = Indexing.IndexDirectoryFullPath;
                return __indexDirectoryPath;
            }
            set
            {
                __indexDirectoryPath = value;
            }
        }


        public static readonly List<string> YesList = new List<string>(new[] { "1", "true", "y", SnTerm.Yes });
        public static readonly List<string> NoList = new List<string>(new[] { "0", "false", "n", SnTerm.No });

        public static ISearchEngine SearchEngine => !Indexing.IsOuterSearchEngineEnabled
            ? InternalSearchEngine.Instance
            : Providers.Instance.SearchEngine;

        public static ISearchEngineSupport ContentRepository { get; set; }

        public static bool ContentQueryIsAllowed => Indexing.IsOuterSearchEngineEnabled &&
                                                    SearchEngine != InternalSearchEngine.Instance;

        public static bool IsOuterEngineEnabled
        {
            get { return Indexing.IsOuterSearchEngineEnabled; }
        }
        public static string IndexDirectoryPath
        {
            get { return Instance.IndexDirectoryPathPrivate; }
        }
        public static void EnableOuterEngine()
        {
            if (false == Indexing.IsOuterSearchEngineEnabled)
                throw new InvalidOperationException("Indexing is not allowed in the configuration");
            Indexing.IsOuterSearchEngineEnabled = true;
        }
        public static void DisableOuterEngine()
        {
            Indexing.IsOuterSearchEngineEnabled = false;
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
    }
}
