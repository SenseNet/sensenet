using System.Threading;
using SenseNet.BackgroundOperations;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository
{
    public class StatisticalDataCollectorMaintenanceTask : IMaintenanceTask
    {
        private readonly IStatisticalDataCollector _collector;
        private readonly IDatabaseUsageHandler _dbUsageHandler;

        public int WaitingSeconds { get; } = 3660; // 61 minutes

        public StatisticalDataCollectorMaintenanceTask(IStatisticalDataCollector collector, IDatabaseUsageHandler dbUsageHandler)
        {
            _collector = collector;
            _dbUsageHandler = dbUsageHandler;
        }

        public async System.Threading.Tasks.Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var data = await _dbUsageHandler.GetDatabaseUsageAsync(false, CancellationToken.None)
                .ConfigureAwait(false);
            await _collector.RegisterGeneralData(new GeneralStatInput { DataType = "DatabaseUsage", Data = data });
        }
    }
}
