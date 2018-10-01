using System.Collections.Generic;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    /// <summary>
    /// Stores event subscriptions for specific types of events (id or path changes, etc.).
    /// </summary>
    public class CacheEventStore
    {
        /// <summary>
        /// Event for a node id change.
        /// </summary>
        public readonly CacheEvent<int> NodeIdChanged =
            new CacheEvent<int>(Cache.NodeIdDependencyEventPartitions);

        /// <summary>
        /// Event for a node type change.
        /// </summary>
        public readonly CacheEvent<int> NodeTypeChanged =
            new CacheEvent<int>(Cache.NodeTypeDependencyEventPartitions);

        /// <summary>
        /// Event for a node path change.
        /// </summary>
        public readonly CacheEvent<string> PathChanged =
            new CacheEvent<string>(Cache.PathDependencyEventPartitions);

        /// <summary>
        /// Event for a portlet change.
        /// </summary>
        public readonly CacheEvent<string> PortletChanged =
            new CacheEvent<string>(Cache.PortletDependencyEventPartitions);

        /// <summary>
        /// Gets the subscription counts for all well-known event types.
        /// </summary>
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
