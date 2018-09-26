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
        public SnMemoryCache(string name, NameValueCollection config = null) : base(name, config)
        {
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int Count { get; }

        public object this[string key]
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public object Get(string key)
        {
            throw new NotImplementedException();
        }

        public void Insert(string key, object value)
        {
            throw new NotImplementedException();
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

            if (dependencies is AggregateCacheDependency aggregateDep)
            {
                foreach(var item in aggregateDep.Dependencies)
                    CreateDependencies(item, policy);
            }
            if (dependencies is NodeIdDependency nodeIdDep)
            {
                policy.ChangeMonitors.Add( new NodeIdChangeMonitor(nodeIdDep.NodeId));
            }
            if (dependencies is NodeTypeDependency nodeTypeDep)
            {
                policy.ChangeMonitors.Add(new NodeTypeChangeMonitor(nodeTypeDep.NodeTypeId));
            }
            if (dependencies is PathDependency pathDep)
            {
                policy.ChangeMonitors.Add(new PathChangeMonitor(pathDep.Path));
            }
            if (dependencies is PortletDependency portletDep)
            {
                policy.ChangeMonitors.Add(new PortleChangeMonitor(portletDep.PortletId));
            }
            //UNDONE: custom CacheDependency is not supported in this cache implementation
            throw new NotImplementedException();
        }


        public void Remove(string key)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

    }
}
