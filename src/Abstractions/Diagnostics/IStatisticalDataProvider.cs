using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Diagnostics
{
    [DebuggerDisplay("{ToString()}")]
    public class Aggregation
    {
        public string DataType { get; set; }
        public DateTime Date { get; set; }
        public TimeResolution Resolution { get; set; }
        //TimeWindow Window { get; set; }

        public string Data { get; set; }

        public override string ToString()
        {
            return $"{Date:yyyy-MM-dd HH:mm:ss} {Resolution}";
        }
    }

    public interface IStatisticalDataProvider
    {
        Task WriteData(IStatisticalDataRecord data);
        Task CleanupAsync(DateTime timeMax, CancellationToken cancel);
        Task LoadUsageListAsync(string dataType, DateTime startTime, TimeResolution resolution, CancellationToken cancel);

        Task<IEnumerable<Aggregation>> LoadAggregatedUsageAsync(string dataType, TimeResolution resolution,
            DateTime startTime, DateTime endTimeExclusive, CancellationToken cancel);

        Task EnumerateDataAsync(string dataType, DateTime startTime, DateTime endTimeExclusive,
            TimeResolution resolution, Action<IStatisticalDataRecord> aggregatorCallback, CancellationToken cancel);

        Task WriteAggregationAsync(Aggregation aggregation, CancellationToken cancel);
    }

}
