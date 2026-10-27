using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Tests.Core.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class RepositoryLifecycleTests : TestBase
    {
        private sealed class LifecycleService : ISnService
        {
            public Exception StartError { get; set; }
            public Exception ShutdownError { get; set; }
            public Action OnShutdown { get; set; }
            public int StartCount { get; private set; }
            public int ShutdownCount { get; private set; }

            public bool Start()
            {
                StartCount++;
                Assert.IsFalse(RepositoryInstance.IsReusable);
                if (StartError != null)
                    throw StartError;
                return true;
            }

            public void Shutdown()
            {
                ShutdownCount++;
                OnShutdown?.Invoke();
                if (ShutdownError != null)
                    throw ShutdownError;
            }
        }

        private sealed class LifecycleLogger : ILogger<RepositoryInstance>
        {
            public List<string> Messages { get; } = new List<string>();
            public Exception Error { get; set; }
            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception exception, Func<TState, Exception, string> formatter)
            {
                if (Error != null)
                    throw Error;
                Messages.Add(formatter(state, exception));
            }
        }

        protected override void CleanupTest()
        {
            Repository.Shutdown();
        }

        [TestMethod]
        public void Repository_ReuseAfterSuccessfulShutdown()
        {
            var builder = CreateRepositoryBuilderForTest();
            RepositoryInstance previousInstance;
            int folderId;
            using (previousInstance = Repository.Start(builder))
            {
                Assert.IsTrue(Repository.Started());
                Assert.IsTrue(RepositoryInstance.IsReusable);
                using (new SystemAccount())
                {
                    var folder = Content.CreateNew("Folder", Repository.Root, "ReuseTestFolder");
                    folder.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    folderId = folder.Id;
                }
            }

            Assert.IsFalse(Repository.Started());
            Assert.IsTrue(RepositoryInstance.IsReusable);

            TestContext.Properties["ReusesRepository"] = true;
            var nextBuilder = CreateRepositoryBuilderForTest();
            Assert.AreSame(builder.Services, nextBuilder.Services);
            using (var instance = Repository.Start(nextBuilder))
            {
                Assert.AreNotSame(previousInstance, instance);
                previousInstance.Dispose();
                Assert.IsTrue(Repository.Started());
                Assert.IsTrue(RepositoryInstance.IsReusable);
                Assert.AreSame(instance, RepositoryInstance.Instance);
                using (new SystemAccount())
                    Assert.AreEqual(folderId, Node.LoadNode("/Root/ReuseTestFolder").Id);
            }
        }

        [TestMethod]
        public void Repository_InvalidInitialDataDoesNotPoisonNextStart()
        {
            var builder = CreateRepositoryBuilderForTest();
            var data = GetInitialData();
            var missingVersionId = data.Nodes.Skip(1).First().LastMajorVersionId;
            builder.UseInitialData(new InitialData
            {
                Schema = data.Schema,
                Nodes = data.Nodes,
                Versions = data.Versions.Where(v => v.VersionId != missingVersionId),
                DynamicProperties = data.DynamicProperties,
                ContentTypeDefinitions = data.ContentTypeDefinitions,
                Blobs = data.Blobs,
                Permissions = data.Permissions
            });

            Assert.ThrowsException<NotSupportedException>(() => Repository.Start(builder));

            AssertCanRestart(builder.Services);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Repository_PartialStartupDoesNotPoisonNextStart(bool shutdownFails)
        {
            var startupError = new InvalidOperationException("Startup failed.");
            var shutdownError = new InvalidOperationException("Shutdown failed.");
            var startedService = new LifecycleService();
            var failingService = new LifecycleService
            {
                StartError = startupError,
                ShutdownError = shutdownFails ? shutdownError : null
            };
            var pendingService = new LifecycleService();
            var builder = CreateRepositoryBuilderForTest(services => services
                .AddSingleton<ISnService>(startedService)
                .AddSingleton<ISnService>(failingService)
                .AddSingleton<ISnService>(pendingService));

            var actual = Assert.ThrowsException<InvalidOperationException>(() => Repository.Start(builder));

            Assert.AreSame(startupError, actual);
            Assert.AreEqual(1, startedService.ShutdownCount);
            Assert.AreEqual(1, failingService.ShutdownCount);
            Assert.AreEqual(0, pendingService.StartCount);
            Assert.AreEqual(0, pendingService.ShutdownCount);
            if (shutdownFails)
                Assert.AreSame(shutdownError, actual.Data["RepositoryShutdownException"]);
            AssertCanRestart(builder.Services);
        }

        [TestMethod]
        public void Repository_PostStartupFailureDoesNotPoisonNextStart()
        {
            var startupError = new InvalidOperationException("Client store initialization failed.");
            var service = new LifecycleService();
            var builder = CreateRepositoryBuilderForTest(services => services
                .AddSingleton<ISnService>(service)
                .AddSingleton<ClientStore>(_ =>
                {
                    Assert.IsTrue(Repository.Started());
                    Assert.IsFalse(RepositoryInstance.IsReusable);
                    throw startupError;
                }));

            var actual = Assert.ThrowsException<InvalidOperationException>(() => Repository.Start(builder));

            Assert.AreSame(startupError, actual);
            Assert.AreEqual(1, service.ShutdownCount);
            AssertCanRestart(builder.Services);
        }

        [TestMethod]
        public void Repository_ShutdownFailureDoesNotPoisonNextStart()
        {
            using var console = new StringWriter();
            var logger = new LifecycleLogger();
            var shutdownError = new InvalidOperationException("Shutdown failed.");
            var service = new LifecycleService();
            var failingService = new LifecycleService
            {
                ShutdownError = shutdownError,
                OnShutdown = () =>
                {
                    CollectionAssert.Contains(logger.Messages, "Shutting down LifecycleService.");
                    StringAssert.Contains(console.ToString(), "Shutting down LifecycleService.");
                }
            };
            var builder = CreateRepositoryBuilderForTest(services => services
                .AddSingleton<ISnService>(service)
                .AddSingleton<ISnService>(failingService)
                .AddSingleton<ILogger<RepositoryInstance>>(logger));
            builder.SetConsole(console);
            var instance = Repository.Start(builder);

            var actual = Assert.ThrowsException<InvalidOperationException>(() => instance.Dispose());

            Assert.AreSame(shutdownError, actual);
            Assert.AreEqual(1, service.ShutdownCount);
            Assert.AreEqual(1, failingService.ShutdownCount);
            foreach (var message in new[] { "Shutting down IndexingEngine.", "Shutting down Security.",
                         "Unregistering AppDomain event handlers.", "Resetting ContextHandler.", "Updating repository status." })
            {
                CollectionAssert.Contains(logger.Messages, message);
                StringAssert.Contains(console.ToString(), message);
            }
            AssertCanRestart(builder.Services);
        }

        [TestMethod]
        public void Repository_ShutdownLoggingFailureDoesNotSkipCleanup()
        {
            using var console = new StringWriter();
            var logger = new LifecycleLogger();
            var service = new LifecycleService();
            var builder = CreateRepositoryBuilderForTest(services => services
                .AddSingleton<ISnService>(service)
                .AddSingleton<ILogger<RepositoryInstance>>(logger));
            builder.SetConsole(console);
            var instance = Repository.Start(builder);
            var loggingError = new InvalidOperationException("Logging failed.");
            logger.Error = loggingError;

            var actual = Assert.ThrowsException<AggregateException>(() => instance.Dispose());

            Assert.IsTrue(actual.InnerExceptions.All(error => ReferenceEquals(error, loggingError)));
            Assert.AreEqual(1, service.ShutdownCount);
            StringAssert.Contains(console.ToString(), "Shutting down LifecycleService.");
            StringAssert.Contains(console.ToString(), "Shutting down Security.");
            AssertCanRestart(builder.Services);
        }

        [TestMethod]
        public void Repository_SettingsOverloadCompletesStartup()
        {
            var builder = CreateRepositoryBuilderForTest();
            using (Repository.Start(builder)) { }

            using (Repository.Start((RepositoryStartSettings)builder))
            {
                Assert.IsTrue(Repository.Started());
                Assert.IsTrue(RepositoryInstance.IsReusable);
            }
        }

        [TestMethod]
        public void Repository_NestedStartDoesNotCompleteOuterStartup()
        {
            RepositoryBuilder builder = null;
            builder = CreateRepositoryBuilderForTest(services => services.AddSingleton(provider =>
            {
                // Installers can start the repository again during the outer startup.
                Repository.Start((RepositoryStartSettings)builder);
                Assert.IsTrue(Repository.Started());
                Assert.IsFalse(RepositoryInstance.IsReusable);
                return new ClientStore(provider.GetRequiredService<IClientStoreDataProvider>(),
                    provider.GetRequiredService<IOptions<ClientStoreOptions>>(),
                    provider.GetRequiredService<ILogger<ClientStore>>());
            }));

            using (Repository.Start(builder))
                Assert.IsTrue(RepositoryInstance.IsReusable);
        }

        private void AssertCanRestart(IServiceProvider failedServices)
        {
            Assert.IsFalse(Repository.Started());
            Assert.IsFalse(RepositoryInstance.IsReusable);
            Assert.IsNull(RepositoryInstance.Instance);

            TestContext.Properties["ReusesRepository"] = true;
            var builder = CreateRepositoryBuilderForTest();
            Assert.AreNotSame(failedServices, builder.Services);
            Assert.IsFalse(RepositoryInstance.IsReusable);
            using (Repository.Start(builder))
            {
                Assert.IsTrue(Repository.Started());
                Assert.IsTrue(RepositoryInstance.IsReusable);
                Assert.IsNotNull(SystemAccount.Execute(() => Repository.Root));
            }
        }
    }
}
