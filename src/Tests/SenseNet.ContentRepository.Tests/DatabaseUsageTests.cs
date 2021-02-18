using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests.Core;
using EventId = Microsoft.Extensions.Logging.EventId;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class DatabaseUsageTests : TestBase
    {
        private class TestDbUsageLogger : ILogger
        {
            public LogLevel LastLevel { get; private set; }
            public string LastMessage { get; private set; }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                LastLevel = logLevel;
                LastMessage = state.ToString();
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                throw new NotImplementedException();
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                throw new NotImplementedException();
            }
        }

        private class DatabaseUsageCacheTestNodeObserver : NodeObserver
        {
            public static bool CreatingCrashEnabledOnce { get; set; }

            protected internal override void OnNodeCreating(object sender, CancellableNodeEventArgs e)
            {
                var node = e.SourceNode;
                var path = $"{node.ParentPath}/{node.Name}";
                if (path != RepositoryPath.GetParentPath(DatabaseUsageHandler.DatabaseUsageCachePath))
                    return;
                if (!CreatingCrashEnabledOnce)
                    return;
                CreatingCrashEnabledOnce = false;

                e.Cancel = true;
            }

            public static bool ModifyingCrashEnabledOnce { get; set; }
            protected internal override void OnNodeModifying(object sender, CancellableNodeEventArgs e)
            {
                var node = e.SourceNode;
                var path = $"{node.ParentPath}/{node.Name}";
                if (path != DatabaseUsageHandler.DatabaseUsageCachePath)
                    return;
                if (!ModifyingCrashEnabledOnce)
                    return;
                ModifyingCrashEnabledOnce = false;

                e.Cancel = true;
            }
        }

        [TestMethod]
        public async STT.Task DbUsage_CachedVsForced()
        {
            await Test(async () =>
            {
                var logger = new TestDbUsageLogger();

                // get initial
                var handler = new DatabaseUsageHandler(logger);
                var usage1 = await handler.GetDatabaseUsageAsync(false, CancellationToken.None);

                // wait a bit and get cached
                await STT.Task.Delay(100);
                handler = new DatabaseUsageHandler(logger);
                var usage2 = await handler.GetDatabaseUsageAsync(false, CancellationToken.None);

                // wait a bit and get fresh data
                await STT.Task.Delay(100);
                handler = new DatabaseUsageHandler(logger);
                var usage3 = await handler.GetDatabaseUsageAsync(true, CancellationToken.None);

                // wait a bit and get cached again
                await STT.Task.Delay(100);
                handler = new DatabaseUsageHandler(logger);
                var usage4 = await handler.GetDatabaseUsageAsync(false, CancellationToken.None);

                Assert.AreEqual(usage1.Executed, usage2.Executed);
                Assert.IsTrue(usage2.Executed < usage3.Executed);
                Assert.AreEqual(usage3.Executed, usage4.Executed);

            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async STT.Task DbUsage_Logger_CreatingCache()
        {
            await Test(builder =>
            {
                builder.EnableNodeObservers(typeof(DatabaseUsageCacheTestNodeObserver));
            },async () =>
            {
                var logger = new TestDbUsageLogger();
                DatabaseUsageCacheTestNodeObserver.CreatingCrashEnabledOnce = true;

                var handler = new DatabaseUsageHandler(logger);
                var _= await handler.GetDatabaseUsageAsync(false, CancellationToken.None);

                Assert.AreEqual(logger.LastLevel, LogLevel.Warning);
                Assert.IsNotNull(logger.LastMessage);
                Assert.IsTrue(logger.LastMessage.Contains("An error occured during saving DatabaseUsage.cache"));

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task DbUsage_Logger_WritingCache()
        {
            await Test(builder =>
            {
                builder.EnableNodeObservers(typeof(DatabaseUsageCacheTestNodeObserver));
            }, async () =>
            {
                var logger = new TestDbUsageLogger();
                DatabaseUsageCacheTestNodeObserver.ModifyingCrashEnabledOnce = true;

                var handler = new DatabaseUsageHandler(logger);
                var _ = await handler.GetDatabaseUsageAsync(false, CancellationToken.None);

                handler = new DatabaseUsageHandler(logger);
                _ = await handler.GetDatabaseUsageAsync(true, CancellationToken.None);

                Assert.AreEqual(logger.LastLevel, LogLevel.Warning);
                Assert.IsNotNull(logger.LastMessage);
                Assert.IsTrue(logger.LastMessage.Contains("An error occured during saving DatabaseUsage.cache"));

            }).ConfigureAwait(false);
        }
    }
}
