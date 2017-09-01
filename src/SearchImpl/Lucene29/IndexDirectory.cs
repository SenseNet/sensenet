using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;

namespace SenseNet.Search.Lucene29
{
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
            Debug.WriteLine($"@> {AppDomain.CurrentDomain.FriendlyName} -------- new index directory: {path}");
            return path;
        }
        public static void Reset()
        {
            Debug.WriteLine($"@> {AppDomain.CurrentDomain.FriendlyName} -------- IndexDirectory reset");
            Instance._currentDirDone = false;
            Instance._currentDirectory = null;
        }
        public static void RemoveUnnecessaryDirectories()
        {
            var root = StorageContext.Search.IndexDirectoryPath;
            if (!System.IO.Directory.Exists(root))
                return;
            var unnecessaryDirs = System.IO.Directory.GetDirectories(root)
                .Where(a => char.IsDigit(System.IO.Path.GetFileName(a)[0]))
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

        public static string IndexLockFilePath => Exists ? System.IO.Path.Combine(CurrentDirectory, "write.lock") : null;

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
                    .Where(a => char.IsDigit(System.IO.Path.GetFileName(a)[0]))
                    .OrderBy(s => s)
                    .LastOrDefault();
            }
            Debug.WriteLine(
                $"@> {AppDomain.CurrentDomain.FriendlyName} -------- GetCurrentDirectory: {(path ?? "[null]")}");
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
}
