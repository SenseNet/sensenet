using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Tests.Core.Implementations;

namespace SenseNet.IntegrationTests.TestCases
{
    public class StatisticsUnitTestCases : TestCaseBase
    {
        private IStatisticalDataProvider _dp;
        private ITestingDataProviderExtension _tdp;

        // ReSharper disable once InconsistentNaming
        private IStatisticalDataProvider DP => _dp ??= Providers.Instance.DataProvider.GetExtension<IStatisticalDataProvider>();

        // ReSharper disable once InconsistentNaming
        private ITestingDataProviderExtension TDP => _tdp ??= Providers.Instance.DataProvider.GetExtension<ITestingDataProviderExtension>();

        public async Task Stat_DataProvider_WriteData()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await TDP.DeleteAllStatisticalDataRecordsAsync(DP);
                var now = DateTime.UtcNow;
                var record1 = new StatisticalDataRecord
                {
                    Id = 0,
                    DataType = "DataType1",
                    WrittenTime = DateTime.MinValue,
                    RequestTime = now.AddMilliseconds(-11),
                    ResponseTime = now.AddMilliseconds(-1),
                    RequestLength = 100,
                    ResponseLength = 1000,
                    ResponseStatusCode = 201,
                    Url = "Url1",
                    WebHookId = 4242,
                    ContentId = 4243,
                    EventName = "Event1",
                    ErrorMessage = "ErrorMessage1",
                    GeneralData = "GeneralData1"
                };
                var record2 = new StatisticalDataRecord
                {
                    Id = 0,
                    DataType = "DataType2",
                    WrittenTime = DateTime.MinValue,
                    RequestTime = null,
                    ResponseTime = null,
                    RequestLength = null,
                    ResponseLength = null,
                    ResponseStatusCode = null,
                    Url = null,
                    WebHookId = null,
                    ContentId = null,
                    EventName = null,
                    ErrorMessage = null,
                    GeneralData = null
                };

                // ACTION
                await DP.WriteDataAsync(record1, CancellationToken.None);
                await DP.WriteDataAsync(record2, CancellationToken.None);

                // ASSERT
                var loadedRecords = (await TDP.LoadAllStatisticalDataRecords(DP)).ToArray();
                Assert.AreEqual(2, loadedRecords.Length);
                var loaded1 = loadedRecords[0];
                Assert.AreNotSame(record1, loaded1);
                Assert.AreNotEqual(record1.Id, loaded1.Id);
                Assert.AreEqual(record1.DataType, loaded1.DataType);
                Assert.IsTrue(loaded1.WrittenTime > loaded1.RequestTime);
                Assert.IsTrue(loaded1.RequestTime.HasValue);
                Assert.IsTrue(loaded1.ResponseTime.HasValue);
                Assert.AreEqual(loaded1.RequestTime.Value, loaded1.ResponseTime.Value.AddMilliseconds(-10));
                Assert.AreEqual(record1.RequestLength, loaded1.RequestLength);
                Assert.AreEqual(record1.ResponseLength, loaded1.ResponseLength);
                Assert.AreEqual(record1.ResponseStatusCode, loaded1.ResponseStatusCode);
                Assert.AreEqual(record1.Url, loaded1.Url);
                Assert.AreEqual(record1.WebHookId, loaded1.WebHookId);
                Assert.AreEqual(record1.ContentId, loaded1.ContentId);
                Assert.AreEqual(record1.EventName, loaded1.EventName);
                Assert.AreEqual(record1.ErrorMessage, loaded1.ErrorMessage);
                Assert.AreEqual(record1.GeneralData, loaded1.GeneralData);

                var loaded2 = loadedRecords[1];
                Assert.AreNotSame(record2, loaded2);
                Assert.AreNotEqual(loaded1.Id, loaded2.Id);
                Assert.AreNotEqual(record2.Id, loaded2.Id);
                Assert.AreEqual(record2.DataType, loaded2.DataType);
                Assert.IsNull(loaded2.RequestTime);
                Assert.IsNull(loaded2.ResponseTime);
                Assert.IsNull(loaded2.RequestLength);
                Assert.IsNull(loaded2.ResponseLength);
                Assert.IsNull(loaded2.ResponseStatusCode);
                Assert.IsNull(loaded2.Url);
                Assert.IsNull(loaded2.WebHookId);
                Assert.IsNull(loaded2.ContentId);
                Assert.IsNull(loaded2.EventName);
                Assert.IsNull(loaded2.ErrorMessage);
                Assert.IsNull(loaded2.GeneralData);
            });
        }
        public async Task Stat_DataProvider_EnumerateData_ByRequestTime()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await TDP.DeleteAllStatisticalDataRecordsAsync(DP);

                var now = new DateTime(2020, 01, 01, 0, 0, 0);
                for (var i = 0; i < 30; i++) // more than 2 month (1 records per day)
                {
                    await DP.WriteDataAsync(new StatisticalDataRecord
                    {
                        DataType = $"DataType{(i % 2) + 1}",
                        RequestTime = now,
                        GeneralData = now.Day.ToString()
                    }, CancellationToken.None);

                    now = now.AddDays(1);
                }

                // ACTION
                var list1 = new List<string>();
                var list2 = new List<string>();
                var start = new DateTime(2020, 01, 11, 0, 0, 0);
                var end = new DateTime(2020, 01, 21, 0, 0, 0);
                await DP.EnumerateDataAsync("DataType1", start, end, record =>
                {
                    list1.Add(record.GeneralData);
                }, CancellationToken.None);
                await DP.EnumerateDataAsync("DataType2", start, end, record =>
                {
                    list2.Add(record.GeneralData);
                }, CancellationToken.None);

                // ASSERT
                Assert.AreEqual("11 13 15 17 19", string.Join(" ", list1));
                Assert.AreEqual("12 14 16 18 20", string.Join(" ", list2));
            });
        }
    }
}
