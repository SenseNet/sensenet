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

            var lockTasks = paths.Select(p => _dataStore.AcquireTreeLockAsync(p, cancellationToken));
            var lockIds = await Task.WhenAll(lockTasks).ConfigureAwait(false);

            for (var i = 0; i < lockIds.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (lockIds[i] == 0)
                {
                    await _dataStore.ReleaseTreeLockAsync(lockIds, cancellationToken).ConfigureAwait(false);
                    var msg = "Cannot acquire a tree lock for " + paths[i];
                    SnTrace.ContentOperation.Write("TreeLock: " + msg);
                    throw new LockedTreeException(msg);
                }
            }

            var logOp = SnTrace.ContentOperation.StartOperation("TreeLock: {0} for {1}", lockIds, paths);
            return new TreeLock(logOp, lockIds);
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
