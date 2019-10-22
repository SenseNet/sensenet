using System.Collections.Generic;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    /// <summary>
    /// Holds multiple cache dependencies for the same cached object.
    /// </summary>
    public class AggregateCacheDependency : CacheDependency
    {
        /// <summary>
        /// List of cache dependencies.
        /// </summary>
        public List<CacheDependency> Dependencies { get; } = new List<CacheDependency>();

        /// <summary>
        /// Adds one or more related dependencies to the collection.
        /// </summary>
        public void Add(params CacheDependency[] dependencies)
        {
            Dependencies.AddRange(dependencies);
        }
    }
}
