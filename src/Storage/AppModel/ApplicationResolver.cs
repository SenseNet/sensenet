using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Storage.AppModel
{
    internal class ApplicationResolver
    {
        internal static IEnumerable<string> GetAvailablePaths(string contextNodePath, NodeType nodeType, string appFolderName, string appName, HierarchyOption hierarchyOption)
        {
            switch (hierarchyOption)
            {
                case HierarchyOption.Type:
                    return GetAvailablePathsByType(contextNodePath, nodeType, appFolderName, appName, hierarchyOption);
                case HierarchyOption.Path:
                    return GetAvailablePathsByPath(contextNodePath, nodeType, appFolderName, appName, hierarchyOption);
                case HierarchyOption.TypeAndPath:
                    return GetAvailablePathsByTypeAndPath(contextNodePath, nodeType, appFolderName, appName, hierarchyOption);
                case HierarchyOption.PathAndType:
                    return GetAvailablePathsByPathAndType(contextNodePath, nodeType, appFolderName, appName, hierarchyOption);
                default:
                    throw new SnNotSupportedException(hierarchyOption.ToString());
            }
        }

        private static IEnumerable<string> GetAvailablePathsByType(string contextNodePath, NodeType nodeType, string appFolderName, string appName, HierarchyOption option)
        {
            contextNodePath = contextNodePath.TrimEnd('/');

            var assocName = appName ?? String.Empty;
            if (assocName.Length > 0)
                assocName = String.Concat("/", assocName);

            var pathBase = String.Concat(contextNodePath, "/", appFolderName);
            var paths = new List<string>();

            paths.Add(String.Concat(pathBase, "/This", assocName));

            while (nodeType != null)
            {
                paths.Add(String.Concat(pathBase, "/", nodeType.Name, assocName));
                nodeType = nodeType.Parent;
            }

            return paths;
        }
        private static IEnumerable<string> GetAvailablePathsByPath(string contextNodePath, NodeType nodeType, string appFolderName, string appName, HierarchyOption option)
        {
            contextNodePath = contextNodePath.TrimEnd('/');

            var assocName = appName ?? String.Empty;
            if (assocName.Length > 0)
                assocName = String.Concat("/", assocName);

            string[] parts = contextNodePath.Split('/');

            var paths = new List<string>();

            paths.Add(String.Concat(contextNodePath, "/", appFolderName, "/This", assocName));

            var position = parts.Length + 1;
            string partpath;
            while (position-- > 2)
            {
                partpath = string.Join("/", parts, 0, position);
                paths.Add(String.Concat(partpath, "/", appFolderName, assocName));
            }
            return paths;
        }
        private static IEnumerable<string> GetAvailablePathsByTypeAndPath(string contextNodePath, NodeType nodeType, string appFolderName, string appName, HierarchyOption option)
        {
            contextNodePath = contextNodePath.TrimEnd('/');

            var assocName = appName ?? String.Empty;
            if (assocName.Length > 0)
                assocName = String.Concat("/", assocName);

            string[] parts = contextNodePath.Split('/');

            var probs = new List<string>();

            while (nodeType != null)
            {
                probs.Add(String.Concat("/{0}/", nodeType.Name, assocName));
                nodeType = nodeType.Parent;
            }

            var paths = new List<string>();

            paths.Add(String.Concat(contextNodePath, "/", appFolderName, "/This", assocName));

            var position = parts.Length + 1;
            string partpath;
            while (position-- > 2)
            {
                partpath = string.Join("/", parts, 0, position);
                foreach (var prob in probs)
                    paths.Add(String.Concat(partpath, string.Format(prob, appFolderName)));
            }
            return paths;
        }
        private static IEnumerable<string> GetAvailablePathsByPathAndType(string contextNodePath, NodeType nodeType, string appFolderName, string appName, HierarchyOption option)
        {
            contextNodePath = contextNodePath.TrimEnd('/');

            var assocName = appName ?? String.Empty;
            if (assocName.Length > 0)
                assocName = String.Concat("/", assocName);

            string[] parts = contextNodePath.Split('/');

            var probs = new List<string>();

            while (nodeType != null)
            {
                probs.Add(String.Concat("/{0}/", nodeType.Name, assocName));
                nodeType = nodeType.Parent;
            }

            var paths = new List<string>();

            paths.Add(String.Concat(contextNodePath, "/", appFolderName, "/This", assocName));

            string partpath;
            foreach (var prob in probs)
            {
                var position = parts.Length + 1;
                while (position-- > 2)
                {
                    partpath = string.Join("/", parts, 0, position);
                    paths.Add(String.Concat(partpath, string.Format(prob, appFolderName)));
                }
            }

            return paths;
        }

        // ======================================================================================

        internal static NodeHead ResolveFirstByPaths(IEnumerable<string> paths)
        {
            if (SearchManager.IsOuterEngineEnabled)
                return ResolveFirstByPathsFromIndexedEngine(paths);
            return DataStore.LoadNodeHeadsFromPredefinedSubTeesAsync(paths, false, false).Result.FirstOrDefault();
        }
        private static NodeHead ResolveFirstByPathsFromIndexedEngine(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                var r = SearchManager.ExecuteContentQuery($"Path:{path}", QuerySettings.AdminSettings);
                if (r.Count > 0)
                    return NodeHead.Get(r.Identifiers.First());
            }
            return null;
        }

        internal static IEnumerable<NodeHead> ResolveAllByPaths(IEnumerable<string> paths, bool resolveChildren)
        {
            return SearchManager.IsOuterEngineEnabled
                ? ResolveAllByPathsFromIndexedEngine(paths, resolveChildren)
                : DataStore.LoadNodeHeadsFromPredefinedSubTeesAsync(paths, true, resolveChildren).Result;
        }
        private static IEnumerable<NodeHead> ResolveAllByPathsFromIndexedEngine(IEnumerable<string> paths, bool resolveChildren)
        {
            var heads = new List<NodeHead>();
            foreach (var path in paths)
            {
                if (resolveChildren)
                {
                    var r = SearchManager.ExecuteContentQuery("InTree:@0.SORT:Path",
                        QuerySettings.AdminSettings, path);
                    if (r.Count > 0)
                        // skip first because it is the root of subtree
                        heads.AddRange(r.Identifiers.Skip(1).Select(NodeHead.Get));
                }
                else
                {
                    var head = NodeHead.Get(path);
                    if (head != null)
                        heads.Add(head);
                }
            }
            return heads;
        }
    }
}
