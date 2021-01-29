using System;
using System.Linq;
using IO = System.IO;

namespace SenseNet.Storage
{
    public static class FileSystemWrapper
    {
        public static class Directory
        {
            public static string[] GetFilesByExtension(string path, string ext)
            {
                return GetFilesByExtension(IO.Directory.GetFiles(path), ext);
            }

            private static string[] GetFilesByExtension(string[] paths, string ext)
            {
                if (!ext.StartsWith("."))
                    ext = "." + ext;
                return paths
                    .Where(x => string.Compare(IO.Path.GetExtension(x), ext, StringComparison.OrdinalIgnoreCase) == 0)
                    .ToArray();
            }
        }
    }
}
