using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.IntegrationTests.Infrastructure
{
    public abstract class IntegrationTest<TPlatform, TTestCase>
        where TPlatform : IPlatform, new()
        where TTestCase : TestCaseBase, new()
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

        private DateTime _startTime;

        [TestInitialize]
        public void _initializeTest()
        {
            _startTime = DateTime.Now;
            Logger.Log($"    {TestContext.TestName} START");
        }
        [TestCleanup]
        public void _cleanupTest()
        {
            var duration = DateTime.Now - _startTime;
            Logger.Log($"    {TestContext.TestName} {TestContext.CurrentTestOutcome} {duration:g}");
        }

        [ClassInitialize]
        public void _initializeClass(TestContext testContext)
        {
            Logger.Log($"InitializeClass {this.GetType().Name}");
        }
        [ClassCleanup]
        public void _cleanupClass()
        {
            Logger.Log($"CleanupClass {this.GetType().Name}");
                TestCaseBase.CleanupClass();
        }
    }
}
