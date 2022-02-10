using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Search
{
    //UNDONE:<?xxx: Delete SearchManager and rename SearchManager_INSTANCE to SearchManager if all references rewritten in the ecosystem
    public class SearchManager
    {
        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static readonly List<string> YesList = new List<string>(new[] { "1", "true", "y", IndexValue.Yes });
        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static readonly List<string> NoList = new List<string>(new[] { "0", "false", "n", IndexValue.No });

        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static bool ContentQueryIsAllowed => Providers.Instance.SearchManager.ContentQueryIsAllowed;

        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static ISearchEngine SearchEngine => Providers.Instance.SearchManager.SearchEngine;

        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static QueryResult ExecuteContentQuery(string text, QuerySettings settings, params object[] parameters)
        {
            return Providers.Instance.SearchManager.ExecuteContentQuery(text, settings, parameters);
        }
        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static IIndexPopulator GetIndexPopulator()
        {
            return Providers.Instance.SearchManager.GetIndexPopulator();
        }
        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return Providers.Instance.SearchManager.GetPerFieldIndexingInfo(fieldName);
        }
        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static IndexDocument CompleteIndexDocument(IndexDocumentData indexDocumentData)
        {
            return Providers.Instance.IndexManager.CompleteIndexDocument(indexDocumentData);
        }

        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static bool IsOuterEngineEnabled => Providers.Instance.SearchManager.IsOuterEngineEnabled;

        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static string IndexDirectoryPath => Providers.Instance.SearchManager.IndexDirectoryPath;

        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static void SetIndexDirectoryPath(string path)
        {
            Providers.Instance.SearchManager.IndexDirectoryPath = path;
        }

        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static IndexDocumentData LoadIndexDocumentByVersionId(int versionId)
        {
            return Providers.Instance.DataStore.LoadIndexDocumentsAsync(new[] { versionId }, CancellationToken.None).GetAwaiter().GetResult()
                .FirstOrDefault();
        }
        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static IEnumerable<IndexDocumentData> LoadIndexDocumentByVersionId(IEnumerable<int> versionId)
        {
            return Providers.Instance.DataStore.LoadIndexDocumentsAsync(versionId, CancellationToken.None).GetAwaiter().GetResult();
        }
        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static IEnumerable<IndexDocumentData> LoadIndexDocumentsByPath(string path, int[] excludedNodeTypes)
        {
            return Providers.Instance.DataStore.LoadIndexDocumentsAsync(path, excludedNodeTypes);
        }

        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static FilterStatus EnableAutofiltersDefaultValue => SnQuery.EnableAutofiltersDefaultValue;
        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static FilterStatus EnableLifespanFilterDefaultValue => SnQuery.EnableLifespanFilterDefaultValue;

        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static bool IsAutofilterEnabled(FilterStatus value) =>
            Providers.Instance.SearchManager.IsAutofilterEnabled(value);
        [Obsolete("Use Providers.Instance.SearchManager instead.", true)]
        public static bool IsLifespanFilterEnabled(FilterStatus value) =>
            Providers.Instance.SearchManager.IsLifespanFilterEnabled(value);

    }
}
