using System.Threading;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
// ReSharper disable CheckNamespace

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
        private static ISharedLockDataProviderExtension Storage => DataStore.DataProvider.GetExtension<ISharedLockDataProviderExtension>();

        /// <summary>
        /// Deletes all shared locks from the system. Not intended for external callers.
        /// </summary>
        public static void RemoveAllLocks(CancellationToken cancellationToken)
        {
            Storage.DeleteAllSharedLocksAsync(cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sets a shared lock for a content. If the lock already exists, the shared lock data provider
        /// may refresh the creation date.
        /// </summary>
        /// <exception cref="LockedNodeException"></exception>
        /// <exception cref="ContentNotFoundException"></exception>
        public static void Lock(int contentId, string @lock, CancellationToken cancellationToken)
        {
            var node = Node.LoadNode(contentId);
            if(node == null)
                throw new ContentNotFoundException(contentId.ToString());
            if(node.Locked)
                throw new LockedNodeException(node.Lock);

            Storage.CreateSharedLockAsync(contentId, @lock, cancellationToken).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Updates an existing shared lock. If the lock already exists, the shared lock data provider
        /// may refresh the creation date.
        /// </summary>
        /// <returns>The same lock value if exists.</returns>
        /// <exception cref="SharedLockNotFoundException"></exception>
        /// <exception cref="LockedNodeException"></exception>
        public static string RefreshLock(int contentId, string @lock, CancellationToken cancellationToken)
        {
            return Storage.RefreshSharedLockAsync(contentId, @lock, cancellationToken).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Replaces an existing shared lock value with a new one.
        /// </summary>
        /// <returns>The original lock value if exists.</returns>
        /// <exception cref="SharedLockNotFoundException"></exception>
        /// <exception cref="LockedNodeException"></exception>
        public static string ModifyLock(int contentId, string @lock, string newLock, CancellationToken cancellationToken)
        {
            return Storage.ModifySharedLockAsync(contentId, @lock, newLock, cancellationToken).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Loads a shared lock value for the specified content id.
        /// </summary>
        /// <returns>The shared lock value if exists or null.</returns>
        public static string GetLock(int contentId, CancellationToken cancellationToken)
        {
            return Storage.GetSharedLockAsync(contentId, cancellationToken).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Deletes a shared lock from a content if exists. Otherwise an exception is thrown.
        /// </summary>
        /// <returns>The original lock value if exists.</returns>
        /// <exception cref="SharedLockNotFoundException"></exception>
        /// <exception cref="LockedNodeException"></exception>
        public static string Unlock(int contentId, string @lock, CancellationToken cancellationToken)
        {
            return Storage.DeleteSharedLockAsync(contentId, @lock, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes expired shared locks. Called by the maintenance task.
        /// </summary>
        public static void Cleanup(CancellationToken cancellationToken)
        {
            SnTrace.Database.Write("Cleanup shared locks.");
            Storage.CleanupSharedLocksAsync(cancellationToken).GetAwaiter().GetResult();
        }
    }
}
