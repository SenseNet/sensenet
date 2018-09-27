using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Caching.Dependency;


namespace SenseNet.ContentRepository.Storage.Caching
{
    public interface ISnCache : IEnumerable<KeyValuePair<string, object>>
    {
        int Count { get; }

        object this[string key] { get; set; }

        CacheEventStore Events { get; set; }

        object Get(string key);
        void Insert(string key, object value);
        void Insert(string key, object value, CacheDependency dependencies);
        void Insert(string key, object value, CacheDependency dependencies,
            DateTime absoluteExpiration, TimeSpan slidingExpiration,
            object onRemoveCallback);
        [Obsolete("Do not use priority in the caching API. Use the expiration times instead.")]
        void Insert(string key, object value, CacheDependency dependencies,
            DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority,
            object onRemoveCallback);
        void Remove(string key);

        void Reset();
    }

    /// <summary>Specifies the relative priority of items stored in the <see cref="T:System.Web.Caching.Cache" /> object.</summary>
    [Obsolete("Do not use priority in the caching API. Use the expiration times instead.")]
    public enum CacheItemPriority
    {
        /// <summary>Cache items with this priority level are the most likely to be deleted from the cache as the server frees system memory.</summary>
        Low = 1,
        /// <summary>Cache items with this priority level are more likely to be deleted from the cache as the server frees system memory than items assigned a <see cref="F:System.Web.Caching.CacheItemPriority.Normal" /> priority.</summary>
        BelowNormal,
        /// <summary>Cache items with this priority level are likely to be deleted from the cache as the server frees system memory only after those items with <see cref="F:System.Web.Caching.CacheItemPriority.Low" /> or <see cref="F:System.Web.Caching.CacheItemPriority.BelowNormal" /> priority. This is the default.</summary>
        Normal,
        /// <summary>Cache items with this priority level are less likely to be deleted as the server frees system memory than those assigned a <see cref="F:System.Web.Caching.CacheItemPriority.Normal" /> priority.</summary>
        AboveNormal,
        /// <summary>Cache items with this priority level are the least likely to be deleted from the cache as the server frees system memory.</summary>
        High,
        /// <summary>The cache items with this priority level will not be automatically deleted from the cache as the server frees system memory. However, items with this priority level are removed along with other items according to the item's absolute or sliding expiration time. </summary>
        NotRemovable,
        /// <summary>The default value for a cached item's priority is <see cref="F:System.Web.Caching.CacheItemPriority.Normal" />.</summary>
        Default = 3
    }
}
