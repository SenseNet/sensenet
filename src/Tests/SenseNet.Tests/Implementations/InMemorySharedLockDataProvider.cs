using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Tests.Implementations
{
    public class InMemorySharedLockDataProvider : ISharedLockDataProviderExtension
    {
        public class SharedLockRow
        {
            public int SharedLockId;
            public int ContentId;
            public string Lock;
            public DateTime CreationDate;

            public SharedLockRow Clone()
            {
                return new SharedLockRow
                {
                    SharedLockId = SharedLockId,
                    ContentId = ContentId,
                    Lock = Lock,
                    CreationDate = CreationDate
                };
            }
        }

        public DataProvider MainProvider { get; set; }
        public List<SharedLockRow> SharedLocks { get; set; } = new List<SharedLockRow>();

        public TimeSpan SharedLockTimeout { get; } = TimeSpan.FromMinutes(30d);

        public void DeleteAllSharedLocks()
        {
            SharedLocks.Clear();
        }

        public void CreateSharedLock(int contentId, string @lock)
        {
            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);
            var row = SharedLocks.FirstOrDefault(x => x.ContentId == contentId);
            if (row != null && row.CreationDate < timeLimit)
            {
                SharedLocks.Remove(row);
                row = null;
            }

            if (row == null)
            {
                var newSharedLockId = SharedLocks.Count == 0 ? 1 : SharedLocks.Max(t => t.SharedLockId) + 1;
                SharedLocks.Add(new SharedLockRow
                {
                    SharedLockId = newSharedLockId,
                    ContentId = contentId,
                    Lock = @lock,
                    CreationDate = DateTime.UtcNow
                });
                return;
            }

            if (row.Lock != @lock)
                throw new LockedNodeException(null, $"The node (#{contentId}) is locked by another shared lock.");

            row.CreationDate = DateTime.UtcNow;
        }

        public string RefreshSharedLock(int contentId, string @lock)
        {
            DeleteTimedOutItems();

            var row = SharedLocks.FirstOrDefault(x => x.ContentId == contentId);
            if (row == null)
                throw new SharedLockNotFoundException("Content is unlocked");
            if (row.Lock != @lock)
                throw new LockedNodeException(null, $"The node (#{contentId}) is locked by another shared lock.");
            row.CreationDate = DateTime.UtcNow;
            return row.Lock;
        }

        public string ModifySharedLock(int contentId, string @lock, string newLock)
        {
            DeleteTimedOutItems();

            var existingItem = SharedLocks.FirstOrDefault(x => x.ContentId == contentId && x.Lock == @lock);
            if (existingItem != null)
            {
                existingItem.Lock = newLock;
                return null;
            }
            var existingLock = SharedLocks.FirstOrDefault(x => x.ContentId == contentId)?.Lock;
            if (existingLock == null)
                throw new SharedLockNotFoundException("Content is unlocked");
            if (existingLock != @lock)
                throw new LockedNodeException(null, $"The node (#{contentId}) is locked by another shared lock.");
            return existingLock;
        }

        public string GetSharedLock(int contentId)
        {
            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);
            return SharedLocks.FirstOrDefault(x => x.ContentId == contentId && x.CreationDate >= timeLimit)?.Lock;
        }

        public string DeleteSharedLock(int contentId, string @lock)
        {
            DeleteTimedOutItems();

            var existingItem = SharedLocks.FirstOrDefault(x => x.ContentId == contentId && x.Lock == @lock);
            if (existingItem != null)
            {
                SharedLocks.Remove(existingItem);
                return null;
            }
            var existingLock = SharedLocks.FirstOrDefault(x => x.ContentId == contentId)?.Lock;
            if (existingLock == null)
                throw new SharedLockNotFoundException("Content is unlocked");
            if (existingLock != @lock)
                throw new LockedNodeException(null, $"The node (#{contentId}) is locked by another shared lock.");
            return existingLock;
        }


        private void DeleteTimedOutItems()
        {
            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);
            var timedOutItems = SharedLocks.Where(x => x.CreationDate < timeLimit).ToArray();
            foreach (var item in timedOutItems)
                SharedLocks.Remove(item);
        }

    }
}
