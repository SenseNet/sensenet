using System;
using System.Linq;
using SenseNet.Communication.Messaging;

namespace SenseNet.ContentRepository.Storage.Caching.DistributedActions
{
    [Serializable]
    public class CacheCleanAction : DistributedAction
    {

        public override void DoAction(bool onRemote, bool isFromMe)
        {
            // Local echo of my action: return without doing anything.
            if (onRemote && isFromMe)
                return;

            var cacheEntryKeys = DistributedApplication.Cache.Select(entry => entry.Key).ToList();

            foreach (var cacheEntryKey in cacheEntryKeys)
                DistributedApplication.Cache.Remove(cacheEntryKey);
        }
    }
}