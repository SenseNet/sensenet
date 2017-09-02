using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Data;
using System.Configuration;
using System.Diagnostics;
using SenseNet.Configuration;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage
{
    public interface IL2Cache
    {
        bool Enabled { get; set; }
        object Get(string key);
        void Set(string key, object value);
        void Clear();
    }
    internal class NullL2Cache : IL2Cache
    {
        public bool Enabled { get; set; }
        public object Get(string key) { return null; }
        public void Set(string key, object value) { return; }
        public void Clear()
        {
            // Do nothing
        }
    }

    public class StorageContext
    {
        //TODO: well configuration resolves this problem.
        internal void FixEfProviderServicesProblem()
        {
            // http://stackoverflow.com/questions/14033193/entity-framework-provider-type-could-not-be-loaded
            // The Entity Framework provider type 'System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer'
            // for the 'System.Data.SqlClient' ADO.NET provider could not be loaded. 
            // Make sure the provider assembly is available to the running application. 
            // See http://go.microsoft.com/fwlink/?LinkId=260882 for more information.

            var instance = System.Data.Entity.SqlServer.SqlProviderServices.Instance;
        }

        public static class Search
        {
            public static readonly string Yes = "yes";
            public static readonly string No = "no";
            public static readonly List<string> YesList = new List<string>(new string[] { "1", "true", "y", Yes });
            public static readonly List<string> NoList = new List<string>(new string[] { "0", "false", "n", No });

            public static ISearchEngine SearchEngine
            {
                get { return Instance.GetSearchEnginePrivate(); }
            }
            public static ISearchEngineSupport ContentRepository { get; set; }

            public static bool ContentQueryIsAllowed => IsOuterEngineEnabled &&
                                                        SearchEngine != InternalSearchEngine.Instance;

            public static bool IsOuterEngineEnabled
            {
                get { return Instance.IsOuterEngineEnabled; }
            }
            public static string IndexDirectoryPath
            {
                get { return Instance.IndexDirectoryPath; }
            }
            public static void EnableOuterEngine()
            {
                if (false == Indexing.IsOuterSearchEngineEnabled)
                    throw new InvalidOperationException("Indexing is not allowed in the configuration");
                Instance.IsOuterEngineEnabled = true;
            }
            public static void DisableOuterEngine()
            {
                Instance.IsOuterEngineEnabled = false;
            }

            public static void SetIndexDirectoryPath(string path)
            {
                Instance.IndexDirectoryPath = path;
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

        private static IL2Cache _l2Cache = new NullL2Cache();
        public static IL2Cache L2Cache
        {
            get { return _l2Cache; }
            set { _l2Cache = value; }
        }


        // ========================================================================== Singleton model

        private static StorageContext instance;
        private static object instanceLock=new object();

        private static StorageContext Instance
        {
            get
            {
                if (instance != null)
                    return instance;
                lock (instanceLock)
                {
                    if (instance != null)
                        return instance;
                    instance = new StorageContext();
                    return instance;
                }
            }
        }

        private StorageContext() { }

        // ========================================================================== Private interface

        private bool? __isOuterEngineEnabled;
        private bool IsOuterEngineEnabled
        {
            get
            {
                if (__isOuterEngineEnabled == null)
                    __isOuterEngineEnabled = Indexing.IsOuterSearchEngineEnabled;
                return (__isOuterEngineEnabled.Value);
            }
            set
            {
                __isOuterEngineEnabled = value;
            }
        }

        private string __indexDirectoryPath;
        private string IndexDirectoryPath
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

        private readonly object _searchEngineLock = new object();
        private ISearchEngine _searchEngine;

        private ISearchEngine GetSearchEnginePrivate()
        {
            if (!IsOuterEngineEnabled)
                return InternalSearchEngine.Instance;

            if (_searchEngine == null)
                lock (_searchEngineLock)
                    if (_searchEngine == null)
                        _searchEngine = TypeHandler.ResolveProvider<ISearchEngine>() ?? InternalSearchEngine.Instance;

            return _searchEngine;
        }
    }
}
