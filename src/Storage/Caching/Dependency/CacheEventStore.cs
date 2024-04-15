using System.Collections.Generic;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    /// <summary>
    /// Stores event subscriptions for specific types of events (id or path changes, etc.).
    /// </summary>
    public class CacheEventStore
    {
        private readonly CacheOptions _options;

        public CacheEventStore(CacheOptions options)
        {
            _options = options;
            NodeIdChanged = new CacheEvent<int>(_options.NodeIdDependencyEventPartitions);
            NodeTypeChanged = new CacheEvent<int>(_options.NodeTypeDependencyEventPartitions);
            PathChanged = new CacheEvent<string>(_options.PathDependencyEventPartitions);
            PortletChanged = new CacheEvent<string>(_options.PortletDependencyEventPartitions);
        }

        /// <summary>
        /// Event for a node id change.
        /// </summary>
        public readonly CacheEvent<int> NodeIdChanged;

        /// <summary>
        /// Event for a node type change.
        /// </summary>
        public readonly CacheEvent<int> NodeTypeChanged;

        /// <summary>
        /// Event for a node path change.
        /// </summary>
        public readonly CacheEvent<string> PathChanged;

        /// <summary>
        /// Event for a portlet change.
        /// </summary>
        public readonly CacheEvent<string> PortletChanged;

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
