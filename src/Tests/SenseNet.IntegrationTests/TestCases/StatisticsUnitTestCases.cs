using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
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
        private IStatisticalDataProvider DP => _dp ??= Providers.Instance.GetProvider<IStatisticalDataProvider>();

        // ReSharper disable once InconsistentNaming
        private ITestingDataProviderExtension TDP => _tdp ??= Providers.Instance.GetProvider<ITestingDataProviderExtension>();

        public async Task Stat_DataProvider_WriteData()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await TDP.DeleteAllStatisticalDataAsync(DP);
                var now = DateTime.UtcNow;
                var record1 = new StatisticalDataRecord
                {
                    Id = 0,
                    DataType = "DataType1",
                    WrittenTime = DateTime.MinValue,
                    CreationTime = now.AddMilliseconds(-11),
                    Duration = TimeSpan.FromMilliseconds(10),
                    RequestLength = 100,
                    ResponseLength = 1000,
                    ResponseStatusCode = 201,
                    Url = "Url1",
                    TargetId = 4242,
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
                    CreationTime = null,
                    Duration = null,
                    RequestLength = null,
                    ResponseLength = null,
                    ResponseStatusCode = null,
                    Url = null,
                    TargetId = null,
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
                Assert.IsTrue(loaded1.WrittenTime > loaded1.CreationTime);
                Assert.AreEqual(TimeSpan.FromMilliseconds(10), loaded1.Duration);
                Assert.AreEqual(record1.RequestLength, loaded1.RequestLength);
                Assert.AreEqual(record1.ResponseLength, loaded1.ResponseLength);
                Assert.AreEqual(record1.ResponseStatusCode, loaded1.ResponseStatusCode);
                Assert.AreEqual(record1.Url, loaded1.Url);
                Assert.AreEqual(record1.TargetId, loaded1.TargetId);
                Assert.AreEqual(record1.ContentId, loaded1.ContentId);
                Assert.AreEqual(record1.EventName, loaded1.EventName);
                Assert.AreEqual(record1.ErrorMessage, loaded1.ErrorMessage);
                Assert.AreEqual(record1.GeneralData, loaded1.GeneralData);

                var loaded2 = loadedRecords[1];
                Assert.AreNotSame(record2, loaded2);
                Assert.AreNotEqual(loaded1.Id, loaded2.Id);
                Assert.AreNotEqual(record2.Id, loaded2.Id);
                Assert.AreEqual(record2.DataType, loaded2.DataType);
                Assert.IsNotNull(loaded2.CreationTime);
                Assert.IsNull(loaded2.Duration);
                Assert.IsNull(loaded2.RequestLength);
                Assert.IsNull(loaded2.ResponseLength);
                Assert.IsNull(loaded2.ResponseStatusCode);
                Assert.IsNull(loaded2.Url);
                Assert.IsNull(loaded2.TargetId);
                Assert.IsNull(loaded2.ContentId);
                Assert.IsNull(loaded2.EventName);
                Assert.IsNull(loaded2.ErrorMessage);
                Assert.IsNull(loaded2.GeneralData);
            });
        }
        public async Task Stat_DataProvider_EnumerateData()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await TDP.DeleteAllStatisticalDataAsync(DP);

                var now = new DateTime(2020, 01, 01, 0, 0, 0);
                for (var i = 0; i < 30; i++) // more than 2 month (1 records per day)
                {
                    await DP.WriteDataAsync(new StatisticalDataRecord
                    {
                        DataType = $"DataType{(i % 2) + 1}",
                        CreationTime = now,
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
        public async Task Stat_DataProvider_WriteAggregation()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await TDP.DeleteAllStatisticalDataAsync(DP);

                // ACTION
                var now = new DateTime(2020, 01, 01, 0, 0, 0);
                for (var i = 0; i < 4; i++)
                {
                    await DP.WriteAggregationAsync(new Aggregation
                            {DataType = $"DataType{i}", Date = now, Resolution = (TimeResolution) i, Data = $"Data{i}"},
                        CancellationToken.None);
                }
                
                // ASSERT
                var aggregations = (await TDP.LoadAllStatisticalDataAggregations(DP)).ToArray();
                Assert.AreEqual(4, aggregations.Length);
                for (var i = 0; i < 4; i++)
                {
                    Assert.AreEqual($"DataType{i}", aggregations[i].DataType);
                    Assert.AreEqual(now, aggregations[i].Date);
                    Assert.AreEqual((TimeResolution)i, aggregations[i].Resolution);
                    Assert.AreEqual($"Data{i}", aggregations[i].Data);
                }
            });
        }
        public async Task Stat_DataProvider_OverwriteAggregation()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await TDP.DeleteAllStatisticalDataAsync(DP);

                // ACTION
                var now = new DateTime(2020, 01, 01, 0, 0, 0);
                for (var i = 0; i < 4; i++)
                {
                    await DP.WriteAggregationAsync(new Aggregation
                            { DataType = $"DataType{i}", Date = now, Resolution = (TimeResolution)i, Data = $"Data{i}" },
                        CancellationToken.None);
                    // Write a modified version
                    await DP.WriteAggregationAsync(new Aggregation
                            { DataType = $"DataType{i}", Date = now, Resolution = (TimeResolution)i, Data = $"Data{i}-updated" },
                        CancellationToken.None);
                }

                // ASSERT (8 writes but 4 existing records)
                var aggregations = (await TDP.LoadAllStatisticalDataAggregations(DP)).ToArray();
                Assert.AreEqual(4, aggregations.Length);
                for (var i = 0; i < 4; i++)
                {
                    Assert.AreEqual($"DataType{i}", aggregations[i].DataType);
                    Assert.AreEqual(now, aggregations[i].Date);
                    Assert.AreEqual((TimeResolution)i, aggregations[i].Resolution);
                    Assert.AreEqual($"Data{i}-updated", aggregations[i].Data);
                }
            });
        }
        public async Task Stat_DataProvider_LoadAggregations()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await TDP.DeleteAllStatisticalDataAsync(DP);

                var now = new DateTime(2010, 01, 01, 0, 0, 0);
                for (var j = 0; j < 30; j++)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        await DP.WriteAggregationAsync(new Aggregation
                                { DataType = $"DT{j % 3}", Date = now, Resolution = (TimeResolution)i, Data = $"{j * 4 + i}" },
                            CancellationToken.None);
                    }
                    now = now.AddMonths(1);
                }

                var start = new DateTime(2011, 01, 1, 0, 0, 0);
                var end = new DateTime(2012, 01, 1, 0, 0, 0);

                for (var j = 0; j < 3; j++)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        // ACTION
                        var aggregations = (await DP.LoadAggregatedUsageAsync("DT" + j, (TimeResolution)i, start, end,
                            CancellationToken.None).ConfigureAwait(false)).ToArray();

                        // ASSERT
                        var q = (12 + j) * 4 + i;
                        var expected = $"{q} {q + 12} {q + 24} {q + 36}";
                        var actual = string.Join(" ", aggregations.Select(x => x.Data));
                        Assert.AreEqual(expected, actual);
                    }
                    now = now.AddMonths(1);
                }
            });
        }
        public async Task Stat_DataProvider_CleanupRecords()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await TDP.DeleteAllStatisticalDataAsync(DP);

                var start = new DateTime(2020, 1, 1, 0, 10, 0);

                var retentionTime = start.AddMinutes(5);
                for (int i = 0; i < 10; i++)
                {
                    await DP.WriteDataAsync(new StatisticalDataRecord
                    {
                        DataType = "DT1",
                        CreationTime = start.AddMinutes(i),
                        GeneralData = ""
                    }, CancellationToken.None);
                    await DP.WriteDataAsync(new StatisticalDataRecord
                    {
                        DataType = "DT2",
                        CreationTime = start.AddMinutes(i),
                        GeneralData = ""
                    }, CancellationToken.None);
                }

                // ACTION
                await DP.CleanupRecordsAsync("DT1", retentionTime, CancellationToken.None);

                // ASSERT
                var loadedRecords = (await TDP.LoadAllStatisticalDataRecords(DP)).ToArray();
                var actual = string.Join(",", loadedRecords
                    .Select(r => $"{r.DataType}-{r.CreationTime.Value.Minute}").ToArray());
                Assert.AreEqual("DT2-10,DT2-11,DT2-12,DT2-13,DT2-14," +
                                "DT1-15,DT2-15,DT1-16,DT2-16,DT1-17,DT2-17,DT1-18,DT2-18,DT1-19,DT2-19", actual);
            });
        }
        public async Task Stat_DataProvider_CleanupAggregations()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await TDP.DeleteAllStatisticalDataAsync(DP);

                var start = new DateTime(2020, 01, 01, 0, 0, 0);
                var retentionTime = start.AddMinutes(1);
                for (int minute = 0; minute < 2; minute++)
                {
                    for (int resolution = 0; resolution < 4; resolution++)
                    {
                        for (int dataType = 0; dataType < 2; dataType++)
                        {
                            await DP.WriteAggregationAsync(new Aggregation
                                {
                                    DataType = $"DT{dataType}",
                                    Date = start.AddMinutes(minute),
                                    Resolution = (TimeResolution)resolution,
                                },
                                CancellationToken.None);
                        }
                    }
                }
                var aggregationsBefore = (await TDP.LoadAllStatisticalDataAggregations(DP)).ToArray();
                Assert.AreEqual(16, aggregationsBefore.Length);

                // ACTION
                await DP.CleanupAggregationsAsync("DT0", TimeResolution.Day, retentionTime, CancellationToken.None)
                    .ConfigureAwait(false);

                // ASSERT
                var aggregationsAfter = (await TDP.LoadAllStatisticalDataAggregations(DP)).ToArray();
                Assert.AreEqual(15, aggregationsAfter.Length);
                Assert.IsFalse(aggregationsAfter.Any(x =>
                    x.DataType == "DT0" && x.Resolution == TimeResolution.Day && x.Date.Minute == 0));

            });
        }
        public async Task Stat_DataProvider_LoadUsageList()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await TDP.DeleteAllStatisticalDataAsync(DP);

                var startTime = new DateTime(2020, 01, 01, 0, 0, 0);
                var now = startTime;
                for (var i = 0; i < 20; i++)
                {
                    await DP.WriteDataAsync(new StatisticalDataRecord
                    {
                        DataType = $"DT{(i % 2) + 1}",
                        CreationTime = now,
                        GeneralData = now.Minute.ToString()
                    }, CancellationToken.None);

                    now = now.AddMinutes(1 + i); // continuously slowing periods
                }


                for (int dt = 1; dt <= 2; dt++)
                {
                    var dataType = $"DT{dt}";
                    var endTime = DateTime.UtcNow;
                    IStatisticalDataRecord lastRecord;

                    // ACTION (get all records with paged queries)
                    var allRecords = new List<IStatisticalDataRecord>();
                    var pageLengths = new List<int>();
                    while (true)
                    {
                        var page =
                            (await DP.LoadUsageListAsync(dataType, new int[0], endTime, 4, CancellationToken.None)
                            .ConfigureAwait(false)).ToArray();
                        lastRecord = page.LastOrDefault();
                        if (lastRecord == null)
                            break;
                        pageLengths.Add(page.Length);
                        allRecords.AddRange(page);
                        endTime = lastRecord.CreationTime ?? lastRecord.WrittenTime;
                    }

                    // ASSERT
                    Assert.AreEqual(10, allRecords.Count);
                    Assert.AreEqual("4 4 2", string.Join(" ", pageLengths.Select(x=>x.ToString())));
                    for (var i = 0; i < 10; i++)
                    {
                        Assert.AreEqual(dataType, allRecords[i].DataType);
                        if (i < 9)
                            Assert.IsTrue(allRecords[i].CreationTime > allRecords[i + 1].CreationTime);
                    }
                }
            });
        }
        public async Task Stat_DataProvider_LoadUsageListByTargetId()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await TDP.DeleteAllStatisticalDataAsync(DP);

                var startTime = new DateTime(2020, 01, 01, 0, 0, 0);
                var now = startTime;
                for (var i = 0; i < 20; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        await DP.WriteDataAsync(new StatisticalDataRecord
                        {
                            DataType = $"DT{(i % 2) + 1}",
                            CreationTime = now,
                            TargetId = (j > 0) ? j + 1000 : (int?) null,
                            GeneralData = now.Minute.ToString()
                        }, CancellationToken.None);

                    }
                    now = now.AddMinutes(1);
                }

                // ACTION: without target filter
                var records = (await DP.LoadUsageListAsync(
                    "DT1", null, DateTime.UtcNow, 40000,
                    CancellationToken.None).ConfigureAwait(false)).ToArray();

                // ASSERT
                Assert.AreEqual(40, records.Length);

                AssertSequenceEqual(new int?[]{null, 1001, 1002, 1003},
                    records.Select(x => x.TargetId).Distinct().OrderBy(x => x ?? 0));

                for (var i = 0; i < 40; i++)
                {
                    Assert.AreEqual("DT1", records[i].DataType);
                    if (i < 39)
                        Assert.IsTrue(records[i].CreationTime >= records[i + 1].CreationTime);
                }

                // ACTION: one target
                records = (await DP.LoadUsageListAsync(
                    "DT1", new[] { 1001 }, DateTime.UtcNow, 40000,
                    CancellationToken.None).ConfigureAwait(false)).ToArray();

                // ASSERT
                Assert.AreEqual(10, records.Length);
                for (var i = 0; i < 10; i++)
                {
                    Assert.AreEqual(1001, records[i].TargetId);
                    Assert.AreEqual("DT1", records[i].DataType);
                    if (i < 9)
                        Assert.IsTrue(records[i].CreationTime > records[i + 1].CreationTime);
                }

                // ACTION: two target
                records = (await DP.LoadUsageListAsync(
                    "DT1", new[] { 1001, 1002 }, DateTime.UtcNow, 40000,
                    CancellationToken.None).ConfigureAwait(false)).ToArray();

                // ASSERT
                Assert.AreEqual(20, records.Length);
                AssertSequenceEqual(new int?[] { 1001, 1002 },
                    records.Select(x => x.TargetId).Distinct().OrderBy(x => x ?? 0));
                for (var i = 0; i < 20; i++)
                {
                    Assert.AreEqual("DT1", records[i].DataType);
                    if (i < 19)
                        Assert.IsTrue(records[i].CreationTime >= records[i + 1].CreationTime);
                }

                // ACTION: two target but only one exists
                records = (await DP.LoadUsageListAsync(
                    "DT1", new[] { 1001, 9999 }, DateTime.UtcNow, 40000,
                    CancellationToken.None).ConfigureAwait(false)).ToArray();

                // ASSERT
                Assert.AreEqual(10, records.Length);
                for (var i = 0; i < 10; i++)
                {
                    Assert.AreEqual(1001, records[i].TargetId);
                    Assert.AreEqual("DT1", records[i].DataType);
                    if (i < 9)
                        Assert.IsTrue(records[i].CreationTime > records[i + 1].CreationTime);
                }
            });
        }
        public async Task LoadFirstAggregationTimesByResolutions()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                // ALIGN-1
                await TDP.DeleteAllStatisticalDataAsync(DP);
                var now = new DateTime(2010, 01, 01, 0, 0, 0);
                for (var j = 0; j < 3; j++)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        await DP.WriteAggregationAsync(new Aggregation
                                { DataType = $"DT{j}", Date = now, Resolution = (TimeResolution)i, Data = $"{j * 4 + i}" },
                            CancellationToken.None);
                        now = now.AddDays(1);
                    }
                }

                // ACTION-1 (DataType does not exist)
                var dates = await DP.LoadFirstAggregationTimesByResolutionsAsync("DT9", CancellationToken.None)
                    .ConfigureAwait(false);
                // ASSERT-1
                Assert.AreEqual(4, dates.Length);
                Assert.IsNull(dates[0]);
                Assert.IsNull(dates[1]);
                Assert.IsNull(dates[2]);
                Assert.IsNull(dates[3]);

                // ACTION-2
                dates = await DP.LoadFirstAggregationTimesByResolutionsAsync("DT1", CancellationToken.None)
                    .ConfigureAwait(false);

                // ASSERT-2
                Assert.AreEqual(4, dates.Length);
                Assert.IsNotNull(dates[0]);
                Assert.AreEqual("2010-01-05", dates[0].Value.ToString("yyyy-MM-dd"));
                Assert.IsNotNull(dates[1]);
                Assert.AreEqual("2010-01-06", dates[1].Value.ToString("yyyy-MM-dd"));
                Assert.IsNotNull(dates[2]);
                Assert.AreEqual("2010-01-07", dates[2].Value.ToString("yyyy-MM-dd"));
                Assert.IsNotNull(dates[3]);
                Assert.AreEqual("2010-01-08", dates[3].Value.ToString("yyyy-MM-dd"));

                //-----------------------------------------------------------------------------------------------

                // ALIGN-2 (one resolution has no data)
                await TDP.DeleteAllStatisticalDataAsync(DP);
                now = new DateTime(2010, 01, 01, 0, 0, 0);
                for (var j = 0; j < 3; j++)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        if (i != 1)
                        {
                            await DP.WriteAggregationAsync(new Aggregation
                            {
                                DataType = $"DT{j}", Date = now, Resolution = (TimeResolution) i, Data = $"{j * 4 + i}"
                            }, CancellationToken.None);
                        }
                        now = now.AddDays(1);
                    }
                }

                // ACTION-3
                dates = await DP.LoadFirstAggregationTimesByResolutionsAsync("DT1", CancellationToken.None)
                    .ConfigureAwait(false);

                // ASSERT-3
                Assert.AreEqual(4, dates.Length);
                Assert.IsNotNull(dates[0]);
                Assert.AreEqual("2010-01-05", dates[0].Value.ToString("yyyy-MM-dd"));
                Assert.IsNull(dates[1]);
                Assert.IsNotNull(dates[2]);
                Assert.AreEqual("2010-01-07", dates[2].Value.ToString("yyyy-MM-dd"));
                Assert.IsNotNull(dates[3]);
                Assert.AreEqual("2010-01-08", dates[3].Value.ToString("yyyy-MM-dd"));
            });
        }
        public async Task LoadLastAggregationTimesByResolutions()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                // ALIGN (one resolution has no data)
                await TDP.DeleteAllStatisticalDataAsync(DP);
                var now = new DateTime(2010, 01, 01, 0, 0, 0);
                for (var j = 0; j < 3; j++)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        if (i != 1)
                        {
                            await DP.WriteAggregationAsync(new Aggregation
                            {
                                DataType = $"DT{j}",
                                Date = now,
                                Resolution = (TimeResolution) i,
                                Data = $"{j * 4 + i}"
                            }, CancellationToken.None);
                        }

                        now = now.AddDays(1);
                    }
                }

                // ACTION
                var dates = await DP.LoadLastAggregationTimesByResolutionsAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual(4, dates.Length);
                Assert.IsNotNull(dates[0]);
                Assert.AreEqual("2010-01-09", dates[0].Value.ToString("yyyy-MM-dd"));
                Assert.IsNull(dates[1]);
                Assert.IsNotNull(dates[2]);
                Assert.AreEqual("2010-01-11", dates[2].Value.ToString("yyyy-MM-dd"));
                Assert.IsNotNull(dates[3]);
                Assert.AreEqual("2010-01-12", dates[3].Value.ToString("yyyy-MM-dd"));
            });
        }
    }
}
