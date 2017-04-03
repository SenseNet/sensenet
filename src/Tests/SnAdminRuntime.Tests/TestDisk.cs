using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SenseNet.Tools.SnAdmin.testability;

namespace SnAdminRuntime.Tests
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
    }
}
