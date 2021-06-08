using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics
{
    internal class StatisticalDataAggregationController
    {
        private readonly IStatisticalDataProvider _statDataProvider;
        private readonly IEnumerable<IStatisticalDataAggregator> _aggregators;

        public StatisticalDataAggregationController(IStatisticalDataProvider statDataProvider,
            IEnumerable<IStatisticalDataAggregator> aggregators)
        {
            _statDataProvider = statDataProvider;
            _aggregators = aggregators;
        }

        public async Task AggregateAsync(DateTime startTime, TimeResolution resolution, CancellationToken cancel)
        {
            var start = startTime.Truncate(resolution);
            var end = startTime.Next(resolution);
            foreach (var aggregator in _aggregators)
            {
                if (!await TryProcessAggregationsAsync(aggregator, start, end, resolution, cancel))
                {
                    await _statDataProvider.EnumerateDataAsync(aggregator.DataType, start, end, resolution, aggregator.Aggregate, cancel);
                }

                if (aggregator.IsEmpty)
                    return;

                var result = new Aggregation
                {
                    DataType = aggregator.DataType,
                    Date = start,
                    Resolution = resolution,
                    Data = Serialize(aggregator.Data)
                };

                await _statDataProvider.WriteAggregationAsync(result, cancel);
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
    }

    public class WebHookStatisticalDataAggregator : IStatisticalDataAggregator
    {
        internal class WebHookAggregation
        {
            public int CallCount { get; set; }
            public int[] StatusCounts { get; set; } = new int[5];
            public long RequestLengths { get; set; }
            public long ResponseLengths { get; set; }
        }

        private WebHookAggregation _aggregation = new WebHookAggregation();

        public string DataType => "WebHook";
        public bool IsEmpty => _aggregation.CallCount == 0;
        public object Data => _aggregation;
        public void Aggregate(IStatisticalDataRecord data)
        {
            _aggregation.CallCount++;
            _aggregation.RequestLengths += data.RequestLength ?? 0;
            _aggregation.ResponseLengths += data.ResponseLength ?? 0;
            var leadDigit = (data.ResponseStatusCode ?? 0) / 100 - 1;
            if (leadDigit is >= 0 and < 5)
                _aggregation.StatusCounts[leadDigit]++;
        }

        public void Summarize(Aggregation[] aggregations)
        {
            foreach (var aggregation in aggregations)
            {
                WebHookAggregation deserialized;
                using (var reader = new StringReader(aggregation.Data))
                    deserialized = JsonSerializer.Create().Deserialize<WebHookAggregation>(new JsonTextReader(reader));
                _aggregation.CallCount += deserialized.CallCount;
                _aggregation.RequestLengths += deserialized.RequestLengths;
                _aggregation.ResponseLengths += deserialized.ResponseLengths;
                var source = deserialized.StatusCounts;
                var target = _aggregation.StatusCounts;
                for (int i = 0; i < source.Length; i++)
                    target[i] += source[i];
            }
        }
    }
    //public class WebTransferStatisticalDataAggregatorDelete : IStatisticalDataAggregator
    //{
    //}
    //public class DatabaseUsageStatisticalDataAggregatorDelete : IStatisticalDataAggregator
    //{
    //}

}
