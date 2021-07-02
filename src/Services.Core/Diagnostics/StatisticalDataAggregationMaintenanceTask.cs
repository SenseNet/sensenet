using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Options;
using SenseNet.BackgroundOperations;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics
{
    public class StatisticalDataAggregationMaintenanceTask : IMaintenanceTask
    {
        private readonly IStatisticalDataAggregationController _aggregationController;

        public StatisticalDataAggregationMaintenanceTask(IStatisticalDataAggregationController aggregationController)
        {
            _aggregationController = aggregationController;
        }

        public int WaitingSeconds => 60;

        public System.Threading.Tasks.Task ExecuteAsync(CancellationToken cancel)
        {
            return ExecuteAsync(DateTime.UtcNow, cancel);
        }
        private async System.Threading.Tasks.Task ExecuteAsync(DateTime now, CancellationToken cancel)
        {
            var aggregationTime = now.Truncate(TimeResolution.Minute).AddSeconds(-1);
            await _aggregationController.AggregateAsync(aggregationTime, TimeResolution.Minute, cancel);
            if (now.Minute == 0)
            {
                await _aggregationController.AggregateAsync(aggregationTime, TimeResolution.Hour, cancel);
                if (now.Hour == 0)
                {
                    await _aggregationController.AggregateAsync(aggregationTime, TimeResolution.Day, cancel);
                    if (now.Day == 1)
                    {
                        await _aggregationController.AggregateAsync(aggregationTime, TimeResolution.Month, cancel);
                    }
                }
            }
        }
    }
}
