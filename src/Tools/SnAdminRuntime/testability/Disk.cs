using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SenseNet.Storage;

namespace SenseNet.Tools.SnAdmin.Testability
{
    internal interface IDisk
    {
        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        string[] GetDirectories(string path);
        bool FileExists(string path);
        string[] GetFiles(string path, string filter = null);
        void FileCopy(string source, string target);
        void DeleteAllFrom(string path);
        string SearchTargetDirectory();
        string DefaultPackageDirectory();
        XmlDocument LoadManifest(string manifestPath);
    }

    internal abstract class Disk
    {
        internal static IDisk Instance { get; set; } = new PhysicalDisk();

        internal static bool DirectoryExists(string path)
        {
            return Instance.DirectoryExists(path);
        }
        internal static void CreateDirectory(string path)
        {
            Instance.CreateDirectory(path);
        }
        internal static string[] GetDirectories(string path)
        {
            return Instance.GetDirectories(path);
        }
        internal static bool FileExists(string path)
        {
            return Instance.FileExists(path);
        }
        internal static string[] GetFiles(string path, string filter = null)
        {
            return Instance.GetFiles(path, filter);
        }
        internal static void FileCopy(string source, string target)
        {
            Instance.FileCopy(source, target);
        }
        internal static void DeleteAllFrom(string path)
        {
            Instance.DeleteAllFrom(path);
        }
        internal static string SearchTargetDirectory()
        {
            return Instance.SearchTargetDirectory();
        }
        public static string DefaultPackageDirectory()
        {
            return Instance.DefaultPackageDirectory();
        }

        public static XmlDocument LoadManifest(string manifestPath)
        {
            return Instance.LoadManifest(manifestPath);
        }
    }

    internal class PhysicalDisk : IDisk
    {
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }
        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }
        public string[] GetDirectories(string path)
        {
            return Directory.GetDirectories(path);
        }
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }
        public string[] GetFiles(string path, string filter = null)
        {
            if (filter == null)
                return Directory.GetFiles(path);

            var files = filter.StartsWith("*.") && !filter.Substring(1).Contains(".")
                ? FileSystemWrapper.Directory.GetFilesByExtension(path, filter.Substring(1))
                : Directory.GetFiles(path, filter);

            return files;
        }
        public void FileCopy(string source, string target)
        {
            File.Copy(source, target);
        }
        public void DeleteAllFrom(string path)
        {
            var dirInfo = new DirectoryInfo(path);
            foreach (FileInfo file in dirInfo.GetFiles())
            {
                if (file.IsReadOnly)
                    file.IsReadOnly = false;
                file.Delete();
            }
            foreach (DirectoryInfo dir in dirInfo.GetDirectories())
                dir.Delete(true);
        }
        public string SearchTargetDirectory()
        {
            var targetDir = Configuration.Packaging.TargetDirectory;
            if (!string.IsNullOrEmpty(targetDir))
                return targetDir;

            // default location: ..\webfolder\Admin\bin
            var workerExe = Assembly.GetExecutingAssembly().Location;
            var path = workerExe;

            // go up on the parent chain
            path = Path.GetDirectoryName(path);
            path = Path.GetDirectoryName(path);

            // get the name of the container directory (should be 'Admin')
            var adminDirName = Path.GetFileName(path);
            path = Path.GetDirectoryName(path);

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
            var pkgDir = Configuration.Packaging.PackageDirectory;
            if (!string.IsNullOrEmpty(pkgDir))
                return pkgDir;
            var workerExe = Assembly.GetExecutingAssembly().Location;
            pkgDir = Path.GetDirectoryName(Path.GetDirectoryName(workerExe));
            return pkgDir;
        }
        public XmlDocument LoadManifest(string manifestPath)
        {
            var xml = new XmlDocument();
            if (manifestPath != null)
                xml.Load(manifestPath);
            return xml;
        }

    }
}
