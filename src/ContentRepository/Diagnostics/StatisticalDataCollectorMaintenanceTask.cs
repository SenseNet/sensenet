using System.Threading;
using Microsoft.Extensions.Options;
using SenseNet.BackgroundOperations;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Diagnostics
{
    public class StatisticalDataCollectorMaintenanceTask : IMaintenanceTask
    {
        private readonly IStatisticalDataCollector _collector;
        private readonly IDatabaseUsageHandler _dbUsageHandler;
        private readonly StatisticsOptions _statisticsOptions;

        public int WaitingSeconds { get; } = 3600; // 60 minutes

        public StatisticalDataCollectorMaintenanceTask(IStatisticalDataCollector collector, IDatabaseUsageHandler dbUsageHandler,
            IOptions<StatisticsOptions> statisticsOptions)
        {
            _collector = collector;
            _dbUsageHandler = dbUsageHandler;
            _statisticsOptions = statisticsOptions.Value;
        }

        public async System.Threading.Tasks.Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!_statisticsOptions.Enabled)
                return;

            var data = await _dbUsageHandler.GetDatabaseUsageAsync(false, CancellationToken.None)
                .ConfigureAwait(false);
            await _collector.RegisterGeneralData("DatabaseUsage", TimeResolution.Hour, data, cancellationToken);
        }
    }
}
