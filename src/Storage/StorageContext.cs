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
    public class IndexDirectory
    {
        private static readonly string DEFAULTDIRECTORYNAME = "0";

        public static bool Exists
        {
            get { return CurrentDirectory != null; }
        }
        public static string CurrentOrDefaultDirectory
        {
            get
            {
                if (CurrentDirectory != null)
                    return CurrentDirectory;
                var path = System.IO.Path.Combine(StorageContext.Search.IndexDirectoryPath, DEFAULTDIRECTORYNAME);
                System.IO.Directory.CreateDirectory(path);
                Reset();
                return CurrentDirectory;
            }
        }
        public static string CurrentDirectory
        {
            get { return Instance.CurrentDirectoryPrivate; }
        }
        public static string CreateNew()
        {
            var name = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var path = System.IO.Path.Combine(StorageContext.Search.IndexDirectoryPath, name);
            System.IO.Directory.CreateDirectory(path);
Debug.WriteLine(String.Format("@> {0} -------- new index directory: {1}", AppDomain.CurrentDomain.FriendlyName, path));
            return path;
        }
        public static void Reset()
        {
Debug.WriteLine(String.Format("@> {0} -------- IndexDirectory reset", AppDomain.CurrentDomain.FriendlyName));
            Instance._currentDirDone = false;
            Instance._currentDirectory = null;
        }
        public static void RemoveUnnecessaryDirectories()
        {
            var root = StorageContext.Search.IndexDirectoryPath;
            if (!System.IO.Directory.Exists(root))
                return;
            var unnecessaryDirs = System.IO.Directory.GetDirectories(root)
                .Where(a => Char.IsDigit(System.IO.Path.GetFileName(a)[0]))
                .OrderByDescending(s => s)
                .Skip(2).Where(x => Deletable(x));
            foreach (var dir in unnecessaryDirs)
            {
                try
                {
                    System.IO.Directory.Delete(dir, true);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(String.Concat("Cannot delete the directory: ", dir, ", ", e.Message));
                    SnLog.WriteWarning("Cannot delete the directory: " + dir, properties: new Dictionary<string, object> { { "Reason", e.Message }, { "StackTrace", e.StackTrace } });
                }
            }
        }
        private static bool Deletable(string path)
        {
            var time = new System.IO.DirectoryInfo(path).CreationTime;
            if (time.AddMinutes(10) < DateTime.UtcNow)
                return true;
            return false;
        }

        // ==================================================================================

        private IndexDirectory() { }
        private static object _sync = new object();
        private static IndexDirectory _instance;
        private static IndexDirectory Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_sync)
                    {
                        if (_instance == null)
                            _instance = new IndexDirectory();
                    }
                }
                return _instance;
            }
        }

        // ==================================================================================

        private string _currentDirectory;
        private bool _currentDirDone;
        private string CurrentDirectoryPrivate
        {
            get
            {
                if (!_currentDirDone)
                {
                    _currentDirectory = GetCurrentDirectory();
                    _currentDirDone = true;
                }
                return _currentDirectory;
            }
        }
        private string GetCurrentDirectory()
        {
            var root = StorageContext.Search.IndexDirectoryPath;
            var rootExists = System.IO.Directory.Exists(root);
            string path = null;
            if (rootExists)
            {
                EnsureFirstDirectory(root);
                path = System.IO.Directory.GetDirectories(root)
                    .Where(a => Char.IsDigit(System.IO.Path.GetFileName(a)[0]))
                    .OrderBy(s => s)
                    .LastOrDefault();
            }
            Debug.WriteLine(String.Format("@> {0} -------- GetCurrentDirectory: {1}", AppDomain.CurrentDomain.FriendlyName, (path ?? "[null]")));
            return path;
        }
        private void EnsureFirstDirectory(string root)
        {
            // backward compatibility: move files to new subdirectory (name = '0')
            var files = System.IO.Directory.GetFiles(root);
            if (files.Length == 0)
                return;
            var firstDir = System.IO.Path.Combine(root, DEFAULTDIRECTORYNAME);
            Debug.WriteLine("@> new index directory: " + firstDir + " copy files.");
            System.IO.Directory.CreateDirectory(firstDir);
            foreach (var file in files)
                System.IO.File.Move(file, System.IO.Path.Combine(firstDir, System.IO.Path.GetFileName(file)));
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
            public static readonly string Yes = "yes";                                                              //UNDONE: Approve and finalize
            public static readonly string No = "no";                                                                //UNDONE: Approve and finalize
            public static readonly List<string> YesList = new List<string>(new string[] { "1", "true", "y", Yes }); //UNDONE: Approve and finalize
            public static readonly List<string> NoList = new List<string>(new string[] { "0", "false", "n", No });  //UNDONE: Approve and finalize

            public static ISearchEngine SearchEngine
            {
                get { return Instance.GetSearchEnginePrivate(); }
            }
            public static ISearchEngineSupport ContentRepository { get; set; }

            public static bool ContentQueryIsAllowed
            {
                get
                {
                    return IsOuterEngineEnabled &&
                           SearchEngine != InternalSearchEngine.Instance &&
                           !SearchEngine.IndexingPaused;
                }
            }

            public static bool IsOuterEngineEnabled
            {
                get { return Instance.IsOuterEngineEnabled; }
            }
            public static string IndexDirectoryPath
            {
                get { return Instance.IndexDirectoryPath; }
            }
            public static string IndexDirectoryBackupPath
            {
                get { return Instance.IndexDirectoryBackupPath; }
            }
            public static string IndexLockFilePath
            {
                get { return IndexDirectory.Exists ? System.IO.Path.Combine(IndexDirectory.CurrentDirectory, "write.lock") : null; }
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

            [Obsolete("After V6.5 PATCH 9: Use Querying.DefaultTopAndGrowth instead.", true)]
            public static int[] DefaultTopAndGrowth = { 100, 1000, 10000, 0 };
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

        private string __indexDirectoryBackupPath;
        private string IndexDirectoryBackupPath
        {
            get
            {
                if (__indexDirectoryBackupPath == null)
                    __indexDirectoryBackupPath = Indexing.IndexDirectoryBackupPath;
                return __indexDirectoryBackupPath;
            }
            set
            {
                __indexDirectoryBackupPath = value;
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
