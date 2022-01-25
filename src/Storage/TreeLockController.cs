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
        Task<TreeLock> AcquireAsync(CancellationToken cancellationToken, params string[] paths);
        Task AssertFreeAsync(CancellationToken cancellationToken, params string[] paths);
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
