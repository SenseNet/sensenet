using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository.InMemory;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Services.Core;
using SenseNet.Services.Core.Diagnostics;
using SenseNet.Testing;
using SenseNet.WebHooks;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    public partial class StatisticsTests
    {
        [TestMethod]
        public async STT.Task Stat_OData_GetApiUsagePeriod_2()
        {
            var services = new ServiceCollection()
                .AddTransient<WebTransferRegistrator>()
                .AddStatisticalDataProvider<InMemoryStatisticalDataProvider>()
                .BuildServiceProvider();

            await ODataTestAsync(builder =>
            {
            }, async () =>
            {
                var now = DateTime.UtcNow;
                var testEnd = now.Truncate(TimeResolution.Month).AddMonths(1);
                var testStart = testEnd.AddYears(-1);
                var statDp = services.GetService<IStatisticalDataProvider>();
                await GenerateApiCallDataForODataTests(statDp, testStart, testEnd, now);
                // Delete every other daily aggregations;
                var dpAcc = new ObjectAccessor(statDp);
                var aggregations = (List<Aggregation>)dpAcc.GetProperty("Aggregations");
                var toDelete = aggregations
                    .Where(x => x.Resolution == TimeResolution.Day && x.Date.Day % 2 == 1)
                    .ToArray();
                foreach (var item in toDelete)
                    aggregations.Remove(item);


                // ACTION-1 
                // request the previous 'full' month to avoid the current date affecting test results
                //var reqTime = now.AddMonths(-1);
                //var reqTimeString = HttpUtility.UrlEncode(reqTime.ToString("o"));
                var response1 = await ODataGetAsync($"/OData.svc/('Root')/GetApiUsagePeriod",
                    "", services).ConfigureAwait(false);

                // ASSERT-1
                var result1 = (JObject)JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(response1)));
                Assert.AreEqual("WebTransfer", result1["DataType"].Value<string>());
                var start1 = testEnd.AddMonths(-1);
                var end1 = testEnd;
                var days1 = end1.AddDays(-1).Day;
                Assert.AreEqual(start1, result1["Start"].Value<DateTime>());
                Assert.AreEqual(end1, result1["End"].Value<DateTime>());
                Assert.AreEqual("Month", result1["TimeWindow"].Value<string>());
                Assert.AreEqual("Day", result1["Resolution"].Value<string>());
                var callCounts = ((JArray) result1["CallCount"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days1, callCounts.Length);
                AssertSequenceEqual(GetExpectedAggregations(now, 86400), callCounts.Take(5));
                var requestLengths = ((JArray)result1["RequestLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days1, requestLengths.Length);
                AssertSequenceEqual(GetExpectedAggregations(now, 8640000), requestLengths.Take(5));
                var responseLengths = ((JArray)result1["ResponseLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days1, responseLengths.Length);
                AssertSequenceEqual(GetExpectedAggregations(now, 86400000), responseLengths.Take(5));

                // ACTION-2
                var startTime2 = now.AddMonths(-1).ToString("yyyy-MM-dd HH:mm:ss");
                var response2 = await ODataGetAsync($"/OData.svc/('Root')/GetApiUsagePeriod",
                    $"?time={startTime2}", services).ConfigureAwait(false);

                // ASSERT-2
                var result2 = (JObject)JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(response2)));
                Assert.AreEqual("WebTransfer", result2["DataType"].Value<string>());
                var start2 = testEnd.AddMonths(-2);
                var end2 = testEnd.AddMonths(-1);
                var days2 = end2.AddDays(-1).Day;
                Assert.AreEqual(start2, result2["Start"].Value<DateTime>());
                Assert.AreEqual(end2, result2["End"].Value<DateTime>());
                Assert.AreEqual("Month", result2["TimeWindow"].Value<string>());
                Assert.AreEqual("Day", result2["Resolution"].Value<string>());
                callCounts = ((JArray) result2["CallCount"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days2, callCounts.Length);
                AssertSequenceEqual(new[]{ 0L, 86400, 0, 86400, 0 }, callCounts.Take(5));
                requestLengths = ((JArray)result2["RequestLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days2, requestLengths.Length);
                AssertSequenceEqual(new[] { 0L, 8640000, 0, 8640000, 0 }, requestLengths.Take(5));
                responseLengths = ((JArray)result2["ResponseLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days2, responseLengths.Length);
                AssertSequenceEqual(new[] { 0L, 86400000, 0, 86400000, 0 }, responseLengths.Take(5));

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task Stat_OData_GetWebHookUsagePeriod_2()
        {
            var services = new ServiceCollection()
                .AddTransient<WebTransferRegistrator>()
                .AddStatisticalDataProvider<InMemoryStatisticalDataProvider>()
                .BuildServiceProvider();

            await ODataTestAsync(builder =>
            {
            }, async () =>
            {
                var now = DateTime.UtcNow;
                var testEnd = now.Truncate(TimeResolution.Month).AddMonths(1);
                var testStart = testEnd.AddYears(-1);
                var statDp = services.GetService<IStatisticalDataProvider>();
                await GenerateWebHookDataForODataTests(statDp, testStart, testEnd, now);
                // Delete every second daily aggregations();
                var dpAcc = new ObjectAccessor(statDp);
                var aggregations = (List<Aggregation>)dpAcc.GetProperty("Aggregations");
                var toDelete = aggregations
                    .Where(x => x.Resolution == TimeResolution.Day && x.Date.Day % 2 == 1)
                    .ToArray();
                foreach (var item in toDelete)
                    aggregations.Remove(item);


                // ACTION-1 
                var response1 = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsagePeriod",
                    "", services).ConfigureAwait(false);

                // ASSERT-1
                var result1 = (JObject)JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(response1)));
                Assert.AreEqual("WebHook", result1["DataType"].Value<string>());
                var start1 = testEnd.AddMonths(-1);
                var end1 = testEnd;
                var days1 = end1.AddDays(-1).Day;
                Assert.AreEqual(start1, result1["Start"].Value<DateTime>());
                Assert.AreEqual(end1, result1["End"].Value<DateTime>());
                Assert.AreEqual("Month", result1["TimeWindow"].Value<string>());
                Assert.AreEqual("Day", result1["Resolution"].Value<string>());
                var callCounts = ((JArray)result1["CallCount"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days1, callCounts.Length);
                AssertSequenceEqual(GetExpectedAggregations(now, 86400), callCounts.Take(5));
                var requestLengths = ((JArray)result1["RequestLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days1, requestLengths.Length);
                AssertSequenceEqual(GetExpectedAggregations(now, 8640000), requestLengths.Take(5));
                var responseLengths = ((JArray)result1["ResponseLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days1, responseLengths.Length);
                AssertSequenceEqual(GetExpectedAggregations(now, 86400000), responseLengths.Take(5));
                var status100 = ((JArray)result1["Status100"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days1, status100.Length);
                AssertSequenceEqual(new[] { 0L, 0, 0, 0, 0 }, status100.Take(5));
                var status200 = ((JArray)result1["Status200"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days1, status200.Length);
                AssertSequenceEqual(GetExpectedAggregations(now, 69120), status200.Take(5));
                var status300 = ((JArray)result1["Status300"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days1, status300.Length);
                AssertSequenceEqual(new[] { 0L, 0, 0, 0, 0 }, status300.Take(5));
                var status400 = ((JArray)result1["Status400"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days1, status400.Length);
                AssertSequenceEqual(GetExpectedAggregations(now, 8640), status400.Take(5));
                var status500 = ((JArray)result1["Status500"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days1, status500.Length);
                AssertSequenceEqual(GetExpectedAggregations(now, 8640), status500.Take(5));


                // ACTION-2
                var startTime2 = now.AddMonths(-1).ToString("yyyy-MM-dd HH:mm:ss");
                var response2 = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsagePeriod",
                    $"?time={startTime2}", services).ConfigureAwait(false);

                // ASSERT-2
                var result2 = (JObject)JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(response2)));
                var start2 = testEnd.AddMonths(-2);
                var end2 = testEnd.AddMonths(-1);
                var days2 = end2.AddDays(-1).Day;
                Assert.AreEqual("WebHook", result2["DataType"].Value<string>());
                Assert.AreEqual(start2, result2["Start"].Value<DateTime>());
                Assert.AreEqual(end2, result2["End"].Value<DateTime>());
                Assert.AreEqual("Month", result2["TimeWindow"].Value<string>());
                Assert.AreEqual("Day", result2["Resolution"].Value<string>());
                callCounts = ((JArray)result2["CallCount"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days2, callCounts.Length);
                AssertSequenceEqual(new[] { 0L, 86400, 0, 86400, 0 }, callCounts.Take(5));
                requestLengths = ((JArray)result2["RequestLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days2, requestLengths.Length);
                AssertSequenceEqual(new[] { 0L, 8640000, 0, 8640000, 0 }, requestLengths.Take(5));
                responseLengths = ((JArray)result2["ResponseLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days2, responseLengths.Length);
                AssertSequenceEqual(new[] { 0L, 86400000, 0, 86400000, 0 }, responseLengths.Take(5));
                status100 = ((JArray)result2["Status100"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days2, status100.Length);
                AssertSequenceEqual(new[] { 0L, 0, 0, 0, 0 }, status100.Take(5));
                status200 = ((JArray)result2["Status200"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days2, status200.Length);
                AssertSequenceEqual(new[] { 0L, 69120, 0, 69120, 0 }, status200.Take(5));
                status300 = ((JArray)result2["Status300"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days2, status300.Length);
                AssertSequenceEqual(new[] { 0L, 0, 0, 0, 0 }, status300.Take(5));
                status400 = ((JArray)result2["Status400"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days2, status400.Length);
                AssertSequenceEqual(new[] { 0L, 8640, 0, 8640, 0 }, status400.Take(5));
                status500 = ((JArray)result2["Status500"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(days2, status500.Length);
                AssertSequenceEqual(new[] { 0L, 8640, 0, 8640, 0 }, status500.Take(5));

            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async STT.Task Stat_Aggregation_FaultTolerance_Records()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            var aggregator = new StatisticalDataAggregationController(statDataProvider,
                new[] { new WebHookStatisticalDataAggregator(GetOptions()) }, GetOptions(),
                    NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());

            // Initial state: records and aggregations simulate lack of two periods (at 2 min and 3 min).
            // 0:00      1:00      2:00      3:00      4:00
            // |         |         |         |         |
            // r r r r r r r r r r r r r r r r r r r r /
            // <Minutely>
            //
            // The action: generating a hourly aggregation at 00:04:00. This operation's original result is the minutely aggregations
            // between 3 min and 4 min.
            // 0:00      1:00      2:00      3:00      4:00
            // |         |         |         |         |
            // r r r r r r r r r r r r r r r r r r r r /
            // <Minutely>                    <Minutely>
            //
            // The expectation: the aggregator produces the original result but fills the gap: generates the 1 min and 2 min
            // aggregations too.
            // 0:00      1:00      2:00      3:00      4:00
            // |         |         |         |         |
            // r r r r r r r r r r r r r r r r r r r r /
            // <Minutely><Minutely><Minutely><Minutely>

            var start =     new DateTime(2021, 6, 28, 0, 0, 0);
            var milestone = new DateTime(2021, 6, 28, 0, 1, 0);
            var end =       new DateTime(2021, 6, 28, 0, 4, 0);
            for (var now = start; now < end; now = now.AddSeconds(15))
            {
                await GenerateWebHookRecordAsync(now, statDataProvider, CancellationToken.None);
                if (now == milestone)
                    await aggregator.AggregateAsync(now.AddSeconds(-1), TimeResolution.Minute, CancellationToken.None);
            }
            var allAggregations = statDataProvider.Aggregations;
            Assert.AreEqual(16, statDataProvider.Storage.Count);
            Assert.AreEqual(1, allAggregations.Count);
            Assert.AreEqual(1, allAggregations.Count(x => x.Resolution == TimeResolution.Minute));

            // ACTION
            var time = end.AddSeconds(-1);
            await aggregator.AggregateAsync(time, TimeResolution.Minute, CancellationToken.None);

            // ASSERT
            allAggregations = statDataProvider.Aggregations;
            Assert.AreEqual(4, allAggregations.Count);
            Assert.AreEqual(4, allAggregations.Count(x => x.Resolution == TimeResolution.Minute));
            Assert.AreEqual("0 1 2 3", string.Join(" ", allAggregations.Select(x=>x.Date.Minute.ToString())));
        }
        [TestMethod]
        public async STT.Task Stat_Aggregation_FaultTolerance_AllAggregations()
        {
            var statDataProvider = new TestStatisticalDataProvider();

            // now                                                   2021-06-29 13:56:12
            // Minute  ... 2021-06-29 13:52:00  2021-06-29 13:53:00  2021-06-29 13:54:00
            // Hour    ... 2021-06-29 10:00:00  2021-06-29 11:00:00  2021-06-29 12:00:00
            // Day     ... 2021-06-26 00:00:00  2021-06-27 00:00:00  2021-06-28 00:00:00
            // Month   ... 2021-03-01 00:00:00  2021-04-01 00:00:00  2021-05-01 00:00:00
            var now = new DateTime(2021, 6, 29, 13, 56, 12);
            for (int i = 0; i < 12; i++)
            {
                var date = now.AddSeconds(-i * 15 - 1);
                await GenerateWebHookRecordAsync(date, statDataProvider, CancellationToken.None);
            }
            for (int i = 0; i < 60 * 3; i++)
            {
                var date = now.Truncate(TimeResolution.Minute).AddMinutes(-i - 2);
                await GenerateWebHookAggregationAsync(date, TimeResolution.Minute, 10, statDataProvider);
            }
            for (int i = 0; i < 24 * 3; i++)
            {
                var date = now.Truncate(TimeResolution.Hour).AddHours(-i - 1);
                await GenerateWebHookAggregationAsync(date, TimeResolution.Hour, 10, statDataProvider);
            }
            for (int i = 0; i < 31 * 3; i++)
            {
                var date = now.Truncate(TimeResolution.Day).AddDays(-i - 1);
                await GenerateWebHookAggregationAsync(date, TimeResolution.Day, 10, statDataProvider);
            }
            for (int i = 0; i < 12 * 3; i++)
            {
                var date = now.Truncate(TimeResolution.Month).AddMonths(-i - 1);
                await GenerateWebHookAggregationAsync(date, TimeResolution.Month, 10, statDataProvider);
            }

            var aggregationCountBefore = statDataProvider.Aggregations.Count;

            // ACTION-1 no repair (every aggregations are present).
            var aggregator = new StatisticalDataAggregationController(statDataProvider,
                new[] { new WebHookStatisticalDataAggregator(GetOptions()) }, GetOptions(),
                    NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            var aggregationTime = now.Truncate(TimeResolution.Minute).AddSeconds(-1);
            await aggregator.AggregateAsync(aggregationTime, TimeResolution.Minute, CancellationToken.None);

            // ASSERT-1 Current aggregation is created.
            var aggregationCountAfter = statDataProvider.Aggregations.Count;
            Assert.AreEqual(aggregationCountBefore + 1, aggregationCountAfter);

            // ALIGN-2 Delete the current aggregation and two of each older aggregations
            var toDelete = new List<Aggregation>();
            toDelete.AddRange(statDataProvider.Aggregations.Where(x => x.Resolution == TimeResolution.Minute)
                .OrderByDescending(x => x.Date).Take(3).ToArray());
            toDelete.AddRange(statDataProvider.Aggregations.Where(x => x.Resolution == TimeResolution.Hour)
                .OrderByDescending(x => x.Date).Take(2).ToArray());
            toDelete.AddRange(statDataProvider.Aggregations.Where(x => x.Resolution == TimeResolution.Day)
                .OrderByDescending(x => x.Date).Take(2).ToArray());
            toDelete.AddRange(statDataProvider.Aggregations.Where(x => x.Resolution == TimeResolution.Month)
                .OrderByDescending(x => x.Date).Take(2).ToArray());
            foreach (var item in toDelete)
                statDataProvider.Aggregations.Remove(item);
            aggregationCountBefore = statDataProvider.Aggregations.Count;

            // ACTION-2 repair 8 and generate 1
            aggregator = new StatisticalDataAggregationController(statDataProvider,
                new[] { new WebHookStatisticalDataAggregator(GetOptions()) }, GetOptions(),
                    NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            aggregationTime = now.Truncate(TimeResolution.Minute).AddSeconds(-1);
            await aggregator.AggregateAsync(aggregationTime, TimeResolution.Minute, CancellationToken.None);

            // ASSERT-2 Current aggregation is created.
            aggregationCountAfter = statDataProvider.Aggregations.Count;
            Assert.AreEqual(aggregationCountBefore + 9, aggregationCountAfter);
        }

        private static long[] GetExpectedAggregations(DateTime now, long value)
        {
            return now.Day switch
            {
                < 3 => new[] { 0L, 0, 0, 0, 0 },
                < 5 => new[] { 0L, value, 0, 0, 0 },
                _ => new[] { 0L, value, 0, value, 0 }
            };
        }
    }
}
