using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Security.Messaging;
using SenseNet.Storage.Diagnostics;
using SenseNet.Tests.Core;
using SenseNet.Tools;
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

        private class TestAccessTokenDataProvider : IAccessTokenDataProvider
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

        private IRepositoryBuilder CreateRepositoryBuilder(DataProvider dataProvider = null, AccessProvider accessProvider = null,
            ISecurityDataProvider securityDbProvider = null, ISearchEngine searchEngine = null)
        {
            var services = CreateServiceProviderForTest();
            Providers.Instance = new Providers(services);

            var dbProvider = dataProvider ?? services.GetRequiredService<DataProvider>();
            return new RepositoryBuilder(services)
                .UseAccessProvider(accessProvider ?? new DesktopAccessProvider())
                .UseDataProvider(dbProvider)
                .UseInitialData(GetInitialData())
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dbProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .AddBlobProvider(new InMemoryBlobProvider())
                .UseSecurityDataProvider(securityDbProvider ?? services.GetRequiredService<ISecurityDataProvider>())
                .UseSearchEngine(searchEngine ?? services.GetRequiredService<ISearchEngine>())
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");
        }

        [TestMethod]
        public void RepositoryStart_NamedProviders()
        {
            var dbProvider = new InMemoryDataProvider();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine(GetInitialIndex());
            var accessProvider = new DesktopAccessProvider();

            // switch this ON here for testing purposes (to check that repo start does not override it)
            SnTrace.Custom.Enabled = true;

            var repoBuilder = CreateRepositoryBuilder(
                dataProvider: dbProvider,
                accessProvider: accessProvider,
                securityDbProvider: securityDbProvider,
                searchEngine: searchEngine);

            using (Repository.Start(repoBuilder))
            {
                Assert.AreSame(dbProvider, Providers.Instance.DataStore.DataProvider);
                Assert.AreEqual(searchEngine, Providers.Instance.SearchManager.SearchEngine);
                Assert.AreEqual(accessProvider, AccessProvider.Current);
                Assert.AreEqual(typeof(ElevatedModificationVisibilityRule), Providers.Instance.ElevatedModificationVisibilityRuleProvider.GetType());

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
            var repoBuilder = CreateRepositoryBuilder()
                .DisableNodeObservers();

            using (Repository.Start(repoBuilder))
            {
                Assert.IsFalse(Providers.Instance.NodeObservers.Any());
            }
        }

        [TestMethod]
        public void RepositoryStart_NodeObservers_EnableOne()
        {
            var repoBuilder = CreateRepositoryBuilder()
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(TestNodeObserver1));


            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(1, Providers.Instance.NodeObservers.Length);
                Assert.AreEqual(typeof(TestNodeObserver1), Providers.Instance.NodeObservers[0].GetType());
            }
        }

        [TestMethod]
        public void RepositoryStart_NodeObservers_EnableMore()
        {
            var repoBuilder = CreateRepositoryBuilder()
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(TestNodeObserver1), typeof(TestNodeObserver2));

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
            var repoBuilder = CreateRepositoryBuilder()
                .DisableNodeObservers(typeof(TestNodeObserver1));

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
            var repoBuilder = CreateRepositoryBuilder();

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
            try
            {
                Test2(services =>
                {
                    services.AddAuditEventWriter<TestAuditEventWriter>();
                }, () =>
                {
                    Assert.IsNotNull(Providers.Instance.AuditEventWriter);
                    Assert.AreSame(Providers.Instance.AuditEventWriter, SnLog.AuditEventWriter);
                });
            }
            finally
            {
                SnLog.AuditEventWriter = originalWriter;
            }
        }

        [TestMethod]
        public void RepositoryStart_DataProviderExtensions_OverrideDefault()
        {
            // switch this ON here for testing purposes (to check that repo start does not override it)
            SnTrace.Custom.Enabled = true;

            Test2(services =>
            {
                services.AddSingleton<IAccessTokenDataProvider, TestAccessTokenDataProvider>();
            }, () =>
            {
                Assert.AreEqual(typeof(TestAccessTokenDataProvider), Providers.Instance.GetProvider<IAccessTokenDataProvider>().GetType());
            });
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
