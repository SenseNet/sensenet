using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
                var path = Path.Combine(StorageContext.Search.IndexDirectoryPath, DEFAULTDIRECTORYNAME);
                Directory.CreateDirectory(path);
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
            var path = Path.Combine(StorageContext.Search.IndexDirectoryPath, name);
            Directory.CreateDirectory(path);
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
            if (!Directory.Exists(root))
                return;
            var unnecessaryDirs = Directory.GetDirectories(root)
                .Where(a => char.IsDigit(Path.GetFileName(a)[0]))
                .OrderByDescending(s => s)
                .Skip(2).Where(x => Deletable(x));
            foreach (var dir in unnecessaryDirs)
            {
                try
                {
                    Directory.Delete(dir, true);
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
            var time = new DirectoryInfo(path).CreationTime;
            if (time.AddMinutes(10) < DateTime.UtcNow)
                return true;
            return false;
        }

        public static string IndexLockFilePath => Exists ? Path.Combine(CurrentDirectory, "write.lock") : null;

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
            EnsureFirstDirectory(root);

            var path = Directory.GetDirectories(root)
                .Where(a => char.IsDigit(Path.GetFileName(a)[0]))
                .OrderBy(s => s)
                .LastOrDefault();

            if (path == null)
                path = CreateNew();

            return path;
        }
        private void EnsureFirstDirectory(string root)
        {
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            var files = Directory.GetFiles(root);
            if (files.Length == 0)
                return;

            var firstDir = Path.Combine(root, DEFAULTDIRECTORYNAME);
            Directory.CreateDirectory(firstDir);

            // backward compatibility: move files to new subdirectory (name = '0')
            foreach (var file in files)
                File.Move(file, Path.Combine(firstDir, Path.GetFileName(file)));
        }
    }
}
