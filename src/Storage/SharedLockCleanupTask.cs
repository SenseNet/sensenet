using System.Threading;
using System.Threading.Tasks;
using SenseNet.BackgroundOperations;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Storage
{
    public class SharedLockCleanupTask : IMaintenanceTask
    {
        public int WaitingSeconds => 4200; // 1:10:00

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            SharedLock.Cleanup(CancellationToken.None);
            return Task.CompletedTask;
        }
    }
}
