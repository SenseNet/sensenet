using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SenseNet.BackgroundOperations;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.TaskManagement.Core;

namespace SenseNet.Storage.BackgroundOperations
{
    public class RefreshTaskManagementTask : IMaintenanceTask
    {
        private readonly ITaskManager _taskManager;
        private readonly TaskManagementOptions _options;
        private readonly ILogger<RefreshTaskManagementTask> _logger;

        public int WaitingSeconds { get; }

        public RefreshTaskManagementTask(ITaskManager taskManager, 
            IOptions<TaskManagementOptions> options, ILogger<RefreshTaskManagementTask> logger)
        {
            _taskManager = taskManager;
            _options = options.Value;
            _logger = logger;

            WaitingSeconds = (_options.ApiKeyExpirationHours * 60 * 60) / 2;
        }
        
        public async Task ExecuteAsync(CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(_options.Url))
                return;

            _logger.LogTrace("Refreshing task management app registration.");

            await SystemAccount.ExecuteAsync(() => _taskManager.RegisterApplicationAsync(cancel)).ConfigureAwait(false);
        }
    }
}
