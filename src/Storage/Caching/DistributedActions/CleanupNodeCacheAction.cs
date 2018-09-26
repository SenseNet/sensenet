using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Diagnostics;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.Communication.Messaging;
using System.Globalization;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.DistributedActions
{
    public class CleanupNodeCacheAction : DistributedAction
    {
        public override void DoAction(bool onRemote, bool isFromMe)
        {
            if (onRemote && isFromMe) return;

            List<string> cacheEntryKeys = new List<string>();

            int localCacheCount = DistributedApplication.Cache.Count;

            foreach (var entry in DistributedApplication.Cache)
            {
                var key = entry.Key;
                if (key.StartsWith("Token", StringComparison.Ordinal))
                {
                    cacheEntryKeys.Add(key);
                }
            }

            foreach (string cacheEntryKey in cacheEntryKeys)
                DistributedApplication.Cache.Remove(cacheEntryKey);
        }
        private static IDictionary<string, object> PropertyCollector(int[] args)
        {
            return new Dictionary<string, object>
            {
                {"CacheEntryKeys", args[0]},
                {"CacheCount", DistributedApplication.Cache.Count},
                {"portletClientCount", args[1]},
                {"PortletDependencyClientCount", -11111}
            };
        }
    }
}
