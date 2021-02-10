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
        public void IntT_InMem_DbUsage_1()
        {
            TestCase.DbUsage_1().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        [TestMethod]
        public void IntT_InMem_DbUsage_CheckPreviewStructure()
        {
            TestCase.DbUsage_CheckPreviewStructure().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        [TestMethod]
        public void IntT_InMem_DbUsage_2()
        {
            TestCase.DbUsage_2().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
