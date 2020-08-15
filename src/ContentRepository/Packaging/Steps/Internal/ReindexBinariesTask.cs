using System;
using System.Threading;
using SenseNet.BackgroundOperations;
using SenseNet.Packaging.Steps.Internal;

namespace SenseNet.ContentRepository.Packaging.Steps.Internal
{
    internal class ReindexBinariesTask : IMaintenanceTask
    {
        private bool? _enabled;
        private DateTime _timeLimit;

        public int WaitingSeconds { get; set; } = 9;

        public System.Threading.Tasks.Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_enabled == null)
            {
                _enabled = ReindexBinaries.IsFeatureActive();
                if (!_enabled.Value)
                    WaitingSeconds = 600000;
                else
                    _timeLimit = ReindexBinaries.GetTimeLimit();
            }
            if (!_enabled.Value)
                return System.Threading.Tasks.Task.CompletedTask;

            var finished = ReindexBinaries.GetBackgroundTasksAndExecute(_timeLimit);
            if (finished)
            {
                ReindexBinaries.Tracer.Write("All binaries are reindexed.");
                ReindexBinaries.InactivateFeature();
                ReindexBinaries.Tracer.Write("ReindexBinaries feature is destroyed.");
            }

            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
