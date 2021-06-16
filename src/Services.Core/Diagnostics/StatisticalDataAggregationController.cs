using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        public StatisticalDataAggregationController(IStatisticalDataProvider statDataProvider,
            IEnumerable<IStatisticalDataAggregator> aggregators, IOptions<StatisticsOptions> options)
        {
            _statDataProvider = statDataProvider;
            _aggregators = aggregators;
            _options = options.Value;
        }

        public async Task AggregateAsync(DateTime startTime, TimeResolution resolution, CancellationToken cancel)
        {
            var start = startTime.Truncate(resolution);
            var end = startTime.Next(resolution);
            foreach (var aggregator in _aggregators)
            {
                if (!await TryProcessAggregationsAsync(aggregator, start, end, resolution, cancel))
                {
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

                    await _statDataProvider.WriteAggregationAsync(result, cancel).ConfigureAwait(false);
                }

                await CleanupAsync(aggregator, resolution, start, cancel).ConfigureAwait(false);
            }
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

            if (aggregations.Length == 0)
                return false;

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

            await _statDataProvider.CleanupAggregationsAsync(aggregator.DataType, resolution, retentionTime, cancel)
                .ConfigureAwait(false);
        }
    }

    //public class WebHookStatisticalDataAggregator : IStatisticalDataAggregator
    //{
    //    internal class WebHookAggregation
    //    {
    //        public int CallCount { get; set; }
    //        public int[] StatusCounts { get; set; } = new int[5];
    //        public long RequestLengths { get; set; }
    //        public long ResponseLengths { get; set; }
    //    }

    //    private WebHookAggregation _aggregation = new WebHookAggregation();
    //    private StatisticsOptions _options;

    //    public WebHookStatisticalDataAggregator(IOptions<StatisticsOptions> options)
    //    {
    //        _options = options.Value;
    //    }

    //    public string DataType => "WebHook";
    //    public bool IsEmpty => _aggregation.CallCount == 0;
    //    public object Data => _aggregation;
    //    public AggregationRetentionPeriods RetentionPeriods =>_options.Retention.WebHooks;

    //    public void Aggregate(IStatisticalDataRecord data)
    //    {
    //        _aggregation.CallCount++;
    //        _aggregation.RequestLengths += data.RequestLength ?? 0;
    //        _aggregation.ResponseLengths += data.ResponseLength ?? 0;
    //        var leadDigit = (data.ResponseStatusCode ?? 0) / 100 - 1;
    //        if (leadDigit is >= 0 and < 5)
    //            _aggregation.StatusCounts[leadDigit]++;
    //    }

    //    public void Summarize(Aggregation[] aggregations)
    //    {
    //        foreach (var aggregation in aggregations)
    //        {
    //            WebHookAggregation deserialized;
    //            using (var reader = new StringReader(aggregation.Data))
    //                deserialized = JsonSerializer.Create().Deserialize<WebHookAggregation>(new JsonTextReader(reader));
    //            _aggregation.CallCount += deserialized.CallCount;
    //            _aggregation.RequestLengths += deserialized.RequestLengths;
    //            _aggregation.ResponseLengths += deserialized.ResponseLengths;
    //            var source = deserialized.StatusCounts;
    //            var target = _aggregation.StatusCounts;
    //            for (int i = 0; i < source.Length; i++)
    //                target[i] += source[i];
    //        }
    //    }
    //}
    public class WebTransferStatisticalDataAggregator : IStatisticalDataAggregator
    {
        internal class WebTransferAggregation
        {
            public int CallCount { get; set; }
            public long RequestLengths { get; set; }
            public long ResponseLengths { get; set; }
        }

        private WebTransferAggregation _aggregation = new WebTransferAggregation();
        private StatisticsOptions _options;

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
    }
    public class DatabaseUsageStatisticalDataAggregator : IStatisticalDataAggregator
    {
        private DatabaseUsage _aggregation = new DatabaseUsage();
        private StatisticsOptions _options;

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
                    DatabaseUsage deserialized;
                    using (var reader = new StringReader(x.Data))
                        deserialized = JsonSerializer.Create().Deserialize<DatabaseUsage>(new JsonTextReader(reader));
                    return deserialized;
                }).ToArray();

            _aggregation.Content = new Dimensions();
            _aggregation.Content.Count = Convert.ToInt32(Math.Round(usages.Average(x => x.Content.Count)));
            _aggregation.Content.Blob = Convert.ToInt32(Math.Round(usages.Average(x => x.Content.Blob)));
            _aggregation.Content.Metadata = Convert.ToInt32(Math.Round(usages.Average(x => x.Content.Metadata)));
            _aggregation.Content.Text = Convert.ToInt32(Math.Round(usages.Average(x => x.Content.Text)));
            _aggregation.Content.Index = Convert.ToInt32(Math.Round(usages.Average(x => x.Content.Index)));

            _aggregation.OldVersions = new Dimensions();
            _aggregation.OldVersions.Count = Convert.ToInt32(Math.Round(usages.Average(x => x.OldVersions.Count)));
            _aggregation.OldVersions.Blob = Convert.ToInt32(Math.Round(usages.Average(x => x.OldVersions.Blob)));
            _aggregation.OldVersions.Metadata = Convert.ToInt32(Math.Round(usages.Average(x => x.OldVersions.Metadata)));
            _aggregation.OldVersions.Text = Convert.ToInt32(Math.Round(usages.Average(x => x.OldVersions.Text)));
            _aggregation.OldVersions.Index = Convert.ToInt32(Math.Round(usages.Average(x => x.OldVersions.Index)));

            _aggregation.Preview = new Dimensions();
            _aggregation.Preview.Count = Convert.ToInt32(Math.Round(usages.Average(x => x.Preview.Count)));
            _aggregation.Preview.Blob = Convert.ToInt32(Math.Round(usages.Average(x => x.Preview.Blob)));
            _aggregation.Preview.Metadata = Convert.ToInt32(Math.Round(usages.Average(x => x.Preview.Metadata)));
            _aggregation.Preview.Text = Convert.ToInt32(Math.Round(usages.Average(x => x.Preview.Text)));
            _aggregation.Preview.Index = Convert.ToInt32(Math.Round(usages.Average(x => x.Preview.Index)));

            _aggregation.System = new Dimensions();
            _aggregation.System.Count = Convert.ToInt32(Math.Round(usages.Average(x => x.System.Count)));
            _aggregation.System.Blob = Convert.ToInt32(Math.Round(usages.Average(x => x.System.Blob)));
            _aggregation.System.Metadata = Convert.ToInt32(Math.Round(usages.Average(x => x.System.Metadata)));
            _aggregation.System.Text = Convert.ToInt32(Math.Round(usages.Average(x => x.System.Text)));
            _aggregation.System.Index = Convert.ToInt32(Math.Round(usages.Average(x => x.System.Index)));

            _aggregation.OperationLog = new LogDimensions();
            _aggregation.OperationLog.Count = Convert.ToInt32(Math.Round(usages.Average(x => x.OperationLog.Count)));
            _aggregation.OperationLog.Metadata = Convert.ToInt32(Math.Round(usages.Average(x => x.OperationLog.Metadata)));
            _aggregation.OperationLog.Text = Convert.ToInt32(Math.Round(usages.Average(x => x.OperationLog.Text)));

            _aggregation.OrphanedBlobs = Convert.ToInt32(Math.Round(usages.Average(x => x.OrphanedBlobs)));
        }
    }

}
