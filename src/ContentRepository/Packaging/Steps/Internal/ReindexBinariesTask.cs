using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Packaging.Steps.Internal;

namespace SenseNet.ContentRepository.Packaging.Steps.Internal
{
    internal class ReindexBinariesTask : IMaintenanceTask
    {
        private bool? _enabled;

        public double WaitingMinutes { get; private set; } = 0.15;

        public void Execute()
        {
            if (_enabled == null)
            {
                _enabled = ReindexBinaries.IsFeatureActive();
                if (!_enabled.Value)
                    WaitingMinutes = 10000.0;
            }
            if (!_enabled.Value)
                return;

            var finished = ReindexBinaries.GetBackgroundTasksAndExecute();
            if (finished)
            {
                ReindexBinaries.Tracer.Write("All binaries are reindexed.");
                ReindexBinaries.InactivateFeature();
                ReindexBinaries.Tracer.Write("ReindexBinaries feature is destroyed.");
            }
        }
    }
}
