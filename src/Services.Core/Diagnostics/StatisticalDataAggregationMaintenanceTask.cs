﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SenseNet.BackgroundOperations;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics
{
    public class StatisticalDataAggregationMaintenanceTask : IMaintenanceTask
    {
        private readonly IStatisticalDataAggregationController _aggregationController;
        private readonly StatisticsOptions _statisticsOptions;

        public StatisticalDataAggregationMaintenanceTask(IStatisticalDataAggregationController aggregationController,
            IOptions<StatisticsOptions> statisticsOptions)
        {
            _aggregationController = aggregationController;
            _statisticsOptions = statisticsOptions.Value;
        }

        public int WaitingSeconds => 60;
        private bool _running;
        private readonly object _runningSync = new();

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            // prevent overlapping executions
            if (_running || !_statisticsOptions.Enabled)
                return;

            // set the running flag exclusively
            lock (_runningSync)
            {
                if (_running)
                    return;

                _running = true;
            }

            try
            {
                await ExecuteAsync(DateTime.UtcNow, cancel).ConfigureAwait(false);
            }
            finally
            {
                _running = false;
            }
        }
        private async Task ExecuteAsync(DateTime now, CancellationToken cancel)
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
