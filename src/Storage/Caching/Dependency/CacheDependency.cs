using System;
using System.Collections.Generic;
using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository.Storage.Caching.DistributedActions;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    /// <summary>
    /// Base class of the object that declares various, implementation-independent cache dependency value.
    /// </summary>
    public class CacheDependency
    {
    }

    public class AggregateCacheDependency : CacheDependency
    {
        public List<CacheDependency> Dependencies { get; } = new List<CacheDependency>();

        public void Add(params CacheDependency[] dependencies)
        {
            Dependencies.AddRange(dependencies);
        }
    }

    public class NodeIdDependency : CacheDependency
    {
        #region private class FireChangedDistributedAction
        [Serializable]
        private class FireChangedDistributedAction : DistributedAction
        {
            private int _nodeId;

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
            lock (SnCache.EventSync)
            {
                SnCache.NodeIdChanged.Fire(null, nodeId);
            }
        }

        public static void Subscribe(EventHandler<EventArgs<int>> eventHandler)
        {
            lock (SnCache.EventSync)
                SnCache.NodeIdChanged.TheEvent += eventHandler;
        }
        public static void Unsubscribe(EventHandler<EventArgs<int>> eventHandler)
        {
            lock (SnCache.EventSync)
                SnCache.NodeIdChanged.TheEvent -= eventHandler;
        }

        public static bool IsChanged(int eventData, int subscriberData)
        {
            if (eventData != subscriberData)
                return false;

            SnTrace.Repository.Write("Cache invalidated by nodeId: " + subscriberData);
            return true;
        }
    }

    public class NodeTypeDependency : CacheDependency
    {
        #region private class FireChangedDistributedAction
        [Serializable]
        private class FireChangedDistributedAction : DistributedAction
        {
            private int _nodeTypeId;

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
            lock (SnCache.EventSync)
            {
                SnCache.NodeTypeChanged.Fire(null, nodeTypeId);
            }
        }

        public static void Subscribe(EventHandler<EventArgs<int>> eventHandler)
        {
            lock (SnCache.EventSync)
                SnCache.NodeTypeChanged.TheEvent += eventHandler;
        }
        public static void Unsubscribe(EventHandler<EventArgs<int>> eventHandler)
        {
            lock (SnCache.EventSync)
                SnCache.NodeTypeChanged.TheEvent -= eventHandler;
        }

        public static bool IsChanged(int eventData, int subscriberData)
        {
            if (eventData != subscriberData)
                return false;

            SnTrace.Repository.Write("Cache invalidated by nodeTypeId: " + subscriberData);
            return true;
        }
    }

    public class PathDependency : CacheDependency
    {
        #region private class FireChangedDistributedAction
        [Serializable]
        private class FireChangedDistributedAction : DistributedAction
        {
            private string _path;

            public FireChangedDistributedAction(string path)
            {
                _path = path;
            }

            public override void DoAction(bool onRemote, bool isFromMe)
            {
                if (onRemote && isFromMe)
                    return;
                FireChangedPrivate(_path);
            }
        }
        // -----------------------------------------------------------------------------------------
        #endregion
        public string Path { get; }
        public PathDependency(string path)
        {
            Path = path;
        }
        public static void FireChanged(string path)
        {
            new FireChangedDistributedAction(path).Execute();
        }
        private static void FireChangedPrivate(string path)
        {
            lock (SnCache.EventSync)
            {
                SnCache.PathChanged.Fire(null, path);
            }
        }

        public static void Subscribe(EventHandler<EventArgs<string>> eventHandler)
        {
            lock (SnCache.EventSync)
                SnCache.PathChanged.TheEvent += eventHandler;
        }
        public static void Unsubscribe(EventHandler<EventArgs<string>> eventHandler)
        {
            lock (SnCache.EventSync)
                SnCache.PathChanged.TheEvent -= eventHandler;
        }

        public static bool IsChanged(string eventData, string subscriberData)
        {
            var match = subscriberData.Equals(eventData, StringComparison.OrdinalIgnoreCase);
            if (!match)
                match = subscriberData.StartsWith(string.Concat(eventData, RepositoryPath.PathSeparator), StringComparison.OrdinalIgnoreCase);

            if (!match)
                return false;

            SnTrace.Repository.Write("Cache invalidated by path: " + subscriberData);
            return true;
        }
    }

    /// <summary>
    /// Represents a dependency that is notified when the portlet changes
    /// to invalidate the related cache item.
    /// </summary>
    public class PortletDependency : CacheDependency
    {
        public string PortletId { get; }
        public PortletDependency(string portletId)
        {
            PortletId = portletId;
        }
        //UNDONE: remove this method (sn-webpages uses this)
        public static void NotifyChange(string portletId)
        {
            new PortletChangedAction(portletId).Execute();
        }
        //UNDONE: use FireChanged pattern
        public static void FireChanged(string portletId)
        {
            lock (SnCache.EventSync)
            {
                SnCache.PortletChanged.Fire(null, portletId);
            }
        }

        public static void Subscribe(EventHandler<EventArgs<string>> eventHandler)
        {
            lock (SnCache.EventSync)
                SnCache.PortletChanged.TheEvent += eventHandler;
        }
        public static void Unsubscribe(EventHandler<EventArgs<string>> eventHandler)
        {
            lock (SnCache.EventSync)
                SnCache.PortletChanged.TheEvent -= eventHandler;
        }

        public static bool IsChanged(string eventData, string subscriberData)
        {
            if (eventData != subscriberData)
                return false;

            SnTrace.Repository.Write("Cache invalidated by portletId: " + subscriberData);
            return true;
        }
    }
}
