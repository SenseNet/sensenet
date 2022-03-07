using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tests.Core;

namespace SenseNet.IntegrationTests.Infrastructure
{
    public abstract class IntegrationTest<TPlatform, TTestCase>
        where TPlatform : IPlatform, new()
        where TTestCase : TestCaseBase, new()
    {
        // Injected by MsTest framework.
        public TestContext TestContext { get; set; }

        protected virtual bool ReusesRepository => true;

        protected TPlatform Platform { get; }
        protected TTestCase TestCase { get; }

        protected IntegrationTest()
        {
            Platform = new TPlatform();
            TestCase = new TTestCase { Platform = Platform };
        }

        [TestInitialize]
        public void _initializeTest()
        {
            TestContext.StartTest(traceToFile: false, reusesRepository: ReusesRepository);
        }
        [TestCleanup]
        public void _cleanupTest()
        {
            TestContext.FinishTestTest();
        }
    }
}
