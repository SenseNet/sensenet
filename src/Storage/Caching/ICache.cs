using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Web.Caching;
using System.Web;

namespace SenseNet.ContentRepository.Storage.Caching
{
    public interface ICache : IEnumerable
    {
        DateTime NoAbsoluteExpiration { get; }
        TimeSpan NoSlidingExpiration { get; }

        int Count { get; }

        long EffectivePercentagePhysicalMemoryLimit { get; }
        long EffectivePrivateBytesLimit { get; }

        object this[string key] { get; set; }

        object Get(string key);
        void Insert(string key, object value);
        void Insert(string key, object value, CacheDependency dependencies);
        void Insert(string key, object value, CacheDependency dependencies,
            DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority,
            CacheItemRemovedCallback onRemoveCallback);
        void Remove(string key);

        void Reset();
        HttpContext CurrentHttpContext { get; set; }
    }

}
