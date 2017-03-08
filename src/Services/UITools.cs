using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Services
{
    public static class UITools
    {
        public static List<string> GetGetContentPickerRootPathList(string path)
        {
            var pathList = new List<string>();
            var contentHead = NodeHead.Get(path);
            var parentPath = string.Empty;

            // add the highest reachable parent
            while (contentHead != null)
            {
                var parent = NodeHead.Get(contentHead.ParentId);

                if (parent == null || !SecurityHandler.HasPermission(parent, PermissionType.See))
                {
                    parentPath = contentHead.Path;
                    break;
                }

                contentHead = parent;
            }

            if (!string.IsNullOrEmpty(parentPath) && !pathList.Contains(parentPath))
                pathList.Add(parentPath);

            // add root
            if (!pathList.Contains(Repository.RootPath))
                pathList.Add(Repository.RootPath);

            return pathList;
        }

        public static string GetGetContentPickerRootPathString(string path)
        {
            var rootPaths = GetGetContentPickerRootPathList(path);
            
            // in case the only path is the /Root, return null
            return rootPaths.Count > 1 || (rootPaths.Count == 1 && rootPaths.First() != Repository.RootPath)
                ? "[" + string.Join(", ", rootPaths.Select(rp => "'" + rp + "'")) + "]"
                : "null";
        }
    }
}
