using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Communication.Messaging;

namespace SenseNet.ContentRepository.Storage.Caching.DistributedActions
{
    public class CleanupNodeCacheAction : DistributedAction
    {
        public override Task DoActionAsync(bool onRemote, bool isFromMe, CancellationToken cancellationToken)
        {
            // Local echo of my action: return without doing anything.
            if (onRemote && isFromMe)
                return Task.CompletedTask;

            var cacheEntryKeys = Cache.Instance.Select(entry => entry.Key)
                .Where(key => key.StartsWith("Token", StringComparison.Ordinal)).ToList();

            foreach (var cacheEntryKey in cacheEntryKeys)
                Cache.Remove(cacheEntryKey);

            return Task.CompletedTask;
        }
    }
}
