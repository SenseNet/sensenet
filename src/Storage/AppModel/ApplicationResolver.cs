using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            if (StorageContext.Search.IsOuterEngineEnabled)
                return ResolveFirstByPathsFromIndexedEngine(paths);

            var script = DataProvider.GetAppModelScript(paths, false, false);

            using (var proc = DataProvider.CreateDataProcedure(script))
            {
                proc.CommandType = System.Data.CommandType.Text;

                using (var reader = proc.ExecuteReader())
                {
                    while (reader.Read())
                        return NodeHead.Get(reader.GetInt32(0));
                }
            }
            return null;
        }
        private static NodeHead ResolveFirstByPathsFromIndexedEngine(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                var r = StorageContext.Search.ContentRepository.ExecuteContentQuery($"Path:{path}", QuerySettings.AdminSettings);
                if (r.Count > 0)
                    return NodeHead.Get(r.Identifiers.First());
            }
            return null;
        }

        internal static IEnumerable<NodeHead> ResolveAllByPaths(IEnumerable<string> paths, bool resolveChildren)
        {
            if (StorageContext.Search.IsOuterEngineEnabled)
                return ResolveAllByPathsFromIndexedEngine(paths, resolveChildren);

            var script = DataProvider.GetAppModelScript(paths, true, resolveChildren);
            var pathIndexer = paths.ToList();

            List<NodeHead>[] resultSorter;
            using (var proc = DataProvider.CreateDataProcedure(script))
            {
                proc.CommandType = System.Data.CommandType.Text;

                resultSorter = new List<NodeHead>[pathIndexer.Count];
                using (var reader = proc.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var nodeHead = NodeHead.Get(reader.GetInt32(0));
                        var searchPath = resolveChildren ? RepositoryPath.GetParentPath(nodeHead.Path) : nodeHead.Path;
                        var index = pathIndexer.IndexOf(searchPath);
                        if (resultSorter[index] == null)
                            resultSorter[index] = new List<NodeHead>();
                        resultSorter[index].Add(nodeHead);
                    }
                }
            }
            var result = new List<NodeHead>();
            foreach (var list in resultSorter)
                if (list != null)
                {
                    list.Sort(CompareByName);
                    foreach (var nodeHead in list)
                        result.Add(nodeHead);
                }
            return result;
        }
        private static IEnumerable<NodeHead> ResolveAllByPathsFromIndexedEngine(IEnumerable<string> paths, bool resolveChildren)
        {
            var heads = new List<NodeHead>();
            foreach (var path in paths)
            {
                if (resolveChildren)
                {
                    var r = StorageContext.Search.ContentRepository.ExecuteContentQuery("InTree:@0.SORT:Path",
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

        private static int CompareByName(NodeHead x, NodeHead y)
        {
            return x.Path.CompareTo(y.Path);
        }

    }
}
