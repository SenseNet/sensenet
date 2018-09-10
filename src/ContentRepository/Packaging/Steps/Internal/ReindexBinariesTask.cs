using System;
using SenseNet.ContentRepository.Storage;
using SenseNet.Packaging.Steps.Internal;

namespace SenseNet.ContentRepository.Packaging.Steps.Internal
{
    internal class ReindexBinariesTask : IMaintenanceTask
    {
        private static bool? _enabled;
        private static double _waitingMinutes = 0.15;
        private static DateTime _timeLimit;

        public double WaitingMinutes => _waitingMinutes;

        public void Execute()
        {
            if (_enabled == null)
            {
                _enabled = ReindexBinaries.IsFeatureActive();
                if (!_enabled.Value)
                    _waitingMinutes = 10000.0;
                _timeLimit = ReindexBinaries.GetTimeLimit();
            }
            if (!_enabled.Value)
                return;

            var finished = ReindexBinaries.GetBackgroundTasksAndExecute(_timeLimit);
            if (finished)
            {
                ReindexBinaries.Tracer.Write("All binaries are reindexed.");
                ReindexBinaries.InactivateFeature();
                ReindexBinaries.Tracer.Write("ReindexBinaries feature is destroyed.");
            }
        }
    }
}
