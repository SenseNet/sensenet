using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemDbUsageTests : IntegrationTest<InMemPlatform, DbUsageTests>
    {
        [TestMethod]
        public void IntT_InMem_DbUsage_PreviewsVersionsBlobsTexts()
        {
            TestCase.DbUsage_PreviewsVersionsBlobsTexts().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
