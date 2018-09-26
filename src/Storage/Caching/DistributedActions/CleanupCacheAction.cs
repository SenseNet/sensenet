using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.Communication.Messaging;
using System.Globalization;

namespace SenseNet.ContentRepository.Storage.Caching.DistributedActions
{
    [Serializable]
    public class CacheCleanAction : DistributedAction
    {

        public override void DoAction(bool onRemote, bool isFromMe)
        {
            // only run on 
            if (onRemote && isFromMe) return;

            List<string> cacheEntryKeys = new List<string>();

            int localCacheCount = DistributedApplication.Cache.Count;

            foreach (var entry in DistributedApplication.Cache)
                cacheEntryKeys.Add(entry.Key);

            foreach (string cacheEntryKey in cacheEntryKeys)
                DistributedApplication.Cache.Remove(cacheEntryKey);
        }
    }
}