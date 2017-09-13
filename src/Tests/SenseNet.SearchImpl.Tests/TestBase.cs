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

            Indexing.IsOuterSearchEngineEnabled = true;
            using (var repo = Repository.Start(new RepositoryBuilder()
                .UseDataProvider(new InMemoryDataProvider())
                .UseSearchEngine(new InMemorySearchEngine())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .StartWorkflowEngine(false)))
            using (new SystemAccount())
            {
                return callback();
            }
        }


        protected static ISecurityDataProvider GetSecurityDataProvider(InMemoryDataProvider repo)
        {
            return new MemoryDataProvider(new DatabaseStorage
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
