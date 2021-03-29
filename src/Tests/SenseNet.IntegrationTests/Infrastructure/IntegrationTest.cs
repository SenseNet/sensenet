using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Diagnostics;
using SenseNet.Testing;

namespace SenseNet.IntegrationTests.Infrastructure
{
    public static class MsTestTestContextExtensions
    {
        public static void StartTest(this TestContext testContext, bool traceToFile = false)
        {
            if (traceToFile)
            {
                var tracers =  SnTrace.SnTracers.ToArray();
                testContext.Properties["SnTrace.Operation.Writers"] = tracers;
                if(!tracers.Any(x => x is SnFileSystemTracer))
                    SnTrace.SnTracers.Add(new SnFileSystemTracer());
            }
            StartTestPrivate(testContext);
        }
        private static void StartTestPrivate(TestContext testContext)
        {
            using (new Swindler<bool>(true, () => SnTrace.Test.Enabled, x => SnTrace.Test.Enabled = x))
                testContext.Properties["SnTrace.Operation"] =
                    SnTrace.Test.StartOperation("TESTMETHOD: " + testContext.TestName);
        }
        public static void FinishTestTest(this TestContext testContext)
        {
            using (new Swindler<bool>(true, () => SnTrace.Test.Enabled, x => SnTrace.Test.Enabled = x))
            {
                var op = (SnTrace.Operation)testContext.Properties["SnTrace.Operation"];
                SnTrace.Test.Write("TESTMETHOD: {0}: {1}", testContext.TestName, testContext.CurrentTestOutcome);
                if (op != null)
                {
                    op.Successful = true;
                    op.Dispose();
                }
                SnTrace.Flush();
            }
            var originalTracers = (ISnTracer[])testContext.Properties["SnTrace.Operation.Writers"];
            if (originalTracers != null)
            {
                SnTrace.SnTracers.Clear();
                SnTrace.SnTracers.AddRange(originalTracers);
            }
        }
    }

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

        [TestInitialize]
        public void _initializeTest()
        {
            TestContext.StartTest();
        }
        [TestCleanup]
        public void _cleanupTest()
        {
            TestContext.FinishTestTest();
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
