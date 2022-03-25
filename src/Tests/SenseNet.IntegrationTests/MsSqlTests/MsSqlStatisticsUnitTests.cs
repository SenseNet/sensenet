using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.IntegrationTests.Common;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;
using SenseNet.Storage.Data.MsSqlClient;
using SenseNet.Testing;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlStatisticsUnitTests : IntegrationTest<MsSqlPlatform, StatisticsUnitTestCases>
    {
        #region MsSql specific infrastructure
        private void ResetDatabase(RepositoryBuilder builder)
        {
            using (var ctx = new MsSqlDataContext(Platform.RepositoryConnectionString, DataOptions.GetLegacyConfiguration(), CancellationToken.None))
            {
                ctx.ExecuteNonQueryAsync(MsSqlStatisticalDataProvider.DropScript).GetAwaiter().GetResult();
                ctx.ExecuteNonQueryAsync(MsSqlStatisticalDataProvider.CreationScript).GetAwaiter().GetResult();
            }
        }

        [TestInitialize]
        public void InitializeTest()
        {
            TestCase.TestInitializer = ResetDatabase;
        }
        #endregion

        [TestMethod] public async Task UT_MsSql_Stat_DataProvider_WriteData() { await TestCase.Stat_DataProvider_WriteData().ConfigureAwait(false); }
        [TestMethod] public async Task UT_MsSql_Stat_DataProvider_EnumerateData() { await TestCase.Stat_DataProvider_EnumerateData().ConfigureAwait(false); }
        [TestMethod] public async Task UT_MsSql_Stat_DataProvider_WriteAggregation() { await TestCase.Stat_DataProvider_WriteAggregation().ConfigureAwait(false); }
        [TestMethod] public async Task UT_MsSql_Stat_DataProvider_OverwriteAggregation() { await TestCase.Stat_DataProvider_OverwriteAggregation().ConfigureAwait(false); }
        [TestMethod] public async Task UT_MsSql_Stat_DataProvider_LoadAggregations() { await TestCase.Stat_DataProvider_LoadAggregations().ConfigureAwait(false); }
        [TestMethod] public async Task UT_MsSql_Stat_DataProvider_CleanupRecords() { await TestCase.Stat_DataProvider_CleanupRecords().ConfigureAwait(false); }
        [TestMethod, TestCategory("Services")] public async Task UT_MsSql_Stat_DataProvider_CleanupAggregations_CSrv() { await TestCase.Stat_DataProvider_CleanupAggregations().ConfigureAwait(false); }
        [TestMethod] public async Task UT_MsSql_Stat_DataProvider_LoadUsageList() { await TestCase.Stat_DataProvider_LoadUsageList().ConfigureAwait(false); }
        [TestMethod] public async Task UT_MsSql_Stat_DataProvider_LoadUsageListByTargetId() { await TestCase.Stat_DataProvider_LoadUsageListByTargetId().ConfigureAwait(false); }
        [TestMethod] public async Task UT_MsSql_Stat_DataProvider_LoadFirstAggregationTimesByResolutions() { await TestCase.LoadFirstAggregationTimesByResolutions().ConfigureAwait(false); }
        [TestMethod] public async Task UT_MsSql_Stat_DataProvider_LoadLastAggregationTimesByResolutions() { await TestCase.LoadLastAggregationTimesByResolutions().ConfigureAwait(false); }
    }
}
