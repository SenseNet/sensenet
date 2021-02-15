
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlDbUsageTests : IntegrationTest<MsSqlPlatform, DbUsageTests>
    {
        [TestMethod]
        public void IntT_MsSql_DbUsage_PreviewsVersionsBlobsTexts()
        {
            TestCase.DbUsage_PreviewsVersionsBlobsTexts().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
