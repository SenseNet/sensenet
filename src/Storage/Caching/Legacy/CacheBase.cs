using System;
using System.Web;
using System.Collections;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Caching.Dependency;

namespace SenseNet.ContentRepository.Storage.Caching.Legacy
{
    public abstract class CacheBase : ICache
    {
        [Obsolete("Do not use this member anymore.", true)]
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
            object onRemoveCallback);

        public abstract void Remove(string key);

        public abstract void Reset();

        //IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        //{
        //    return (IEnumerator<KeyValuePair<string, object>>)GetEnumerator();
        //}

        public abstract IEnumerator<KeyValuePair<string, object>> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual DateTime NoAbsoluteExpiration => DateTime.MaxValue;

        public virtual TimeSpan NoSlidingExpiration => TimeSpan.Zero;

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
