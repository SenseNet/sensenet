using System;
using System.Collections;
using System.Web;
using System.Web.Caching;
using SenseNet.ContentRepository.Storage.Caching;
using System.Collections.Generic;

namespace SenseNet.ContentRepository.Tests.Implementations
{
    public class EmptyCache : ICache
    {
        private readonly IDictionary _emptyCache = new Dictionary<string, object>();

        public IEnumerator GetEnumerator()
        {
            return _emptyCache.GetEnumerator();
        }

        public DateTime NoAbsoluteExpiration { get; } = DateTime.MaxValue;
        public TimeSpan NoSlidingExpiration { get; } = TimeSpan.MaxValue;
        public int Count { get; } = 0;
        public long EffectivePercentagePhysicalMemoryLimit { get; } = 0;
        public long EffectivePrivateBytesLimit { get; } = 0;
        public HttpContext CurrentHttpContext { get; set; }

        public object this[string key]
        {
            get
            {
                return null;
            }
            set
            { 
                // do nothing
            }
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
            TimeSpan slidingExpiration, CacheItemPriority priority, CacheItemRemovedCallback onRemoveCallback)
        {
            // do nothing
        }
        public void Insert(string key, object value, CacheDependency dependencies)
        {
            // do nothing
        }
    }
}
