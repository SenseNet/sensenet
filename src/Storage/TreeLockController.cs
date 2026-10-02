using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    public interface ITreeLockController
    {
        /// <summary>
        /// Locks one or more subtrees in the Content Repository. If a subtree is locked, no modifications (Save operations) can be made there.
        /// Use this method with a using statement to make sure that the lock is released when not needed anymore.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="paths">One or more Content Repository paths to be locked.</param>
        /// <exception cref="LockedTreeException">Thrown when any of the requested paths (or any of the parent containers) are already locked.</exception>
        /// <returns>A Task that represents the asynchronous operation and wraps the new tree lock
        /// object containing the lock ids.</returns>
        Task<TreeLock> AcquireAsync(CancellationToken cancellationToken, params string[] paths);

        /// <summary>
        /// Checks whether a subtree is locked. Used by save operations to make sure that it is OK to make modifications.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="paths">One or more Content Repository paths to check for locked state.</param>
        /// <exception cref="LockedTreeException">Thrown when any of the requested paths (or any of the parent containers) are already locked.</exception>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task AssertFreeAsync(CancellationToken cancellationToken, params string[] paths);

        /// <summary>
        /// Gets all existing locks in the system.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a lock id, path dictionary.</returns>
        Task<Dictionary<int, string>> GetAllLocksAsync(CancellationToken cancellationToken);
    }

    public class TreeLockController : ITreeLockController
    {
        private readonly IDataStore _dataStore;
        private readonly ILogger _logger;

        public TreeLockController(IDataStore dataStore, ILogger<TreeLock> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task<TreeLock> AcquireAsync(CancellationToken cancellationToken, params string[] paths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SnTrace.ContentOperation.Write("TreeLock: Acquiring lock for {0}", string.Join(", ", paths));

            var lockIds = new int[0];
            Task<int>[] lockTasks = null;

            try
            {
                lockTasks = paths.Select(p => _dataStore.AcquireTreeLockAsync(p, CancellationToken.None)).ToArray();
                lockIds = await Task.WhenAll(lockTasks).ConfigureAwait(false);

                for (var i = 0; i < lockIds.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (lockIds[i] == 0)
                    {
                        var msg = "Cannot acquire a tree lock for " + paths[i];
                        await WriteBlockingLocksAsync(paths[i]).ConfigureAwait(false);
                        SnTrace.ContentOperation.Write("TreeLock: " + msg);
                        throw new LockedTreeException(msg);
                    }
                }
            }
            catch
            {
                if ((lockIds == null || lockIds.Length == 0) && lockTasks != null)
                    lockIds = lockTasks
                        .Where(task => task.IsCompletedSuccessfully)
                        .Select(task => task.Result)
                        .ToArray();

                try
                {
                    await ReleaseLocksAsync(lockIds).ConfigureAwait(false);
                }
                catch (System.Exception e)
                {
                    _logger.LogWarning(e, "TreeLock cleanup failed after an unsuccessful acquire.");
                }
                throw;
            }

            var logOp = SnTrace.ContentOperation.StartOperation("TreeLock: {0} for {1}", lockIds, paths);
            return new TreeLock(logOp, _dataStore, lockIds);
        }

        private async Task ReleaseLocksAsync(IEnumerable<int> lockIds)
        {
            var existingLockIds = lockIds?.Where(id => id != 0).ToArray();
            if (existingLockIds == null || existingLockIds.Length == 0)
                return;

            await _dataStore.ReleaseTreeLockAsync(existingLockIds, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task WriteBlockingLocksAsync(string path)
        {
            try
            {
                var locks = await _dataStore.LoadAllTreeLocksAsync(CancellationToken.None).ConfigureAwait(false);
                var blockingLocks = locks
                    .Where(x => IsBlocking(path, x.Value))
                    .Select(x => $"{x.Key}:{x.Value}")
                    .ToArray();

                SnTrace.ContentOperation.Write("TreeLock: Blocking locks for {0}: {1}",
                    path, blockingLocks.Length == 0 ? "[none]" : string.Join(", ", blockingLocks));
            }
            catch (System.Exception e)
            {
                _logger.LogWarning(e, "Could not load blocking tree locks for {Path}.", path);
            }
        }

        private static bool IsBlocking(string requestedPath, string lockedPath)
        {
            return IsSameOrAncestor(requestedPath, lockedPath) || IsSameOrAncestor(lockedPath, requestedPath);
        }

        private static bool IsSameOrAncestor(string path, string ancestorPath)
        {
            return path.Equals(ancestorPath, System.StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith(ancestorPath + "/", System.StringComparison.OrdinalIgnoreCase);
        }

        public async Task AssertFreeAsync(CancellationToken cancellationToken, params string[] paths)
        {
            SnTrace.ContentOperation.Write("TreeLock: Checking {0}", string.Join(", ", paths));

            foreach (var path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (await _dataStore.IsTreeLockedAsync(path, cancellationToken).ConfigureAwait(false))
                {
                    var msg = "Cannot perform the operation because another process is making changes on this path: " + path;
                    SnTrace.ContentOperation.Write("TreeLock: Checking {0}", string.Join(", ", paths));
                    throw new LockedTreeException(msg);
                }
            }
        }
        public Task<Dictionary<int, string>> GetAllLocksAsync(CancellationToken cancellationToken)
        {
            return _dataStore.LoadAllTreeLocksAsync(cancellationToken);
        }
    }
}
