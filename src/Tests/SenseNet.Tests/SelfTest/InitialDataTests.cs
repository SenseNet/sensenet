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
using SenseNet.Diagnostics;
using SenseNet.Portal;
using SenseNet.Tests.Implementations;
using SenseNet.Tests.Implementations2;

namespace SenseNet.Tests.SelfTest
{
    [TestClass]
    public class InitialDataTests : TestBase
    {
        //[TestMethod]
        public void InitialData_RebuildIndex()
        {
            if (SnTrace.SnTracers.Count != 2)
                SnTrace.SnTracers.Add(new SnDebugViewTracer());

            Test(() =>
            {
                using (var op = SnTrace.Test.StartOperation("@@ ========= Populate"))
                {
                    try
                    {
                        SearchManager.GetIndexPopulator()
                            .RebuildIndexDirectly("/Root", IndexRebuildLevel.DatabaseAndIndex);
                    }
                    catch (Exception e)
                    {
                        Assert.Fail();
                    }
                    op.Successful = true;
                }

                using (var ptw = new StreamWriter(@"D:\_InitialData\propertyTypes.txt", false, Encoding.UTF8))
                using (var ntw = new StreamWriter(@"D:\_InitialData\nodeTypes.txt", false, Encoding.UTF8))
                using (var nw = new StreamWriter(@"D:\_InitialData\nodes.txt", false, Encoding.UTF8))
                using (var vw = new StreamWriter(@"D:\_InitialData\versions.txt", false, Encoding.UTF8))
                using (var dw = new StreamWriter(@"D:\_InitialData\dynamicData.txt", false, Encoding.UTF8))
                    InitialData.Save(ptw, ntw, nw, vw, dw, null,
                        () => ((InMemoryDataProvider)DataStore.DataProvider).DB.Nodes.Select(x => x.NodeId)); //DB:ok

                var index = ((InMemorySearchEngine)Providers.Instance.SearchEngine).Index;
                index.Save(@"D:\_InitialData\index.txt");

            });
        }

        //[TestMethod]
        public void InitialData_Create()
        {
            Test(() =>
            {
                using (var ptw = new StreamWriter(@"D:\propertyTypes.txt", false))
                using (var ntw = new StreamWriter(@"D:\nodeTypes.txt", false))
                using (var nw = new StreamWriter(@"D:\nodes.txt", false))
                using (var vw = new StreamWriter(@"D:\versions.txt", false))
                using (var dw = new StreamWriter(@"D:\dynamicData.txt", false))
                    InitialData.Save(ptw, ntw, nw, vw, dw, null,
                        () => ((InMemoryDataProvider)DataStore.DataProvider).DB.Nodes.Select(x => x.NodeId)); //DB:ok

                var index = ((InMemorySearchEngine)Providers.Instance.SearchEngine).Index;
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
            Test(() =>
            {
                var index = ((InMemorySearchEngine)Providers.Instance.SearchEngine).Index;
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
