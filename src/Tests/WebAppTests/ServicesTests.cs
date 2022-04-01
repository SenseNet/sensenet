using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.BackgroundOperations;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Packaging;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.AppModel;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.Preview;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Lucene29;
using SenseNet.Search.Querying;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Security.EFCSecurityStore;
using SenseNet.Security.Messaging;
using SenseNet.Services.Core;
using SenseNet.Services.Core.Cors;
using SenseNet.Storage.Data.MsSqlClient;
using SenseNet.Storage.Diagnostics;
using SenseNet.TaskManagement.Core;
using SenseNet.Testing;
using SenseNet.Tests.Core;
using SenseNet.Tests.Core.Implementations;
using SenseNet.Tools.Diagnostics;
using SenseNet.Tools.SnInitialDataGenerator;
using BlobStorage = SenseNet.ContentRepository.Storage.Data.BlobStorage;

namespace WebAppTests
{
    [TestClass]
    public class ServicesTests : TestBase
    {
        #region Infrastructure
        private void StartupServicesTest<T>(IDictionary<Type, Type> platformSpecificExpectations, IDictionary<Type, Type> customizedExpectations)
        {
            var configurationBuilder = new ConfigurationBuilder();
            //configuration.AddJsonFile("appSettings.json")
            var configuration = configurationBuilder.Build();

            var serviceCollection = new ServiceCollection();
            var cnOptions = Options.Create<ConnectionStringOptions>(new ConnectionStringOptions { Repository = "fake" });
            serviceCollection.AddSingleton<IOptions<ConnectionStringOptions>>(cnOptions);

            //var startup = new SnWebApplication.Api.InMem.Admin.Startup(configuration);
            var ctor = typeof(T).GetConstructor(new[] {typeof(IConfiguration) });
            //var startup = (T)ctor.Invoke();

            // ACTION
            var startupAcc = new ObjectAccessor(typeof(T), new[] {typeof(IConfiguration) }, new[] { configuration });
            startupAcc.Invoke("ConfigureServices", new[] {typeof(IServiceCollection) }, new[] { serviceCollection });

            // ASSERT
            AssertServices(serviceCollection, platformSpecificExpectations, customizedExpectations);
        }

        private void AssertServices(IServiceCollection serviceCollection, IDictionary<Type, Type> platformSpecificExpectations, IDictionary<Type, Type> customizedExpectations)
        {
            AssertServices(true, serviceCollection.BuildServiceProvider(), platformSpecificExpectations, customizedExpectations);
        }
        private void AssertServices(bool useHosting, IServiceProvider services, IDictionary<Type, Type> platformSpecificExpectations, IDictionary<Type, Type> customizedExpectations = null)
        {
            if (useHosting)
            {
                var repositoryHostedService = (RepositoryHostedService)services.GetService<IEnumerable<IHostedService>>()?
                    .FirstOrDefault(x => x.GetType() == typeof(RepositoryHostedService));
                repositoryHostedService?.BuildProviders();
            }

            var expectation = GetGeneralizedExpectations();
            if(platformSpecificExpectations != null)
                foreach (var item in platformSpecificExpectations)
                    expectation.Add(item);
            if(customizedExpectations != null)
                foreach (var item in customizedExpectations)
                    expectation[item.Key] = item.Value;

            var dump = expectation.ToDictionary(
                x => x.Key,
                x => services.GetService(x.Key)?.GetType());

            Assert.AreEqual(0, dump
                .Where(x => x.Value != null)
                .Count(x => x.Value != expectation[x.Key]), GetMessageForDifferences(dump, expectation));
            Assert.AreEqual(0, dump.Values.Count(x => x == null), GetMessageForNulls(dump));

            AssertProvidersInstance();
        }
        private string GetMessageForDifferences(Dictionary<Type, Type> dump, IDictionary<Type, Type> expectation)
        {
            var diffs = dump.Where(x => x.Value != expectation[x.Key])
                .Where(x => x.Value != null)
                .ToDictionary(x => x.Key, x => (Actual: x.Value, Expected: expectation[x.Key]))
                .Select(x => $"{x.Key.Name}: actual: {x.Value.Actual?.Name ?? "null"}, expected: {x.Value.Expected.Name}");
            return $"Different services: {string.Join(", ", diffs)}";
        }
        private string GetMessageForNulls(IDictionary<Type, Type> dump)
        {
            var nulls = dump.Where(x => x.Value == null).Select(x => x.Key.Name);
            return $"Missing services: {string.Join(", ", nulls)}";
        }
        #endregion

        //UNDONE:ServicesTest: Resolve method .AddSenseNetCors()
        //UNDONE:ServicesTest: Resolve method .AddSenseNetIdentityServerClients()
        //UNDONE:ServicesTest: Resolve method .AddSenseNetDefaultClientManager()
        //UNDONE:ServicesTest: Resolve method .AddSenseNetApiKeys()
        //UNDONE:ServicesTest: Resolve method .AddSenseNetEmailManager(...
        //UNDONE:ServicesTest: Resolve method .AddSenseNetRegistration()
        //UNDONE:ServicesTest: Resolve method .AddStatistics()
        //UNDONE:ServicesTest: Resolve "add maintenance tasks"

        private IDictionary<Type, Type> GetGeneralizedExpectations()
        {
            return new Dictionary<Type, Type>
            {
                {typeof(IEventLogger), typeof(SnILogger)},
                {typeof(ISnTracer), typeof(SnILoggerTracer)},

                {typeof(IBlobProviderStore), typeof(BlobProviderStore)},
                {typeof(IBlobStorage), typeof(BlobStorage)},
                {typeof(IExternalBlobProviderFactory), typeof(NullExternalBlobProviderFactory)},

                {typeof(SecurityHandler), typeof(SecurityHandler)},
                {typeof(IMissingEntityHandler), typeof(SnMissingEntityHandler)},

                {typeof(IPasswordHashProvider), typeof(SenseNetPasswordHashProvider)},
                {typeof(IPasswordHashProviderForMigration), typeof(Sha256PasswordHashProviderWithoutSalt)},

                // Search
                {typeof(ISearchManager), typeof(SearchManager)},
                {typeof(IIndexManager), typeof(IndexManager)},
                {typeof(IIndexPopulator), typeof(DocumentPopulator)},

                // TaskManager
                {typeof(ITaskManager), typeof(TaskManagerBase)},
                {typeof(ITaskManagementClient), typeof(TaskManagementClient)},

                // Preview
                {typeof(IPreviewProvider), typeof(DefaultDocumentPreviewProvider)},

                // Components
                {typeof(ILatestComponentStore), typeof(DefaultLatestComponentStore)},

                // Platform independent additions
                {typeof(ElevatedModificationVisibilityRule), typeof(ElevatedModificationVisibilityRule)},

                // Not used?
                {typeof(IApplicationCache), typeof(ApplicationCache)},
            };
        }
        private IDictionary<Type, Type> GetInMemoryPlatform()
        {
            return new Dictionary<Type, Type>
            {
                // DataProvider
                {typeof(DataProvider), typeof(InMemoryDataProvider)},

                // Blob
                {typeof(IBlobStorageMetaDataProvider), typeof(InMemoryBlobStorageMetaDataProvider)},
                {typeof(IBlobProvider), typeof(InMemoryBlobProvider)},
                {typeof(IBlobProviderSelector), typeof(InMemoryBlobProviderSelector)},

                // Security
                {typeof(ISecurityDataProvider), typeof(MemoryDataProvider)},
               // {typeof(IMessageProvider), typeof(DefaultMessageProvider)},

                // ????
                {typeof(ISharedLockDataProvider), typeof(InMemorySharedLockDataProvider)},
                {typeof(IExclusiveLockDataProvider), typeof(InMemoryExclusiveLockDataProvider)},
                {typeof(IAccessTokenDataProvider), typeof(InMemoryAccessTokenDataProvider)},
                {typeof(IPackagingDataProvider), typeof(InMemoryPackageStorageProvider)},
                {typeof(IAuditEventWriter), typeof(InactiveAuditEventWriter)},

                //UNDONE: Add to services
                //{typeof(ISearchEngine), typeof(InMemorySearchEngine)},
                //InMemoryIndex?
                //{typeof(IIndexingEngine), typeof(InMemoryIndexingEngine)},
                //{typeof(IQueryEngine), typeof(InMemoryQueryEngine)},
            };
        }
        private IDictionary<Type, Type> GetMsSqlPlatform()
        {
            return new Dictionary<Type, Type>
            {
                // DataProvider
                {typeof(DataProvider), typeof(MsSqlDataProvider)},
                {typeof(IDataInstaller), typeof(MsSqlDataInstaller)},
                {typeof(MsSqlDatabaseInstaller), typeof(MsSqlDatabaseInstaller)},

                // Blob
                {typeof(IBlobStorageMetaDataProvider), typeof(MsSqlBlobMetaDataProvider)},
                {typeof(IBlobProvider), typeof(BuiltInBlobProvider)},
                {typeof(IBlobProviderSelector), typeof(BuiltInBlobProviderSelector)},

                {typeof(ISecurityDataProvider), typeof(EFCSecurityDataProvider)},

                // ????
                {typeof(ISharedLockDataProvider), typeof(MsSqlSharedLockDataProvider)},
                {typeof(IExclusiveLockDataProvider), typeof(MsSqlExclusiveLockDataProvider)},
                {typeof(IAccessTokenDataProvider), typeof(MsSqlAccessTokenDataProvider)},
                {typeof(IPackagingDataProvider), typeof(MsSqlPackagingDataProvider)},
                {typeof(IAuditEventWriter), typeof(DatabaseAuditEventWriter)},

                //UNDONE: Add to services
                //{typeof(ISearchEngine), typeof(Lucene29SearchEngine)},
                //IndexDirectory?
                //{typeof(IIndexingEngine), typeof(Lucene29LocalIndexingEngine)},
                //{typeof(IQueryEngine), typeof(Lucene29LocalQueryEngine)},
            };
        }
        private void AssertProvidersInstance()
        {
            var pi = Providers.Instance;
            Assert.IsNotNull(pi);
            Assert.IsNotNull(pi.EventLogger);
            Assert.IsNotNull(pi.PropertyCollector);
            Assert.IsNotNull(pi.AuditEventWriter);
            Assert.IsNotNull(pi.DataProvider);
            Assert.IsNotNull(pi.DataStore);
            Assert.IsNotNull(pi.BlobMetaDataProvider);
            Assert.IsNotNull(pi.BlobProviderSelector);
//Assert.IsNotNull(pi.BlobStorage);
            Assert.IsNotNull(pi.BlobProviders);
            Assert.IsNotNull(pi.SearchEngine);
            Assert.IsNotNull(pi.SearchManager);
            Assert.IsNotNull(pi.IndexManager);
            Assert.IsNotNull(pi.IndexPopulator);
            Assert.IsNotNull(pi.AccessProvider);
            Assert.IsNotNull(pi.SecurityDataProvider);
            Assert.IsNotNull(pi.SecurityMessageProvider);
            Assert.IsNotNull(pi.SecurityHandler);
            Assert.IsNotNull(pi.PasswordHashProvider);
            Assert.IsNotNull(pi.PasswordHashProviderForMigration);
            Assert.IsNotNull(pi.ContentNamingProvider);
            Assert.IsNotNull(pi.PreviewProvider);
     //? inmem only?       Assert.IsNotNull(pi.ElevatedModificationVisibilityRuleProvider);
            Assert.IsNotNull(pi.MembershipExtender);
            Assert.IsNotNull(pi.CacheProvider);
            Assert.IsNotNull(pi.ApplicationCacheProvider);
            Assert.IsNotNull(pi.ClusterChannelProvider);
            Assert.IsNotNull(pi.PermissionFilterFactory);
            Assert.IsNotNull(pi.IndexDocumentProvider);
            Assert.IsNotNull(pi.ContentProtector);
            Assert.IsNotNull(pi.StorageSchema);
            Assert.IsNotNull(pi.CompatibilitySupport);
            Assert.IsNotNull(pi.EventDistributor);
            //Assert.IsNotNull(pi.AuditLogEventProcessor);
//Assert.IsNotNull(pi.TreeLock);
            Assert.IsNotNull(pi.TaskManager);

            Assert.IsTrue(pi.NodeObservers.Length > 0);
            //Assert.IsTrue(pi.AsyncEventProcessors.Count > 0);
            Assert.IsTrue(pi.Components.Count > 0);

            //private readonly Dictionary<string, object> _providersByName = new Dictionary<string, object>();
            //private readonly Dictionary<Type, object> _providersByType = new Dictionary<Type, object>();
        }

        [TestMethod, TestCategory("Services")]
        public void WebApp_Services_Api_InMem_Admin()
        {
            StartupServicesTest<SnWebApplication.Api.InMem.Admin.Startup>(GetInMemoryPlatform(), new Dictionary<Type, Type>
            {
                {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
            });
        }
        [TestMethod, TestCategory("Services")]
        public void WebApp_Services_Api_InMem_TokenAuth()
        {
            StartupServicesTest<SnWebApplication.Api.InMem.TokenAuth.Startup>(GetInMemoryPlatform(), new Dictionary<Type, Type>
            {
                {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
            });
        }

        [TestMethod, TestCategory("Services")]
        public void WebApp_Services_Api_Sql_Admin()
        {
            StartupServicesTest<SnWebApplication.Api.Sql.Admin.Startup>(GetMsSqlPlatform(), new Dictionary<Type, Type>
            {
                {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
            });
        }
        [TestMethod, TestCategory("Services")]
        public void WebApp_Services_Api_Sql_TokenAuth()
        {
            StartupServicesTest<SnWebApplication.Api.Sql.TokenAuth.Startup>(GetMsSqlPlatform(), new Dictionary<Type, Type>
            {
                {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
            });
        }

        [TestMethod, TestCategory("Services")]
        public void WebApp_Services_Api_Sql_SearchService_Admin()
        {
            StartupServicesTest<SnWebApplication.Api.Sql.SearchService.Admin.Startup>(GetMsSqlPlatform(), new Dictionary<Type, Type>
            {
                {typeof(IMessageProvider), typeof(SenseNet.Security.Messaging.RabbitMQ.RabbitMQMessageProvider)},
            });
        }
        [TestMethod, TestCategory("Services")]
        public void WebApp_Services_Api_Sql_SearchService_TokenAuth()
        {
            StartupServicesTest<SnWebApplication.Api.Sql.SearchService.TokenAuth.Startup>(GetMsSqlPlatform(), new Dictionary<Type, Type>
            {
                {typeof(IMessageProvider), typeof(SenseNet.Security.Messaging.RabbitMQ.RabbitMQMessageProvider)},
            });
        }

        /* ========================================================================= Test tests */

        private class TestClassForTestingServices : TestBase
        { public IServiceProvider CreateServiceProvider() => CreateServiceProviderForTest(); }

        [TestMethod, TestCategory("Services")]
        public void Test_Services_TestBase()
        {
            // ACTION
            var services = new TestClassForTestingServices().CreateServiceProvider();

            // ASSERT
            AssertServices(true, services, GetInMemoryPlatform(), new Dictionary<Type, Type>
            {
                {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
                {typeof(ITestingDataProvider), typeof(InMemoryTestingDataProvider)},
            });
        }

        [TestMethod, TestCategory("Services")]
        public void Test_Services_Integration_InMem()
        {
            var platform = new InMemPlatform();
            var configuration = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();

            // ACTION
            platform.BuildServices(configuration, services);

            // ASSERT
            AssertServices(services, GetInMemoryPlatform(), new Dictionary<Type, Type>
            {
                {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
                {typeof(ITestingDataProvider), typeof(InMemoryTestingDataProvider)},
            });
        }
        [TestMethod, TestCategory("Services")]
        public void Test_Services_Integration_MsSql()
        {
            var platform = new MsSqlPlatform();
            var configuration = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();
            var cnOptions = Options.Create<ConnectionStringOptions>(new ConnectionStringOptions {Repository = "fake"});
            services.AddSingleton<IOptions<ConnectionStringOptions>>(cnOptions);

            // ACTION
            platform.BuildServices(configuration, services);

            // ASSERT
            AssertServices(services, GetMsSqlPlatform(), new Dictionary<Type, Type>
            {
                {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
                {typeof(ITestingDataProvider), typeof(MsSqlTestingDataProvider)},
            });
        }

        [TestMethod, TestCategory("Services")]
        public void Test_Services_InitialDataGenerator()
        {
            // ACTION
            var builder = SenseNet.Tools.SnInitialDataGenerator.Program.CreateRepositoryBuilder(null);
            var services = builder.Services;

            // ASSERT
            AssertServices(true, services, GetInMemoryPlatform(), new Dictionary<Type, Type>
            {
                {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
                {typeof(ISearchEngine), typeof(SearchEngineForInitialDataGenerator)}, // overrides the platform's service
            });
        }

    }
}
