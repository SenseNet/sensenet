using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Tests.Implementations;
using SenseNet.Diagnostics;
using SenseNet.Portal;
using SenseNet.Portal.Virtualization;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class DataProviderTests : TestBase
    {
        [TestMethod]
        public void DPAB_Schema_Save()
        {
            DPTest(() =>
            {
                var storedSchema = GetStoredSchema();
                Assert.AreEqual(0L, storedSchema.Timestamp);
                Assert.IsNull(storedSchema.PropertyTypes);
                Assert.IsNull(storedSchema.NodeTypes);
                Assert.IsNull(storedSchema.ContentListTypes);

                var ed = new SchemaEditor();
                ed.Load();
                var xml = new XmlDocument();
                xml.LoadXml(ed.ToXml());

                DataStore.Enabled = true;
                var ed2 = new SchemaEditor();
                ed2.Load(xml);
                ed2.Register();

                storedSchema = GetStoredSchema();

                Assert.IsTrue(0L < storedSchema.Timestamp);
                Assert.AreEqual(ActiveSchema.PropertyTypes.Count, storedSchema.PropertyTypes.Count);
                Assert.AreEqual(ActiveSchema.NodeTypes.Count, storedSchema.NodeTypes.Count);
                Assert.AreEqual(ActiveSchema.ContentListTypes.Count, storedSchema.ContentListTypes.Count);
                //UNDONE:DB ----Deep check: storedSchema
            });
        }
        private RepositorySchemaData GetStoredSchema()
        {
            return ((InMemoryDataProvider2) Providers.Instance.DataProvider2).DB.Schema;
        }

        [TestMethod]
        public void DPAB_Create()
        {
            DPTest(() =>
            {
                // ACTION-A
                DataStore.SnapshotsEnabled = true;
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                
                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();

                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);
                CheckDynamicDataByVersionId(folderA.VersionId);
            });
        }
        [TestMethod]
        public void DPAB_Create_TextProperty()
        {
            DPTest(() =>
            {
                var description = "text property value.";

                // ACTION-A
                DataStore.SnapshotsEnabled = true;
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Description = description;
                folderA.Save();

                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Description = description;
                folderB.Save();

                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);
                CheckDynamicDataByVersionId(folderA.VersionId);
            });
        }
        [TestMethod]
        public void DPAB_CreateFile()
        {
            DPTest(() =>
            {
                var filecontent = "File content.";

                // ACTION-A
                DataStore.SnapshotsEnabled = false;
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                var fileA = new File(folderA) { Name = "File1" };
                fileA.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent));
                DataStore.SnapshotsEnabled = true;
                fileA.Save();

                // ACTION-B
                DataStore.Enabled = true;
                DataStore.SnapshotsEnabled = false;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                var fileB = new File(folderB) { Name = "File1" };
                fileB.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent));
                DataStore.SnapshotsEnabled = true;
                fileB.Save();

                // ASSERT
                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);
                CheckDynamicDataByVersionId(folderA.VersionId);

            });
        }
        [TestMethod]
        public void DPAB_Update()
        {
            DPTest(() =>
            {
                // PROVIDER-A
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                folderA = Node.Load<SystemFolder>(folderA.Id);
                folderA.Index++;
                DataStore.SnapshotsEnabled = true;
                folderA.Save();
                DataStore.SnapshotsEnabled = false;

                // PROVIDER-B
                DataStore.Enabled = true;
                DistributedApplication.Cache.Reset();
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                folderB = Node.Load<SystemFolder>(folderB.Id);
                folderB.Index++;
                DataStore.SnapshotsEnabled = true;
                folderB.Save();
                DataStore.SnapshotsEnabled = false;

                // Check
                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;

                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);
                Assert.IsTrue(nodeDataBeforeB.NodeTimestamp < nodeDataAfterB.NodeTimestamp);
                Assert.IsTrue(nodeDataBeforeB.VersionTimestamp < nodeDataAfterB.VersionTimestamp);
                CheckDynamicDataByVersionId(folderA.VersionId);
            });
        }
        [TestMethod]
        public void DPAB_UpdateFile()
        {
            DPTest(() =>
            {
                var filecontent1 = "File content 1.";
                var filecontent2 = "File content 2.";

                //// ACTION-A
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                var fileA = new File(folderA) { Name = "File1" };
                fileA.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileA.Save();
                fileA = Node.Load<File>(fileA.Id);
                var binaryA = fileA.Binary;
                binaryA.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileA.Binary = binaryA;
                DataStore.SnapshotsEnabled = true;
                fileA.Save();
                DataStore.SnapshotsEnabled = false;

                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                var fileB = new File(folderB) { Name = "File1" };
                fileB.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileB.Save();
                fileB = Node.Load<File>(fileB.Id);
                var binaryB = fileB.Binary;
                binaryB.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileB.Binary = binaryB;
                DataStore.SnapshotsEnabled = true;
                fileB.Save();
                DataStore.SnapshotsEnabled = false;

                // ASSERT
                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);
                CheckDynamicDataByVersionId(folderA.VersionId);
            });
        }

        //UNDONE:DB TEST: DP_Create with all kind of dynamic properties (string, int, datetime, money, text, reference, binary)
        //UNDONE:DB TEST: DP_Update with all kind of DYNAMIC PROPERTIES (string, int, datetime, money, text, reference, binary)
        //UNDONE:DB TEST: DP_Update with RENAME (assert paths changed in the subtree)


        private void CheckDynamicDataByVersionId(int versionId)
        {
            DataStore.SnapshotsEnabled = false;

            DataStore.Enabled = false;
            DistributedApplication.Cache.Reset();
            var nodeA = Node.LoadNodeByVersionId(versionId);
            var dymacPropertyValuesA = nodeA.PropertyTypes.Select(p => $"{p.Name}:{nodeA[p]}").ToArray();

            DataStore.Enabled = true;
            DistributedApplication.Cache.Reset();
            var nodeB = Node.LoadNodeByVersionId(versionId);
            var dymacPropertyValuesB = nodeB.PropertyTypes.Select(p => $"{p.Name}:{nodeB[p]}").ToArray();

            DataProviderChecker.Assert_AreEqual(nodeA.Data, nodeB.Data);
        }

        private void DPTest(Action callback)
        {
            DataStore.Enabled = false;
            DataStore.SnapshotsEnabled = false;

            DistributedApplication.Cache.Reset();
            ContentTypeManager.Reset();
            var portalContextAcc = new PrivateType(typeof(PortalContext));
            portalContextAcc.SetStaticField("_sites", new Dictionary<string, Site>());

            var builder = CreateRepositoryBuilderForTest();

            Indexing.IsOuterSearchEngineEnabled = true;

            var dp2 = new InMemoryDataProvider2();
            Providers.Instance.DataProvider2 = dp2;
            Providers.Instance.BlobMetaDataProvider2 = new InMemoryBlobStorageMetaDataProvider2(dp2);

            using (Repository.Start(builder))
            {
                DataStore.InstallDefaultStructure();
                new SnMaintenance().Shutdown();
                using (new SystemAccount())
                    callback();
            }
        }
    }
}
