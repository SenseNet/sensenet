using System;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
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

            public override void DoAction(bool onRemote, bool isFromMe)
            {
                if (onRemote && isFromMe)
                    return;
                FireChangedPrivate(_nodeTypeId);
            }
        }
        #endregion

        private static readonly EventServer<int> Changed = new EventServer<int>(Cache.NodeTypeDependencyEventPartitions);

        public int NodeTypeId { get; }
        public NodeTypeDependency(int nodeTypeId)
        {
            NodeTypeId = nodeTypeId;
        }
        public static void FireChanged(int nodeTypeId)
        {
            new FireChangedDistributedAction(nodeTypeId).Execute();
        }
        private static void FireChangedPrivate(int nodeTypeId)
        {
            lock (EventSync)
                Changed.Fire(null, nodeTypeId);
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

            SnTrace.Repository.Write("Cache invalidated by nodeTypeId: " + subscriberData);
            return true;
        }
    }
}
