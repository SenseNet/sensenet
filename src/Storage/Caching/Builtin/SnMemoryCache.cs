using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
// ReSharper disable RedundantBaseQualifier

namespace SenseNet.ContentRepository.Storage.Caching.Builtin
{
    public class SnMemoryCache : MemoryCache, ISnCache
    {
        public SnMemoryCache() : this(Guid.NewGuid().ToString("N"))
        {
        }

        public SnMemoryCache(string name, NameValueCollection config = null) : base(name, config)
        {
        }

        public int Count => (int)base.GetCount();

        public object Get(string key)
        {
            return base.Get(key);
        }

        public void Insert(string key, object value)
        {
            Insert(key, value, null);
        }

        public virtual void Insert(string key, object value, CacheDependency dependencies)
        {
            Insert(key, value, dependencies, DateTime.MaxValue, NoSlidingExpiration, CacheItemPriority.Normal,
                null);
        }

        public void Insert(string key, object value, CacheDependency dependencies, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority, object onRemoveCallback)
        {
            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = absoluteExpiration,
                SlidingExpiration = slidingExpiration,
                Priority = priority == CacheItemPriority.NotRemovable
                    ? System.Runtime.Caching.CacheItemPriority.NotRemovable
                    : System.Runtime.Caching.CacheItemPriority.Default,
            };
            CreateDependencies(dependencies, policy);
            base.Set(key, value, policy);
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
                //UNDONE: custom CacheDependency is not supported in this cache implementation
                throw new NotImplementedException();
            }
        }


        public void Remove(string key)
        {
            base.Remove(key);
        }

        private static readonly object LockObject = new object();
        public void Reset()
        {
            List<string> keys = new List<string>();
            lock (LockObject)
            {
                foreach (var entry in this)
                    keys.Add(entry.Key);
            }

            foreach (var key in keys)
            {
                Remove(key);
            }
        }

    }
}
