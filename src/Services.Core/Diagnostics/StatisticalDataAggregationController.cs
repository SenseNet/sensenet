using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SenseNet.Diagnostics;
using SenseNet.Storage.DataModel.Usage;
using NotSupportedException = System.NotSupportedException;

namespace SenseNet.Services.Core.Diagnostics
{
    public interface IStatisticalDataAggregationController
    {
        Task AggregateAsync(DateTime startTime, TimeResolution resolution, CancellationToken cancel);
    }

    public class StatisticalDataAggregationController : IStatisticalDataAggregationController
    {
        private readonly IStatisticalDataProvider _statDataProvider;
        private readonly IEnumerable<IStatisticalDataAggregator> _aggregators;
        private readonly StatisticsOptions _options;
        private readonly DateTime[] _lastGenerationTimes = Enumerable.Range(0, Enum.GetNames(typeof(TimeResolution)).Length)
            .Select(_ => default(DateTime)).ToArray();

        private readonly ILogger _logger;

        public StatisticalDataAggregationController(IStatisticalDataProvider statDataProvider,
            IEnumerable<IStatisticalDataAggregator> aggregators,
            IOptions<StatisticsOptions> options,
            ILogger<StatisticalDataAggregationController> logger)
        {
            _statDataProvider = statDataProvider;
            _aggregators = aggregators;
            _options = options.Value;
            _logger = logger;
        }

        public async Task AggregateAsync(DateTime startTime, TimeResolution resolution, CancellationToken cancel)
        {
            var start = startTime.Truncate(resolution);
            var end = startTime.Next(resolution);
            if(resolution == TimeResolution.Minute)
                await RepairAsync(start, cancel);
            await AggregateAsync(start, end, resolution, true, cancel);
        }
        private async Task AggregateAsync(DateTime start, DateTime end, TimeResolution resolution, bool cleanup, CancellationToken cancel)
        {
            if (start > DateTime.UtcNow)
            {
                //_logger.LogTrace($"Skipping statistical aggregation for a future date: {start}");
                return;
            }

            //_logger.LogTrace($"Aggregating statistical data START: {start}, END: {end}, resolution: {resolution}");

            foreach (var aggregator in _aggregators)
            {
                aggregator.Clear();

                if (!await TryProcessAggregationsAsync(aggregator, start, end, resolution, cancel))
                {
                    //_logger.LogTrace($"Aggregator enumerating data: {aggregator.DataType}");
                    await _statDataProvider.EnumerateDataAsync(aggregator.DataType, start, end, aggregator.Aggregate, cancel);
                }

                if (!aggregator.IsEmpty)
                {
                    var result = new Aggregation
                    {
                        DataType = aggregator.DataType,
                        Date = start,
                        Resolution = resolution,
                        Data = Serialize(aggregator.Data)
                    };

                    //_logger.LogTrace($"Writing statistical aggregation for {aggregator.DataType}, START: {start}, resolution: {resolution}");

                    await _statDataProvider.WriteAggregationAsync(result, cancel).ConfigureAwait(false);
                }

                if (cleanup)
                    await CleanupAsync(aggregator, resolution, start, cancel).ConfigureAwait(false);

            }

            //_logger.LogTrace($"Aggregation finished, last gen time: {start}");

            _lastGenerationTimes[(int)resolution] = start;
        }

        private async Task RepairAsync(DateTime endTime, CancellationToken cancel)
        {
            var length = Enum.GetNames(typeof(TimeResolution)).Length;
            var lastGenerationTimes = (DateTime[])_lastGenerationTimes.Clone();
            if (lastGenerationTimes[(int)TimeResolution.Minute] == DateTime.MinValue)
            {
                lastGenerationTimes = await LoadLastGenerationTimes(cancel);
                //_logger.LogTrace($"Statistics aggregation: last generation times loaded: {string.Join(", ", lastGenerationTimes)}");
            }

            // Execute for all resolutions
            for (var resolutionIndex = 0; resolutionIndex < length; resolutionIndex++)
                await RepairAsync(lastGenerationTimes[resolutionIndex], endTime, (TimeResolution)resolutionIndex, cancel);
        }
        private async Task RepairAsync(DateTime lastGenerationTime, DateTime endTime, TimeResolution resolution, CancellationToken cancel)
        {
            var time = lastGenerationTime == DateTime.MinValue 
                ? endTime.AddPeriods(-3, resolution)
                : lastGenerationTime.Next(resolution);

            var next = time.Next(resolution);

            //_logger.LogTrace($"Repairing aggregations. Time: {time}, Next: {next}, resolution {resolution}");

            while (time < endTime.Truncate(resolution))
            {
                await AggregateAsync(time, next, resolution, false, cancel);
                time = next;
                next = time.Next(resolution);
            }
        }

        private async Task<DateTime[]> LoadLastGenerationTimes(CancellationToken cancel)
        {
            return (await _statDataProvider.LoadLastAggregationTimesByResolutionsAsync(cancel).ConfigureAwait(false))
                .Select(x => x ?? DateTime.MinValue).ToArray();
        }

        private async Task<bool> TryProcessAggregationsAsync(IStatisticalDataAggregator aggregator,
            DateTime startTime, DateTime endTimeExclusive,
            TimeResolution targetResolution, CancellationToken cancel)
        {
            var resolution = targetResolution - 1;
            if (resolution < 0)
                return false;
            
            var aggregations =
                (await _statDataProvider.LoadAggregatedUsageAsync(aggregator.DataType, resolution, startTime, endTimeExclusive, cancel))
                .ToArray();

            aggregations = aggregations.Where(x => !string.IsNullOrEmpty(x.Data) && x.Data != "null").ToArray();
            if (aggregations.Length == 0)
                return false;

            //_logger.LogTrace($"Summarizing aggregations for {aggregator.DataType}, resolution: {targetResolution}, count: {aggregations.Length}");

            aggregator.Summarize(aggregations);
            return true;
        }
        internal static string Serialize(object data)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
                JsonSerializer.Create().Serialize(writer, data);
            return sb.ToString();
        }

        internal async Task CleanupAsync(IStatisticalDataAggregator aggregator, TimeResolution resolution, DateTime startTime, CancellationToken cancel)
        {
            var retentionPeriods = aggregator.RetentionPeriods;
            if (resolution == TimeResolution.Minute)
            {
                var recordsRetentionTime = startTime.AddMinutes(-retentionPeriods.Momentary);

                //_logger.LogTrace($"Cleaning up records for {aggregator.DataType}, resolution {resolution}, starttime {startTime}, retention time {recordsRetentionTime}");

                await _statDataProvider.CleanupRecordsAsync(aggregator.DataType, recordsRetentionTime, cancel)
                    .ConfigureAwait(false);
            }

            DateTime retentionTime;
            switch (resolution)
            {
                case TimeResolution.Minute: retentionTime = startTime.AddHours(-retentionPeriods.Minutely); break;
                case TimeResolution.Hour: retentionTime = startTime.AddDays(-retentionPeriods.Hourly); break;
                case TimeResolution.Day: retentionTime = startTime.AddMonths(-retentionPeriods.Daily); break;
                case TimeResolution.Month: retentionTime = startTime.AddYears(-retentionPeriods.Monthly); break;
                default: throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
            }

            //_logger.LogTrace($"Cleaning up aggregations for {aggregator.DataType}, resolution {resolution}, starttime {startTime}");

            await _statDataProvider.CleanupAggregationsAsync(aggregator.DataType, resolution, retentionTime, cancel)
                .ConfigureAwait(false);
        }
    }

    public class WebTransferStatisticalDataAggregator : IStatisticalDataAggregator
    {
        internal class WebTransferAggregation
        {
            public int CallCount { get; set; }
            public long RequestLengths { get; set; }
            public long ResponseLengths { get; set; }
        }

        private WebTransferAggregation _aggregation = new WebTransferAggregation();
        private readonly StatisticsOptions _options;

        public WebTransferStatisticalDataAggregator(IOptions<StatisticsOptions> options)
        {
            _options = options.Value;
        }

        public string DataType => "WebTransfer";
        public bool IsEmpty => _aggregation.CallCount == 0;
        public object Data => _aggregation;
        public AggregationRetentionPeriods RetentionPeriods => _options.Retention.ApiCalls;

        public void Aggregate(IStatisticalDataRecord data)
        {
            _aggregation.CallCount++;
            _aggregation.RequestLengths += data.RequestLength ?? 0;
            _aggregation.ResponseLengths += data.ResponseLength ?? 0;
        }

        public void Summarize(Aggregation[] aggregations)
        {
            foreach (var aggregation in aggregations)
            {
                WebTransferAggregation deserialized;
                using (var reader = new StringReader(aggregation.Data))
                    deserialized = JsonSerializer.Create().Deserialize<WebTransferAggregation>(new JsonTextReader(reader));
                _aggregation.CallCount += deserialized.CallCount;
                _aggregation.RequestLengths += deserialized.RequestLengths;
                _aggregation.ResponseLengths += deserialized.ResponseLengths;
            }
        }

        public void Clear()
        {
            _aggregation = new WebTransferAggregation();
        }
    }
    public class DatabaseUsageStatisticalDataAggregator : IStatisticalDataAggregator
    {
        private DatabaseUsage _aggregation = new();
        private readonly StatisticsOptions _options;

        public DatabaseUsageStatisticalDataAggregator(IOptions<StatisticsOptions> options)
        {
            _options = options.Value;
        }

        public string DataType => "DatabaseUsage";
        public bool IsEmpty => (_aggregation.System?.Count ?? 0) == 0;
        public object Data => _aggregation;
        public AggregationRetentionPeriods RetentionPeriods => _options.Retention.DatabaseUsage;

        public void Aggregate(IStatisticalDataRecord data)
        {
            throw new NotSupportedException();
        }

        public void Summarize(Aggregation[] aggregations)
        {
            var usages =
                aggregations.Select(x =>
                {
                    using var reader = new StringReader(x.Data);
                    var deserialized = JsonSerializer.Create().Deserialize<DatabaseUsage>(new JsonTextReader(reader));
                    return deserialized;
                }).ToArray();

            _aggregation.Content = new Dimensions
            {
                Count = Convert.ToInt32(Math.Round(usages.Average(x => x.Content.Count))),
                Blob = Convert.ToInt32(Math.Round(usages.Average(x => x.Content.Blob))),
                Metadata = Convert.ToInt32(Math.Round(usages.Average(x => x.Content.Metadata))),
                Text = Convert.ToInt32(Math.Round(usages.Average(x => x.Content.Text))),
                Index = Convert.ToInt32(Math.Round(usages.Average(x => x.Content.Index)))
            };

            _aggregation.OldVersions = new Dimensions
            {
                Count = Convert.ToInt32(Math.Round(usages.Average(x => x.OldVersions.Count))),
                Blob = Convert.ToInt32(Math.Round(usages.Average(x => x.OldVersions.Blob))),
                Metadata = Convert.ToInt32(Math.Round(usages.Average(x => x.OldVersions.Metadata))),
                Text = Convert.ToInt32(Math.Round(usages.Average(x => x.OldVersions.Text))),
                Index = Convert.ToInt32(Math.Round(usages.Average(x => x.OldVersions.Index)))
            };

            _aggregation.Preview = new Dimensions
            {
                Count = Convert.ToInt32(Math.Round(usages.Average(x => x.Preview.Count))),
                Blob = Convert.ToInt32(Math.Round(usages.Average(x => x.Preview.Blob))),
                Metadata = Convert.ToInt32(Math.Round(usages.Average(x => x.Preview.Metadata))),
                Text = Convert.ToInt32(Math.Round(usages.Average(x => x.Preview.Text))),
                Index = Convert.ToInt32(Math.Round(usages.Average(x => x.Preview.Index)))
            };

            _aggregation.System = new Dimensions
            {
                Count = Convert.ToInt32(Math.Round(usages.Average(x => x.System.Count))),
                Blob = Convert.ToInt32(Math.Round(usages.Average(x => x.System.Blob))),
                Metadata = Convert.ToInt32(Math.Round(usages.Average(x => x.System.Metadata))),
                Text = Convert.ToInt32(Math.Round(usages.Average(x => x.System.Text))),
                Index = Convert.ToInt32(Math.Round(usages.Average(x => x.System.Index)))
            };

            _aggregation.OperationLog = new LogDimensions
            {
                Count = Convert.ToInt32(Math.Round(usages.Average(x => x.OperationLog.Count))),
                Metadata = Convert.ToInt32(Math.Round(usages.Average(x => x.OperationLog.Metadata))),
                Text = Convert.ToInt32(Math.Round(usages.Average(x => x.OperationLog.Text)))
            };

            _aggregation.OrphanedBlobs = Convert.ToInt32(Math.Round(usages.Average(x => x.OrphanedBlobs)));
        }

        public void Clear()
        {
            _aggregation = new DatabaseUsage();
        }
    }

}
