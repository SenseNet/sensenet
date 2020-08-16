using System;
using System.Threading;
using Newtonsoft.Json;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;
using SenseNet.TaskManagement.Core;
using Task = System.Threading.Tasks.Task;

// ReSharper disable once CheckNamespace
namespace SenseNet.BackgroundOperations.Legacy
{
    /// <summary>
    /// This internal class was created to have a backward compatible solution for starting AD synchronization.
    /// The new class with the same name does not have a default constructor, therefore cannot be instantiated
    /// automatically.
    /// </summary>
    internal class StartActiveDirectorySynchronizationTask : IMaintenanceTask
    {
        public int WaitingSeconds { get; } = 40;
        private static readonly string ADSyncSettingsName = "SyncAD2Portal";
        // ReSharper disable once InconsistentNaming
        private bool? _ADSynchIsAvailable;
        private readonly TaskManagementOptions _taskManagementOptions;
        private readonly ITaskManager _taskManager;

        public StartActiveDirectorySynchronizationTask()
        {
            _taskManagementOptions = new TaskManagementOptions();
#pragma warning disable 618
            _taskManager = SnTaskManager.Instance;
#pragma warning restore 618
        }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!_ADSynchIsAvailable.HasValue)
            {
                _ADSynchIsAvailable = Settings.GetSettingsByName<Settings>(ADSyncSettingsName, null) != null;
                SnLog.WriteInformation("Active Directory synch feature is " + (_ADSynchIsAvailable.Value ? string.Empty : "not ") + "available.");
            }
            if (!_ADSynchIsAvailable.Value)
                return Task.CompletedTask;

            // skip check if the feature is not enabled
            if (!Settings.GetValue(ADSyncSettingsName, "Enabled", null, false))
                return Task.CompletedTask;

            if (!AdSyncTimeArrived())
                return Task.CompletedTask;

#pragma warning disable 618
            // fallback for legacy code
            var appId = _taskManagementOptions.ApplicationId;
            if (string.IsNullOrEmpty(appId))
                appId = SnTaskManager.Settings.AppId;
            var appUrl = _taskManagementOptions.ApplicationUrl;
            if (string.IsNullOrEmpty(appUrl))
                appUrl = SnTaskManager.Settings.AppUrl;
#pragma warning restore 618

            var requestData = new RegisterTaskRequest
            {
                Type = "SyncAD2Portal",
                Title = "SyncAD2Portal",
                Priority = TaskPriority.Immediately,

                AppId = appId,
                TaskData = JsonConvert.SerializeObject(new { SiteUrl = appUrl }),
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