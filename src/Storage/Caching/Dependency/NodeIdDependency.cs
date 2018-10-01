using System;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    /// <summary>
    /// Represents a cache dependency based on a node id that is triggered by a node change.
    /// </summary>
    public class NodeIdDependency : CacheDependency
    {
        #region private class FireChangedDistributedAction
        [Serializable]
        private class FireChangedDistributedAction : DistributedAction
        {
            private readonly int _nodeId;

            public FireChangedDistributedAction(int nodeId)
            {
                _nodeId = nodeId;
            }

            public override void DoAction(bool onRemote, bool isFromMe)
            {
                if (onRemote && isFromMe)
                    return;
                FireChangedPrivate(_nodeId);
            }
        }
        #endregion

        /// <summary>
        /// Gets the id of the changed node.
        /// </summary>
        public int NodeId { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="NodeIdDependency"/> class.
        /// </summary>
        /// <param name="nodeId">The id of the changed node.</param>
        public NodeIdDependency(int nodeId)
        {
            NodeId = nodeId;
        }
        /// <summary>
        /// Fires a distributed action for a node change.
        /// </summary>
        public static void FireChanged(int nodeId)
        {
            new FireChangedDistributedAction(nodeId).Execute();
        }
        private static void FireChangedPrivate(int nodeId)
        {
            lock (EventSync)
                Providers.Instance.CacheProvider.Events.NodeIdChanged.Fire(null, nodeId);
        }

        /// <summary>
        /// Subscribe to a NodeIdChanged event.
        /// </summary>
        /// <param name="eventHandler">Event handler for a node change.</param>
        public static void Subscribe(EventHandler<EventArgs<int>> eventHandler)
        {
            lock (EventSync)
                Providers.Instance.CacheProvider.Events.NodeIdChanged.Subscribe(eventHandler);
        }
        /// <summary>
        /// Unsubscribe from the NodeIdChanged event.
        /// </summary>
        public static void Unsubscribe(EventHandler<EventArgs<int>> eventHandler)
        {
            lock (EventSync)
                Providers.Instance.CacheProvider.Events.NodeIdChanged.Unsubscribe(eventHandler);
        }

        /// <summary>
        /// Determines whether the changed node (represented by the <see cref="eventData"/> node id parameter)
        /// should invalidate the <see cref="subscriberData"/> cached object.
        /// </summary>
        public static bool IsChanged(int eventData, int subscriberData)
        {
            if (eventData != subscriberData)
                return false;

            SnTrace.Repository.Write("Cache invalidated by nodeId: " + subscriberData);
            return true;
        }
    }
}
