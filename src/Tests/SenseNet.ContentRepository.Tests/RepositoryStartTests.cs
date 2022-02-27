﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Schema;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Security.Data;
using SenseNet.Security.Messaging;
using SenseNet.Storage.Diagnostics;
using SenseNet.Tests.Core;
using SenseNet.Tests.Core.Implementations;
using SenseNet.Tools.Diagnostics;

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

            public Task<IEnumerable<ComponentInfo>> LoadIncompleteComponentsAsync(CancellationToken cancellationToken)
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

            public Dictionary<string, string> GetContentPathsWhereTheyAreAllowedChildren(List<string> names)
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

            public System.Threading.Tasks.Task DeleteAccessTokensAsync(int userId, int contentId, string feature, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public System.Threading.Tasks.Task CleanupAccessTokensAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
        private class TestAuditEventWriter : IAuditEventWriter
        {
            public void Write(IAuditEvent auditEvent, IDictionary<string, object> properties)
            {
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
                .UseAccessProvider(new DesktopAccessProvider())
                .UseDataProvider(dbProvider)
                .UseInitialData(GetInitialData())
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .AddBlobProvider(new InMemoryBlobProvider())
                .UseSecurityDataProvider(securityDbProvider)
                .UseSecurityMessageProvider(new DefaultMessageProvider(new MessageSenderManager()))
                .UseSearchManager(new SearchManager(Providers.Instance.DataStore))
                .UseIndexManager(new IndexManager(Providers.Instance.DataStore, Providers.Instance.SearchManager))
                .UseIndexPopulator(new DocumentPopulator(Providers.Instance.DataStore, Providers.Instance.IndexManager))
                .UseSearchEngine(searchEngine)
                .UseAccessProvider(accessProvider)
                .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            using (Repository.Start(repoBuilder))
            {
                Assert.AreSame(dbProvider, Providers.Instance.DataStore.DataProvider);
                Assert.AreEqual(searchEngine, Providers.Instance.SearchManager.SearchEngine);
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
                .UseAccessProvider(new DesktopAccessProvider())
                .UseDataProvider(dbProvider)
                .UseInitialData(GetInitialData())
                .UseSharedLockDataProviderExtension(new InMemorySharedLockDataProvider())
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .AddBlobProvider(new InMemoryBlobProvider())
                .UseAccessTokenDataProviderExtension(new InMemoryAccessTokenDataProvider())
                .UseSearchManager(new SearchManager(Providers.Instance.DataStore))
                .UseIndexManager(new IndexManager(Providers.Instance.DataStore, Providers.Instance.SearchManager))
                .UseIndexPopulator(new DocumentPopulator(Providers.Instance.DataStore, Providers.Instance.IndexManager))
                .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSecurityMessageProvider(new DefaultMessageProvider(new MessageSenderManager()))
                .UseTestingDataProviderExtension(new InMemoryTestingDataProvider())
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
            var dbProvider = new InMemoryDataProvider();

            var repoBuilder = new RepositoryBuilder()
                .UseAccessProvider(new DesktopAccessProvider())
                .UseDataProvider(dbProvider)
                .UseInitialData(GetInitialData())
                .UseSharedLockDataProviderExtension(new InMemorySharedLockDataProvider())
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .AddBlobProvider(new InMemoryBlobProvider())
                .UseAccessTokenDataProviderExtension(new InMemoryAccessTokenDataProvider())
                .UseSearchManager(new SearchManager(Providers.Instance.DataStore))
                .UseIndexManager(new IndexManager(Providers.Instance.DataStore, Providers.Instance.SearchManager))
                .UseIndexPopulator(new DocumentPopulator(Providers.Instance.DataStore, Providers.Instance.IndexManager))
                .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSecurityMessageProvider(new DefaultMessageProvider(new MessageSenderManager()))
                .UseTestingDataProviderExtension(new InMemoryTestingDataProvider())
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
            var dbProvider = new InMemoryDataProvider();

            var repoBuilder = new RepositoryBuilder()
                .UseAccessProvider(new DesktopAccessProvider())
                .UseDataProvider(dbProvider)
                .UseInitialData(GetInitialData())
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .AddBlobProvider(new InMemoryBlobProvider())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSecurityMessageProvider(new DefaultMessageProvider(new MessageSenderManager()))
                .UseSearchManager(new SearchManager(Providers.Instance.DataStore))
                .UseIndexManager(new IndexManager(Providers.Instance.DataStore, Providers.Instance.SearchManager))
                .UseIndexPopulator(new DocumentPopulator(Providers.Instance.DataStore, Providers.Instance.IndexManager))
                .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
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
            var dbProvider = new InMemoryDataProvider();

            var repoBuilder = new RepositoryBuilder()
                .UseAccessProvider(new DesktopAccessProvider())
                .UseDataProvider(dbProvider)
                .UseInitialData(GetInitialData())
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .AddBlobProvider(new InMemoryBlobProvider())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSecurityMessageProvider(new DefaultMessageProvider(new MessageSenderManager()))
                .UseSearchManager(new SearchManager(Providers.Instance.DataStore))
                .UseIndexManager(new IndexManager(Providers.Instance.DataStore, Providers.Instance.SearchManager))
                .UseIndexPopulator(new DocumentPopulator(Providers.Instance.DataStore, Providers.Instance.IndexManager))
                .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
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
            var dbProvider2 = new InMemoryDataProvider();
            Providers.Instance.DataProvider = dbProvider2;
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine(GetInitialIndex());
            var accessProvider = new DesktopAccessProvider();
            var emvrProvider = new ElevatedModificationVisibilityRule();

            var repoBuilder = new RepositoryBuilder()
                .UseAccessProvider(new DesktopAccessProvider())
                .UseDataProvider(dbProvider2)
                .UseInitialData(GetInitialData())
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider2))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .AddBlobProvider(new InMemoryBlobProvider())
                .UseSecurityDataProvider(securityDbProvider)
                .UseSecurityMessageProvider(new DefaultMessageProvider(new MessageSenderManager()))
                .UseSearchManager(new SearchManager(Providers.Instance.DataStore))
                .UseIndexManager(new IndexManager(Providers.Instance.DataStore, Providers.Instance.SearchManager))
                .UseIndexPopulator(new DocumentPopulator(Providers.Instance.DataStore, Providers.Instance.IndexManager))
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
                    Assert.IsFalse(Providers.Instance.SearchManager.IsOuterEngineEnabled);
                    Assert.AreEqual(typeof(InternalSearchEngine),
                        Providers.Instance.SearchManager.SearchEngine.GetType());
                    var populator = Providers.Instance.SearchManager.GetIndexPopulator();
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
            var originalTracers = SnTrace.SnTracers.ToArray();

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
        public void RepositoryStart_AuditEventWriter()
        {
            var originalWriter = SnLog.AuditEventWriter;
            var auditWriter = new TestAuditEventWriter();

            try
            {
                Test(repoBuilder =>
                {
                    repoBuilder
                        .UseAuditEventWriter(auditWriter);
                }, () =>
                {
                    Assert.AreSame(auditWriter, Providers.Instance.AuditEventWriter);
                    Assert.AreSame(auditWriter, SnLog.AuditEventWriter);
                });
            }
            finally
            {
                SnLog.AuditEventWriter = originalWriter;
            }
        }
        [TestMethod]
        public void RepositoryStart_AuditEventWriter_Database()
        {
            var originalWriter = SnLog.AuditEventWriter;
            var auditWriter = new DatabaseAuditEventWriter();

            var dbProvider = new InMemoryDataProvider();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine(GetInitialIndex());
            var accessProvider = new DesktopAccessProvider();
            var emvrProvider = new ElevatedModificationVisibilityRule();

            try
            {
                // Clear the slot to ensure a real test.
                Providers.Instance.AuditEventWriter = null;

                var repoBuilder = new RepositoryBuilder()
                    .UseAccessProvider(new DesktopAccessProvider())
                    .UseDataProvider(dbProvider)
                    .UseAuditEventWriter(auditWriter) // <-- The important line
                    .UseInitialData(GetInitialData())
                    .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                    .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                    .AddBlobProvider(new InMemoryBlobProvider())
                    .UseSecurityDataProvider(securityDbProvider)
                    .UseSecurityMessageProvider(new DefaultMessageProvider(new MessageSenderManager()))
                    .UseSearchManager(new SearchManager(Providers.Instance.DataStore))
                    .UseIndexManager(new IndexManager(Providers.Instance.DataStore, Providers.Instance.SearchManager))
                    .UseIndexPopulator(new DocumentPopulator(Providers.Instance.DataStore, Providers.Instance.IndexManager))
                    .UseSearchEngine(searchEngine)
                    .UseAccessProvider(accessProvider)
                    .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                    .StartIndexingEngine(false)
                    .StartWorkflowEngine(false)
                    .UseTraceCategories("Test", "Web", "System");

                using (Repository.Start(repoBuilder))
                {
                    Assert.AreSame(auditWriter, Providers.Instance.AuditEventWriter);
                    Assert.AreSame(auditWriter, SnLog.AuditEventWriter);
                }
            }
            finally
            {
                SnLog.AuditEventWriter = originalWriter;
            }
        }
        [TestMethod]
        public void RepositoryStart_AuditEventWriter_Inactive()
        {
            var originalWriter = SnLog.AuditEventWriter;

            var dbProvider = new InMemoryDataProvider();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine(GetInitialIndex());
            var accessProvider = new DesktopAccessProvider();
            var emvrProvider = new ElevatedModificationVisibilityRule();

            try
            {
                // Clear the slot to ensure a real test.
                Providers.Instance.AuditEventWriter = null;

                var repoBuilder = new RepositoryBuilder()
                    .UseAccessProvider(new DesktopAccessProvider())
                    .UseDataProvider(dbProvider)
                    .UseInactiveAuditEventWriter() // <-- The important line
                    .UseInitialData(GetInitialData())
                    .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                    .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                    .AddBlobProvider(new InMemoryBlobProvider())
                    .UseSecurityDataProvider(securityDbProvider)
                    .UseSecurityMessageProvider(new DefaultMessageProvider(new MessageSenderManager()))
                    .UseSearchManager(new SearchManager(Providers.Instance.DataStore))
                    .UseIndexManager(new IndexManager(Providers.Instance.DataStore, Providers.Instance.SearchManager))
                    .UseIndexPopulator(new DocumentPopulator(Providers.Instance.DataStore, Providers.Instance.IndexManager))
                    .UseSearchEngine(searchEngine)
                    .UseAccessProvider(accessProvider)
                    .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                    .StartIndexingEngine(false)
                    .StartWorkflowEngine(false)
                    .UseTraceCategories("Test", "Web", "System");

                using (Repository.Start(repoBuilder))
                {
                    Assert.IsTrue(Providers.Instance.AuditEventWriter is InactiveAuditEventWriter);
                    Assert.AreSame(Providers.Instance.AuditEventWriter, SnLog.AuditEventWriter);
                }
            }
            finally
            {
                SnLog.AuditEventWriter = originalWriter;
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
                .UseAccessProvider(new DesktopAccessProvider())
                .UseDataProvider(dbProvider)
                .UseInitialData(GetInitialData())
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .AddBlobProvider(new InMemoryBlobProvider())
                .UseSecurityDataProvider(securityDbProvider)
                .UseSecurityMessageProvider(new DefaultMessageProvider(new MessageSenderManager()))
                .UseSearchManager(new SearchManager(Providers.Instance.DataStore))
                .UseIndexManager(new IndexManager(Providers.Instance.DataStore, Providers.Instance.SearchManager))
                .UseIndexPopulator(new DocumentPopulator(Providers.Instance.DataStore, Providers.Instance.IndexManager))
                .UseSearchEngine(searchEngine)
                .UseAccessProvider(accessProvider)
                .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(typeof(MsSqlPackagingDataProvider), Providers.Instance.DataProvider.GetExtension<IPackagingDataProviderExtension>().GetType());
                Assert.AreEqual(typeof(MsSqlAccessTokenDataProvider), Providers.Instance.DataProvider.GetExtension<IAccessTokenDataProviderExtension>().GetType());
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
                .UseAccessProvider(new DesktopAccessProvider())
                .UseDataProvider(dbProvider)
                .UseInitialData(GetInitialData())
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .AddBlobProvider(new InMemoryBlobProvider())
                .UseAccessTokenDataProviderExtension(new TestAccessTokenDataProvider())     // ACTION: set test provider
                .UseSecurityDataProvider(securityDbProvider)
                .UseSecurityMessageProvider(new DefaultMessageProvider(new MessageSenderManager()))
                .UseSearchManager(new SearchManager(Providers.Instance.DataStore))
                .UseIndexManager(new IndexManager(Providers.Instance.DataStore, Providers.Instance.SearchManager))
                .UseIndexPopulator(new DocumentPopulator(Providers.Instance.DataStore, Providers.Instance.IndexManager))
                .UseSearchEngine(searchEngine)
                .UseAccessProvider(accessProvider)
                .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(typeof(TestAccessTokenDataProvider), Providers.Instance.DataProvider.GetExtension<IAccessTokenDataProviderExtension>().GetType());
            }
        }

        [TestMethod]
        public void RepositoryStart_IndexAnalyzers()
        {
            var searchEngineImpl = new InMemorySearchEngine(GetInitialIndex());

            Test(repoBuilder =>
            {
                repoBuilder.UseSearchEngine(searchEngineImpl);
            }, () =>
            {
                var searchEngine = Providers.Instance.SearchEngine;

                Assert.AreSame(searchEngineImpl, searchEngine);

                var expectedAnalyzers = GetAnalyzers(ContentTypeManager.Instance.IndexingInfo);
                var analyzers = searchEngine.GetAnalyzers();
                analyzers.Should().Equal(expectedAnalyzers);

                // double check
                ResetContentTypeManager();
                ContentType.GetByName("GenericContent");
                var analyzers2 = searchEngine.GetAnalyzers();
                analyzers2.Should().Equal(expectedAnalyzers);

            });
        }
        private Dictionary<string, IndexFieldAnalyzer> GetAnalyzers(IDictionary<string, IPerFieldIndexingInfo> indexingInfo)
        {
            var analyzerTypes = new Dictionary<string, IndexFieldAnalyzer>();

            foreach (var item in indexingInfo)
            {
                var fieldName = item.Key;
                var fieldInfo = item.Value;
                if (fieldInfo.Analyzer != IndexFieldAnalyzer.Default)
                    analyzerTypes.Add(fieldName, fieldInfo.Analyzer);
            }

            return analyzerTypes;
        }

    }
}
