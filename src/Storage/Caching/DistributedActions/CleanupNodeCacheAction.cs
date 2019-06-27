using System;
using System.Linq;
using SenseNet.Communication.Messaging;

namespace SenseNet.ContentRepository.Storage.Caching.DistributedActions
{
    public class CleanupNodeCacheAction : DistributedAction
    {
        public override void DoAction(bool onRemote, bool isFromMe)
        {
            // Local echo of my action: return without doing anything.
            if (onRemote && isFromMe)
                return;

            var cacheEntryKeys = Cache.Instance.Select(entry => entry.Key)
                .Where(key => key.StartsWith("Token", StringComparison.Ordinal)).ToList();

            foreach (var cacheEntryKey in cacheEntryKeys)
                Cache.Remove(cacheEntryKey);
        }
    }
}
