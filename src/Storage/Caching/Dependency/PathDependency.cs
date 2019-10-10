using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    /// <summary>
    /// Represents a cache dependency based on a node path that is triggered by a node change.
    /// </summary>
    public class PathDependency : CacheDependency
    {
        #region private class FireChangedDistributedAction
        [Serializable]
        private class FireChangedDistributedAction : DistributedAction
        {
            private readonly string _path;

            public FireChangedDistributedAction(string path)
            {
                _path = path;
            }

            public override Task DoActionAsync(bool onRemote, bool isFromMe, CancellationToken cancellationToken)
            {
                if (onRemote && isFromMe)
                    return Task.CompletedTask;
                FireChangedPrivate(_path);

                return Task.CompletedTask;
            }
        }
        #endregion

        public string Path { get; }
        public PathDependency(string path)
        {
            Path = path;
        }
        /// <summary>
        /// Fires a distributed action for a node change.
        /// </summary>
        public static void FireChanged(string path)
        {
            new FireChangedDistributedAction(path).ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        private static void FireChangedPrivate(string path)
        {
            lock (EventSync)
                Providers.Instance.CacheProvider.Events.PathChanged.Fire(null, path);
        }

        /// <summary>
        /// Subscribe to a PathChanged event.
        /// </summary>
        /// <param name="eventHandler">Event handler for a node change.</param>
        public static void Subscribe(EventHandler<EventArgs<string>> eventHandler)
        {
            lock (EventSync)
                Providers.Instance.CacheProvider.Events.PathChanged.Subscribe(eventHandler);
        }
        /// <summary>
        /// Unsubscribe from the PathChanged event.
        /// </summary>
        public static void Unsubscribe(EventHandler<EventArgs<string>> eventHandler)
        {
            lock (EventSync)
                Providers.Instance.CacheProvider.Events.PathChanged.Unsubscribe(eventHandler);
        }

        /// <summary>
        /// Determines whether the changed node (represented by the <see cref="eventData"/> path parameter)
        /// should invalidate the <see cref="subscriberData"/> cached object.
        /// </summary>
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
}
