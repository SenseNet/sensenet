using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.BackgroundOperations;
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
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Security.EFCSecurityStore;
using SenseNet.Security.Messaging;
using SenseNet.Services.Core.Cors;
using SenseNet.Storage.Data.MsSqlClient;
using SenseNet.TaskManagement.Core;
using SenseNet.Testing;
using SenseNet.Tests.Core;
using SenseNet.Tests.Core.Implementations;

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
            AssertServices(serviceCollection.BuildServiceProvider(), platformSpecificExpectations, customizedExpectations);
        }
        private void AssertServices(IServiceProvider services, IDictionary<Type, Type> platformSpecificExpectations, IDictionary<Type, Type> customizedExpectations = null)
        {
            var expectation = GetGeneralizedExpectations();
            if(platformSpecificExpectations != null)
                foreach (var item in platformSpecificExpectations)
                    expectation.Add(item);
            if(customizedExpectations != null)
                foreach (var item in customizedExpectations)
                    expectation.Add(item);

            var dump = expectation.ToDictionary(
                x => x.Key,
                x => services.GetService(x.Key)?.GetType());

            Assert.AreEqual(0, dump
                .Where(x => x.Value != null)
                .Count(x => x.Value != expectation[x.Key]), GetMessageForDifferences(dump, expectation));
            Assert.AreEqual(0, dump.Values.Count(x => x == null), GetMessageForNulls(dump));
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
            };
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
            var expectation = GetGeneralizedExpectations();
            var x = GetInMemoryPlatform();

            // ACTION
            var services = new TestClassForTestingServices().CreateServiceProvider();

            // ASSERT
            AssertServices(services, GetInMemoryPlatform(), new Dictionary<Type, Type>
            {
                {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
                {typeof(ITestingDataProvider), typeof(InMemoryTestingDataProvider)},
            });
        }

        [TestMethod, TestCategory("Services")]
        public void Test_Services_Integration_InMem()
        {
            var expectation = GetGeneralizedExpectations();
            var x = GetInMemoryPlatform();

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
            var expectation = GetGeneralizedExpectations();

            var platform = new MsSqlPlatform();
            var configuration = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();

            // ACTION
            platform.BuildServices(configuration, services);

            // ASSERT
            AssertServices(services, GetMsSqlPlatform(), new Dictionary<Type, Type>
            {
                {typeof(IMessageProvider), typeof(DefaultMessageProvider)},
                {typeof(ITestingDataProvider), typeof(MsSqlTestingDataProvider)},
            });
        }
    }
}
