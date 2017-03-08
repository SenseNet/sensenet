using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository
{
    public abstract class JsonTreeCache<T> : TreeCache<T> where T : File
    {
        public class CachedScript
        {
            public string Script { get; private set; }
            public DateTime CacheDate { get; private set; }
            public bool FromCache { get; set; }

            public CachedScript(string script) : this(script, true) {}
            public CachedScript(string script, bool fromCache)
            {
                Script = script;
                CacheDate = DateTime.UtcNow;
                FromCache = fromCache;
            }
        }

        // ReSharper disable once StaticMemberInGenericType
        // (we intend to have a different lock object per generic type here, so this is ok)
        private static readonly object Sync = new object();
        private static JsonTreeCache<T> __instance;
        // ReSharper disable once StaticMemberInGenericType
        // (we intend to have a different type object per generic type here, so this is ok)
        protected static Type _cacheType;
        protected internal static JsonTreeCache<T> Instance
        {
            get
            {
                // this property is protected internal to let tests access it

                if (__instance == null)
                {
                    lock (Sync)
                    {
                        __instance = (JsonTreeCache<T>)GetInstance(_cacheType);

                        SnTrace.System.Write("{0} tree cache instance realoaded.", _cacheType.Name);
                    }
                }
                return __instance;
            }
        }

        // ============================================================================== Properties

        private static readonly string _scriptTemplate = @"var SN=SN||{{}};SN.{0}=SN.{0}||{{}};SN.{0}[""{1}""] = {2};";
        protected virtual string ScriptTemplate { get { return _scriptTemplate; } }
        protected abstract string LocalFolderName { get; }

        // ============================================================================== Overrides

        protected override void InstanceChanged()
        {
            lock (Sync)
            {
                __instance = null;

                FireChanged();
            }
        }
        protected override void Invalidate()
        {
            base.Invalidate();
            FireChanged();
        }

        protected override List<TNode> LoadItems()
        {
            var items = Tools.LoadItemsByContentType(typeof(T).Name);

            SnTrace.System.Write("{0} tree cache reloaded: {1} items.", typeof(T).Name, items.Count);

            return items;
        }

        protected override void OnNodeModified(object sender, NodeEventArgs e)
        {
            if ((e.OriginalSourcePath != e.SourceNode.Path && IsSubtreeContaining(e.OriginalSourcePath)) || e.SourceNode is T)
                Invalidate();
        }

        // ============================================================================== Static API

        public static CachedScript GetScript(string path, string skin, string category = null)
        {
            return Instance.GetScriptInternal(path, skin, category);
        }

        // ============================================================================== Internal instance API

        protected CachedScript GetScriptInternal(string path, string skin, string category = null)
        {
            // serve script from the cache if possible
            var key = GetCacheKey(path, skin, category);
            var cachedScript = DistributedApplication.Cache.Get(key) as CachedScript;
            if (cachedScript != null)
            {
                if (!string.IsNullOrEmpty(cachedScript.Script))
                    return cachedScript;
            }

            // generate script
            var json = GetJson(path, skin, category);
            var script = string.IsNullOrEmpty(category)
                ? string.Format(ScriptTemplate, LocalFolderName, json)
                : string.Format(ScriptTemplate, LocalFolderName, category, json);

            var typeName = typeof (T).Name;

            // insert into cache
            DistributedApplication.Cache.Insert(key, new CachedScript(script), new NodeTypeDependency(ActiveSchema.NodeTypes[typeName].Id));

            SnTrace.Web.Write("JsonTreeCache: generated {0} script with cache key {1}.", typeName, key);

            // we have to return a different object indicating that the script was newly generated
            return new CachedScript(script, false);
        }
        
        protected internal virtual string GetJson(string path, string skin, string category = null)
        {
            // this method is protected internal to let tests access it

            var items = GetItemsInternal(path, skin, category);
            var settings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                TypeNameHandling = TypeNameHandling.None,
                Formatting = Formatting.Indented
            };

            return JsonConvert.SerializeObject(items.ToDictionary(t => t.Key, t => GetBinaryText(t.Value)), settings);
        }
        protected virtual IDictionary<string, T> GetItemsInternal(string path, string skin, string category = null)
        {
            // This method collects all relevant items for a content path. It merges them 
            // into a dictionary by name. Local items override inherited ones with the same name.
            // Skin-level item: /Root/Skins/myskin/templates/action/linkbutton.html
            // Local item     : /Root/Sites/Default_Site/MyWorkspace/templates/action/linkbutton.html

            if (string.IsNullOrEmpty(skin))
                throw new ArgumentNullException("skin");

            if (string.IsNullOrEmpty(path) || string.Compare(path, Repository.RootPath, StringComparison.Ordinal) == 0)
            {
                var globalPath = string.IsNullOrEmpty(category)
                    ? RepositoryPath.Combine(RepositoryStructure.SkinRootFolderPath, skin, LocalFolderName)
                    : RepositoryPath.Combine(RepositoryStructure.SkinRootFolderPath, skin, LocalFolderName, category);

                // Collect global templates from under the skin in this category
                // (for example /Root/Skins/myskin/templates/button/...).
                var skinItems = Content.All.DisableAutofilters().Where(c => 
                    c.InTree(globalPath) &&
                    c.TypeIs(typeof(T).Name)).AsEnumerable().Select(c => c.ContentHandler).Cast<T>();

                return skinItems.ToDictionary(st => st.Name, st => st);
            }

            var localItems = FindNearestItems(path, p => string.IsNullOrEmpty(category)
                ? RepositoryPath.Combine(p, LocalFolderName)
                : RepositoryPath.Combine(p, LocalFolderName, category));

            // no items found in the tree cache, fallback to the skin
            if (localItems == null || localItems.Length == 0)
                return GetItemsInternal(null, skin, category);

            var localItemNodes = Node.LoadNodes(localItems.Select(t => t.Id));

            // Go upwards in the parent chain 4 levels to search for the next item level.
            // Current item path : /Root/Sites/Default_Site/MyWorkspace/templates/action/linkbutton.html
            // Continue with this: /Root/Sites/Default_Site
            var latestPath = localItemNodes[0].Path;
            for (var i = 0; i < 4; i++)
            {
                latestPath = RepositoryPath.GetParentPath(latestPath);
            }

            // collect inherited items and override the slots that are defined locally
            var inheritedItems = GetItemsInternal(latestPath, skin, category);
            foreach (var node in localItemNodes)
            {
                inheritedItems[node.Name] = (T)node;
            }

            return inheritedItems;
        }

        // ============================================================================== Helper methods

        protected static string GetCacheKey(string path, string skin, string category = null)
        {
            return string.Format("{0}+{1}+{2}", path ?? string.Empty, skin, category ?? string.Empty);
        }
        protected static string GetBinaryText(File file)
        {
            if (file == null)
                return string.Empty;

            using (var stream = file.Binary.GetStream())
            {
                return RepositoryTools.GetStreamString(stream) ?? string.Empty;
            }
        }

        protected static void FireChanged()
        {
            var nodeType = ActiveSchema.NodeTypes[typeof(T).Name];

            // It is possible that the nodetype is not registered yet (for 
            // example during initial import).
            if (nodeType != null)
                NodeTypeDependency.FireChanged(nodeType.Id);
            else
                SnTrace.System.Write("TreeCache: Unknown content type: {0}.", typeof(T).Name);
        }
    }
}
