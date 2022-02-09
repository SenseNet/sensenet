using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.Search
{
    //UNDONE:<?xxx: Delete SearchManager and rename SearchManager_INSTANCE to SearchManager if all references rewritten in the ecosystem
    /// <summary>
    /// Provides indexing and querying related management elements for all service layers. 
    /// </summary>
    public class SearchManager_INSTANCE : ISearchManager
    {
        private IDataStore DataStore => Providers.Instance.DataStore;

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

        public bool IsAutofilterEnabled(FilterStatus value)
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
            return ContentQuery.Query(text, settings, parameters);
        }
        public IIndexPopulator GetIndexPopulator()
        {
            return Providers.Instance.SearchManager.IsOuterEngineEnabled
                ? (IIndexPopulator)new DocumentPopulator()
                : NullPopulator.Instance;
        }
        public virtual IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return ContentTypeManager.GetPerFieldIndexingInfo(fieldName);
        }
        public IndexDocument CompleteIndexDocument(IndexDocumentData indexDocumentData)
        {
            return Providers.Instance.IndexManager.CompleteIndexDocument(indexDocumentData);
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
}
