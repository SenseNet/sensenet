using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.Infrastructure
{
    public abstract class IntegrationTest<TPlatform, TTestCase>
        where TPlatform : IPlatform, new()
        where TTestCase : TestCase, new()
    {
        // Injected by MsTest framework.
        public TestContext TestContext { get; set; }

        protected TPlatform Platform { get; }
        protected TTestCase TestCase { get; }

        protected IntegrationTest()
        {
            Platform = new TPlatform();
            TestCase = new TTestCase { Platform = Platform };
        }

        [TestInitialize]
        public void InitializeTest()
        {
            Logger.Log($"{TestContext.TestName} START");
        }
        [TestCleanup]
        public void CleanupTest()
        {
            Logger.Log($"{TestContext.TestName} {TestContext.CurrentTestOutcome}");
        }

    }
}
