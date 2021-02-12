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
        public void IntT_InMem_DbUsage_CheckPreviewStructure()
        {
            TestCase.DbUsage_Previews().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
