using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Collections;
using System.Configuration;
using System.Globalization;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching
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
            CacheItemRemovedCallback onRemoveCallback)
        {
            _cache.Insert(key, value, dependencies, absoluteExpiration, slidingExpiration, priority, onRemoveCallback);
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

        public override IEnumerator GetEnumerator()
        {
            return _cache.GetEnumerator();
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

        public string WhatIsInTheCache() // for tests
        {
            var sb = new StringBuilder();
            foreach (DictionaryEntry x in _cache)
                sb.AppendLine(x.Key.ToString());
            return sb.ToString();
        }
    }
}
