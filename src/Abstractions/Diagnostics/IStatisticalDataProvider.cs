using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Data;

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

    public interface IStatisticalDataProvider : IDataProviderExtension
    {
        Task WriteDataAsync(IStatisticalDataRecord data, CancellationToken cancel);

        Task<IEnumerable<IStatisticalDataRecord>> LoadUsageListAsync(string dataType, DateTime endTimeExclusive, int count, CancellationToken cancel);

        Task<IEnumerable<Aggregation>> LoadAggregatedUsageAsync(string dataType, TimeResolution resolution,
            DateTime startTime, DateTime endTimeExclusive, CancellationToken cancel);

        Task EnumerateDataAsync(string dataType, DateTime startTime, DateTime endTimeExclusive,
            Action<IStatisticalDataRecord> aggregatorCallback, CancellationToken cancel);

        Task WriteAggregationAsync(Aggregation aggregation, CancellationToken cancel);
        Task CleanupRecordsAsync(string dataType, DateTime retentionTime, CancellationToken cancel);
        Task CleanupAggregationsAsync(string dataType, TimeResolution resolution, DateTime retentionTime, CancellationToken cancel);
    }

}
