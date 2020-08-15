using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Storage.Security
{
    internal class AccessTokenCleanupTask : IMaintenanceTask
    {
        public int WaitingSeconds => 4500; // 1:15:00

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return AccessTokenVault.CleanupAsync(cancellationToken);
        }
    }
}
