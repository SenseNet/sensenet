using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.Virtualization;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.Portal
{
    public class SkinManagerBase
    {
        /// <summary>
        /// Internal class for holding skin-related configuration values in this lower layer as a workaround.
        /// It looks for values in the same config section as the real Skin config class in the upper layer.
        /// </summary>
        internal class SkinConfig : SnConfig
        {
            private const string SectionName = "sensenet/skin";

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static string DefaultSkinName { get; internal set; } = GetString(SectionName, "DefaultSkinName", "sensenet");
        }

        /* ================================================================== Members */

        private SortedDictionary<string, SortedDictionary<string, string>> _skinMap;
        private ReaderWriterLockSlim _skinMapLock;

        private static SkinManagerBase _instance;
        public static readonly string skinPrefix = "$skin/";
        public static readonly string DefaultSkinName = "default";

        // ================================================================== Singleton instance

        protected SkinManagerBase() { }
        private static readonly object _startSync = new object();
        protected internal static SkinManagerBase Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_startSync)
                    {
                        if (_instance == null)
                        {
                            SkinManagerBase sm;

                            if (string.IsNullOrEmpty(Providers.SkinManagerClassName))
                            {
                                sm = new SkinManagerBase();
                            }
                            else
                            {
                                try
                                {
                                    sm = (SkinManagerBase)TypeResolver.CreateInstance(Providers.SkinManagerClassName);
                                }
                                catch (Exception)
                                {
                                    SnLog.WriteWarning(
                                        "Error loading skinmanager type " + Providers.SkinManagerClassName,
                                        EventId.RepositoryLifecycle);

                                    sm = new SkinManagerBase();
                                }
                            }

                            sm._skinMap = new SortedDictionary<string, SortedDictionary<string, string>>();
                            sm._skinMapLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
                            sm.ReadSkinStructure();

                            _instance = sm;

                            SnLog.WriteInformation("SkinManager created: " + _instance.GetType().FullName);
                        }
                    }
                }
                return _instance;
            }
            internal set { _instance = value; }
        }

        // ================================================================== Overridable methods

        public virtual Node GetCurrentSkin()
        {
            if (PortalContext.Current != null)
            {
                if (PortalContext.Current.ContextWorkspace != null && PortalContext.Current.ContextWorkspace.WorkspaceSkin != null)
                    return PortalContext.Current.ContextWorkspace.WorkspaceSkin;
                if (PortalContext.Current.Site != null && PortalContext.Current.Site.SiteSkin != null)
                    return PortalContext.Current.Site.SiteSkin;
            }

            var path = RepositoryPath.Combine(RepositoryStructure.SkinRootFolderPath, SkinConfig.DefaultSkinName);
            return Node.LoadNode(path);
        }

        // ================================================================== Static API

        /// <summary>
        /// Gets the name of the current skin, based on the current request.
        /// </summary>
        public static string CurrentSkinName => PortalContext.Current?
            .GetOrAdd<string>("CurrentSkinName", key => GetCurrentSkinName());
        
        public static string GetCurrentSkinName()
        {
            var skin = Instance.GetCurrentSkin();

            return skin == null ? string.Empty : skin.Name;
        }        
        public static string Resolve(string relpath)
        {
            return Instance.ResolvePath(relpath, GetCurrentSkinName());
        }
        public static bool TryResolve(string relpath, out string resolvedpath)
        {
            return Instance.TryResolvePath(relpath, GetCurrentSkinName(), out resolvedpath);
        }
        public static bool IsNotSkinRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            return !(path.StartsWith(skinPrefix));
        }
        public static string TrimSkinPrefix(string relPath)
        {
            return relPath.Remove(0, skinPrefix.Length);
        }

        // ================================================================== Instance methods 

        private string ResolvePath(string relpath, string skinname)
        {
            return ResolvePath(relpath, skinname, true);
        }
        private bool TryResolvePath(string relpath, string skinname, out string resolvedpath)
        {
            resolvedpath = ResolvePath(relpath, skinname, false);
            return !string.IsNullOrEmpty(resolvedpath);
        }

        // ================================================================== Helper methods

        private string ResolvePath(string relpath, string skinname, bool fallbackToRoot)
        {
            // absolute path is given: no fallback, no check
            if (IsNotSkinRelativePath(relpath))
                return relpath;

            var skinRelPath = TrimSkinPrefix(relpath);
            if (!string.IsNullOrEmpty(skinname))
            {
                try
                {
                    _skinMapLock.TryEnterReadLock(RepositoryEnvironment.DefaultLockTimeout);

                    // look for the file under the current skin or the default skin
                    var resolved = ResolvePathInternal(skinname, skinRelPath);
                    if (!string.IsNullOrEmpty(resolved))
                        return resolved;
                }
                finally
                {
                    if (_skinMapLock.IsReadLockHeld)
                        _skinMapLock.ExitReadLock();
                }
            }

            // if fallback to root is not requested
            if (!fallbackToRoot)
                return string.Empty;

            // backward compatibility: fallback to the global folder
            return RepositoryPath.Combine(RepositoryStructure.SkinGlobalFolderPath, skinRelPath);
        }

        private string ResolvePathInternal(string skinName, string skinRelPath)
        {
            // This method tries to find a file under the current skin. If it is not there,
            // it looks into the default skin as a fallback. It does not take the Global
            // folder into account as it will be obsolete soon.

            SortedDictionary<string, string> skin;
            string path;

            // look for the file in the current skin
            if (_skinMap.TryGetValue(skinName, out skin) && skin != null)
            {
                if (skin.TryGetValue(skinRelPath, out path) && !string.IsNullOrEmpty(path))
                    return path;
            }

            // look for the file in the default skin
            if (_skinMap.TryGetValue(DefaultSkinName, out skin) && skin != null)
            {
                if (skin.TryGetValue(skinRelPath, out path) && !string.IsNullOrEmpty(path))
                    return path;
            }
            
            return null;
        }

        private void ReadSkinStructure()
        {
            Node[] nodes;
            if (RepositoryInstance.ContentQueryIsAllowed)
            {
                var query = new NodeQuery();
                query.Add(new StringExpression(StringAttribute.Path, StringOperator.StartsWith, RepositoryStructure.SkinRootFolderPath));
                query.Add(new TypeExpression(NodeType.GetByName("Skin")));
                nodes = query.Execute().Nodes.ToArray();
            }
            else
            {
                nodes = NodeQuery.QueryNodesByTypeAndPath(NodeType.GetByName("Skin"), false, RepositoryStructure.SkinRootFolderPath, false).Nodes.ToArray();
            }

            try
            {
                _skinMapLock.TryEnterWriteLock(RepositoryEnvironment.DefaultLockTimeout);
                foreach (Node n in nodes)
                    _skinMap.Add(n.Name, MapSkin(n));
            }
            finally
            {
                if (_skinMapLock.IsWriteLockHeld)
                    _skinMapLock.ExitWriteLock();
            }
        }
        private static SortedDictionary<string, string> MapSkin(Node skin)
        {
            NodeQueryResult result;
            if (RepositoryInstance.ContentQueryIsAllowed)
            {
                var query = new NodeQuery();
                query.Add(new StringExpression(StringAttribute.Path, StringOperator.StartsWith, skin.Path));
                result = query.Execute();
            }
            else
            {
                result = NodeQuery.QueryNodesByPath(skin.Path, false);
            }

            var dict = new SortedDictionary<string, string>();
            foreach (Node n in result.Nodes)
            {
                if (n.Id != skin.Id)
                {
                    var relpath = n.Path.Substring(skin.Path.Length + 1);
                    if (!dict.ContainsKey(relpath))
                        dict.Add(relpath, n.Path);
                }
            }

            return dict;
        }
        
        // ================================================================== Manage skin map

        internal void AddToMap(string fullPath)
        {
            var s = SplitPath(fullPath);
            if (s == null)
                return;

            try
            {
                _skinMapLock.TryEnterUpgradeableReadLock(RepositoryEnvironment.DefaultLockTimeout);
                if (_skinMap.ContainsKey(s[0]))
                {
                    var localskinMap = _skinMap[s[0]];
                    if (localskinMap != null)
                    {
                        if (s.Length > 1 && !string.IsNullOrEmpty(s[1]) && !localskinMap.ContainsKey(s[1]))
                        {
                            try
                            {
                                _skinMapLock.TryEnterWriteLock(RepositoryEnvironment.DefaultLockTimeout);
                                localskinMap.Add(s[1], fullPath);
                            }
                            finally
                            {
                                if (_skinMapLock.IsWriteLockHeld)
                                    _skinMapLock.ExitWriteLock();
                            }
                        }
                    }
                }
                else
                {
                    if (s.Length < 2 || string.IsNullOrEmpty(s[1]))
                    {
                        var n = Node.LoadNode(fullPath);
                        if (n.NodeType.IsInstaceOfOrDerivedFrom("Skin"))
                        {
                            try
                            {
                                _skinMapLock.TryEnterWriteLock(RepositoryEnvironment.DefaultLockTimeout);
                                _skinMap.Add(s[0], MapSkin(n));
                            }
                            finally
                            {
                                if (_skinMapLock.IsWriteLockHeld)
                                    _skinMapLock.ExitWriteLock();
                            }
                        }
                    }
                }
            }
            finally
            {
                if (_skinMapLock.IsUpgradeableReadLockHeld)
                    _skinMapLock.ExitUpgradeableReadLock();
            }
        }
        internal void RemoveFromMap(string fullPath)
        {
            var s = SplitPath(fullPath);
            if (s == null)
                return;

            try
            {
                _skinMapLock.TryEnterUpgradeableReadLock(RepositoryEnvironment.DefaultLockTimeout);
                if (_skinMap.ContainsKey(s[0]))
                {
                    var skinMap = _skinMap[s[0]];
                    if (skinMap != null)
                    {
                        try
                        {
                            _skinMapLock.TryEnterWriteLock(RepositoryEnvironment.DefaultLockTimeout);
                            if (s.Length > 1 && !string.IsNullOrEmpty(s[1]))
                                skinMap.Remove(s[1]);
                            else
                                _skinMap.Remove(s[0]);
                        }
                        finally
                        {
                            if (_skinMapLock.IsWriteLockHeld)
                                _skinMapLock.ExitWriteLock();
                        }
                    }
                }
            }
            finally
            {
                if (_skinMapLock.IsUpgradeableReadLockHeld)
                    _skinMapLock.ExitUpgradeableReadLock();
            }
        }
        private static string[] SplitPath(string fullPath)
        {
            if (!fullPath.StartsWith(RepositoryStructure.SkinRootFolderPath))
                throw new InvalidOperationException("Skin update system called for non-skin path " + fullPath);

            if (fullPath.Length <= RepositoryStructure.SkinRootFolderPath.Length + 1)
                return null;

            var rippedPath = fullPath.Substring(RepositoryStructure.SkinRootFolderPath.Length + 1);
            var splitPath = rippedPath.Split(new[] { '/' }, 2);

            return splitPath;
        }
    }
}
