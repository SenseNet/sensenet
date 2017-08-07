using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using SenseNet.Search.Tests.Implementations;
using SenseNet.SearchImpl.Tests.Implementations;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Security.Messaging;

namespace SenseNet.SearchImpl.Tests
{
    [TestClass]
    public class InMemoryDataProviderTests
    {
        [TestMethod]
        public void InMemDb_LoadRootById()
        {
            var node = Test(() => Node.LoadNode(Identifiers.PortalRootId));

            Assert.AreEqual(Identifiers.PortalRootId, node.Id);
            Assert.AreEqual(Identifiers.RootPath, node.Path);
        }
        [TestMethod]
        public void InMemDb_LoadRootByPath()
        {
            var node = Test(() => Node.LoadNode(Identifiers.RootPath));

            Assert.AreEqual(Identifiers.PortalRootId, node.Id);
            Assert.AreEqual(Identifiers.RootPath, node.Path);
        }
        [TestMethod]
        public void InMemDb_Create()
        {
            var lastNodeId = InMemoryDataProvider.LastNodeId;

            var node = Test<Node>(() =>
            {
                var root = Node.LoadNode(Identifiers.RootPath);
                var n = new TestNode(root)
                {
                    Name = "Node1",
                    DisplayName = "Node 1"
                };
                foreach (var observer in NodeObserver.GetObserverTypes())
                    n.DisableObserver(observer);
                n.Save();
                n = Node.Load<TestNode>(n.Id);
                return n;
            });

            Assert.AreEqual(lastNodeId+1, node.Id);
            Assert.AreEqual("/Root/Node1", node.Path);
        }



        /* ============================================================================ */

        public static T Test<T>(Func<T> callback)
        {
            TypeHandler.Initialize(new Dictionary<Type, Type[]>
            {
                {typeof(ElevatedModificationVisibilityRule), new[] {typeof(SnElevatedModificationVisibilityRule)}}
            });

            var dataProvider = new InMemoryDataProvider();
            StartSecurity(dataProvider);

            DistributedApplication.Cache.Reset();

            using (new Tools.SearchEngineSwindler(new TestSearchEngine()))
            using (Tools.Swindle(typeof(StorageContext.Search), "ContentRepository", new TestSearchEngineSupport(DefaultIndexingInfo)))
            using (Tools.Swindle(typeof(AccessProvider), "_current", new DesktopAccessProvider()))
            using (Tools.Swindle(typeof(DataProvider), "_current", dataProvider))
            using (new SystemAccount())
            {
                return callback();
            }
        }

        private static readonly Dictionary<string, IPerFieldIndexingInfo>
            DefaultIndexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
            {
                {"_Text", new TestPerfieldIndexingInfoString()},
                {"Id", new TestPerfieldIndexingInfoInt()},
                {"Name", new TestPerfieldIndexingInfoString()},
            };

        private static void StartSecurity(InMemoryDataProvider repo)
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

    }
}
