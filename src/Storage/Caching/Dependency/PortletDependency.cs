using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Caching.DistributedActions;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    /// <summary>
    /// Represents a dependency that is notified when the portlet changes
    /// to invalidate the related cache item.
    /// </summary>
    public class PortletDependency : CacheDependency
    {
        #region private class FireChangedDistributedAction
        [Serializable]
        private class FireChangedDistributedAction : DistributedAction
        {
            private readonly string _portletId;

            public FireChangedDistributedAction(string portletId)
            {
                _portletId = portletId;
            }

            public override void DoAction(bool onRemote, bool isFromMe)
            {
                if (onRemote && isFromMe)
                    return;
                FireChangedPrivate(_portletId);
            }
        }
        #endregion

        private static readonly EventServer<string> Changed = new EventServer<string>(Cache.PortletDependencyEventPartitions);

        public string PortletId { get; }
        public PortletDependency(string portletId)
        {
            PortletId = portletId;
        }

        public static void FireChanged(string portletId)
        {
            new FireChangedDistributedAction(portletId).Execute();
        }
        private static void FireChangedPrivate(string portletId)
        {
            lock (EventSync)
                Changed.Fire(null, portletId);
        }

        public static void Subscribe(EventHandler<EventArgs<string>> eventHandler)
        {
            lock (EventSync)
                Changed.TheEvent += eventHandler;
        }
        public static void Unsubscribe(EventHandler<EventArgs<string>> eventHandler)
        {
            lock (EventSync)
                Changed.TheEvent -= eventHandler;
        }

        public static bool IsChanged(string eventData, string subscriberData)
        {
            if (eventData != subscriberData)
                return false;

            SnTrace.Repository.Write("Cache invalidated by portletId: " + subscriberData);
            return true;
        }


        [Obsolete("Use FireChanged(string) method instead.")]
        public static void NotifyChange(string portletId)
        {
            FireChanged(portletId);
        }

    }
}
