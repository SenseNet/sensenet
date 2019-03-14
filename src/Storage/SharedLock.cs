using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage
{
    public class SharedLock
    {
        private static ISharedLockDataProviderExtension Storage => DataProvider.GetExtension<ISharedLockDataProviderExtension>();

        public static void RemoveAllLocks()
        {
            Storage.DeleteAllSharedLocks();
        }

        public static void Lock(int contentId, string @lock)
        {
            var node = Node.LoadNode(contentId);
            if(node == null)
                throw new ContentNotFoundException(contentId.ToString());
            if(node.Locked)
                throw new LockedNodeException(node.Lock);

            Storage.CreateSharedLock(contentId, @lock);
        }
        public static string RefreshLock(int contentId, string @lock)
        {
            return Storage.RefreshSharedLock(contentId, @lock);
        }
        public static string ModifyLock(int contentId, string @lock, string newLock)
        {
            return Storage.ModifySharedLock(contentId, @lock, newLock);
        }
        public static string GetLock(int contentId)
        {
            return Storage.GetSharedLock(contentId);
        }
        public static string Unlock(int contentId, string @lock)
        {
            return Storage.DeleteSharedLock(contentId, @lock);
        }

        public static void Cleanup()
        {
            SnTrace.Database.Write("Cleanup shared locks.");
            Storage.CleanupSharedLocks();
        }
    }
}
