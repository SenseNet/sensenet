using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.Search
{
    /// <summary>
    /// Provides indexing and querying related management elements for all service layers. 
    /// </summary>
    public class SearchManager : ISearchManager
    {
        private IDataStore _dataStore;

        public ISearchEngine SearchEngine => !Configuration.Indexing.IsOuterSearchEngineEnabled  //TODO: Set once in startup seq.
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

        public SearchManager(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }

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
            return ContentQuery.QueryAsync(text, settings, CancellationToken.None, parameters)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }
        public IIndexPopulator GetIndexPopulator()
        {
            return IsOuterEngineEnabled ? Providers.Instance.IndexPopulator : NullPopulator.Instance;
        }
        public virtual IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return ContentTypeManager.GetPerFieldIndexingInfo(fieldName);
        }

        public object GetConfigurationForHealthDashboard()
        {
            throw new NotImplementedException();
        }

        public Task<object> GetHealthAsync(CancellationToken cancel)
        {
            IDictionary<string, string> data = null;
            string error = null;
            TimeSpan? elapsed = null;

            try
            {
                var timer = Stopwatch.StartNew();
                data = this.SearchEngine.IndexingEngine.GetIndexDocumentByVersionId(1);
                timer.Stop();
                elapsed = timer.Elapsed;
            }
            catch (Exception e)
            {
                error = e.Message;
            }

            object result;
            if (error != null)
            {
                result = new
                {
                    Color = "Red", // Error
                    Reason = $"ERROR: {error}",
                    Method = "SearchManager (InProc) ties to get index document by VersionId 1."
                };
            }
            else if (data == null)
            {
                result = new
                {
                    Color = "Yellow", // Problem
                    Reason = "No result",
                    Method = "SearchManager (InProc) ties to get index document by VersionId 1."
                };
            }
            else
            {
                result = new
                {
                    Color = "Green", // Working well
                    ResponseTime = elapsed,
                    Method = "Measure the time of getting the index document by VersionId 1 in secs from SearchManager (InProc)."
                };
            }

            return System.Threading.Tasks.Task.FromResult(result);
        }
    }
}
