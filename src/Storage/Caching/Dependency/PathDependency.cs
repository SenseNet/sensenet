using System;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
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

            public override void DoAction(bool onRemote, bool isFromMe)
            {
                if (onRemote && isFromMe)
                    return;
                FireChangedPrivate(_path);
            }
        }
        // -----------------------------------------------------------------------------------------
        #endregion

        private static readonly EventServer<string> Changed = new EventServer<string>(Cache.PathDependencyEventPartitions);

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
            lock (EventSync)
                Changed.Fire(null, path);
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
