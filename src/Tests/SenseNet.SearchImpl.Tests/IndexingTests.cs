using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
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
            var indexingInfo = new Lucene.Net.Support.Dictionary<string, IPerFieldIndexingInfo>
            {
                {"_Text", new TestPerfieldIndexingInfoString()},
                {"Id", new TestPerfieldIndexingInfoInt()},
                {"Name", new TestPerfieldIndexingInfoString()},
            };

            StorageContext.Search.ContentRepository = new TestSearchEngineSupport(indexingInfo);

            var storageContextAcc = new PrivateType(typeof(StorageContext));
            var storageContextInstance = storageContextAcc.GetStaticFieldOrProperty("Instance");
            var storageContextInstanceAcc = new PrivateObject(storageContextInstance);
            storageContextInstanceAcc.SetField("_searchEngine", new TestSearchEngine());

            var securityHandlerAcc = new PrivateType(typeof(SecurityHandler));
            securityHandlerAcc.SetStaticField("_securityContextFactory", new DynamicSecurityContextFactory());


            TypeHandler.Initialize(new Lucene.Net.Support.Dictionary<Type, Type[]>
            {
                {typeof(ElevatedModificationVisibilityRule), new [] {typeof(SnElevatedModificationVisibilityRule) }}
            });

            var dataProvider = new InMemoryDataProvider();
            StartSecurity(dataProvider);

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
                node.DisableObserver(typeof(SenseNet.ContentRepository.Storage.AppModel.AppCacheInvalidator));
                node.DisableObserver(typeof(SenseNet.ContentRepository.Storage.AppModel.RepositoryEventRouter));
                node.DisableObserver(typeof(SenseNet.Preview.DocumentPreviewObserver));
                node.DisableObserver(typeof(SenseNet.ApplicationModel.AppStorageInvalidator));
                node.DisableObserver(typeof(SenseNet.ContentRepository.SettingsCache));
                node.DisableObserver(typeof(SenseNet.ContentRepository.Storage.Security.GroupMembershipObserver));
                node.Save();

                var indexDoc = DataProvider.Current.LoadIndexDocumentByVersionId(node.VersionId);
                Assert.IsNotNull(indexDoc);
            }
        }

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
    }
}
