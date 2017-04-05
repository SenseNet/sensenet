using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using SenseNet.Tools.SnAdmin.Testability;

namespace SnAdminRuntime.Tests.Implementations
{
    internal class TestDisk : IDisk
    {
        internal List<string> Directories { get; set; }
        internal List<string> Files { get; set; }
        internal Dictionary<string, XmlDocument> Manifests { get; set; }

        public TestDisk(IEnumerable<string> directories, IEnumerable<string> files, Dictionary<string, XmlDocument> manifests)
        {
            Directories = directories.ToList();
            Files = files.ToList();
            Manifests = manifests;
        }

        public bool DirectoryExists(string path)
        {
            return Directories.Contains(path, StringComparer.InvariantCultureIgnoreCase);
        }
        public void CreateDirectory(string path)
        {
            Directories.Add(path);
        }
        public string[] GetDirectories(string path)
        {
            return Directories
                .Where(p => p.StartsWith(path, StringComparison.OrdinalIgnoreCase) && p.Length > path.Length)
                .Where(p => !p.Substring(path.Length + 1).Contains("\\"))
                .ToArray();
        }
        public bool FileExists(string path)
        {
            return Files.Contains(path, StringComparer.InvariantCultureIgnoreCase);
        }
        public string[] GetFiles(string path, string filter = null)
        {
            var files = Files
                .Where(p => p.StartsWith(path, StringComparison.OrdinalIgnoreCase) && p.Length > path.Length)
                .Where(p => !p.Substring(path.Length + 1).Contains("\\"));
            if (filter != null)
                files = files.Where(p => p.EndsWith(filter.TrimStart('*'), StringComparison.InvariantCultureIgnoreCase));
            return files.ToArray();
        }
        public void FileCopy(string source, string target)
        {
            if (!Files.Contains(target, StringComparer.InvariantCultureIgnoreCase))
                Files.Add(target);
        }
        public void DeleteAllFrom(string path)
        {
            var dirs = Directories.Where(p => p.StartsWith(path + "\\", StringComparison.InvariantCultureIgnoreCase));
            Directories = Directories.Except(dirs).ToList();

            var files = Files.Where(p => p.StartsWith(path + "\\", StringComparison.InvariantCultureIgnoreCase));
            Files = Files.Except(files).ToList();
        }
        public string SearchTargetDirectory()
        {
            // default location: ..\webfolder\Admin\bin
            var workerExe = Files[0];
            var path = workerExe;

            // go up on the parent chain
            path = Path.GetDirectoryName(path);
            path = Path.GetDirectoryName(path);

            // get the name of the container directory (should be 'Admin')
            var adminDirName = Path.GetFileName(path);
            path = Path.GetDirectoryName(path) ?? "\\";

            if (string.Compare(adminDirName, "Admin", StringComparison.OrdinalIgnoreCase) == 0)
            {
                // look for the web.config
                if (Disk.FileExists(Path.Combine(path, "web.config")))
                    return path;
            }
            throw new ApplicationException("Configure the TargetDirectory. This path does not exist or it is not a valid target: " + path);
        }
        public string DefaultPackageDirectory()
        {
            return Path.GetDirectoryName(Path.GetDirectoryName(Files[0]));
        }
        public XmlDocument LoadManifest(string manifestPath)
        {
            return Manifests[manifestPath];
        }
    }
}
