using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage
{
    internal class NodeDataParticipant : ITransactionParticipant
    {
        public NodeData Data { get; set; }
        public NodeSaveSettings Settings { get; set; }
        public bool IsNewNode { get; set; }

        public void Commit()
        {
            DataBackingStore.RemoveFromCache(this);
        }
        public void Rollback()
        {
            DataBackingStore.OnNodeDataRollback(this);
        }
    }

    internal class InsertCacheParticipant : ITransactionParticipant
    {
        public string CacheKey { get; set; }

        public void Commit()
        {
            // do nothing
        }
        public void Rollback()
        {
            DistributedApplication.Cache.Remove(CacheKey);
        }
    }
}
