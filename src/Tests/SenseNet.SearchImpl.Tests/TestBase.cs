using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Lucene29;
using SenseNet.SearchImpl.Tests.Implementations;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Security.Messaging;

namespace SenseNet.SearchImpl.Tests
{
    [TestClass]
    public class TestBase
    {
        #region Prototype
        private class RepoBuilder
        {
            // Ensure that all providers use these slots
            public DataProvider DataProvider { get; private set; }
            public ITransactionFactory TransactionFactory { get; private set; }
            public ISearchEngine SearchEngine { get; private set; }
            public ISearchEngineSupport SearchEngineSupport { get; private set; }
            public AccessProvider AccessProvider { get; private set; }
            /* ... */

            public Dictionary<Type, Type[]> TypeHandlerInitialization { get; private set; }
            /* ... */

            public RepoBuilder UseDataProvider(DataProvider dataProvider)
            {
                this.DataProvider = dataProvider;
                return this;
            }
            public RepoBuilder UseTransactionFactory(ITransactionFactory transactionFactory)
            {
                this.TransactionFactory = transactionFactory;
                return this;
            }
            public RepoBuilder UseSearchEngine(ISearchEngine searchEngine)
            {
                this.SearchEngine = searchEngine;
                return this;
            }
            public RepoBuilder UseSearchEngineSupport(ISearchEngineSupport searchEngineSupport)
            {
                this.SearchEngineSupport = searchEngineSupport;
                return this;
            }
            public RepoBuilder UseAccessProvider(AccessProvider accessProvider)
            {
                this.AccessProvider = accessProvider;
                return this;
            }
            /* ... */

            public RepoBuilder InitializeTypeHandler(Dictionary<Type, Type[]> providers)
            {
                this.TypeHandlerInitialization = providers;
                return this;
            }
            /* ... */
        }

        private class RepositoryThatBuiltBasedOnAVeryMNodernApproach : IDisposable
        {
            private readonly List<IDisposable> _swindlers = new List<IDisposable>();

            public RepositoryThatBuiltBasedOnAVeryMNodernApproach(RepoBuilder builder)
            {
                if (builder.DataProvider == null)
                {
                    // read from config and instantiate
                }
                if (builder.TransactionFactory == null)
                {
                    // read from config and instantiate
                }
                /* ... */

                #region hacked version

                if (builder.TypeHandlerInitialization != null)
                    TypeHandler.Initialize(builder.TypeHandlerInitialization);

                if (builder.DataProvider != null)
                    _swindlers.Add(Tools.Swindle(typeof(DataProvider), "_current", builder.DataProvider));
                if (builder.TransactionFactory != null)
                    _swindlers.Add(Tools.Swindle(typeof(CommonComponents), "TransactionFactory", builder.TransactionFactory));

                var inMemDb = builder.DataProvider as InMemoryDataProvider;
                if (inMemDb != null)
                    StartSecurity(inMemDb);

                if (builder.SearchEngine != null)
                    _swindlers.Add(new Tools.SearchEngineSwindler(builder.SearchEngine));
                if (builder.SearchEngineSupport != null)
                    _swindlers.Add(Tools.Swindle(typeof(StorageContext.Search), "ContentRepository", builder.SearchEngineSupport));
                if (builder.AccessProvider != null)
                    _swindlers.Add(Tools.Swindle(typeof(AccessProvider), "_current", builder.AccessProvider));

                #endregion
            }

            public void Dispose()
            {
                foreach(var swindler in _swindlers)
                    swindler.Dispose();
            }
        }

        private IDisposable RepositoryStart(RepoBuilder builder)
        {
            return new RepositoryThatBuiltBasedOnAVeryMNodernApproach(builder);
        }
        #endregion

        // ORIGINAL TEST WITHOUT USING PROTOTYPE
        //protected T Test<T>(Func<T> callback)
        //{
        //    TypeHandler.Initialize(new Dictionary<Type, Type[]>
        //    {
        //        {typeof(ElevatedModificationVisibilityRule), new[] {typeof(SnElevatedModificationVisibilityRule)}}
        //    });

        //    var dataProvider = new InMemoryDataProvider();
        //    StartSecurity(dataProvider);

        //    DistributedApplication.Cache.Reset();

        //    using (new Tools.SearchEngineSwindler(new InMemorySearchEngine()))
        //    using (Tools.Swindle(typeof(StorageContext.Search), "ContentRepository", new SearchEngineSupport()))
        //    using (Tools.Swindle(typeof(AccessProvider), "_current", new DesktopAccessProvider()))
        //    using (Tools.Swindle(typeof(DataProvider), "_current", dataProvider))
        //    using (new SystemAccount())
        //    {
        //        CommonComponents.TransactionFactory = dataProvider;
        //        IndexManager.Start(new InMemoryIndexingEngineFactory(), TextWriter.Null);
        //        return callback();
        //    }
        //}

        protected T Test<T>(Func<T> callback)
        {
            DistributedApplication.Cache.Reset();

            var repoBuilder = new RepoBuilder();
            using (RepositoryStart(repoBuilder
                .UseDataProvider(new InMemoryDataProvider())
                .UseTransactionFactory(repoBuilder.DataProvider)
                .UseSearchEngine(new InMemorySearchEngine())
                .UseSearchEngineSupport(new SearchEngineSupport())
                .UseAccessProvider(new DesktopAccessProvider())
                .InitializeTypeHandler(new Dictionary<Type, Type[]>
                {
                    {typeof(ElevatedModificationVisibilityRule), new[] {typeof(SnElevatedModificationVisibilityRule)}}
                })
                ))
            using (new SystemAccount())
            {
                IndexManager.Start(TextWriter.Null);
                return callback();
            }
        }


        protected static void StartSecurity(InMemoryDataProvider repo)
        {
            var securityDataProvider = new MemoryDataProvider(new DatabaseStorage
            {
                Aces = new List<StoredAce>
                {
                    new StoredAce {EntityId = 2, IdentityId = 1, LocalOnly = false, AllowBits = 0x0EF, DenyBits = 0x000}
                },
                Entities = repo.GetSecurityEntities().ToDictionary(e => e.Id, e => e),
                Memberships = new List<Membership>
                {
                    new Membership
                    {
                        GroupId = Identifiers.AdministratorsGroupId,
                        MemberId = Identifiers.AdministratorUserId,
                        IsUser = true
                    }
                },
                Messages = new List<Tuple<int, DateTime, byte[]>>()
            });

            SecurityHandler.StartSecurity(false, securityDataProvider, new DefaultMessageProvider());
        }

        protected void SaveInitialIndexDocuments()
        {
            var idSet = DataProvider.LoadIdsOfNodesThatDoNotHaveIndexDocument(0, 1100);
            var nodes = Node.LoadNodes(idSet);

            if (nodes.Count == 0)
                return;

            foreach (var node in nodes)
            {
                bool hasBinary;
                DataBackingStore.SaveIndexDocument(node, false, false, out hasBinary);
            }
        }

    }
}
