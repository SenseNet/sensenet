using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;

namespace SenseNet.Search.Lucene29
{
    public class IndexDirectory
    {
        private static readonly string DEFAULTDIRECTORYNAME = "0";

        //================================================================================== Instance API

        public string Name { get; }

        private readonly Lazy<string> _currentDirectory;
        public string CurrentDirectory => _currentDirectory.Value;
        public string IndexLockFilePath => Path.Combine(CurrentDirectory, "write.lock");
        
        //================================================================================== Static API
        
        public static void RemoveUnnecessaryDirectories()
        {
            var root = StorageContext.Search.IndexDirectoryPath;
            if (!Directory.Exists(root))
                return;
            var unnecessaryDirs = Directory.GetDirectories(root)
                .Where(a => char.IsDigit(Path.GetFileName(a)[0]))
                .OrderByDescending(s => s)
                .Skip(2).Where(Deletable);
            foreach (var dir in unnecessaryDirs)
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (Exception e)
                {
                    SnLog.WriteWarning("Cannot delete the directory: " + dir, properties: new Dictionary<string, object> { { "Reason", e.Message }, { "StackTrace", e.StackTrace } });
                }
            }
        }
        private static bool Deletable(string path)
        {
            var time = new DirectoryInfo(path).CreationTime;

            return time.AddMinutes(10) < DateTime.UtcNow;
        }

        //================================================================================== Construction

        internal IndexDirectory(string name = null)
        {
            Name = name;
            _currentDirectory = new Lazy<string>(GetCurrentDirectory);
        }

        //================================================================================== Helper methods

        private string CreateNew()
        {
            var name = Name ?? DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var path = Path.Combine(StorageContext.Search.IndexDirectoryPath, name);

            Directory.CreateDirectory(path);
            SnTrace.Index.Write("New index directory: {0}", path);

            return path;
        }
        private string GetCurrentDirectory()
        {
            var root = StorageContext.Search.IndexDirectoryPath;
            EnsureFirstDirectory(root);

            if (!string.IsNullOrEmpty(Name))
            {
                //UNDONE: maybe clean up the directory if we have found it?
                var namedDirectory = Path.Combine(root, Name);
                if (!Directory.Exists(namedDirectory))
                    CreateNew();

                return namedDirectory;
            }

            var path = Directory.GetDirectories(root)
                .Where(a => char.IsDigit(Path.GetFileName(a)[0]))
                .OrderBy(s => s)
                .LastOrDefault() ?? CreateNew();

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
