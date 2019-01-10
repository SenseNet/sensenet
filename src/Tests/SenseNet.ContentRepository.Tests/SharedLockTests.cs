using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal;
using SenseNet.Portal.Virtualization;
using SenseNet.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tests.Implementations;
// ReSharper disable CoVariantArrayConversion

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class SharedLockTests : TestBase
    {
        [TestMethod]
        public void SharedLock_LockAndGetLock()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var expectedLockValue = Guid.NewGuid().ToString();
            Assert.IsNull(SharedLock.GetLock(nodeId));

            // ACTION
            SharedLock.Lock(nodeId, expectedLockValue);

            var actualLockValue = SharedLock.GetLock(nodeId);
            Assert.AreEqual(expectedLockValue, actualLockValue);
        }
        [TestMethod]
        [ExpectedException(typeof(ContentNotFoundException))]
        public void SharedLock_Lock_MissingContent()
        {
            SharedLock.Lock(0, "LCK_0");
        }
        [TestMethod]
        [ExpectedException(typeof(LockedNodeException))]
        public void SharedLock_Lock_CheckedOut()
        {
            var node = CreaeTestContent();
            node.CheckOut();
            var nodeId = node.Id;
            var expectedLockValue = Guid.NewGuid().ToString();
            Assert.IsNull(SharedLock.GetLock(nodeId));

            // ACTION
            SharedLock.Lock(nodeId, expectedLockValue);
        }
        [TestMethod]
        public void SharedLock_Lock_Same()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var expectedLockValue = Guid.NewGuid().ToString();
            SharedLock.Lock(nodeId, expectedLockValue);
            SetSharedLockCreationDate(nodeId, DateTime.UtcNow.AddMinutes(-10.0d));

            // ACTION
            SharedLock.Lock(nodeId, expectedLockValue);

            Assert.IsTrue((DateTime.UtcNow - GetSharedLockCreationDate(nodeId)).TotalSeconds < 1);
        }
        [TestMethod]
        [ExpectedException(typeof(LockedNodeException))]
        public void SharedLock_Lock_Different()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var oldLockValue = Guid.NewGuid().ToString();
            var newLockValue = Guid.NewGuid().ToString();
            SharedLock.Lock(nodeId, oldLockValue);
            SetSharedLockCreationDate(nodeId, DateTime.UtcNow.AddMinutes(-10.0d));

            // ACTION
            SharedLock.Lock(nodeId, newLockValue);
        }
        [TestMethod]
        public void SharedLock_Lock_DifferentTimedOut()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var oldLockValue = Guid.NewGuid().ToString();
            var newLockValue = Guid.NewGuid().ToString();
            SharedLock.Lock(nodeId, oldLockValue);
            SetSharedLockCreationDate(nodeId, DateTime.UtcNow.AddHours(-1.0d));

            // ACTION
            SharedLock.Lock(nodeId, newLockValue);

            var actualLockValue = SharedLock.GetLock(nodeId);
            Assert.AreEqual(newLockValue, actualLockValue);
        }

        [TestMethod]
        public void SharedLock_ModifyLock()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var oldLockValue = Guid.NewGuid().ToString();
            var newLockValue = Guid.NewGuid().ToString();
            Assert.IsNull(SharedLock.GetLock(nodeId));
            SharedLock.Lock(nodeId, oldLockValue);
            Assert.AreEqual(oldLockValue, SharedLock.GetLock(nodeId));

            // ACTION
            SharedLock.ModifyLock(nodeId, oldLockValue, newLockValue);

            Assert.AreEqual(newLockValue, SharedLock.GetLock(nodeId));
        }
        [TestMethod]
        public void SharedLock_ModifyLockDifferent()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var oldLockValue = Guid.NewGuid().ToString();
            var newLockValue = Guid.NewGuid().ToString();
            Assert.IsNull(SharedLock.GetLock(nodeId));
            SharedLock.Lock(nodeId, oldLockValue);
            Assert.AreEqual(oldLockValue, SharedLock.GetLock(nodeId));

            // ACTION
            var actualLock = SharedLock.ModifyLock(nodeId, "DifferentLock", newLockValue);

            Assert.AreEqual(oldLockValue, actualLock);
        }
        [TestMethod]
        [ExpectedException(typeof(SharedLockNotFoundException))]
        public void SharedLock_ModifyLock_Missing()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var oldLockValue = Guid.NewGuid().ToString();
            var newLockValue = Guid.NewGuid().ToString();

            // ACTION
            SharedLock.ModifyLock(nodeId, oldLockValue, newLockValue);
        }

        [TestMethod]
        public void SharedLock_RefreshLock()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var lockValue = "LCK_" + Guid.NewGuid();
            SharedLock.Lock(nodeId, lockValue);
            SetSharedLockCreationDate(nodeId, DateTime.UtcNow.AddMinutes(-10.0d));

            // ACTION
            SharedLock.RefreshLock(nodeId, lockValue);

            Assert.IsTrue((DateTime.UtcNow - GetSharedLockCreationDate(nodeId)).TotalSeconds < 1);
        }
        [TestMethod]
        public void SharedLock_RefreshLock_Different()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var lockValue = "LCK_" + Guid.NewGuid();
            SharedLock.Lock(nodeId, lockValue);

            // ACTION
            var actualLock = SharedLock.RefreshLock(nodeId, "DifferentLock");

            Assert.AreEqual(lockValue, actualLock);
        }
        [TestMethod]
        [ExpectedException(typeof(SharedLockNotFoundException))]
        public void SharedLock_RefreshLock_Missing()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var lockValue = "LCK_" + Guid.NewGuid();

            // ACTION
            SharedLock.RefreshLock(nodeId, lockValue);
        }

        [TestMethod]
        public void SharedLock_Unlock()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var existingLock = "LCK_" + Guid.NewGuid();
            SharedLock.Lock(nodeId, existingLock);

            // ACTION
            SharedLock.Unlock(nodeId, existingLock);

            Assert.IsNull(SharedLock.GetLock(nodeId));
        }
        [TestMethod]
        public void SharedLock_Unlock_Different()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var existingLock = "LCK_" + Guid.NewGuid();
            SharedLock.Lock(nodeId, existingLock);

            // ACTION
            var actualLock = SharedLock.Unlock(nodeId, "DifferentLock");

            Assert.AreEqual(existingLock, actualLock);
        }
        [TestMethod]
        [ExpectedException(typeof(SharedLockNotFoundException))]
        public void SharedLock_Unlock_Missing()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var existingLock = "LCK_" + Guid.NewGuid();

            // ACTION
            SharedLock.Unlock(nodeId, existingLock);
        }

        [TestMethod]
        public void SharedLock_Save()
        {
            //var node = CreaeTestContent();
            //var nodeId = node.Id;
            //var lockValue = "LCK_" + Guid.NewGuid();
            //SharedLock.Lock(nodeId, lockValue);

            //node.Index++;
            //node.Save(lockValue);

            //var reloadedNode = Node.LoadNode(nodeId);
            //Assert.AreEqual(node.Index + 1, reloadedNode.Index);

            Assert.Inconclusive();
        }
        [TestMethod]
        [ExpectedException(typeof(SharedLockNotFoundException))]
        public void SharedLock_Save_MissingLock()
        {
            //var node = CreaeTestContent();
            //var nodeId = node.Id;
            //var lockValue = "LCK_" + Guid.NewGuid();
            //SharedLock.Lock(nodeId, lockValue);

            //node.Index++;
            //node.Save(lockValue);

            Assert.Inconclusive();
        }
        [TestMethod]
        //[ExpectedException(typeof(DifferentSharedLockException))]
        public void SharedLock_Save_DifferentLock()
        {
            //var node = CreaeTestContent();
            //var nodeId = node.Id;
            //var lockValue = "LCK_" + Guid.NewGuid();
            //SharedLock.Lock(nodeId, lockValue);

            //node.Index++;
            //node.Save(lockValue);

            Assert.Inconclusive();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void SharedLock_Save_WithoutSharedLock()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var lockValue = "LCK_" + Guid.NewGuid();
            SharedLock.Lock(nodeId, lockValue);

            node.Index++;
            node.Save();
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void SharedLock_Checkout()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var lockValue = "LCK_" + Guid.NewGuid();
            SharedLock.Lock(nodeId, lockValue);

            node.CheckOut();
        }
        [TestMethod]
        [ExpectedException(typeof(LockedNodeException))]
        public void SharedLock_Delete()
        {
            var node = CreaeTestContent();
            var nodeId = node.Id;
            var lockValue = "LCK_" + Guid.NewGuid();
            SharedLock.Lock(nodeId, lockValue);

            // ACTION
            node.ForceDelete();
        }

        /* ======================================================================== */

        private static RepositoryInstance _repository;

        [ClassInitialize]
        public static void InitializeRepositoryInstance(TestContext context)
        {
            DistributedApplication.Cache.Reset();
            ContentTypeManager.Reset();
            var portalContextAcc = new PrivateType(typeof(PortalContext));
            portalContextAcc.SetStaticField("_sites", new Dictionary<string, Site>());

            var builder = CreateRepositoryBuilderForTest();

            Indexing.IsOuterSearchEngineEnabled = true;

            _repository = Repository.Start(builder);

            using (new SystemAccount())
            {
                SecurityHandler.CreateAclEditor()
                    .Allow(Identifiers.PortalRootId, Identifiers.AdministratorsGroupId, false, PermissionType.BuiltInPermissionTypes)
                    .Allow(Identifiers.PortalRootId, Identifiers.AdministratorUserId, false, PermissionType.BuiltInPermissionTypes)
                    .Apply();
            }
        }
        [ClassCleanup]
        public static void ShutDownRepository()
        {
            _repository?.Dispose();
        }

        [TestInitialize]
        public void RemoveAllLocks()
        {
            SharedLock.RemoveAllLocks();
        }

        private SystemFolder CreaeTestContent()
        {
            var folder = new SystemFolder(Repository.Root) {Name = Guid.NewGuid().ToString()};
            folder.Save();
            return folder;
        }

        private void SetSharedLockCreationDate(int nodeId, DateTime value)
        {
            var dataProvider = (InMemoryDataProvider)DataProvider.Current;
            var sharedLockRow = dataProvider.DB.SharedLocks.First(x => x.ContentId == nodeId);
            sharedLockRow.CreationDate = value;
        }
        private DateTime GetSharedLockCreationDate(int nodeId)
        {
            var dataProvider = (InMemoryDataProvider)DataProvider.Current;
            var sharedLockRow = dataProvider.DB.SharedLocks.First(x => x.ContentId == nodeId);
            return sharedLockRow.CreationDate;
        }
    }
}
