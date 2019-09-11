using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
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

            public override Task DoActionAsync(bool onRemote, bool isFromMe, CancellationToken cancellationToken)
            {
                if (onRemote && isFromMe)
                    return Task.CompletedTask;
                FireChangedPrivate(_portletId);

                return Task.CompletedTask;
            }
        }
        #endregion

        /// <summary>
        /// Gets the id of the changed portlet.
        /// </summary>
        public string PortletId { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="PortletDependency"/> class.
        /// </summary>
        public PortletDependency(string portletId)
        {
            PortletId = portletId;
        }

        /// <summary>
        /// Fires a distributed action for a portlet change.
        /// </summary>
        public static void FireChanged(string portletId)
        {
            new FireChangedDistributedAction(portletId).ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        private static void FireChangedPrivate(string portletId)
        {
            lock (EventSync)
                Providers.Instance.CacheProvider.Events.PortletChanged.Fire(null, portletId);
        }

        /// <summary>
        /// Subscribe to a PortletChanged event.
        /// </summary>
        /// <param name="eventHandler">Event handler for a portlet change.</param>
        public static void Subscribe(EventHandler<EventArgs<string>> eventHandler)
        {
            lock (EventSync)
                Providers.Instance.CacheProvider.Events.PortletChanged.Subscribe(eventHandler);
        }
        /// <summary>
        /// Unsubscribe from the PortletChanged event.
        /// </summary>
        public static void Unsubscribe(EventHandler<EventArgs<string>> eventHandler)
        {
            lock (EventSync)
                Providers.Instance.CacheProvider.Events.PortletChanged.Unsubscribe(eventHandler);
        }

        /// <summary>
        /// Determines whether the changed portlet (represented by the <see cref="eventData"/> parameter)
        /// should invalidate the <see cref="subscriberData"/> cached object.
        /// </summary>
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
