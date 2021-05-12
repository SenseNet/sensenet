using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage
{
    public class TreeLock : IDisposable
    {
        private static DataStore DataStore => Providers.Instance.DataStore; 

        private readonly SnTrace.Operation _logOp;
        private readonly int[] _lockIds;

        private TreeLock(SnTrace.Operation logOp, params int[] lockIds)
        {
            this._logOp = logOp;
            _lockIds = lockIds;
        }

        /// <summary>
        /// Locks one or more subtrees in the Content Repository. If a subtree is locked, no modifications (Save operations) can be made there.
        /// Use this method with a using statement to make sure that the lock is released when not needed anymore.
        /// </summary>
        /// <exception cref="SenseNet.ContentRepository.Storage.LockedTreeException">Thrown when any of the requested paths (or any of the parent containers) are already locked.</exception>
        /// <param name="paths">One or more Content Repository paths to be locked.</param>
        [Obsolete("Use the async version instead.")]
        public static TreeLock Acquire(params string[] paths)
        {
            SnTrace.ContentOperation.Write("TreeLock: Acquiring lock for {0}", paths);

            var lockIds = paths.Select(p =>  DataStore.AcquireTreeLockAsync(p, CancellationToken.None).GetAwaiter().GetResult())
                .ToArray();
            for (var i = 0; i < lockIds.Length; i++)
            {
                if (lockIds[i] == 0)
                {
                    DataStore.ReleaseTreeLockAsync(lockIds, CancellationToken.None).GetAwaiter().GetResult();
                    var msg = "Cannot acquire a tree lock for " + paths[i];
                    SnTrace.ContentOperation.Write("TreeLock: " + msg);
                    throw new LockedTreeException(msg);
                }
            }

            var logOp = SnTrace.ContentOperation.StartOperation("TreeLock: {0} for {1}", lockIds, paths);
            return new TreeLock(logOp, lockIds);
        }
        /// <summary>
        /// Locks one or more subtrees in the Content Repository. If a subtree is locked, no modifications (Save operations) can be made there.
        /// Use this method with a using statement to make sure that the lock is released when not needed anymore.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="paths">One or more Content Repository paths to be locked.</param>
        /// <exception cref="LockedTreeException">Thrown when any of the requested paths (or any of the parent containers) are already locked.</exception>
        /// <returns>A Task that represents the asynchronous operation and wraps the new tree lock
        /// object containing the lock ids.</returns>
        public static async Task<TreeLock> AcquireAsync(CancellationToken cancellationToken, params string[] paths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SnTrace.ContentOperation.Write("TreeLock: Acquiring lock for {0}", paths);

            var lockTasks = paths.Select(p => DataStore.AcquireTreeLockAsync(p, cancellationToken));
            var lockIds = await Task.WhenAll(lockTasks).ConfigureAwait(false);

            for (var i = 0; i < lockIds.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (lockIds[i] == 0)
                {
                    await DataStore.ReleaseTreeLockAsync(lockIds, cancellationToken).ConfigureAwait(false);
                    var msg = "Cannot acquire a tree lock for " + paths[i];
                    SnTrace.ContentOperation.Write("TreeLock: " + msg);
                    throw new LockedTreeException(msg);
                }
            }

            var logOp = SnTrace.ContentOperation.StartOperation("TreeLock: {0} for {1}", lockIds, paths);
            return new TreeLock(logOp, lockIds);
        }

        /// <summary>
        /// Checks whether a subtree is locked. Used by save operations to make sure that it is OK to make modifications.
        /// </summary>
        /// <exception cref="SenseNet.ContentRepository.Storage.LockedTreeException">Thrown when any of the requested paths (or any of the parent containers) are already locked.</exception>
        /// <param name="paths">One or more Content Repository paths to check for locked state.</param>
        [Obsolete("Use the async version instead.")]
        public static void AssertFree(params string[] paths)
        {
            SnTrace.ContentOperation.Write("TreeLock: Checking {0}", String.Join(", ", paths));

            foreach (var path in paths)
            {
                if (DataStore.IsTreeLockedAsync(path, CancellationToken.None).GetAwaiter().GetResult())
                {
                    var msg = "Cannot perform the operation because another process is making changes on this path: " + path;
                    SnTrace.ContentOperation.Write("TreeLock: Checking {0}", String.Join(", ", paths));
                    throw new LockedTreeException(msg);
                }
            }
        }
        /// <summary>
        /// Checks whether a subtree is locked. Used by save operations to make sure that it is OK to make modifications.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="paths">One or more Content Repository paths to check for locked state.</param>
        /// <exception cref="LockedTreeException">Thrown when any of the requested paths (or any of the parent containers) are already locked.</exception>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static async Task AssertFreeAsync(CancellationToken cancellationToken, params string[] paths)
        {
            SnTrace.ContentOperation.Write("TreeLock: Checking {0}", string.Join(", ", paths));

            foreach (var path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (await DataStore.IsTreeLockedAsync(path, cancellationToken).ConfigureAwait(false))
                {
                    var msg = "Cannot perform the operation because another process is making changes on this path: " + path;
                    SnTrace.ContentOperation.Write("TreeLock: Checking {0}", string.Join(", ", paths));
                    throw new LockedTreeException(msg);
                }
            }
        }

        public void Dispose()
        {
            //TODO: find a better design instead of calling an asynchronous method
            // synchronously inside a Dispose method.
            DataStore.ReleaseTreeLockAsync(_lockIds, CancellationToken.None).GetAwaiter().GetResult();
            if (_logOp != null)
            {
                _logOp.Successful = true;
                _logOp.Dispose();
            }
        }

        /// <summary>
        /// Gets all existing locks from the database.
        /// </summary>
        [Obsolete("Use the async version instead.")]
        public static Dictionary<int, string> GetAllLocks()
        {
            return DataStore.LoadAllTreeLocksAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Gets all existing locks in the system.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a lock id, path dictionary.</returns>
        public static Task<Dictionary<int, string>> GetAllLocksAsync(CancellationToken cancellationToken)
        {
            return DataStore.LoadAllTreeLocksAsync(cancellationToken);
        }
    }
}
