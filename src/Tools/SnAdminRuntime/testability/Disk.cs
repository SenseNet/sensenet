using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SenseNet.Tools.SnAdmin.Testability
{
    internal interface IDisk
    {
        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        string[] GetDirectories(string path);
        bool FileExists(string path);
        string[] GetFiles(string path, string filter = null);
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
            return Directory.GetFiles(path, filter);
        }

    }
}
