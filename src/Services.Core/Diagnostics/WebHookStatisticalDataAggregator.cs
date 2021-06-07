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
    public class WebHookStatisticalDataAggregator : IStatisticalDataAggregator
    {
        public static readonly string DataType = "WebHook";

        internal class WebHookAggregation
        {
            public int CallCount { get; set; }
            public int[] StatusCounts { get; set; } = new int[5];
            public long RequestLengths { get; set; }
            public long ResponseLengths { get; set; }
        }

        private readonly IStatisticalDataProvider _statDataProvider;
        private WebHookAggregation _aggregation;

        public WebHookStatisticalDataAggregator(IStatisticalDataProvider statDataProvider)
        {
            _statDataProvider = statDataProvider;
        }

        public async Task AggregateAsync(DateTime startTime, TimeResolution resolution, CancellationToken cancel)
        {
            var start = startTime.Truncate(resolution);
            var end = startTime.Next(resolution);
            _aggregation = new WebHookAggregation();

            if (!await TryProcessAggregationsAsync(start, end, resolution, cancel))
            {
                _aggregation = new WebHookAggregation();
                await _statDataProvider.EnumerateDataAsync("WebHook", start, end, resolution, Aggregate, cancel);
            }

            if (_aggregation.CallCount == 0)
                return;

            var aggregation = new Aggregation
            {
                DataType = DataType,
                Date = start,
                Resolution = resolution,
                Data = Serialize(_aggregation)
            };

            await _statDataProvider.WriteAggregationAsync(aggregation, cancel);
        }

        private async Task<bool> TryProcessAggregationsAsync(DateTime startTime, DateTime endTimeExclusive,
            TimeResolution targetResolution, CancellationToken cancel)
        {
            var resolution = targetResolution - 1;
            if (resolution < 0)
                return false;

            var aggregations =
                (await _statDataProvider.LoadAggregatedUsageAsync("WebHook", resolution, startTime, endTimeExclusive, cancel))
                .ToArray();

            if (aggregations.Length == 0)
                return false;
            //if (aggregations[aggregations.Length - 1].Date.Next(resolution) < startTime.Next(targetResolution))
            //    return false;

            Summarize(aggregations);
            return true;
        }
        private void Summarize(Aggregation[] aggregations)
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

        private void Aggregate(IStatisticalDataRecord record)
        {
            _aggregation.CallCount++;
            _aggregation.RequestLengths += record.RequestLength ?? 0;
            _aggregation.ResponseLengths += record.ResponseLength ?? 0;
            var leadDigit = (record.ResponseStatusCode ?? 0) / 100 - 1;
            if (leadDigit is >= 0 and < 5)
                _aggregation.StatusCounts[leadDigit]++;
        }

        internal static string Serialize(WebHookAggregation aggregation)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
                JsonSerializer.Create().Serialize(writer, aggregation);
            return sb.ToString();
        }
        internal static WebHookAggregation Deserialize(string jsonSource)
        {
            using (var reader = new StringReader(jsonSource))
                return JsonSerializer.Create().Deserialize<WebHookAggregation>(new JsonTextReader(reader));
        }
    }
}
