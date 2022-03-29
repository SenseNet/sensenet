using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests.Core;
using SenseNet.Tests.Core.Implementations;
using SenseNet.Tools.Diagnostics;

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

    internal class NullEventPropertyCollector : IEventPropertyCollector
    {
        public IDictionary<string, object> Collect(IDictionary<string, object> properties)
        {
            return properties;
        }
    }

    [TestClass]
    public class LoggerTests : TestBase
    {
        [TestMethod, TestCategory("Services")]
        public void Provider_Logger_Configured_CSrv()
        {
            var loggerTypeName = typeof(TestLogger).FullName;

            // backup configuration
            var providersBackup = Providers.Instance;

            // configure the logger provider and reinitialize the instance
            Providers.Instance = new Providers(new ServiceCollection().BuildServiceProvider());
            var propCollector = Providers.Instance.PropertyCollector;

            // Workaround: the default property collector tries to load the
            // current user, even if the repo is not running.
            Providers.Instance.DataProvider = new InMemoryDataProvider();
            Providers.Instance.PropertyCollector = new NullEventPropertyCollector();

            try
            {
                Test(builder =>
                {
                    // ACTION
                    builder.UseLogger(new TestLogger());
                },
                () =>
                {
                    Assert.AreEqual(loggerTypeName, SnLog.Instance.GetType().FullName);
                });
            }
            finally
            {
                // rollback to the original configuration
                Providers.Instance = providersBackup;
            }

            // test of the restoration: logger instance need to be the default
            Test(() =>
            {
                Assert.AreNotEqual(loggerTypeName, SnLog.Instance.GetType().FullName);
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

        [TestMethod]
        public void Logger_Audit_Default()
        {
            Test(() =>
            {
                // operations for a "content created" audit event
                var folder = new SystemFolder(Repository.Root) {Name = "Folder1"};
                folder.Save();
                var folderId = folder.Id;

                // operations for a "content modified" audit event
                folder = Node.Load<SystemFolder>(folderId);
                folder.Index++;
                folder.Save();

                // operations for a "content deleted" audit event
                folder = Node.Load<SystemFolder>(folderId);
                folder.ForceDelete();

                // load audit log entries
                var entries = Providers.Instance.GetProvider<ITestingDataProvider>().LoadLastAuditLogEntries(10);
                var relatedEntries = entries.Where(e => e.ContentId == folderId).ToArray();

                // assertions
                Assert.AreEqual(3, relatedEntries.Length);
                var messages = string.Join(", ", relatedEntries.Select(e=>e.Message).ToArray());
                Assert.AreEqual("ContentCreated, ContentUpdated, ContentDeleted", messages);
            });
        }
    }
}
