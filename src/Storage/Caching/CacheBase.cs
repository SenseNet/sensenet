using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Collections;

namespace SenseNet.ContentRepository.Storage.Caching
{
    public abstract class CacheBase : ICache
    {
        protected HttpContext _currentContext;

        public abstract object Get(string key);

        public abstract void Insert(string key, object value);

        public virtual void Insert(string key, object value, CacheDependency dependencies)
        {
            Insert(key, value, dependencies, NoAbsoluteExpiration, NoSlidingExpiration, CacheItemPriority.Normal,
                null);
        }

        public abstract void Insert(string key, object value, CacheDependency dependencies,
            DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority,
            CacheItemRemovedCallback onRemoveCallback);

        public abstract void Remove(string key);

        public abstract void Reset();

        public abstract IEnumerator GetEnumerator();

        public virtual DateTime NoAbsoluteExpiration
        {
            get { return System.Web.Caching.Cache.NoAbsoluteExpiration; }
        }

        public virtual TimeSpan NoSlidingExpiration
        {
            get { return System.Web.Caching.Cache.NoSlidingExpiration; }
        }

        public abstract int Count
        {
            get;
        }

        public abstract long EffectivePercentagePhysicalMemoryLimit
        {
            get;
        }

        public abstract long EffectivePrivateBytesLimit
        {
            get;
        }

        public abstract object this[string key]
        {
            get;
            set;
        }



        private HttpContext _currentHttpContext;

        public HttpContext CurrentHttpContext
        {
            get { return _currentHttpContext; }
            set { _currentHttpContext = value; }
        }
    }

}
