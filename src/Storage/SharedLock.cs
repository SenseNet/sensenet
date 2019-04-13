using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// API for locking content items. Only a single shared lock can be acquired for a content.
    /// A shared lock is not user-specific so it can be shared among multiple users (for example
    /// when editing a document on the web). Features may use the SharedLock api for validating 
    /// an existing lock or setting a new one.
    /// </summary>
    public static class SharedLock
    {
        private static ISharedLockDataProviderExtension Storage
        {
            get
            {
                if (DataStore.Enabled)
                    return DataStore.GetDataProviderExtension<ISharedLockDataProviderExtension>();
                else
                    return DataProvider.GetExtension<ISharedLockDataProviderExtension>();
            }
        }

        /// <summary>
        /// Deletes all shared locks from the system. Not intended for external callers.
        /// </summary>
        public static void RemoveAllLocks()
        {
            Storage.DeleteAllSharedLocks();
        }

        /// <summary>
        /// Sets a shared lock for a content. If the lock already exists, the shared lock data provider
        /// may refresh the creation date.
        /// </summary>
        /// <exception cref="LockedNodeException"></exception>
        /// <exception cref="ContentNotFoundException"></exception>
        public static void Lock(int contentId, string @lock)
        {
            var node = Node.LoadNode(contentId);
            if(node == null)
                throw new ContentNotFoundException(contentId.ToString());
            if(node.Locked)
                throw new LockedNodeException(node.Lock);

            Storage.CreateSharedLock(contentId, @lock);
        }
        /// <summary>
        /// Updates an existing shared lock. If the lock already exists, the shared lock data provider
        /// may refresh the creation date.
        /// </summary>
        /// <returns>The same lock value if exists.</returns>
        /// <exception cref="SharedLockNotFoundException"></exception>
        /// <exception cref="LockedNodeException"></exception>
        public static string RefreshLock(int contentId, string @lock)
        {
            return Storage.RefreshSharedLock(contentId, @lock);
        }
        /// <summary>
        /// Replaces an existing shared lock value with a new one.
        /// </summary>
        /// <returns>The original lock value if exists.</returns>
        /// <exception cref="SharedLockNotFoundException"></exception>
        /// <exception cref="LockedNodeException"></exception>
        public static string ModifyLock(int contentId, string @lock, string newLock)
        {
            return Storage.ModifySharedLock(contentId, @lock, newLock);
        }
        /// <summary>
        /// Loads a shared lock value for the specified content id.
        /// </summary>
        /// <returns>The shared lock value if exists or null.</returns>
        public static string GetLock(int contentId)
        {
            return Storage.GetSharedLock(contentId);
        }
        /// <summary>
        /// Deletes a shared lock from a content if exists. Otherwise an exception is thrown.
        /// </summary>
        /// <returns>The original lock value if exists.</returns>
        /// <exception cref="SharedLockNotFoundException"></exception>
        /// <exception cref="LockedNodeException"></exception>
        public static string Unlock(int contentId, string @lock)
        {
            return Storage.DeleteSharedLock(contentId, @lock);
        }

        /// <summary>
        /// Deletes expired shared locks. Called by the maintenance task.
        /// </summary>
        public static void Cleanup()
        {
            SnTrace.Database.Write("Cleanup shared locks.");
            Storage.CleanupSharedLocks();
        }
    }
}
