using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal;
using SenseNet.Portal.Virtualization;
using SenseNet.Tests.Implementations;

namespace SenseNet.Tests.SelfTest
{
    [TestClass]
    public class InitialDataTests : TestBase
    {
        //[TestMethod]
        public void InitialData_Create()
        {
            DataStore.Enabled = false;
            Test(() =>
            {
                using (var ptw = new StreamWriter(@"D:\propertyTypes.txt", false))
                using (var ntw = new StreamWriter(@"D:\nodeTypes.txt", false))
                using (var nw = new StreamWriter(@"D:\nodes.txt", false))
                using (var vw = new StreamWriter(@"D:\versions.txt", false))
                using (var dw = new StreamWriter(@"D:\dynamicData.txt", false))
                    InitialData.Save(ptw, ntw, nw, vw, dw, null,
                        () => ((InMemoryDataProvider)DataProvider.Current).DB.Nodes.Select(x => x.NodeId)); //DB:ok

                var index = ((InMemoryIndexingEngine)Providers.Instance.SearchEngine.IndexingEngine).Index;
                index.Save(@"D:\index.txt");
            });
            Assert.Inconclusive();
        }
        //[TestMethod]
        public void InitialData_Parse()
        {
            InitialData initialData;
            {
                using (var ptr = new StreamReader(@"D:\propertyTypes.txt"))
                using (var ntr = new StreamReader(@"D:\nodeTypes.txt"))
                using (var nr = new StreamReader(@"D:\nodes.txt"))
                using (var vr = new StreamReader(@"D:\versions.txt"))
                using (var dr = new StreamReader(@"D:\dynamicData.txt"))
                    initialData = InitialData.Load(ptr, ntr, nr, vr, dr);
            }
            Assert.IsTrue(initialData.Nodes.Any());
            Assert.Inconclusive();
        }
        //[TestMethod]
        public void InitialData_LoadIndex()
        {
            DataStore.Enabled = false;
            Test(() =>
            {
                var index = ((InMemoryIndexingEngine)Providers.Instance.SearchEngine.IndexingEngine).Index;
                index.Save(@"D:\index.txt");

                var loaded = new InMemoryIndex();
                loaded.Load(@"D:\index.txt");

                loaded.Save(@"D:\index1.txt");
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
        private void InitialDataTest(Action callback)
        {
            DataStore.Enabled = EnableDataStore;

            DistributedApplication.Cache.Reset();
            ContentTypeManager.Reset();
            var portalContextAcc = new PrivateType(typeof(PortalContext));
            portalContextAcc.SetStaticField("_sites", new Dictionary<string, Site>());

            var builder = CreateRepositoryBuilderForTest();

            Indexing.IsOuterSearchEngineEnabled = true;

            DistributedApplication.Cache.Reset();
            ContentTypeManager.Reset();

            using (Repository.Start(builder))
            {
                using (new SystemAccount())
                {
                    SecurityHandler.CreateAclEditor()
                        .Allow(Identifiers.PortalRootId, Identifiers.AdministratorsGroupId, false, PermissionType.BuiltInPermissionTypes)
                        .Allow(Identifiers.PortalRootId, Identifiers.AdministratorUserId, false, PermissionType.BuiltInPermissionTypes)
                        .Apply();

                    new SnMaintenance().Shutdown();

                    callback();
                }
            }
        }

    }
}
