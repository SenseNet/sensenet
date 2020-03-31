using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.InMemory;
using SenseNet.Diagnostics;
using SenseNet.Security.Data;
using SenseNet.Tests.Implementations;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Tests.SelfTest
{
    [TestClass]
    public class InitialDataTests : TestBase
    {
        //[TestMethod]
        public async Task InitialData_RebuildIndex()
        {
            if (SnTrace.SnTracers.Count != 2)
                SnTrace.SnTracers.Add(new SnDebugViewTracer());

            await Test(async () =>
            {
                using (var op = SnTrace.Test.StartOperation("@@ ========= Populate"))
                {
                    try
                    {
                        await SearchManager.GetIndexPopulator().RebuildIndexDirectlyAsync("/Root",
                            CancellationToken.None, IndexRebuildLevel.DatabaseAndIndex).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Assert.Fail();
                    }
                    op.Successful = true;
                }

                InitialData.Save(@"D:\_InitialData", null,
                    () => ((InMemoryDataProvider)DataStore.DataProvider).DB.Nodes.Select(x => x.NodeId));

                var index = ((InMemorySearchEngine)Providers.Instance.SearchEngine).Index;
                index.Save(@"D:\_InitialData\index.txt");
            });
        }

        //[TestMethod]
        public void InitialData_Create()
        {
            Test(() =>
            {
                InitialData.Save(@"D:\_InitialData", null,
                    () => ((InMemoryDataProvider)DataStore.DataProvider).DB.Nodes.Select(x => x.NodeId));

                var index = ((InMemorySearchEngine)Providers.Instance.SearchEngine).Index;
                index.Save(@"D:\index.txt");
            });
            Assert.Inconclusive();
        }
        //[TestMethod]
        public void InitialData_Parse()
        {
            var initialData = InitialData.Load(@"D:\_InitialData");
            
            Assert.IsTrue(initialData.Nodes.Any());
            Assert.Inconclusive();
        }
        //[TestMethod]
        public void InitialData_LoadIndex()
        {
            Test(() =>
            {
                var index = ((InMemorySearchEngine)Providers.Instance.SearchEngine).Index;
                index.Save(@"D:\_InitialData\index.txt");

                var loaded = new InMemoryIndex();
                loaded.Load(@"D:\index.txt");

                loaded.Save(@"D:\_InitialData\index1.txt");
            });
            Assert.Inconclusive();
        }
        //[TestMethod]
        public void InitialData_CtdLoad()
        {
            InitialDataTest(() =>
            {
                var fileContentType = ContentType.GetByName("File");
                var ctd = RepositoryTools.GetStreamString(fileContentType.Binary.GetStream());
                Assert.IsNotNull(ctd);
                Assert.IsTrue(ctd.Length > 10);
            });
        }

        [TestMethod]
        public void InitialData_Permissions_Parse()
        {
            var src = new List<string>
            {
                "+2|Normal|+1:_____________________________________________+++++++++++++++++++",
                // break is irrelevant because this is the second entry.
                "-2|Normal|+7:_____________________________________________+++++++++++++++++++",
                "+6|Normal|-6:_______________________________________________________________+",
                // break is relevant because this is the first entry.
                "-1000|Normal|+6:_______________________________________________________________+",
            };
            InitialDataTest(() =>
            {
                var actions = SecurityHandler.ParseInitialPermissions(SecurityHandler.SecurityContext, src).ToArray();

                Assert.AreEqual(3, actions.Length);

                Assert.IsFalse(actions[0].Break);
                Assert.IsFalse(actions[0].Unbreak);
                Assert.AreEqual(2, actions[0].Entries.Count);
                Assert.AreEqual(2, actions[0].Entries[0].EntityId);
                Assert.AreEqual(1, actions[0].Entries[0].IdentityId);
                Assert.AreEqual(0x07FFFFUL, actions[0].Entries[0].AllowBits);
                Assert.AreEqual(0UL, actions[0].Entries[0].DenyBits);
                Assert.IsFalse(actions[0].Entries[0].LocalOnly);
                Assert.AreEqual(2, actions[0].Entries[1].EntityId);
                Assert.AreEqual(7, actions[0].Entries[1].IdentityId);
                Assert.AreEqual(0x07FFFFUL, actions[0].Entries[1].AllowBits);
                Assert.AreEqual(0UL, actions[0].Entries[1].DenyBits);
                Assert.IsFalse(actions[0].Entries[1].LocalOnly);

                Assert.IsFalse(actions[1].Break);
                Assert.IsFalse(actions[1].Unbreak);
                Assert.AreEqual(1, actions[1].Entries.Count);
                Assert.AreEqual(6, actions[1].Entries[0].EntityId);
                Assert.AreEqual(6, actions[1].Entries[0].IdentityId);
                Assert.AreEqual(1UL, actions[1].Entries[0].AllowBits);
                Assert.AreEqual(0UL, actions[1].Entries[0].DenyBits);
                Assert.IsTrue(actions[1].Entries[0].LocalOnly);

                Assert.IsTrue(actions[2].Break);
                Assert.IsFalse(actions[2].Unbreak);
                Assert.AreEqual(1, actions[2].Entries.Count);
                Assert.AreEqual(1000, actions[2].Entries[0].EntityId);
                Assert.AreEqual(6, actions[2].Entries[0].IdentityId);
                Assert.AreEqual(1UL, actions[2].Entries[0].AllowBits);
                Assert.AreEqual(0UL, actions[2].Entries[0].DenyBits);
                Assert.IsFalse(actions[2].Entries[0].LocalOnly);
            });
        }
        [TestMethod]
        public void InitialData_Permissions_Apply()
        {
            var initialData = new InitialData
            {
                Permissions = new List<string>
                {
                    "+6|Normal|-6:_______________________________________________________________+",
                    "-1000|Normal|+6:_______________________________________________________________+",
                }
            };

            InitialSecurityDataTest(() =>
            {
                // PRECHECKS
                // Administrators group has 1 entry on the Root.
                Assert.AreEqual(1, SecurityHandler.GetExplicitEntries(2, new[] { 7 }).Count);
                // Visitor has no any permission.
                Assert.IsFalse(SecurityHandler.HasPermission(User.Visitor, 6, PermissionType.See));
                Assert.IsFalse(SecurityHandler.HasPermission(User.Visitor, 1000, PermissionType.See));
                // There is no break.
                Assert.IsTrue(SecurityHandler.IsEntityInherited(6));
                Assert.IsTrue(SecurityHandler.IsEntityInherited(1000));

                // ACTION
                SecurityHandler.SecurityInstaller.InstallDefaultSecurityStructure(initialData);

                // ASSERT
                // Administrators group has an entry on the Root.
                Assert.AreEqual(1 , SecurityHandler.GetExplicitEntries(2, new[] { 7 }).Count);
                // Visitor has See permission on both contents.
                Assert.IsTrue(SecurityHandler.HasPermission(User.Visitor, 6, PermissionType.See));
                Assert.IsTrue(SecurityHandler.HasPermission(User.Visitor, 1000, PermissionType.See));
                // The second content is not inherited.
                Assert.IsTrue(SecurityHandler.IsEntityInherited(6));
                Assert.IsFalse(SecurityHandler.IsEntityInherited(1000));
            });
        }

        private void InitialSecurityDataTest(Action callback)
        {
            InitialDataTestPrivate(callback, false);
        }
        private void InitialDataTest(Action callback)
        {
            InitialDataTestPrivate(callback, true);
        }
        private void InitialDataTestPrivate(Action callback, bool withSecurity)
        {
            Cache.Reset();
            ContentTypeManager.Reset();

            var builder = CreateRepositoryBuilderForTest();

            Indexing.IsOuterSearchEngineEnabled = true;

            Cache.Reset();
            ContentTypeManager.Reset();

            using (Repository.Start(builder))
            {
                using (new SystemAccount())
                {
                    if (withSecurity)
                    {
                        var sdbp = new PrivateType(typeof(MemoryDataProvider));
                        var db = (DatabaseStorage)sdbp.GetStaticFieldOrProperty("Storage");
                        db.Aces.Clear();

                        SecurityHandler.CreateAclEditor()
                            .Allow(Identifiers.PortalRootId, Identifiers.AdministratorsGroupId, false,
                                PermissionType.BuiltInPermissionTypes)
                            .Allow(Identifiers.PortalRootId, Identifiers.AdministratorUserId, false,
                                PermissionType.BuiltInPermissionTypes)
                            .Apply();
                    }

                    new SnMaintenance().Shutdown();

                    callback();
                }
            }
        }

    }
}
