using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemAppModelTests : IntegrationTest<InMemPlatform, AppModelTestCases>
    {
        [TestMethod, TestCategory("Services")]
        public void IntT_InMem_AppModel_ResolveFromPredefinedPaths_First_CSrv() { TestCase.AppModel_ResolveFromPredefinedPaths_First(); }
        [TestMethod]
        public void IntT_InMem_AppModel_ResolveFromPredefinedPaths_All() { TestCase.AppModel_ResolveFromPredefinedPaths_All(); }
    }
}
