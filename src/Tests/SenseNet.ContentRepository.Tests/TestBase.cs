using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Tests.Implementations;
using SenseNet.Diagnostics;
using SenseNet.Security;
using SenseNet.Security.Data;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class TestBase
    {
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void InitializeTest()
        {
            SnTrace.Test.Enabled = true;
            SnTrace.Test.Write("START test: {0}", TestContext.TestName);
        }

        [TestCleanup]
        public void CleanupTest()
        {
            SnTrace.Test.Enabled = true;
            SnTrace.Test.Write("END test: {0}", TestContext.TestName);
            SnTrace.Flush();
        }

        protected T Test<T>(Func<T> callback)
        {
            DistributedApplication.Cache.Reset();

            Indexing.IsOuterSearchEngineEnabled = true;
            using (Repository.Start(new RepositoryBuilder()
                .UseDataProvider(new InMemoryDataProvider())
                .UseSearchEngine(new InMemorySearchEngine())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .StartWorkflowEngine(false)
                .UseTraceCategories(new[] { "Test", "Event", "System", "Repository" })))
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
