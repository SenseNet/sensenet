using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Security.Data;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;
using SenseNet.Tests.Implementations2;

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
            public IEnumerable<ComponentInfo> LoadInstalledComponents()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<Package> LoadInstalledPackages()
            {
                throw new NotImplementedException();
            }

            public void SavePackage(Package package)
            {
                throw new NotImplementedException();
            }

            public void UpdatePackage(Package package)
            {
                throw new NotImplementedException();
            }

            public bool IsPackageExist(string componentId, PackageType packageType, Version version)
            {
                throw new NotImplementedException();
            }

            public void DeletePackage(Package package)
            {
                throw new NotImplementedException();
            }

            public void DeleteAllPackages()
            {
                throw new NotImplementedException();
            }

            public void LoadManifest(Package package)
            {
                throw new NotImplementedException();
            }
        }
        private class TestAccessTokenDataProvider : IAccessTokenDataProviderExtension
        {
            public void DeleteAllAccessTokens()
            {
                throw new NotImplementedException();
            }

            public void SaveAccessToken(AccessToken token)
            {
                throw new NotImplementedException();
            }

            public AccessToken LoadAccessTokenById(int accessTokenId)
            {
                throw new NotImplementedException();
            }

            public AccessToken LoadAccessToken(string tokenValue, int contentId, string feature)
            {
                throw new NotImplementedException();
            }

            public AccessToken[] LoadAccessTokens(int userId)
            {
                throw new NotImplementedException();
            }

            public void UpdateAccessToken(string tokenValue, DateTime newExpirationDate)
            {
                throw new NotImplementedException();
            }

            public void DeleteAccessToken(string tokenValue)
            {
                throw new NotImplementedException();
            }

            public void DeleteAccessTokensByUser(int userId)
            {
                throw new NotImplementedException();
            }

            public void DeleteAccessTokensByContent(int contentId)
            {
                throw new NotImplementedException();
            }

            public void CleanupAccessTokens()
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        [TestMethod]
        public void RepositoryStart_NamedProviders()
        {
           if(DataStore.Enabled)
               RepositoryStart_NamedProviders_NEW();
            else
                RepositoryStart_NamedProviders_OLD();
        }
        private void RepositoryStart_NamedProviders_NEW()
        {
            var dbProvider = new InMemoryDataProvider2();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine();
            var accessProvider = new DesktopAccessProvider();
            var emvrProvider = new ElevatedModificationVisibilityRule();

            // switch this ON here for testing purposes (to check that repo start does not override it)
            SnTrace.Custom.Enabled = true;

            ////UNDONE:DB ----RepositoryBuilder and InMemoryBlobStorageMetaDataProvider2
            //Providers.Instance.BlobMetaDataProvider2 = new InMemoryBlobStorageMetaDataProvider2(dbProvider);

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider2(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider2(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(securityDbProvider)
                .UseSearchEngine(searchEngine)
                .UseAccessProvider(accessProvider)
                .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            DataStore.InstallDataPackage(GetInitialData());

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
        private void RepositoryStart_NamedProviders_OLD()
        {
            var dbProvider = new InMemoryDataProvider();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine();
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

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(dbProvider, DataProvider.Current); //DB:??test??
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
            if (DataStore.Enabled)
                RepositoryStart_NodeObservers_DisableAll_NEW();
            else
                RepositoryStart_NodeObservers_DisableAll_OLD();
        }
        private void RepositoryStart_NodeObservers_DisableAll_NEW()
        {
            var dbProvider = new InMemoryDataProvider2();

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider2(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider2(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine())
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers()
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            DataStore.InstallDataPackage(GetInitialData());

            using (Repository.Start(repoBuilder))
            {
                Assert.IsFalse(Providers.Instance.NodeObservers.Any());
            }
        }
        private void RepositoryStart_NodeObservers_DisableAll_OLD()
        {
            var dbProvider = new InMemoryDataProvider();

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine())
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers()
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            using (Repository.Start(repoBuilder))
            {
                Assert.IsFalse(Providers.Instance.NodeObservers.Any());
            }
        }

        [TestMethod]
        public void RepositoryStart_NodeObservers_EnableOne()
        {
            if (DataStore.Enabled)
                RepositoryStart_NodeObservers_EnableOne_NEW();
            else
                RepositoryStart_NodeObservers_EnableOne_OLD();
        }
        private void RepositoryStart_NodeObservers_EnableOne_NEW()
        {
            var dbProvider = new InMemoryDataProvider2();

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider2(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider2(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine())
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(TestNodeObserver1))
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            DataStore.InstallDataPackage(GetInitialData());

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(1, Providers.Instance.NodeObservers.Length);
                Assert.AreEqual(typeof(TestNodeObserver1), Providers.Instance.NodeObservers[0].GetType());
            }
        }
        private void RepositoryStart_NodeObservers_EnableOne_OLD()
        {
            var dbProvider = new InMemoryDataProvider();

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine())
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(TestNodeObserver1))
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(1, Providers.Instance.NodeObservers.Length);
                Assert.AreEqual(typeof(TestNodeObserver1), Providers.Instance.NodeObservers[0].GetType());
            }
        }

        [TestMethod]
        public void RepositoryStart_NodeObservers_EnableMore()
        {
            if (DataStore.Enabled)
                RepositoryStart_NodeObservers_EnableMore_NEW();
            else
                RepositoryStart_NodeObservers_EnableMore_OLD();
        }
        private void RepositoryStart_NodeObservers_EnableMore_NEW()
        {
            var dbProvider = new InMemoryDataProvider2();

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider2(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider2(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine())
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(TestNodeObserver1), typeof(TestNodeObserver2))
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            DataStore.InstallDataPackage(GetInitialData());

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(2, Providers.Instance.NodeObservers.Length);
                Assert.IsTrue(Providers.Instance.NodeObservers.Any(no => no.GetType() == typeof(TestNodeObserver1)));
                Assert.IsTrue(Providers.Instance.NodeObservers.Any(no => no.GetType() == typeof(TestNodeObserver2)));
            }
        }
        private void RepositoryStart_NodeObservers_EnableMore_OLD()
        {
            var dbProvider = new InMemoryDataProvider();

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine())
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(TestNodeObserver1), typeof(TestNodeObserver2))
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

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
            if (DataStore.Enabled)
                RepositoryStart_NodeObservers_DisableOne_NEW();
            else
                RepositoryStart_NodeObservers_DisableOne_OLD();
        }
        private void RepositoryStart_NodeObservers_DisableOne_NEW()
        {
            var dbProvider = new InMemoryDataProvider2();

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider2(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider2(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine())
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers(typeof(TestNodeObserver1))
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            DataStore.InstallDataPackage(GetInitialData());

            using (Repository.Start(repoBuilder))
            {
                Assert.IsFalse(Providers.Instance.NodeObservers.Any(no => no.GetType() == typeof(TestNodeObserver1)));

                //TODO: currently this does not work, because observers are enabled/disabled globally.
                // Itt will, when we move to a per-thread environment in tests.
                //Assert.IsTrue(Providers.Instance.NodeObservers.Any(no => no.GetType() == typeof(TestNodeObserver2)));
            }
        }
        private void RepositoryStart_NodeObservers_DisableOne_OLD()
        {
            var dbProvider = new InMemoryDataProvider();

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine())
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers(typeof(TestNodeObserver1))
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

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
            var dbProvider = new InMemoryDataProvider();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine();
            var accessProvider = new DesktopAccessProvider();
            var emvrProvider = new ElevatedModificationVisibilityRule();

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
            if (DataStore.Enabled)
                RepositoryStart_DataProviderExtensions_Default_NEW();
            else
                RepositoryStart_DataProviderExtensions_Default_OLD();
        }
        private void RepositoryStart_DataProviderExtensions_Default_NEW()
        {
            var dbProvider = new InMemoryDataProvider2();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine();
            var accessProvider = new DesktopAccessProvider();
            var emvrProvider = new ElevatedModificationVisibilityRule();

            // switch this ON here for testing purposes (to check that repo start does not override it)
            SnTrace.Custom.Enabled = true;

            ////UNDONE:DB ----RepositoryBuilder and InMemoryBlobStorageMetaDataProvider2
            //Providers.Instance.BlobMetaDataProvider2 = new InMemoryBlobStorageMetaDataProvider2(dbProvider);

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider2(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider2(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseSecurityDataProvider(securityDbProvider)
                .UseSearchEngine(searchEngine)
                .UseAccessProvider(accessProvider)
                .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            DataStore.InstallDataPackage(GetInitialData());

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(typeof(SqlPackagingDataProvider), DataStore.DataProvider.GetExtensionInstance<IPackagingDataProviderExtension>().GetType()); //DB:??test??
                Assert.AreEqual(typeof(SqlAccessTokenDataProvider), DataStore.DataProvider.GetExtensionInstance<IAccessTokenDataProviderExtension>().GetType()); //DB:??test??
            }
        }
        private void RepositoryStart_DataProviderExtensions_Default_OLD()
        {
            var dbProvider = new InMemoryDataProvider();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine();
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

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(typeof(SqlPackagingDataProvider), DataProvider.GetExtension<IPackagingDataProviderExtension>().GetType()); //DB:??test??
                Assert.AreEqual(typeof(SqlAccessTokenDataProvider), DataProvider.GetExtension<IAccessTokenDataProviderExtension>().GetType()); //DB:??test??
            }
        }

        [TestMethod]
        public void RepositoryStart_DataProviderExtensions_OverrideDefault()
        {
            if (DataStore.Enabled)
                RepositoryStart_DataProviderExtensions_OverrideDefault_NEW();
            else
                RepositoryStart_DataProviderExtensions_OverrideDefault_OLD();
        }
        private void RepositoryStart_DataProviderExtensions_OverrideDefault_NEW()
        {
            var dbProvider = new InMemoryDataProvider2();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine();
            var accessProvider = new DesktopAccessProvider();
            var emvrProvider = new ElevatedModificationVisibilityRule();

            // switch this ON here for testing purposes (to check that repo start does not override it)
            SnTrace.Custom.Enabled = true;

            ////UNDONE:DB ----RepositoryBuilder and InMemoryBlobStorageMetaDataProvider2
            //Providers.Instance.BlobMetaDataProvider2 = new InMemoryBlobStorageMetaDataProvider2(dbProvider);

            // switch this ON here for testing purposes (to check that repo start does not override it)
            SnTrace.Custom.Enabled = true;

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider2(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider2(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseAccessTokenDataProviderExtension(new TestAccessTokenDataProvider())     // ACTION: set test provider
                .UseSecurityDataProvider(securityDbProvider)
                .UseSearchEngine(searchEngine)
                .UseAccessProvider(accessProvider)
                .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            DataStore.InstallDataPackage(GetInitialData());

            using (Repository.Start(repoBuilder))
            {
                //Assert.AreEqual(typeof(TestPackagingDataProvider), DataProvider.GetExtension<IPackagingDataProviderExtension>().GetType()); //DB:??test??
                Assert.AreEqual(typeof(TestAccessTokenDataProvider), DataStore.GetDataProviderExtension<IAccessTokenDataProviderExtension>().GetType()); //DB:??test??
            }
        }
        private void RepositoryStart_DataProviderExtensions_OverrideDefault_OLD()
        {
            var dbProvider = new InMemoryDataProvider();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine();
            var accessProvider = new DesktopAccessProvider();
            var emvrProvider = new ElevatedModificationVisibilityRule();

            // switch this ON here for testing purposes (to check that repo start does not override it)
            SnTrace.Custom.Enabled = true;

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UsePackagingDataProviderExtension(new TestPackagingDataProvider())         // ACTION: set test provider
                .UseAccessTokenDataProviderExtension(new TestAccessTokenDataProvider())     // ACTION: set test provider
                .UseSecurityDataProvider(securityDbProvider)
                .UseSearchEngine(searchEngine)
                .UseAccessProvider(accessProvider)
                .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(typeof(TestPackagingDataProvider), DataProvider.GetExtension<IPackagingDataProviderExtension>().GetType()); //DB:??test??
                Assert.AreEqual(typeof(TestAccessTokenDataProvider), DataProvider.GetExtension<IAccessTokenDataProviderExtension>().GetType()); //DB:??test??
            }
        }
    }
}
