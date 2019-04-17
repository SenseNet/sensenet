using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Tests.Implementations2 //UNDONE:DB -------CLEANUP: move to SenseNet.Tests.Implementations
{
    public class InMemorySharedLockDataProvider2 : ISharedLockDataProviderExtension
    {
        public DataCollection<SharedLockDoc> GetSharedLocks()
        {
            return ((InMemoryDataProvider2)DataStore.DataProvider).DB.GetCollection<SharedLockDoc>();
        }


        public TimeSpan SharedLockTimeout { get; } = TimeSpan.FromMinutes(30d);

        public void DeleteAllSharedLocks()
        {
            GetSharedLocks().Clear();
        }

        public void CreateSharedLock(int contentId, string @lock)
        {
            var sharedLocks = GetSharedLocks();
            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);
            var row = sharedLocks.FirstOrDefault(x => x.ContentId == contentId);
            if (row != null && row.CreationDate < timeLimit)
            {
                sharedLocks.Remove(row);
                row = null;
            }

            if (row == null)
            {
                var newSharedLockId = sharedLocks.Count == 0 ? 1 : sharedLocks.Max(t => t.SharedLockId) + 1;
                sharedLocks.Add(new SharedLockDoc
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

            var row = GetSharedLocks().FirstOrDefault(x => x.ContentId == contentId);
            if (row == null)
                throw new SharedLockNotFoundException("Content is unlocked");
            if (row.Lock != @lock)
                throw new LockedNodeException(null, $"The node (#{contentId}) is locked by another shared lock.");
            row.CreationDate = DateTime.UtcNow;
            return row.Lock;
        }

        public string ModifySharedLock(int contentId, string @lock, string newLock)
        {
            var sharedLocks = GetSharedLocks();

            DeleteTimedOutItems();

            var existingItem = sharedLocks.FirstOrDefault(x => x.ContentId == contentId && x.Lock == @lock);
            if (existingItem != null)
            {
                existingItem.Lock = newLock;
                return null;
            }
            var existingLock = sharedLocks.FirstOrDefault(x => x.ContentId == contentId)?.Lock;
            if (existingLock == null)
                throw new SharedLockNotFoundException("Content is unlocked");
            if (existingLock != @lock)
                throw new LockedNodeException(null, $"The node (#{contentId}) is locked by another shared lock.");
            return existingLock;
        }

        public string GetSharedLock(int contentId)
        {
            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);
            return GetSharedLocks().FirstOrDefault(x => x.ContentId == contentId && x.CreationDate >= timeLimit)?.Lock;
        }

        public string DeleteSharedLock(int contentId, string @lock)
        {
            var sharedLocks = GetSharedLocks();

            DeleteTimedOutItems();

            var existingItem = sharedLocks.FirstOrDefault(x => x.ContentId == contentId && x.Lock == @lock);
            if (existingItem != null)
            {
                sharedLocks.Remove(existingItem);
                return null;
            }
            var existingLock = sharedLocks.FirstOrDefault(x => x.ContentId == contentId)?.Lock;
            if (existingLock == null)
                throw new SharedLockNotFoundException("Content is unlocked");
            if (existingLock != @lock)
                throw new LockedNodeException(null, $"The node (#{contentId}) is locked by another shared lock.");
            return existingLock;
        }

        public void CleanupSharedLocks()
        {
            // do nothing
        }

        public void SetSharedLockCreationDate(int nodeId, DateTime value)
        {
            var sharedLockRow = GetSharedLocks().First(x => x.ContentId == nodeId);
            sharedLockRow.CreationDate = value;
        }

        public DateTime GetSharedLockCreationDate(int nodeId)
        {
            var sharedLockRow = GetSharedLocks().First(x => x.ContentId == nodeId);
            return sharedLockRow.CreationDate;
        }


        private void DeleteTimedOutItems()
        {
            var sharedLocks = GetSharedLocks();

            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);
            var timedOutItems = sharedLocks.Where(x => x.CreationDate < timeLimit).ToArray();
            foreach (var item in timedOutItems)
                sharedLocks.Remove(item);
        }
    }
}
