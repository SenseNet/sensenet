using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.TaskManagement.Core;

// ReSharper disable once CheckNamespace
namespace SenseNet.BackgroundOperations
{
    /// <summary>
    /// Registers an Active Directory synchronization task in the task management framework
    /// periodically if the feature is available.
    /// </summary>
    public class StartActiveDirectorySynchronizationTask : IMaintenanceTask
    {
        public int WaitingSeconds { get; } = 40;
        private static readonly string ADSyncSettingsName = "SyncAD2Portal";
        // ReSharper disable once InconsistentNaming
        private bool? _ADSynchIsAvailable;
        private readonly TaskManagementOptions _taskManagementOptions;
        private readonly ITaskManager _taskManager;

        public StartActiveDirectorySynchronizationTask(IOptions<TaskManagementOptions> taskManagementOptions, ITaskManager taskManager)
        {
            _taskManagementOptions = taskManagementOptions?.Value ?? new TaskManagementOptions();
            _taskManager = taskManager;
        }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!_ADSynchIsAvailable.HasValue)
            {
                _ADSynchIsAvailable = Settings.IsSettingsAvailable(ADSyncSettingsName);
                SnLog.WriteInformation("Active Directory synch feature is " + (_ADSynchIsAvailable.Value ? string.Empty : "not ") + "available.");
            }
            if (!_ADSynchIsAvailable.Value)
                return Task.CompletedTask;

            // skip check if the feature is not enabled
            if (!Settings.GetValue(ADSyncSettingsName, "Enabled", null, false))
                return Task.CompletedTask;

            if (!AdSyncTimeArrived())
                return Task.CompletedTask;

            var requestData = new RegisterTaskRequest
            {
                Type = "SyncAD2Portal",
                Title = "SyncAD2Portal",
                Priority = TaskPriority.Immediately,

                AppId = _taskManagementOptions.ApplicationIdOrSetting,
                TaskData = JsonConvert.SerializeObject(new { SiteUrl = _taskManagementOptions.ApplicationUrlOrSetting }),
                Tag = string.Empty,
                FinalizeUrl = "/odata.svc/('Root')/Ad2PortalSyncFinalizer"
            };

            // Fire and forget: we do not need the result of the register operation.
            // (we have to start a task here instead of calling RegisterTaskAsync 
            // directly because the asp.net sync context callback would fail)
            Task.Run(() => _taskManager.RegisterTaskAsync(requestData), cancellationToken);

            return Task.CompletedTask;
        }

        private static bool AdSyncTimeArrived()
        {
            var now = DateTime.UtcNow;
            var dayOffSetInMinutes = now.Hour * 60 + now.Minute;

            var frequency = Math.Max(0, Settings.GetValue(ADSyncSettingsName, "Scheduling.Frequency", null, 0));
            if (frequency > 0)
                return dayOffSetInMinutes % frequency < 1;

            var exactTimeVal = Settings.GetValue(ADSyncSettingsName, "Scheduling.ExactTime", null, string.Empty);

            if (TimeSpan.TryParse(exactTimeVal, out var exactTime) && exactTime > TimeSpan.MinValue)
                return dayOffSetInMinutes == Convert.ToInt64(Math.Truncate(exactTime.TotalMinutes));

            return false;
        }
    }
}
