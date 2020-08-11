using System;
using System.Collections;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Caching;
using SenseNet.ContentRepository.Storage.Caching.Dependency;

namespace SenseNet.Tests.Core.Implementations
{
    public class EmptyCache : ISnCache
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly Dictionary<string, object> _emptyCache = new Dictionary<string, object>();

        public CacheEventStore Events { get; set; }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _emptyCache.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count { get; } = 0;

        public object this[string key]
        {
            get => null;
            set { /* do nothing */ }
        }

        public object Get(string key)
        {
            return null;
        }
        public void Insert(string key, object value)
        {
            // do nothing
        }
        public void Remove(string key)
        {
            // do nothing
        }
        public void Reset()
        {
            // do nothing
        }
        public void Insert(string key, object value, CacheDependency dependencies, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, object onRemoveCallback)
        {
            // do nothing
        }
        [Obsolete("Do not use priority in the caching API. Use the expiration times instead.")]
        public void Insert(string key, object value, CacheDependency dependencies, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority, object onRemoveCallback)
        {
            // do nothing
        }
        public void Insert(string key, object value, CacheDependency dependencies)
        {
            // do nothing
        }
    }
}
