using System;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
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

        private static readonly EventServer<int> Changed = new EventServer<int>(Cache.NodeIdDependencyEventPartitions);

        public int NodeId { get; }
        public NodeIdDependency(int nodeId)
        {
            NodeId = nodeId;
        }
        public static void FireChanged(int nodeId)
        {
            new FireChangedDistributedAction(nodeId).Execute();
        }
        private static void FireChangedPrivate(int nodeId)
        {
            lock (EventSync)
                Changed.Fire(null, nodeId);
        }

        public static void Subscribe(EventHandler<EventArgs<int>> eventHandler)
        {
            lock (EventSync)
                Changed.TheEvent += eventHandler;
        }
        public static void Unsubscribe(EventHandler<EventArgs<int>> eventHandler)
        {
            lock (EventSync)
                Changed.TheEvent -= eventHandler;
        }

        public static bool IsChanged(int eventData, int subscriberData)
        {
            if (eventData != subscriberData)
                return false;

            SnTrace.Repository.Write("Cache invalidated by nodeId: " + subscriberData);
            return true;
        }
    }
}
