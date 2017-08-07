using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Tests.Implementations;
using SenseNet.SearchImpl.Tests.Implementations;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Security.Messaging;

namespace SenseNet.SearchImpl.Tests
{
    [TestClass]
    public class IndexingTests
    {
        [TestMethod]
        public void Indexing_0()
        {
            TypeHandler.Initialize(new Dictionary<Type, Type[]>
            {
                {typeof(ElevatedModificationVisibilityRule), new [] {typeof(SnElevatedModificationVisibilityRule) }}
            });

            var dataProvider = new InMemoryDataProvider();
            StartSecurity(dataProvider);

            using (new SearchEngineSwindler(new TestSearchEngine()))
            using (Tools.Swindle(typeof(StorageContext.Search), "ContentRepository", new TestSearchEngineSupport(_defaultIndexingInfo)))
            using (Tools.Swindle(typeof(AccessProvider), "_current", new DesktopAccessProvider()))
            using (Tools.Swindle(typeof(DataProvider), "_current", dataProvider))
            using (new SystemAccount())
            {
                var root = Node.LoadNode(2);
                var node = new TestNode(root)
                {
                    Name = "Node1",
                    DisplayName = "Node 1"
                };
                foreach (var observer in NodeObserver.GetObserverTypes())
                    node.DisableObserver(observer);
                node.Save();

                var indexDoc = DataProvider.Current.LoadIndexDocumentByVersionId(node.VersionId);
                Assert.IsNotNull(indexDoc);
            }
        }

        /* ============================================================================ */

        private readonly Dictionary<string, IPerFieldIndexingInfo> 
            _defaultIndexingInfo = new Dictionary <string, IPerFieldIndexingInfo>
            {
                {"_Text", new TestPerfieldIndexingInfoString()},
                {"Id", new TestPerfieldIndexingInfoInt()},
                {"Name", new TestPerfieldIndexingInfoString()},
            };

        private void StartSecurity(InMemoryDataProvider repo)
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

        private class SearchEngineSwindler : IDisposable
        {
            private readonly PrivateObject _accessor;
            private string _memberName = "_searchEngine";
            private readonly object _originalSearchEngine;

            public SearchEngineSwindler(ISearchEngine searchEngine)
            {
                var storageContextAcc = new PrivateType(typeof(StorageContext));
                var storageContextInstance = storageContextAcc.GetStaticFieldOrProperty("Instance");
                _accessor = new PrivateObject(storageContextInstance);
                _originalSearchEngine = _accessor.GetField(_memberName);
                _accessor.SetField(_memberName, searchEngine);
            }
            public void Dispose()
            {
                _accessor.SetField(_memberName, _originalSearchEngine);
            }
        }

    }
}
