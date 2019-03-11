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
using System.Web.UI.WebControls;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Portal.AppModel;
using SenseNet.Services.Wopi;
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
            var node = CreateTestFolder();
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
            var node = CreateTestFolder();
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
            var node = CreateTestFolder();
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
            var node = CreateTestFolder();
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
            var node = CreateTestFolder();
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
            var node = CreateTestFolder();
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
            var node = CreateTestFolder();
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
            var node = CreateTestFolder();
            var nodeId = node.Id;
            var oldLockValue = Guid.NewGuid().ToString();
            var newLockValue = Guid.NewGuid().ToString();

            // ACTION
            SharedLock.ModifyLock(nodeId, oldLockValue, newLockValue);
        }

        [TestMethod]
        public void SharedLock_RefreshLock()
        {
            var node = CreateTestFolder();
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
            var node = CreateTestFolder();
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
            var node = CreateTestFolder();
            var nodeId = node.Id;
            var lockValue = "LCK_" + Guid.NewGuid();

            // ACTION
            SharedLock.RefreshLock(nodeId, lockValue);
        }

        [TestMethod]
        public void SharedLock_Unlock()
        {
            var node = CreateTestFolder();
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
            var node = CreateTestFolder();
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
            var node = CreateTestFolder();
            var nodeId = node.Id;
            var existingLock = "LCK_" + Guid.NewGuid();

            // ACTION
            SharedLock.Unlock(nodeId, existingLock);
        }

        //[TestMethod]
        //public void SharedLock_Save_Matching()
        //{
        //    var file = CreateTestFile(CreateTestFolder(), "Lorem ipsum...");
        //    var fileId = file.Id;
        //    var index = file.Index;
        //    var lockValue = "LCK_" + Guid.NewGuid();
        //    SharedLock.Lock(fileId, lockValue);

        //    file.Index++;
        //    WopiHandler_SaveFile(file, lockValue);

        //    var reloadedNode = Node.LoadNode(fileId);
        //    Assert.AreEqual(index + 1, reloadedNode.Index);
        //}
        //[TestMethod]
        //[ExpectedException(typeof(LockedNodeException))]
        //public void SharedLock_Save_MissingLock()
        //{
        //    //var node = CreaeTestContent();
        //    //var lockValue = "LCK_" + Guid.NewGuid();

        //    //node.Index++;
        //    //node.SaveWithSharedLock(lockValue);
        //    Assert.Inconclusive();
        //}
        //[TestMethod]
        //[ExpectedException(typeof(LockedNodeException))]
        //public void SharedLock_Save_DifferentLock()
        //{
        //    //var node = CreaeTestContent();
        //    //var nodeId = node.Id;
        //    //var lockValue1 = "LCK_" + Guid.NewGuid();
        //    //var lockValue2 = "LCK_" + Guid.NewGuid();
        //    //SharedLock.Lock(nodeId, lockValue1);

        //    //node.Index++;
        //    //node.SaveWithSharedLock(lockValue2);
        //    Assert.Inconclusive();
        //}
        //[TestMethod]
        //[ExpectedException(typeof(LockedNodeException))]
        //public void SharedLock_Save_Locked()
        //{
        //    var file = CreateTestFile(CreateTestFolder(), "Lorem ipsum...");
        //    var fileId = file.Id;
        //    var lockValue = "LCK_" + Guid.NewGuid();
        //    SharedLock.Lock(fileId, lockValue);

        //    file.Index++;
        //    file.Save();
        //}



        [TestMethod]
        public void SharedLock_Save_Unchecked_Unlocked_MetaOnly()
        {
            var context = SavingTestContext.Create().SaveMetadata(42);

            Assert.AreEqual(42, context.LoadTestFile().Index);
        }
        [TestMethod]
        public void SharedLock_Save_Unchecked_Unlocked_Upload()
        {
            var newContent = "Dolor sit amet...";

            var context = SavingTestContext.Create().UpdateFileContent(newContent);

            Assert.AreEqual(newContent, RepositoryTools.GetStreamString(context.LoadTestFile().Binary.GetStream()));
        }
        [TestMethod]
        public void SharedLock_Save_Unchecked_Unlocked_PutFile()
        {
            var newContent = "Dolor sit amet...";
            var lockValue = "LCK_" + Guid.NewGuid();

            var context = SavingTestContext.Create().WopiSave(lockValue, newContent);

            // expected result: file was not changed
            Assert.AreEqual(SavingTestContext.OriginalFileContent, RepositoryTools.GetStreamString(context.LoadTestFile().Binary.GetStream()));
        }
        [TestMethod]
        public void SharedLock_Save_Unchecked_Locked_MetaOnly()
        {
            var lockValue = "LCK_" + Guid.NewGuid();

            var context = SavingTestContext.Create().Lock(lockValue).SaveMetadata(42);

            Assert.AreEqual(42, context.LoadTestFile().Index);
        }
        [TestMethod]
        public void SharedLock_Save_Unchecked_Locked_Upload()
        {
            var newContent = "Dolor sit amet...";
            var lockValue = "LCK_" + Guid.NewGuid();

            ExpectError(typeof(InvalidContentActionException), () =>
            {
                var context = SavingTestContext.Create().Lock(lockValue).UpdateFileContent(newContent);
            });
        }
        [TestMethod]
        public void SharedLock_Save_Unchecked_LockedSame_PutFile()
        {
            var newContent = "Dolor sit amet...";
            var lockValue = "LCK_" + Guid.NewGuid();

            var context = SavingTestContext.Create().Lock(lockValue).WopiSave(lockValue, newContent);

            Assert.AreEqual(newContent, RepositoryTools.GetStreamString(context.LoadTestFile().Binary.GetStream()));
        }
        [TestMethod]
        public void SharedLock_Save_Unchecked_LockedAnother_PutFile()
        {
            var newContent = "Dolor sit amet...";
            var lockValue1 = "LCK_" + Guid.NewGuid();
            var lockValue2 = "LCK_" + Guid.NewGuid();

            var context = SavingTestContext.Create().Lock(lockValue1).WopiSave(lockValue2, newContent);

            // expected result: file was not changed
            Assert.AreEqual(SavingTestContext.OriginalFileContent, RepositoryTools.GetStreamString(context.LoadTestFile().Binary.GetStream()));
        }

        [TestMethod]
        public void SharedLock_Save_CheckedOutForMe_Unlocked_MetaOnly()
        {
            var context = SavingTestContext.Create().Checkout().SaveMetadata(42);

            Assert.AreEqual(42, context.LoadTestFile().Index);
        }
        [TestMethod]
        public void SharedLock_Save_CheckedOutForMe_Unlocked_Upload()
        {
            var newContent = "Dolor sit amet...";

            var context = SavingTestContext.Create().Checkout().UpdateFileContent(newContent);

            Assert.AreEqual(newContent, RepositoryTools.GetStreamString(context.LoadTestFile().Binary.GetStream()));
        }
        [TestMethod]
        public void SharedLock_Save_CheckedOutForMe_Unlocked_PutFile()
        {
            var newContent = "Dolor sit amet...";
            var lockValue = "LCK_" + Guid.NewGuid();

            var context = SavingTestContext.Create().Checkout().WopiSave(lockValue, newContent);

            // expected result: file was not changed
            Assert.AreEqual(SavingTestContext.OriginalFileContent, RepositoryTools.GetStreamString(context.LoadTestFile().Binary.GetStream()));
        }
        [TestMethod]
        public void SharedLock_Save_CheckedOutForMe_Locked_MetaOnly()
        {
            var lockValue = "LCK_" + Guid.NewGuid();

            var context = SavingTestContext.Create().Lock(lockValue).Checkout().SaveMetadata(42);

            Assert.AreEqual(42, context.LoadTestFile().Index);
        }
        [TestMethod]
        public void SharedLock_Save_CheckedOutForMe_Locked_Upload()
        {
            var newContent = "Dolor sit amet...";
            var lockValue = "LCK_" + Guid.NewGuid();

            ExpectError(typeof(InvalidContentActionException), () =>
            {
                var context = SavingTestContext.Create().Lock(lockValue).Checkout().UpdateFileContent(newContent);
            });
        }
        [TestMethod]
        public void SharedLock_Save_CheckedOutForMe_LockedSame_PutFile()
        {
            var newContent = "Dolor sit amet...";
            var lockValue = "LCK_" + Guid.NewGuid();

            var context = SavingTestContext.Create().Lock(lockValue).Checkout().WopiSave(lockValue, newContent);

            Assert.AreEqual(newContent, RepositoryTools.GetStreamString(context.LoadTestFile().Binary.GetStream()));
        }
        [TestMethod]
        public void SharedLock_Save_CheckedOutForMe_LockedAnother_PutFile()
        {
            var newContent = "Dolor sit amet...";
            var lockValue1 = "LCK_" + Guid.NewGuid();
            var lockValue2 = "LCK_" + Guid.NewGuid();

            var context = SavingTestContext.Create().Lock(lockValue1).Checkout().WopiSave(lockValue2, newContent);

            // expected result: file was not changed
            Assert.AreEqual(SavingTestContext.OriginalFileContent, RepositoryTools.GetStreamString(context.LoadTestFile().Binary.GetStream()));
        }

        [TestMethod] public void SharedLock_Save_CheckedOutAnother_Unlocked_MetaOnly()
        {
            ExpectError(typeof(InvalidContentActionException), () =>
            {
                var context = SavingTestContext.Create().Checkout("User1").SaveMetadata(42);
            });
        }

        private void ExpectError(Type expectedErrorType, Action action)
        {
            try
            {
                action();
                Assert.Fail($"Expected {expectedErrorType.Name} exception was not thrown.");
            }
            catch (Exception e)
            {
                Assert.AreEqual(expectedErrorType, e.GetType());
            }
        }

        private class SavingTestContext
        {
            public static readonly string OriginalFileContent = "Lorem ipsum...";
            private int _testFileId;

            public File LoadTestFile()
            {
                if (_testFileId == 0)
                {
                    var folder = new SystemFolder(Repository.Root) {Name = Guid.NewGuid().ToString()};
                    folder.Save();
                    var file = new File(folder) {Name = Guid.NewGuid().ToString()};
                    file.Binary.SetStream(RepositoryTools.GetStreamFromString(OriginalFileContent));
                    file.Save();
                    _testFileId = file.Id;
                }
                return Node.Load<File>(_testFileId);
            }

            public static SavingTestContext Create()
            {
                return new SavingTestContext();
            }

            public SavingTestContext Checkout(string userContentName = null)
            {
                var file = LoadTestFile();
                if (userContentName != null)
                {
                    var user = LoadOrCreateUser(userContentName);
                    var origUser = User.Current;
                    User.Current = user;
                    file.CheckOut();
                    User.Current = origUser;
                }
                else
                {
                    file.CheckOut();
                }
                return this;
            }
            private User LoadOrCreateUser(string name)
            {
                var user = Node.Load<User>($"{RepositoryStructure.ImsFolderPath}/Domain1/{name}");
                if (user == null)
                {
                    var testDomain = new Domain(Repository.ImsFolder) { Name = "Domain1" };
                    testDomain.Save();

                    user = new User(testDomain) { Name = "User1" };
                    user.Save();

                    Group.Administrators.AddMember(user);
                }
                return user;
            }

            public SavingTestContext Lock(string lockValue)
            {
                SharedLock.Lock(LoadTestFile().Id, lockValue);
                return this;
            }

            public SavingTestContext SaveMetadata(int index)
            {
                var file = LoadTestFile();
                file.Index = index;
                file.Save();
                return this;
            }
            public SavingTestContext UpdateFileContent(string newContent)
            {
                var file = LoadTestFile();
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(newContent));
                file.Save();
                return this;
            }
            public SavingTestContext WopiSave(string lockValue, string newContent)
            {
                WopiHandler.ProcessPutFileRequest(LoadTestFile(), lockValue, RepositoryTools.GetStreamFromString(newContent));
                return this;
            }

        }



        [TestMethod]
        public void SharedLock_Checkout()
        {
            var node = CreateTestFolder();
            var nodeId = node.Id;
            var lockValue = "LCK_" + Guid.NewGuid();
            SharedLock.Lock(nodeId, lockValue);

            node.CheckOut();

            Assert.IsTrue(node.Locked);
            Assert.AreEqual(lockValue, SharedLock.GetLock(nodeId));
        }
        [TestMethod]
        [ExpectedException(typeof(LockedNodeException))]
        public void SharedLock_Delete()
        {
            //UNDONE: remove this line when SharedLock assert is implemented in the repo
            Assert.Inconclusive();

            var node = CreateTestFolder();
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

            builder.UseSharedLockDataProviderExtension(new InMemorySharedLockDataProvider());

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

        private SystemFolder CreateTestFolder()
        {
            var folder = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
            folder.Save();
            return folder;
        }
        private File CreateTestFile(Node parent, string fileContent)
        {
            var file = new File(parent) { Name = Guid.NewGuid().ToString() };
            file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent));
            file.Save();
            return file;
        }

        private void SetSharedLockCreationDate(int nodeId, DateTime value)
        {
            if (!(DataProvider.GetExtension<ISharedLockDataProviderExtension>() is InMemorySharedLockDataProvider dataProvider))
                throw new InvalidOperationException("InMemorySharedLockDataProvider not configured.");

            var sharedLockRow = dataProvider.SharedLocks.First(x => x.ContentId == nodeId);
            sharedLockRow.CreationDate = value;
        }
        private DateTime GetSharedLockCreationDate(int nodeId)
        {
            if (!(DataProvider.GetExtension<ISharedLockDataProviderExtension>() is InMemorySharedLockDataProvider dataProvider))
                throw new InvalidOperationException("InMemorySharedLockDataProvider not configured.");

            var sharedLockRow = dataProvider.SharedLocks.First(x => x.ContentId == nodeId);
            return sharedLockRow.CreationDate;
        }

        private readonly PrivateType _wopiHandlerAcc = new PrivateType(typeof(WopiHandler));
        private void WopiHandler_SaveFile(File file, string lockValue)
        {
            _wopiHandlerAcc.InvokeStatic("SaveFile", file, lockValue);
        }
    }
}
