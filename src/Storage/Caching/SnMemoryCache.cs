using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Caching;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
// ReSharper disable RedundantBaseQualifier

namespace SenseNet.ContentRepository.Storage.Caching
{
    /// <summary>
    /// Built-in memory cache implementation of the <see cref="ISnCache"/> interface.
    /// </summary>
    public class SnMemoryCache : MemoryCache, ISnCache
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnMemoryCache"/> class.
        /// </summary>
        public SnMemoryCache() : this(Guid.NewGuid().ToString("N"))
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SnMemoryCache"/> class.
        /// </summary>
        public SnMemoryCache(string name, NameValueCollection config = null) : base(name, config)
        {
        }

        /// <inheritdoc />
        public int Count => (int)base.GetCount();

        /// <inheritdoc />
        public CacheEventStore Events { get; set; }

        /// <inheritdoc />
        public object Get(string key)
        {
            return base.Get(key);
        }

        /// <inheritdoc />
        public void Insert(string key, object value)
        {
            Insert(key, value, null);
        }
        
        /// <inheritdoc />
        public virtual void Insert(string key, object value, CacheDependency dependencies)
        {
            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTime.MaxValue,
                SlidingExpiration = NoSlidingExpiration,
                Priority = System.Runtime.Caching.CacheItemPriority.Default,
            };
            CreateDependencies(dependencies, policy);
            base.Set(key, value, policy);
        }

        [Obsolete("Do not use priority in the caching API. Use the expiration times instead.")]
        public void Insert(string key, object value, CacheDependency dependencies, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, object onRemoveCallback)
        {
            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = absoluteExpiration,
                SlidingExpiration = slidingExpiration,
                Priority = System.Runtime.Caching.CacheItemPriority.Default,
            };
            CreateDependencies(dependencies, policy);
            base.Set(key, value, policy);
        }

        [Obsolete("Do not use priority in the caching API. Use the expiration times instead.")]
        public void Insert(string key, object value, CacheDependency dependencies, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority, object onRemoveCallback)
        {
            Insert(key, value, dependencies, absoluteExpiration, slidingExpiration, onRemoveCallback);
        }
        private void CreateDependencies(CacheDependency dependencies, CacheItemPolicy policy)
        {
            if (dependencies == null)
                return;

            if(dependencies is AggregateCacheDependency aggregateDep)
            {
                foreach(var item in aggregateDep.Dependencies)
                    CreateDependencies(item, policy);
            }
            else if(dependencies is NodeIdDependency nodeIdDep)
            {
                policy.ChangeMonitors.Add( new NodeIdChangeMonitor(nodeIdDep.NodeId));
            }
            else if(dependencies is NodeTypeDependency nodeTypeDep)
            {
                policy.ChangeMonitors.Add(new NodeTypeChangeMonitor(nodeTypeDep.NodeTypeId));
            }
            else if(dependencies is PathDependency pathDep)
            {
                policy.ChangeMonitors.Add(new PathChangeMonitor(pathDep.Path));
            }
            else if (dependencies is PortletDependency portletDep)
            {
                policy.ChangeMonitors.Add(new PortleChangeMonitor(portletDep.PortletId));
            }
            else
            {
                throw new SnNotSupportedException(
                    "Unknown cache dependency is not supported in this version. Type: " + 
                    dependencies.GetType().FullName);
            }
        }

        /// <inheritdoc />
        public void Remove(string key)
        {
            base.Remove(key);
        }

        private static readonly object LockObject = new object();
        /// <inheritdoc />
        public void Reset()
        {
            var keys = new List<string>();
            lock (LockObject)
            {
#pragma warning disable CS0279 // Type does not implement the collection pattern; member is either static or not public
                foreach (var entry in this)
#pragma warning restore CS0279 // Type does not implement the collection pattern; member is either static or not public
                    keys.Add(entry.Key);
            }

            foreach (var key in keys)
            {
                Remove(key);
            }
        }
    }
}
