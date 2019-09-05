using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    /// <summary>
    /// Represents a cache dependency that is triggered by a type change.
    /// </summary>
    public class NodeTypeDependency : CacheDependency
    {
        #region private class FireChangedDistributedAction
        [Serializable]
        private class FireChangedDistributedAction : DistributedAction
        {
            private readonly int _nodeTypeId;

            public FireChangedDistributedAction(int nodeTypeId)
            {
                _nodeTypeId = nodeTypeId;
            }

            public override Task DoActionAsync(bool onRemote, bool isFromMe, CancellationToken cancellationToken)
            {
                if (onRemote && isFromMe)
                    return Task.CompletedTask;
                FireChangedPrivate(_nodeTypeId);

                return Task.CompletedTask;
            }
        }
        #endregion

        /// <summary>
        /// Gets the id of the changed node type.
        /// </summary>
        public int NodeTypeId { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="NodeTypeDependency"/> class.
        /// </summary>
        /// <param name="nodeTypeId">The id of the changed node type.</param>
        public NodeTypeDependency(int nodeTypeId)
        {
            NodeTypeId = nodeTypeId;
        }
        /// <summary>
        /// Fires a distributed action for a node type change.
        /// </summary>
        public static void FireChanged(int nodeTypeId)
        {
            new FireChangedDistributedAction(nodeTypeId).ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        private static void FireChangedPrivate(int nodeTypeId)
        {
            lock (EventSync)
                Providers.Instance.CacheProvider.Events.NodeTypeChanged.Fire(null, nodeTypeId);
        }

        /// <summary>
        /// Subscribe to a NodeTypeChanged event.
        /// </summary>
        /// <param name="eventHandler">Event handler for a node type change.</param>
        public static void Subscribe(EventHandler<EventArgs<int>> eventHandler)
        {
            lock (EventSync)
                Providers.Instance.CacheProvider.Events.NodeTypeChanged.Subscribe(eventHandler);
        }
        /// <summary>
        /// Unsubscribe from the NodeTypeChanged event.
        /// </summary>
        public static void Unsubscribe(EventHandler<EventArgs<int>> eventHandler)
        {
            lock (EventSync)
                Providers.Instance.CacheProvider.Events.NodeTypeChanged.Unsubscribe(eventHandler);
        }

        /// <summary>
        /// Determines whether the changed node type (represented by the <see cref="eventData"/> 
        /// node type id parameter) should invalidate the <see cref="subscriberData"/> cached object.
        /// </summary>
        public static bool IsChanged(int eventData, int subscriberData)
        {
            if (eventData != subscriberData)
                return false;

            SnTrace.Repository.Write("Cache invalidated by nodeTypeId: " + subscriberData);
            return true;
        }
    }
}
