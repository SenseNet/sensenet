using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlStatisticsUnitTests : IntegrationTest<MsSqlPlatform, StatisticsUnitTestCases>
    {
        [TestMethod] public async Task UT_MsSql_Stat_DataProvider_WriteData() { await TestCase.Stat_DataProvider_WriteData().ConfigureAwait(false); }
        [TestMethod] public async Task UT_MsSql_Stat_DataProvider_EnumerateData() { await TestCase.Stat_DataProvider_EnumerateData().ConfigureAwait(false); }
        [TestMethod] public async Task UT_MsSql_Stat_DataProvider_WriteAggregation() { await TestCase.Stat_DataProvider_WriteAggregation().ConfigureAwait(false); }
        [TestMethod] public async Task UT_MsSql_Stat_DataProvider_LoadAggregations() { await TestCase.Stat_DataProvider_LoadAggregations().ConfigureAwait(false); }
    }
}
