using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Collections;
using System.Linq;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Legacy
{
    /// <summary>
    /// Wrapper class around the good old ASP.NET cache 
    /// main features: populator (create/load/whatever the cached item)
    /// Distributed environment wireup
    /// </summary>
    public class AspNetCache : CacheBase
    {
        private static object _lockObject = new object();

        public enum TraceVerbosity { Silent, Basic, Verbose };

        private System.Web.Caching.Cache _cache;

        public AspNetCache()
        {
            _cache = HttpRuntime.Cache;
        }

        public override object Get(string key)
        {
            return _cache.Get(key);
        }

        public override void Insert(string key, object value)
        {
            _cache.Insert(key, value);
        }

        public override void Insert(string key, object value, CacheDependency dependencies,
            DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority,
            object onRemoveCallback)
        {
            if(onRemoveCallback != null) //UNDONE: discuss: onRemoveCallback is not supported
                throw new NotSupportedException("The onRemoveCallback is not supported in this version.");

            var realPriority = (System.Web.Caching.CacheItemPriority) ((int) priority);
            var dependencyImplementations = CreateDependencies(dependencies);
            _cache.Insert(key, value, dependencyImplementations, absoluteExpiration, slidingExpiration, realPriority, null);
        }
        private System.Web.Caching.CacheDependency CreateDependencies(CacheDependency dependencies)
        {
            if (dependencies is AggregateCacheDependency aggregateDep)
            {
                var result = new System.Web.Caching.AggregateCacheDependency();
                result.Add(aggregateDep.Dependencies.Select(CreateDependencies).ToArray());
                return result;
            }
            if (dependencies is NodeIdDependency nodeIdDep)
            {
                return new NodeIdDependencyImpl(nodeIdDep.NodeId);
            }
            if (dependencies is NodeTypeDependency nodeTypeDep)
            {
                return new NodeTypeDependencyImpl(nodeTypeDep.NodeTypeId);
            }
            if (dependencies is PathDependency pathDep)
            {
                return new PathDependencyImpl(pathDep.Path);
            }
            if (dependencies is PortletDependency portletDep)
            {
                return new PortletDependencyImpl(portletDep.PortletId);
            }
            //UNDONE: custom CacheDependency is not supported in this cache implementation
            throw new NotImplementedException();
        }

        public override void Remove(string key)
        {
            _cache.Remove(key);
        }

        public override void Reset()
        {
            SnLog.WriteInformation("Cache Reset. StackTrace: " + System.Environment.StackTrace);

            List<string> keys = new List<string>();
            lock (_lockObject)
            {
                foreach (DictionaryEntry entry in _cache)
                    keys.Add(entry.Key.ToString());
            }

            foreach (var key in keys)
            {
                _cache.Remove(key);
            }
        }

        public override int Count
        {
            get { return _cache.Count; }
        }

        public override long EffectivePercentagePhysicalMemoryLimit
        {
            get { return _cache.EffectivePercentagePhysicalMemoryLimit; }
        }

        public override long EffectivePrivateBytesLimit
        {
            get { return _cache.EffectivePrivateBytesLimit; }
        }

        public override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _cache
                .Cast<DictionaryEntry>()
                .Select(x => new KeyValuePair<string, object>(x.Key.ToString(), x.Value))
                .GetEnumerator();
        }

        public override object this[string key]
        {
            get
            {
                return _cache[key];
            }
            set
            {
                _cache[key] = value;
            }
        }
    }
}
