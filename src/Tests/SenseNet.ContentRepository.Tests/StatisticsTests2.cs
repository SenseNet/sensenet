using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository.InMemory;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Services.Core;
using SenseNet.Testing;
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
                var now = new DateTime(2021, 6, 15, 8, 14, 28);
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
                var response1 = await ODataGetAsync($"/OData.svc/('Root')/GetApiUsagePeriod",
                    "", services).ConfigureAwait(false);

                // ASSERT-1
                var result1 = (JObject)JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(response1)));
                Assert.AreEqual("WebTransfer", result1["DataType"].Value<string>());
                Assert.AreEqual(new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc), result1["Start"].Value<DateTime>());
                Assert.AreEqual(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc), result1["End"].Value<DateTime>());
                Assert.AreEqual("Month", result1["TimeWindow"].Value<string>());
                Assert.AreEqual("Day", result1["Resolution"].Value<string>());
                var callCounts = ((JArray) result1["CallCount"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, callCounts.Length);
                AssertSequenceEqual(new[]{ 0L, 86400, 0, 86400, 0 }, callCounts.Take(5));
                var requestLengths = ((JArray)result1["RequestLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, requestLengths.Length);
                AssertSequenceEqual(new[] { 0L, 8640000, 0, 8640000, 0 }, requestLengths.Take(5));
                var responseLengths = ((JArray)result1["ResponseLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, responseLengths.Length);
                AssertSequenceEqual(new[] { 0L, 86400000, 0, 86400000, 0 }, responseLengths.Take(5));

                // ACTION-2
                var startTime2 = now.AddDays(-16).ToString("yyyy-MM-dd HH:mm:ss");
                var response2 = await ODataGetAsync($"/OData.svc/('Root')/GetApiUsagePeriod",
                    $"?time={startTime2}", services).ConfigureAwait(false);

                // ASSERT-2
                var result2 = (JObject)JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(response2)));
                Assert.AreEqual("WebTransfer", result2["DataType"].Value<string>());
                Assert.AreEqual(new DateTime(2021, 5, 1, 0, 0, 0, DateTimeKind.Utc), result2["Start"].Value<DateTime>());
                Assert.AreEqual(new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc), result2["End"].Value<DateTime>());
                Assert.AreEqual("Month", result2["TimeWindow"].Value<string>());
                Assert.AreEqual("Day", result2["Resolution"].Value<string>());
                callCounts = ((JArray) result2["CallCount"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(31, callCounts.Length);
                AssertSequenceEqual(new[]{ 0L, 86400, 0, 86400, 0 }, callCounts.Take(5));
                requestLengths = ((JArray)result2["RequestLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(31, requestLengths.Length);
                AssertSequenceEqual(new[] { 0L, 8640000, 0, 8640000, 0 }, requestLengths.Take(5));
                responseLengths = ((JArray)result2["ResponseLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(31, responseLengths.Length);
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
                var now = new DateTime(2021, 6, 15, 8, 14, 28);
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
                Assert.AreEqual(new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc), result1["Start"].Value<DateTime>());
                Assert.AreEqual(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc), result1["End"].Value<DateTime>());
                Assert.AreEqual("Month", result1["TimeWindow"].Value<string>());
                Assert.AreEqual("Day", result1["Resolution"].Value<string>());
                var callCounts = ((JArray)result1["CallCount"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, callCounts.Length);
                AssertSequenceEqual(new[] { 0L, 86400, 0, 86400, 0 }, callCounts.Take(5));
                var requestLengths = ((JArray)result1["RequestLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, requestLengths.Length);
                AssertSequenceEqual(new[] { 0L, 8640000, 0, 8640000, 0 }, requestLengths.Take(5));
                var responseLengths = ((JArray)result1["ResponseLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, responseLengths.Length);
                AssertSequenceEqual(new[] { 0L, 86400000, 0, 86400000, 0 }, responseLengths.Take(5));
                var status100 = ((JArray)result1["Status100"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, status100.Length);
                AssertSequenceEqual(new[] { 0L, 0, 0, 0, 0 }, status100.Take(5));
                var status200 = ((JArray)result1["Status200"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, status200.Length);
                AssertSequenceEqual(new[] { 0L, 69120, 0, 69120, 0 }, status200.Take(5));
                var status300 = ((JArray)result1["Status300"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, status300.Length);
                AssertSequenceEqual(new[] { 0L, 0, 0, 0, 0 }, status300.Take(5));
                var status400 = ((JArray)result1["Status400"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, status400.Length);
                AssertSequenceEqual(new[] { 0L, 8640, 0, 8640, 0 }, status400.Take(5));
                var status500 = ((JArray)result1["Status500"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, status500.Length);
                AssertSequenceEqual(new[] { 0L, 8640, 0, 8640, 0 }, status500.Take(5));


                // ACTION-2
                var startTime2 = now.AddDays(-16).ToString("yyyy-MM-dd HH:mm:ss");
                var response2 = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsagePeriod",
                    $"?time={startTime2}", services).ConfigureAwait(false);

                // ASSERT-2
                var result2 = (JObject)JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(response2)));
                Assert.AreEqual("WebHook", result2["DataType"].Value<string>());
                Assert.AreEqual(new DateTime(2021, 5, 1, 0, 0, 0, DateTimeKind.Utc), result2["Start"].Value<DateTime>());
                Assert.AreEqual(new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc), result2["End"].Value<DateTime>());
                Assert.AreEqual("Month", result2["TimeWindow"].Value<string>());
                Assert.AreEqual("Day", result2["Resolution"].Value<string>());
                callCounts = ((JArray)result1["CallCount"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, callCounts.Length);
                AssertSequenceEqual(new[] { 0L, 86400, 0, 86400, 0 }, callCounts.Take(5));
                requestLengths = ((JArray)result1["RequestLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, requestLengths.Length);
                AssertSequenceEqual(new[] { 0L, 8640000, 0, 8640000, 0 }, requestLengths.Take(5));
                responseLengths = ((JArray)result1["ResponseLengths"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, responseLengths.Length);
                AssertSequenceEqual(new[] { 0L, 86400000, 0, 86400000, 0 }, responseLengths.Take(5));
                status100 = ((JArray)result1["Status100"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, status100.Length);
                AssertSequenceEqual(new[] { 0L, 0, 0, 0, 0 }, status100.Take(5));
                status200 = ((JArray)result1["Status200"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, status200.Length);
                AssertSequenceEqual(new[] { 0L, 69120, 0, 69120, 0 }, status200.Take(5));
                status300 = ((JArray)result1["Status300"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, status300.Length);
                AssertSequenceEqual(new[] { 0L, 0, 0, 0, 0 }, status300.Take(5));
                status400 = ((JArray)result1["Status400"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, status400.Length);
                AssertSequenceEqual(new[] { 0L, 8640, 0, 8640, 0 }, status400.Take(5));
                status500 = ((JArray)result1["Status500"]).Select(x => x.Value<long>()).ToArray();
                Assert.AreEqual(30, status500.Length);
                AssertSequenceEqual(new[] { 0L, 8640, 0, 8640, 0 }, status500.Take(5));

            }).ConfigureAwait(false);
        }
    }
}
