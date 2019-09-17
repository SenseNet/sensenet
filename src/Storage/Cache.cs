using System;
using System.Collections.Generic;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Caching;
using SenseNet.ContentRepository.Storage.Caching.Dependency;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository
{
    public static class Cache
    {
        public static ISnCache Instance => Providers.Instance.CacheProvider;

        public static CacheEventStore Events => Instance.Events;

        public static int Count => Instance.Count;

        public static object Get(string key)
        {
            return Instance.Get(key);
        }

        public static IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return Instance.GetEnumerator();
        }

        public static void Insert(string key, object value)
        {
            Instance.Insert(key, value);
        }
        public static void Insert(string key, object value, CacheDependency dependencies)
        {
            Instance.Insert(key, value, dependencies);
        }
        public static void Insert(string key, object value, CacheDependency dependencies, DateTime absoluteExpiration, TimeSpan slidingExpiration, object onRemoveCallback)
        {
            Instance.Insert(key, value, dependencies, absoluteExpiration, slidingExpiration, onRemoveCallback);
        }

        public static void Remove(string key)
        {
            Instance.Remove(key);
        }

        public static void Reset()
        {
            Instance.Reset();
        }
    }
}
