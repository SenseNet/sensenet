﻿using System.Threading;
using SenseNet.BackgroundOperations;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Diagnostics
{
    public class StatisticalDataCollectorMaintenanceTask : IMaintenanceTask
    {
        private readonly IStatisticalDataCollector _collector;
        private readonly IDatabaseUsageHandler _dbUsageHandler;

        public int WaitingSeconds { get; } = 3600; // 60 minutes

        public StatisticalDataCollectorMaintenanceTask(IStatisticalDataCollector collector, IDatabaseUsageHandler dbUsageHandler)
        {
            _collector = collector;
            _dbUsageHandler = dbUsageHandler;
        }

        public async System.Threading.Tasks.Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var data = await _dbUsageHandler.GetDatabaseUsageAsync(false, CancellationToken.None)
                .ConfigureAwait(false);
            await _collector.RegisterGeneralData("DatabaseUsage", TimeResolution.Hour, data, cancellationToken);
        }
    }
}
