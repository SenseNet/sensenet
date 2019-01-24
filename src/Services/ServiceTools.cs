using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using File = SenseNet.ContentRepository.File;

namespace SenseNet.Services
{
    public static class ServiceTools
    {
        public static string GetClientIpAddress()
        {
            if (HttpContext.Current == null)
                return string.Empty;

            var clientIpAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrEmpty(clientIpAddress))
                return clientIpAddress;

            clientIpAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            if (!string.IsNullOrEmpty(clientIpAddress))
                return clientIpAddress;

            return HttpContext.Current.Request.UserHostAddress ?? string.Empty;
        }

        /// <summary>
        /// Goes through the files in a directory (optionally also files in subdirectories) both in the file system and the repository.
        /// Returns true if the given path was a directory, false if it wasn't.
        /// </summary>
        public static bool RecurseFilesInVirtualPath(string path, bool includesubdirs, Action<string> action, bool skipRepo = false)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (path.StartsWith("http://") || path.StartsWith("https://") || path.ContainsIllegalCharacters())
                return false;

            var nodeHead = NodeHead.Get(path);
            var isFolder = nodeHead != null && nodeHead.GetNodeType().IsInstaceOfOrDerivedFrom("Folder");
            var fsPath = HostingEnvironment.MapPath(path);

            // Take care of folders in the repository
            if (isFolder && !skipRepo)
            {
                // Find content items
                var contents = Content.All.DisableAutofilters()
                    .Where(c => (includesubdirs ? c.InTree(nodeHead.Path) : c.InFolder(nodeHead.Path)) && c.TypeIs(typeof(File).Name))
                    .OrderBy(c => c.Index);

                // Add paths
                foreach (var c in contents)
                    action(c.Path);
            }

            // Take care of folders in the file system
            if (!string.IsNullOrEmpty(fsPath) && Directory.Exists(fsPath))
            {
                // Add files
                foreach (var virtualPath in Directory.GetFiles(fsPath).Select(GetVirtualPath))
                {
                    action(virtualPath);
                }

                // Recurse subdirectories
                if (includesubdirs)
                {
                    foreach (var virtualPath in Directory.GetDirectories(fsPath).Select(GetVirtualPath))
                    {
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        RecurseFilesInVirtualPath(virtualPath, includesubdirs, action, true);
                    }
                }

                isFolder = true;
            }

            return isFolder;
        }

        private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars().Concat(new[] { '?', '&', '#' }).ToArray();
        /// <summary>
        /// Checks whether the path contains characters that are considered illegal in a file system path. 
        /// Used before mapping a virtual path to a server file system path.
        /// </summary>
        private static bool ContainsIllegalCharacters(this string path)
        {
            return path.IndexOfAny(InvalidPathChars) >= 0;
        }
        private static string GetVirtualPath(string physicalPath)
        {
            return physicalPath.Replace(HostingEnvironment.ApplicationPhysicalPath, HostingEnvironment.ApplicationVirtualPath).Replace(@"\", "/");
        }
    }
}
