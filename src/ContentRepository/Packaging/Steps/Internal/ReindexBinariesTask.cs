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

        public double WaitingMinutes { get; private set; } = 1.0;

        public void Execute()
        {
            if (_enabled == null)
            {
                _enabled = ReindexBinaries.IsFeatureActive();
                if (!_enabled.Value)
                    WaitingMinutes = 10000.0;
            }

            using (var op = SnTrace.Index.StartOperation("GetBackgroundTasksAndExecute"))
            {
                var finished = ReindexBinaries.GetBackgroundTasksAndExecute();
                if (finished)
                {
                    SnTrace.Index.Write("All binaries are reindexed.");
                    ReindexBinaries.InactivateFeature();
                    SnTrace.Index.Write("ReindexBinaries feature destroyed.");
                }
                op.Successful = true;
            }
        }
    }
}
