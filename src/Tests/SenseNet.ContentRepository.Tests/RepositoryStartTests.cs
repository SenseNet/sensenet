using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Security.Data;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class RepositoryStartTests : TestBase
    {
        #region Nested test classes
        public class TestNodeObserver1 : NodeObserver { }
        public class TestNodeObserver2 : NodeObserver { }

        public class TestEventLogger : IEventLogger
        {
            public void Write(object message, ICollection<string> categories, int priority, int eventId, TraceEventType severity, string title,
                IDictionary<string, object> properties)
            {
                //do nothing
            }
        }
        public class TestSnTracer : ISnTracer
        {
            public void Write(string line)
            {
                //do nothing
            }

            public void Flush()
            {
                //do nothing
            }
        }

        private class TestPackagingDataProvider : IPackagingDataProviderExtension
        {
            public System.Threading.Tasks.Task DeleteAllPackagesAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public System.Threading.Tasks.Task DeletePackageAsync(Package package, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<bool> IsPackageExistAsync(string componentId, PackageType packageType, Version version, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<IEnumerable<ComponentInfo>> LoadInstalledComponentsAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<IEnumerable<Package>> LoadInstalledPackagesAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public System.Threading.Tasks.Task LoadManifestAsync(Package package, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public System.Threading.Tasks.Task SavePackageAsync(Package package, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public System.Threading.Tasks.Task UpdatePackageAsync(Package package, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
        private class TestAccessTokenDataProvider : IAccessTokenDataProviderExtension
        {
            public System.Threading.Tasks.Task DeleteAllAccessTokensAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public System.Threading.Tasks.Task SaveAccessTokenAsync(AccessToken token, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<AccessToken> LoadAccessTokenByIdAsync(int accessTokenId, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<AccessToken> LoadAccessTokenAsync(string tokenValue, int contentId, string feature,
                CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<AccessToken[]> LoadAccessTokensAsync(int userId, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public System.Threading.Tasks.Task UpdateAccessTokenAsync(string tokenValue, DateTime newExpirationDate,
                CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public System.Threading.Tasks.Task DeleteAccessTokenAsync(string tokenValue, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public System.Threading.Tasks.Task DeleteAccessTokensByUserAsync(int userId, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public System.Threading.Tasks.Task DeleteAccessTokensByContentAsync(int contentId, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public System.Threading.Tasks.Task CleanupAccessTokensAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        [TestMethod]
        public void RepositoryStart_NamedProviders()
        {
            var dbProvider = new InMemoryDataProvider();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine(GetInitialIndex());
            var accessProvider = new DesktopAccessProvider();
            var emvrProvider = new ElevatedModificationVisibilityRule();

            // switch this ON here for testing purposes (to check that repo start does not override it)
            SnTrace.Custom.Enabled = true;

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(securityDbProvider)
                .UseSearchEngine(searchEngine)
                .UseAccessProvider(accessProvider)
                .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            DataStore.InstallInitialDataAsync(GetInitialData()).Wait();

            using (Repository.Start(repoBuilder))
            {
                Assert.AreSame(dbProvider, DataStore.DataProvider); //DB:??test??
                Assert.AreEqual(searchEngine, SearchManager.SearchEngine);
                Assert.AreEqual(accessProvider, AccessProvider.Current);
                Assert.AreEqual(emvrProvider, Providers.Instance.ElevatedModificationVisibilityRuleProvider);

                // Currently this does not work, because the property below re-creates the security 
                // db provider from the prototype, so it cannot be ref equal with the original.
                // Assert.AreEqual(securityDbProvider, SecurityHandler.SecurityContext.DataProvider);
                Assert.AreEqual(securityDbProvider, Providers.Instance.SecurityDataProvider);

                // Check a few trace categories that were switched ON above.
                Assert.IsTrue(SnTrace.Custom.Enabled);
                Assert.IsTrue(SnTrace.Test.Enabled);
                Assert.IsTrue(SnTrace.Web.Enabled);
                Assert.IsTrue(SnTrace.System.Enabled);
                Assert.IsFalse(SnTrace.TaskManagement.Enabled);
                Assert.IsFalse(SnTrace.Workflow.Enabled);
            }
        }

        [TestMethod]
        public void RepositoryStart_NodeObservers_DisableAll()
        {
            var dbProvider = new InMemoryDataProvider();

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers()
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            DataStore.InstallInitialDataAsync(GetInitialData()).Wait();

            using (Repository.Start(repoBuilder))
            {
                Assert.IsFalse(Providers.Instance.NodeObservers.Any());
            }
        }

        [TestMethod]
        public void RepositoryStart_NodeObservers_EnableOne()
        {
            var dbProvider = new InMemoryDataProvider();

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(TestNodeObserver1))
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            DataStore.InstallInitialDataAsync(GetInitialData()).Wait();

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(1, Providers.Instance.NodeObservers.Length);
                Assert.AreEqual(typeof(TestNodeObserver1), Providers.Instance.NodeObservers[0].GetType());
            }
        }

        [TestMethod]
        public void RepositoryStart_NodeObservers_EnableMore()
        {
            var dbProvider = new InMemoryDataProvider();

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(TestNodeObserver1), typeof(TestNodeObserver2))
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            DataStore.InstallInitialDataAsync(GetInitialData()).Wait();

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(2, Providers.Instance.NodeObservers.Length);
                Assert.IsTrue(Providers.Instance.NodeObservers.Any(no => no.GetType() == typeof(TestNodeObserver1)));
                Assert.IsTrue(Providers.Instance.NodeObservers.Any(no => no.GetType() == typeof(TestNodeObserver2)));
            }
        }

        [TestMethod]
        public void RepositoryStart_NodeObservers_DisableOne()
        {
            var dbProvider = new InMemoryDataProvider();

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers(typeof(TestNodeObserver1))
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            DataStore.InstallInitialDataAsync(GetInitialData()).Wait();

            using (Repository.Start(repoBuilder))
            {
                Assert.IsFalse(Providers.Instance.NodeObservers.Any(no => no.GetType() == typeof(TestNodeObserver1)));

                //TODO: currently this does not work, because observers are enabled/disabled globally.
                // Itt will, when we move to a per-thread environment in tests.
                //Assert.IsTrue(Providers.Instance.NodeObservers.Any(no => no.GetType() == typeof(TestNodeObserver2)));
            }
        }

        [TestMethod]
        public void RepositoryStart_NullPopulator()
        {
            var dbProvider2 = new InMemoryDataProvider();
            DataStore.InstallInitialDataAsync(GetInitialData()).Wait();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine(GetInitialIndex());
            var accessProvider = new DesktopAccessProvider();
            var emvrProvider = new ElevatedModificationVisibilityRule();

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider2)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider2))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(securityDbProvider)
                .UseSearchEngine(searchEngine)
                .UseAccessProvider(accessProvider)
                .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            var originalIsOuterSearchEngineEnabled = Indexing.IsOuterSearchEngineEnabled;
            Indexing.IsOuterSearchEngineEnabled = false;
            try
            {
                using (Repository.Start(repoBuilder))
                {
                    Assert.IsFalse(SearchManager.IsOuterEngineEnabled);
                    Assert.AreEqual(typeof(InternalSearchEngine), SearchManager.SearchEngine.GetType());
                    var populator = SearchManager.GetIndexPopulator();
                    Assert.AreEqual(typeof(NullPopulator), populator.GetType());
                }
            }
            finally
            {
                Indexing.IsOuterSearchEngineEnabled = originalIsOuterSearchEngineEnabled;
            }
        }

        [TestMethod]
        public void RepositoryStart_Loggers()
        {
            var originalLogger = SnLog.Instance;
            var originalTracers = SnTrace.SnTracers;

            try
            {
                Test(repoBuilder =>
                {
                    repoBuilder
                        .UseLogger(new TestEventLogger())
                        .UseTracer(new TestSnTracer());
                }, () =>
                {
                    //test that the loggers were set correctly
                    Assert.AreEqual(1, SnTrace.SnTracers.Count);
                    Assert.IsTrue(SnTrace.SnTracers.First() is TestSnTracer);
                    Assert.IsTrue(SnLog.Instance is TestEventLogger);
                });
            }
            finally
            {
                SnLog.Instance = originalLogger;
                SnTrace.SnTracers.Clear();
                SnTrace.SnTracers.AddRange(originalTracers);
            }
        }

        [TestMethod]
        public void RepositoryStart_DataProviderExtensions_Default()
        {
            var dbProvider = new InMemoryDataProvider();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine(GetInitialIndex());
            var accessProvider = new DesktopAccessProvider();
            var emvrProvider = new ElevatedModificationVisibilityRule();

            // switch this ON here for testing purposes (to check that repo start does not override it)
            SnTrace.Custom.Enabled = true;

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(securityDbProvider)
                .UseSearchEngine(searchEngine)
                .UseAccessProvider(accessProvider)
                .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            DataStore.InstallInitialDataAsync(GetInitialData()).Wait();

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(typeof(MsSqlPackagingDataProvider), DataStore.DataProvider.GetExtensionInstance<IPackagingDataProviderExtension>().GetType()); //DB:??test??
                Assert.AreEqual(typeof(MsSqlAccessTokenDataProvider), DataStore.DataProvider.GetExtensionInstance<IAccessTokenDataProviderExtension>().GetType()); //DB:??test??
            }
        }

        [TestMethod]
        public void RepositoryStart_DataProviderExtensions_OverrideDefault()
        {
            var dbProvider = new InMemoryDataProvider();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine(GetInitialIndex());
            var accessProvider = new DesktopAccessProvider();
            var emvrProvider = new ElevatedModificationVisibilityRule();

            // switch this ON here for testing purposes (to check that repo start does not override it)
            SnTrace.Custom.Enabled = true;

            // switch this ON here for testing purposes (to check that repo start does not override it)
            SnTrace.Custom.Enabled = true;

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseAccessTokenDataProviderExtension(new TestAccessTokenDataProvider())     // ACTION: set test provider
                .UseSecurityDataProvider(securityDbProvider)
                .UseSearchEngine(searchEngine)
                .UseAccessProvider(accessProvider)
                .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            DataStore.InstallInitialDataAsync(GetInitialData()).Wait();

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(typeof(TestAccessTokenDataProvider), DataStore.GetDataProviderExtension<IAccessTokenDataProviderExtension>().GetType()); //DB:??test??
            }
        }
    }
}
