using System.Threading;
using System.Threading.Tasks;
using SenseNet.BackgroundOperations;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Storage.Security
{
    public class AccessTokenCleanupTask : IMaintenanceTask
    {
        public int WaitingSeconds => 4500; // 1:15:00

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return AccessTokenVault.CleanupAsync(cancellationToken);
        }
    }
}
