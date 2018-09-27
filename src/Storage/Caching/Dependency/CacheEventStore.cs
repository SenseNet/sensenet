using System.Collections.Generic;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    public class CacheEventStore
    {
        public readonly CacheEvent<int> NodeIdChanged =
            new CacheEvent<int>(Cache.NodeIdDependencyEventPartitions);

        public readonly CacheEvent<int> NodeTypeChanged =
            new CacheEvent<int>(Cache.NodeTypeDependencyEventPartitions);

        public readonly CacheEvent<string> PathChanged =
            new CacheEvent<string>(Cache.PathDependencyEventPartitions);

        public readonly CacheEvent<string> PortletChanged =
            new CacheEvent<string>(Cache.PortletDependencyEventPartitions);

        public Dictionary<string, int[]> GetCounts()
        {
            return new Dictionary<string, int[]>
            {
                {"NodeIdChanged", NodeIdChanged.GetCounts()},
                {"NodeTypeChanged", NodeTypeChanged.GetCounts()},
                {"PathChanged", PathChanged.GetCounts()},
                {"PortletChanged", PortletChanged.GetCounts()},
            };
        }
    }
}
