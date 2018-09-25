using System.Collections.Generic;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    public class AggregateCacheDependency : CacheDependency
    {
        public List<CacheDependency> Dependencies { get; } = new List<CacheDependency>();

        public void Add(params CacheDependency[] dependencies)
        {
            Dependencies.AddRange(dependencies);
        }
    }
}
