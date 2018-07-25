using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    internal class TestLogger : IEventLogger
    {
        public void Write(object message, ICollection<string> categories, int priority, int eventId, TraceEventType severity, string title,
            IDictionary<string, object> properties)
        {
            // do nothing
        }
    }

    [TestClass]
    public class LoggerTests : TestBase
    {
        [TestMethod]
        public void Provider_Logger_Default()
        {
            Test(() =>
            {
                Assert.IsTrue(SnLog.Instance is DebugWriteLoggerAdapter);
            });
        }
        [TestMethod]
        public void Provider_Logger_Configured()
        {
            var loggerTypeName = typeof(TestLogger).FullName;

            // backup configuration
            var eventLoggerClassNameBackup = Providers.EventLoggerClassName;

            // configure the logger provider and reinitialize the instance
            Providers.EventLoggerClassName = loggerTypeName;
            Providers.Instance = new Providers();

            try
            {
                Test(() =>
                {
                    Assert.AreEqual(loggerTypeName, SnLog.Instance.GetType().FullName);
                });
            }
            finally
            {
                // rollback to the original configuration
                Providers.EventLoggerClassName = eventLoggerClassNameBackup;
                Providers.Instance = new Providers();
            }

            // test of the restoration: logger instance need to be the default
            Test(() =>
            {
                Assert.IsTrue(SnLog.Instance is DebugWriteLoggerAdapter);
            });
        }
        [TestMethod]
        public void Provider_Logger_Overridden()
        {
            var testLogger = new TestLogger();
            Test(builder => { builder.UseLogger(testLogger); }, () =>
            {
                Assert.AreSame(testLogger, SnLog.Instance);
            });
        }
    }
}
