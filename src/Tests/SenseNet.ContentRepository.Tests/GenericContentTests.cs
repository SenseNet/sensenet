using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class GenericContentTests : TestBase
    {
        // Test name rule:
        //           public void GC_CreateWithLock_CheckIn_FullFalse()
        // General prefix -----> ##
        // Content workflow ------> ######################
        // Versioning and approving ---------------------> #########

        /* ============================================================================================== Update */

        [TestMethod]
        public void GC_Update_NoneFalse()
        {
            UpdateTest(VersioningType.None, ApprovingType.False, "V1.0.A", "V1.0.A");
        }
        [TestMethod]
        public void GC_Update_MajorFalse()
        {
            UpdateTest(VersioningType.MajorOnly, ApprovingType.False, "V1.0.A", "V2.0.A");
        }
        [TestMethod]
        public void GC_Update_FullFalse()
        {
            UpdateTest(VersioningType.MajorAndMinor, ApprovingType.False, "V1.0.A", "V1.1.D");
        }
        [TestMethod]
        public void GC_Update_InheritedFalse()
        {
            UpdateTestInheritedVersioning(InheritableVersioningType.None, ApprovingType.False, "V1.0.A", "V1.0.A");
            UpdateTestInheritedVersioning(InheritableVersioningType.MajorOnly, ApprovingType.False, "V1.0.A", "V2.0.A");
            UpdateTestInheritedVersioning(InheritableVersioningType.MajorAndMinor, ApprovingType.False, "V0.1.D", "V0.2.D");
        }
        [TestMethod]
        public void GC_Update_NoneTrue()
        {
            UpdateTest(VersioningType.None, ApprovingType.True, "V1.0.A", "V2.0.P");
        }
        [TestMethod()]
        public void GC_Update_MajorTrue()
        {
            UpdateTest(VersioningType.MajorOnly, ApprovingType.True, "V1.0.A", "V2.0.P");
        }
        [TestMethod]
        public void GC_Update_FullTrue()
        {
            UpdateTest(VersioningType.MajorAndMinor, ApprovingType.True, "V1.0.A", "V1.1.D");
        }
        [TestMethod]
        public void GC_Update_InheritedTrue()
        {
            UpdateTestInheritedVersioning(InheritableVersioningType.None, ApprovingType.True, "V1.0.A", "V2.0.P");
            UpdateTestInheritedVersioning(InheritableVersioningType.MajorOnly, ApprovingType.True, "V1.0.A", "V2.0.P");
            UpdateTestInheritedVersioning(InheritableVersioningType.MajorAndMinor, ApprovingType.True, "V0.1.D", "V0.2.D");
        }

        [TestMethod]
        public void GC_Update_MajorInherited()
        {
            UpdateTestInheritedApproving(VersioningType.MajorOnly, ApprovingType.False, "V1.0.A", "V2.0.A");
            UpdateTestInheritedApproving(VersioningType.MajorOnly, ApprovingType.True, "V1.0.P", "V1.0.P");
        }
        [TestMethod]
        public void GC_Update_FullInherited()
        {
            UpdateTestInheritedApproving(VersioningType.MajorAndMinor, ApprovingType.False, "V1.0.A", "V1.1.D");
            UpdateTestInheritedApproving(VersioningType.MajorAndMinor, ApprovingType.True, "V1.0.P", "V1.1.D");
        }

        private void UpdateTest(VersioningType versioning, ApprovingType approving, string originalVersion, string expectedVersion)
        {
            Test(() =>
            {
                var file = CreateTestFile();
                if (originalVersion != file.Version.ToString())
                    Assert.Inconclusive();

                file.VersioningMode = versioning;
                file.ApprovingMode = approving;

                file.Save();

                Assert.AreEqual(expectedVersion, file.Version.ToString());

                var file1 = Node.Load<File>(file.Id);
                Assert.AreNotSame(file, file1);
                Assert.AreEqual(file.Version.ToString(), file1.Version.ToString());

            });
        }
        private void UpdateTestInheritedVersioning(InheritableVersioningType parentVersioning, ApprovingType approving, string originalVersion, string expectedVersion)
        {
            Test(() =>
            {
                var parent = CreateTestRoot();
                parent.InheritableVersioningMode = parentVersioning;
                parent.Save();

                var file = CreateTestFile("GCSaveTest", parent);

                if (originalVersion != file.Version.ToString())
                    Assert.Inconclusive();

                file.VersioningMode = VersioningType.Inherited;
                file.ApprovingMode = approving;

                file.Save();
                Assert.AreEqual(expectedVersion, file.Version.ToString());

                var file1 = Node.Load<File>(file.Id);
                Assert.AreNotSame(file, file1);
                Assert.AreEqual(file.Version.ToString(), file1.Version.ToString());

            });
        }
        private void UpdateTestInheritedApproving(VersioningType versioning, ApprovingType parentApproving, string originalVersion, string expectedVersion)
        {
            Test(() =>
            {
                var parent = CreateTestRoot();
                parent.InheritableApprovingMode = parentApproving;
                parent.Save();

                var file = CreateTestFile("GCSaveTest", parent);

                Assert.AreEqual(originalVersion, file.Version.ToString());

                file.VersioningMode = versioning;
                file.ApprovingMode = ApprovingType.Inherited;

                file.Save();
                Assert.AreEqual(expectedVersion, file.Version.ToString());

                var file1 = Node.Load<File>(file.Id);
                Assert.AreNotSame(file, file1);
                Assert.AreEqual(file.Version.ToString(), file1.Version.ToString());

            });
        }

        /* ============================================================================================== */

        [TestMethod]
        public void GC_CreateWithLock_IsLocked()
        {
            Test(() =>
            {
                var file = CreateTestFile(save: false);
                file.Save(SavingMode.KeepVersionAndLock);
                Assert.AreEqual(User.Current.Id, file.LockedById);
            });
        }
        [TestMethod]
        public void GC_CreateWithLock_CheckIn_NoneFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile(save: false);
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.False;

                file.Save(SavingMode.KeepVersionAndLock);

                var fileId = file.Id;
                file = Node.Load<File>(fileId);
                Assert.AreEqual("V1.0.L", file.Version.ToString());
                Assert.AreEqual(1, file.Versions.Count());

                file.CheckIn();

                file = Node.Load<File>(fileId);
                Assert.AreEqual("V1.0.A", file.Version.ToString());
                Assert.AreEqual(1, file.Versions.Count());
            });
        }
        [TestMethod]
        public void GC_CreateWithLock_CheckIn_Publish_FullTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile(save: false);
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.True;

                file.Save(SavingMode.KeepVersionAndLock);

                var fileId = file.Id;
                file = Node.Load<File>(fileId);
                Assert.AreEqual("V0.1.L", file.Version.ToString());
                Assert.AreEqual(1, file.Versions.Count());

                file.CheckIn();

                file = Node.Load<File>(fileId);
                Assert.AreEqual("V0.1.D", file.Version.ToString());
                Assert.AreEqual(1, file.Versions.Count());

                file.Publish();

                file = Node.Load<File>(fileId);
                Assert.AreEqual("V0.1.P", file.Version.ToString());
                Assert.AreEqual(1, file.Versions.Count());
            });
        }
        [TestMethod]
        public void GC_CreateWithLock_CheckIn_FullFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile(save: false);
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.False;

                file.Save(SavingMode.KeepVersionAndLock);

                var fileId = file.Id;
                file = Node.Load<File>(fileId);
                Assert.AreEqual("V0.1.L", file.Version.ToString(), "Version number is not correct after locked save.");
                Assert.AreEqual(1, file.Versions.Count(), "Version count is not correct after locked save.");

                file.CheckIn();

                file = Node.Load<File>(fileId);
                Assert.AreEqual("V0.1.D", file.Version.ToString(), "Version number is not correct after checkin.");
                Assert.AreEqual(1, file.Versions.Count(), "Version count is not correct after checkin.");
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void GC_CreateWithLock_Undo_Error()
        {
            Test(() =>
            {
                var file = CreateTestFile(save: false);
                file.Save(SavingMode.KeepVersionAndLock);
                file.UndoCheckOut();
            });
        }

        /* ============================================================================================== CheckOut */

        [TestMethod]
        public void GC_CheckOut_None_False()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.False;

                file.CheckOut();

                Assert.AreEqual("V2.0.L", file.Version.ToString());
            });
        }
        [TestMethod]
        public void GC_CheckOut_MajorOnly_False()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorOnly;
                file.ApprovingMode = ApprovingType.False;

                file.CheckOut();

                Assert.AreEqual("V2.0.L", file.Version.ToString());
            });
        }
        [TestMethod]
        public void GC_CheckOut_MajorAndMinor_False()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.False;

                file.CheckOut();

                Assert.AreEqual("V1.1.L", file.Version.ToString());
            });
        }
        [TestMethod()]
        public void GC_CheckOut_None_True()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.True;

                file.CheckOut();

                Assert.AreEqual("V2.0.L", file.Version.ToString());
            });
        }
        [TestMethod()]
        public void GC_CheckOut_MajorOnly_True()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorOnly;
                file.ApprovingMode = ApprovingType.True;

                file.CheckOut();

                Assert.AreEqual("V2.0.L", file.Version.ToString());
            });
        }
        [TestMethod()]
        public void GC_CheckOut_MajorAndMinor_True()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.True;

                file.CheckOut();

                Assert.AreEqual("V1.1.L", file.Version.ToString());
            });
        }
        [TestMethod]
        public void GC_CheckOut_SystemUser()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.False;

                using (new SystemAccount())
                    file.CheckOut();

                Assert.AreEqual(User.LoggedInUser.Id, file.LockedById);
                Assert.AreEqual("V2.0.L", file.Version.ToString());
            });
        }

        /* ============================================================================================== CheckIn */

        [TestMethod]
        public void GC_CheckIn_NoneFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.False;

                file.CheckOut();
                file.CheckIn();

                Assert.AreEqual("V1.0.A", file.Version.ToString());
                Assert.AreEqual(1, Node.GetVersionNumbers(file.Id).Count);
            });
        }
        [TestMethod]
        public void GC_CheckIn_MajorFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorOnly;
                file.ApprovingMode = ApprovingType.False;

                file.CheckOut();
                file.CheckIn();

                Assert.AreEqual("V2.0.A", file.Version.ToString());
                Assert.AreEqual(2, Node.GetVersionNumbers(file.Id).Count);
            });
        }
        [TestMethod]
        public void GC_CheckIn_FullFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.False;

                file.CheckOut();
                file.CheckIn();

                Assert.AreEqual("V1.1.D", file.Version.ToString());
                Assert.AreEqual(2, Node.GetVersionNumbers(file.Id).Count);
            });
        }
        [TestMethod]
        public void GC_CheckIn_NoneTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.True;

                file.CheckOut();
                file.CheckIn();

                Assert.AreEqual("V2.0.P", file.Version.ToString());
                Assert.AreEqual(2, Node.GetVersionNumbers(file.Id).Count);
            });
        }
        [TestMethod]
        public void GC_CheckIn_MajorTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorOnly;
                file.ApprovingMode = ApprovingType.True;

                file.CheckOut();
                file.CheckIn();

                Assert.AreEqual("V2.0.P", file.Version.ToString());
                Assert.AreEqual(2, Node.GetVersionNumbers(file.Id).Count);
            });
        }
        [TestMethod]
        public void GC_CheckIn_FullTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.True;

                file.CheckOut();
                file.CheckIn();

                Assert.AreEqual("V1.1.D", file.Version.ToString());
                Assert.AreEqual(2, Node.GetVersionNumbers(file.Id).Count);
            });
        }

        /* ============================================================================================== UndoCheckOut */

        [TestMethod()]
        public void GC_UndoCheckOut_NoneFalse()
        {
            Test(() =>
            {
                var test = CreateTestFile();
                test.VersioningMode = VersioningType.None;
                test.ApprovingMode = ApprovingType.False;

                test.CheckOut();
                test.UndoCheckOut();

                Assert.AreEqual("V1.0.A", test.Version.ToString());
                Assert.AreEqual(1, Node.GetVersionNumbers(test.Id).Count);
            });
        }
        [TestMethod()]
        public void GC_UndoCheckOut_MajorFalse()
        {
            Test(() =>
            {
                var test = CreateTestFile();
                test.VersioningMode = VersioningType.MajorOnly;
                test.ApprovingMode = ApprovingType.False;

                test.CheckOut();
                test.UndoCheckOut();

                Assert.AreEqual("V1.0.A", test.Version.ToString());
                Assert.AreEqual(1, Node.GetVersionNumbers(test.Id).Count);
            });
        }
        [TestMethod()]
        public void GC_UndoCheckOut_FullFalse()
        {
            Test(() =>
            {
                var test = CreateTestFile();
                test.VersioningMode = VersioningType.MajorAndMinor;
                test.ApprovingMode = ApprovingType.False;

                test.CheckOut();
                test.UndoCheckOut();

                Assert.AreEqual("V1.0.A", test.Version.ToString());
                Assert.AreEqual(1, Node.GetVersionNumbers(test.Id).Count);
            });
        }
        [TestMethod()]
        public void GC_UndoCheckOut_NoneTrue()
        {
            Test(() =>
            {
                var test = CreateTestFile();
                test.VersioningMode = VersioningType.None;
                test.ApprovingMode = ApprovingType.True;

                test.CheckOut();
                test.UndoCheckOut();

                Assert.AreEqual("V1.0.A", test.Version.ToString());
                Assert.AreEqual(1, Node.GetVersionNumbers(test.Id).Count);
            });
        }
        [TestMethod()]
        public void GC_UndoCheckOut_MajorTrue()
        {
            Test(() =>
            {
                var test = CreateTestFile();
                test.VersioningMode = VersioningType.MajorOnly;
                test.ApprovingMode = ApprovingType.True;

                test.CheckOut();
                test.UndoCheckOut();

                Assert.AreEqual("V1.0.A", test.Version.ToString());
                Assert.AreEqual(1, Node.GetVersionNumbers(test.Id).Count);
            });
        }
        [TestMethod()]
        public void GC_UndoCheckOut_FullTrue()
        {
            Test(() =>
            {
                var test = CreateTestFile();
                test.VersioningMode = VersioningType.MajorAndMinor;
                test.ApprovingMode = ApprovingType.True;

                test.CheckOut();
                test.UndoCheckOut();

                Assert.AreEqual("V1.0.A", test.Version.ToString());
                Assert.AreEqual(1, Node.GetVersionNumbers(test.Id).Count);
            });
        }




























        [TestMethod]
        public void GC_SaveWithLock_Exception_Test()
        {
            Assert.Inconclusive();
            ////============ Creating a test user
            //var testUser = new User(User.Administrator.Parent)
            //{
            //    Name = "UserFor_GenericContent_SaveWithLock_Exception_Test",
            //    Email = "userfor_genericcontent_savewithlock_exception_test@example.com",
            //    Enabled = true
            //};
            //testUser.Save();
            //testUser = Node.Load<User>(testUser.Id);
            //File testFile = null;
            //var origUser = User.Current;
            //try
            //{
            //    try
            //    {
            //        //============ Orig user creates a file
            //        testFile = CreateFile(save: true);
            //        SecurityHandler.CreateAclEditor()
            //            .Allow(testFile.Id, testUser.Id, false, PermissionType.Save)
            //            .Apply();

            //        //============ Orig user modifies and locks the file
            //        testFile.Index++;
            //        testFile.Save(SavingMode.KeepVersionAndLock); //(must work properly)

            //        //============ Orig user makes free thr file
            //        testFile.CheckIn();

            //        //============ Test user locks the file
            //        User.Current = testUser;
            //        testFile.CheckOut();

            //        //============ Test user tries to lock the file
            //        User.Current = origUser;
            //        testFile = Node.Load<File>(testFile.Id);
            //        testFile.Index++;
            //        try
            //        {
            //            testFile.Save(SavingMode.KeepVersionAndLock);

            //            //============ forbidden code branch
            //            Assert.Fail("InvalidContentActionException was not thrown");
            //        }
            //        catch (InvalidContentActionException)
            //        {
            //        }
            //    }
            //    finally
            //    {
            //        //============ restoring content state
            //        if (testFile.Locked)
            //        {
            //            User.Current = testFile.LockedBy;
            //            testFile.CheckIn();
            //        }
            //    }
            //}
            //finally
            //{
            //    //============ restoring orig user
            //    User.Current = origUser;
            //}
        }

        [TestMethod]
        public void GC_ElevatedSaveWithLock_CheckedOutByAnotherUser()
        {
            Assert.Inconclusive();

            ////============ Creating a test user
            //var userName = "GenericContent_ElevatedSaveWithLock_CheckedOutByAnotherUser_Test";
            //var testUser = Node.Load<User>(RepositoryPath.Combine(User.Administrator.Parent.Path, userName));
            //if (testUser == null)
            //{
            //    testUser = new User(User.Administrator.Parent)
            //    {
            //        Name = userName,
            //        Email = "genericcontent_elevatedsavewithlock_checkedoutbyanotheruser_test@example.com",
            //        Enabled = true
            //    };
            //    testUser.Save();
            //    testUser = Node.Load<User>(testUser.Id);
            //}

            //File testFile = null;
            //var modifierIdBefore = 0;
            //var modifierIdAfter = 0;
            //var origUser = User.Current;
            //try
            //{
            //    try
            //    {
            //        //============ Orig user creates a file
            //        testFile = CreateFile(save: true, name: Guid.NewGuid().ToString());
            //        SecurityHandler.CreateAclEditor()
            //            .Allow(testFile.Id, testUser.Id, false, PermissionType.Save)
            //            .Apply();

            //        //============ Orig user modifies and locks the file
            //        testFile.Index++;
            //        testFile.Save(SavingMode.KeepVersionAndLock); //(must work properly)

            //        //============ Orig user makes free thr file
            //        testFile.CheckIn();

            //        //============ Test user locks the file
            //        User.Current = testUser;
            //        testFile.CheckOut();

            //        //============ Test user tries to lock the file
            //        User.Current = origUser;
            //        testFile = Node.Load<File>(testFile.Id);
            //        testFile.Index++;

            //        //============ System user always can save
            //        modifierIdBefore = testFile.ModifiedById;
            //        using (new SystemAccount())
            //            testFile.Save(SavingMode.KeepVersion);
            //        modifierIdAfter = testFile.ModifiedById;
            //    }
            //    finally
            //    {
            //        //============ restoring content state
            //        if (testFile.Locked)
            //        {
            //            User.Current = testFile.LockedBy;
            //            testFile.CheckIn();
            //        }
            //    }
            //}
            //finally
            //{
            //    //============ restoring orig user
            //    User.Current = origUser;
            //}

            //Assert.AreEqual(modifierIdBefore, modifierIdAfter);
        }


        [TestMethod]
        public void GC_SaveWithLock_RestorePreviousVersion_Test()
        {
            Test(() =>
            {
                var file = CreateTestFile(save: false);
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.Save();
                var versionId = file.VersionId;

                file.CheckOut();
                file.CheckIn();

                //lock by the current user
                file.CheckOut();

                var lockedVersion = file.Version;

                //restore original content
                file = (File) Node.LoadNodeByVersionId(versionId);
                file.Save(SavingMode.KeepVersionAndLock);

                Assert.IsTrue(file.Locked, "File is not locked after restore.");
                Assert.IsTrue(file.IsLatestVersion, "File version is not correct after restore.");
                Assert.AreEqual(lockedVersion.ToString(), file.Version.ToString());
                Assert.AreEqual(versionId, file.VersionId);
            });
        }



        //-------------------------------------------------------------------- SaveSameVersion ----------

        [TestMethod()]
        public void GC_SaveSameVersion_VersionModeNone_ApprovingFalse_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //VersionNumber vn = test.Version;

            //test.VersioningMode = VersioningType.None;
            //test.ApprovingMode = ApprovingType.False;

            //test.Save(SavingMode.KeepVersion);

            //Assert.AreEqual(vn, test.Version, "#1");
            //Assert.AreEqual(test.Version.Status, VersionStatus.Approved, "#2");
        }

        [TestMethod()]
        public void GC_SaveSameVersion_VersionModeMajorOnly_ApprovingFalse_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //VersionNumber vn = test.Version;

            //test.VersioningMode = VersioningType.MajorOnly;
            //test.ApprovingMode = ApprovingType.False;

            //test.Save(SavingMode.KeepVersion);

            //Assert.AreEqual(vn, test.Version, "#1");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
        }

        [TestMethod()]
        public void GC_SaveSameVersion_VersionModeMajorAndMinor_ApprovingFalse_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //VersionNumber vn = test.Version;

            //test.VersioningMode = VersioningType.MajorAndMinor;
            //test.ApprovingMode = ApprovingType.False;

            //test.Save(SavingMode.KeepVersion);

            //Assert.AreEqual(vn, test.Version, "#1");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
        }

        [TestMethod()]
        public void GC_SaveSameVersion_VersionModeNone_ApprovingTrue_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //VersionNumber vn = test.Version;

            //test.VersioningMode = VersioningType.None;
            //test.ApprovingMode = ApprovingType.True;

            //test.Save(SavingMode.KeepVersion);

            //Assert.AreEqual(vn, test.Version, "#1");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
        }

        [TestMethod()]
        public void GC_SaveSameVersion_VersionModeMajorOnly_ApprovingTrue_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //VersionNumber vn = test.Version;

            //test.VersioningMode = VersioningType.MajorOnly;
            //test.ApprovingMode = ApprovingType.True;

            //test.Save(SavingMode.KeepVersion);

            //Assert.AreEqual(vn, test.Version, "#1");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
        }

        [TestMethod()]
        public void GC_SaveSameVersion_VersionModeMajorAndMinor_ApprovingTrue_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //VersionNumber vn = test.Version;

            //test.VersioningMode = VersioningType.MajorAndMinor;
            //test.ApprovingMode = ApprovingType.True;

            //test.Save(SavingMode.KeepVersion);

            //Assert.AreEqual(vn, test.Version, "#1");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
        }

        //-------------------------------------------------------------------- Start multistep save -----------------

        [TestMethod()]
        public void GC_MultistepSave_VersionModeMajorAndMinor_ApprovingFalse_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //var vn = test.Version;

            //test.VersioningMode = VersioningType.MajorAndMinor;
            //test.ApprovingMode = ApprovingType.False;

            //test.CustomMeta = Guid.NewGuid().ToString();

            //test.Save(SavingMode.StartMultistepSave);

            //Assert.IsTrue(vn < test.Version, "#1 version hasn't been raised.");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Locked, "#2 status is not locked");
            //Assert.AreEqual(ContentSavingState.Modifying, test.SavingState, string.Format("#3 saving state is incorrect."));

            //test.FinalizeContent();

            //Assert.IsTrue(test.Version.Status == VersionStatus.Draft, "#4 status is not correct");
            //Assert.AreEqual(ContentSavingState.Finalized, test.SavingState, string.Format("#5 saving state is incorrect."));

            //test.CheckOut();

            //vn = test.Version;

            //test.Save(SavingMode.StartMultistepSave);

            //Assert.AreEqual(vn, test.Version, "#6 version is not correct");
            //Assert.AreEqual(ContentSavingState.ModifyingLocked, test.SavingState, string.Format("#7 saving state is incorrect."));

            //test.FinalizeContent();

            //Assert.AreEqual(vn, test.Version, "#8 version is not correct");
            //Assert.AreEqual(ContentSavingState.Finalized, test.SavingState, string.Format("#9 saving state is incorrect."));
        }


        //-------------------------------------------------------------------- Publish ------------------

        [TestMethod()]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void GC_Publish_VersionModeNone_ApprovingFalse_Test()
        {
            Assert.Inconclusive();

            ////Assert.Inconclusive("Approving off, None: CheckedOut ==> Publish");

            //Page test = CreatePage("GCSaveTest");

            //test.VersioningMode = VersioningType.None;
            //test.ApprovingMode = ApprovingType.False;

            //test.CheckOut();

            //Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Locked, "#2");

            ////this throws an exception: cannot publish 
            ////the content if approving is OFF
            //test.Publish();
        }
        [TestMethod()]
        public void GC_Save_CheckOutIn_VersionModeNone_ApprovingFalse_Bug3167()
        {
            Assert.Inconclusive();

            //var page = CreatePage("GCSaveTest");
            //var pageId = page.Id;
            //var versionString1 = page.Version.ToString();
            //page.Binary.SetStream(RepositoryTools.GetStreamFromString("Binary1"));
            //page.Binary.FileName = "1.html";
            //page.PersonalizationSettings.SetStream(RepositoryTools.GetStreamFromString("PersonalizationSettings1"));
            //page.PersonalizationSettings.FileName = "PersonalizationSettings";
            //page.VersioningMode = VersioningType.None;
            //page.ApprovingMode = ApprovingType.False;
            //page.Save();

            //page = Node.Load<Page>(pageId);
            //page.CheckOut();
            //var versionString2 = page.Version.ToString();
            //page.Binary.SetStream(RepositoryTools.GetStreamFromString("Binary2"));
            //page.PersonalizationSettings.SetStream(RepositoryTools.GetStreamFromString("PersonalizationSettings2"));
            //page.Save();

            //var versionString3 = page.Version.ToString();

            ////page = Node.Load<Page>(pageId);

            //page.CheckIn();

            //page = Node.Load<Page>(pageId);

            //var versionString4 = page.Version.ToString();
            //var vnList = Node.GetVersionNumbers(page.Id);
            //var binString = RepositoryTools.GetStreamString(page.Binary.GetStream());
            //var persString = RepositoryTools.GetStreamString(page.PersonalizationSettings.GetStream());

            //Assert.IsTrue(binString == "Binary2", "#1");
            //Assert.IsTrue(page.Version.Major == 1 && page.Version.Minor == 0, "#2");
            //Assert.IsTrue(persString == "PersonalizationSettings2", "#3");
            //Assert.IsTrue(vnList.Count() == 1, "#3");
            //Assert.IsTrue(versionString1 == "V1.0.A", "#4");
            //Assert.IsTrue(versionString2 == "V2.0.L", "#5");
            //Assert.IsTrue(versionString3 == "V2.0.L", "#6");
            //Assert.IsTrue(versionString4 == "V1.0.A", "#7");
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void GC_Publish_VersionModeMajorOnly_ApprovingFalse_Test()
        {
            Assert.Inconclusive();

            ////Assert.Inconclusive("Approving off, MajorOnly: CheckedOut ==> Publish");

            //Page test = CreatePage("GCSaveTest");

            //test.VersioningMode = VersioningType.MajorOnly;
            //test.ApprovingMode = ApprovingType.False;

            //test.CheckOut();

            //Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Locked, "#2");

            ////this throws an exception: cannot publish 
            ////the content if approving is OFF
            //test.Publish();
        }

        [TestMethod()]
        public void GC_Publish_VersionModeMajorAndMinor_ApprovingFalse_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //VersionNumber vn = test.Version;
            //string cm = test.CustomMeta;

            //test.VersioningMode = VersioningType.MajorAndMinor;
            //test.ApprovingMode = ApprovingType.False;

            //test.CheckOut();

            //test.CustomMeta = Guid.NewGuid().ToString();

            //test.Publish();

            //Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
            //Assert.AreNotEqual(test.CustomMeta, cm, "#3");
        }

        [TestMethod()]
        public void GC_Publish_VersionModeMajorAndMinor_ApprovingTrue_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //VersionNumber vn = test.Version;
            //string cm = test.CustomMeta;

            //test.VersioningMode = VersioningType.MajorAndMinor;
            //test.ApprovingMode = ApprovingType.True;

            //test.CheckOut();

            //test.CustomMeta = Guid.NewGuid().ToString();

            //test.Publish();

            //Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == 1, "#1");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Pending, "#2");
            //Assert.AreNotEqual(test.CustomMeta, cm, "#3");
        }

        //-------------------------------------------------------------------- Approve ------------------

        [TestMethod()]
        public void GC_Approve_VersionModeNone_ApprovingTrue_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //VersionNumber vn = test.Version;
            //string cm = test.CustomMeta;

            //test.VersioningMode = VersioningType.None;
            //test.ApprovingMode = ApprovingType.True;

            //test.Save();

            //test.CustomMeta = Guid.NewGuid().ToString();

            //test.Approve();

            //Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == 0, "#1");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
            //Assert.AreNotEqual(test.CustomMeta, cm, "#3");
        }

        [TestMethod()]
        public void GC_Approve_VersionModeMajorOnly_ApprovingTrue_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //VersionNumber vn = test.Version;
            //string cm = test.CustomMeta;

            //test.VersioningMode = VersioningType.MajorOnly;
            //test.ApprovingMode = ApprovingType.True;

            //test.Save();

            //test.CustomMeta = Guid.NewGuid().ToString();

            //test.Approve();

            //Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
            //Assert.AreNotEqual(test.CustomMeta, cm, "#3");
        }

        [TestMethod()]
        public void GC_Approve_VersionModeMajorAndMinor_ApprovingTrue_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //VersionNumber vn = test.Version;
            //string cm = test.CustomMeta;

            //test.VersioningMode = VersioningType.MajorAndMinor;
            //test.ApprovingMode = ApprovingType.True;

            //test.Save();

            //test.CustomMeta = Guid.NewGuid().ToString();

            //test.Publish();

            //test.Approve();

            //Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
            //Assert.AreNotEqual(test.CustomMeta, cm, "#3");
        }

        //-------------------------------------------------------------------- Reject -------------------

        [TestMethod()]
        public void GC_Reject_VersionModeNone_ApprovingTrue_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //VersionNumber vn = test.Version;
            //string cm = test.CustomMeta;

            //test.VersioningMode = VersioningType.None;
            //test.ApprovingMode = ApprovingType.True;

            //test.Save();

            //test.CustomMeta = Guid.NewGuid().ToString();

            //test.Reject();

            //Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Rejected, "#2");
            //Assert.AreNotEqual(test.CustomMeta, cm, "#3");
        }

        [TestMethod()]
        public void GC_Reject_VersionModeMajorOnly_ApprovingTrue_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //VersionNumber vn = test.Version;
            //string cm = test.CustomMeta;

            //test.VersioningMode = VersioningType.MajorOnly;
            //test.ApprovingMode = ApprovingType.True;

            //test.Save();

            //test.CustomMeta = Guid.NewGuid().ToString();

            //test.Reject();

            //Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Rejected, "#2");
            //Assert.AreNotEqual(test.CustomMeta, cm, "#3");
        }

        [TestMethod()]
        public void GC_Reject_VersionModeMajorAndMinor_ApprovingTrue_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //VersionNumber vn = test.Version;
            //string cm = test.CustomMeta;

            //test.VersioningMode = VersioningType.MajorAndMinor;
            //test.ApprovingMode = ApprovingType.True;

            //test.Save();

            //test.CustomMeta = Guid.NewGuid().ToString();

            //test.Publish();

            //test.Reject();

            //Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == 1, "#1");
            //Assert.IsTrue(test.Version.Status == VersionStatus.Rejected, "#2");
            //Assert.AreNotEqual(test.CustomMeta, cm, "#3");
        }

        //-------------------------------------------------------------------- Explicit version ----------------

        [TestMethod]
        public void GC_SaveExplicitVersion()
        {
            Assert.Inconclusive();

            //SystemFolder container;
            //GenericContent content;

            //container = new SystemFolder(TestRoot) { Name = Guid.NewGuid().ToString(), InheritableVersioningMode = InheritableVersioningType.MajorAndMinor };
            //container.Save();
            //content = new GenericContent(container, "HTMLContent") { Name = Guid.NewGuid().ToString() };
            //content["HTMLFragment"] = "<h1>Testcontent</h1>";
            //content.Save();
            //Assert.AreEqual(VersionNumber.Parse("V0.1.D"), content.Version);

            //var versionCount = 0;
            //content = new GenericContent(container, "HTMLContent") { Name = Guid.NewGuid().ToString() };
            //content["HTMLFragment"] = "<h1>Testcontent</h1>";
            //content.Version = new VersionNumber(2, 0);
            //content.SaveExplicitVersion(); // must be called
            //versionCount++;
            //content.Version = new VersionNumber(2, 1);
            //content.Save();
            //versionCount++;
            //content.Version = new VersionNumber(2, 2);
            //content.Save();
            //versionCount++;
            //content.Version = new VersionNumber(3, 0);
            //content.Save();
            //versionCount++;
            //content.Version = new VersionNumber(6, 3);
            //content.Save();
            //versionCount++;
            //content.Version = new VersionNumber(42, 0);
            //content.Save();
            //versionCount++;

            //var expected = "V2.0.A, V2.1.D, V2.2.D, V3.0.A, V6.3.D, V42.0.A";
            //var actual = String.Join(", ", content.Versions.Select(x => x.Version));
            //Assert.AreEqual(expected, actual);
            //Assert.AreEqual(versionCount, NodeHead.Get(content.Id).Versions.Length);

            ////------------------------ modify is allowed
            //content.Version = new VersionNumber(42, 0);
            //content["HTMLFragment"] = "<h1>Testcontent (modified)</h1>";
            //content.SaveExplicitVersion();
            //Assert.AreEqual(VersionNumber.Parse("V42.0.A"), content.Version);
            //Assert.AreEqual(versionCount, NodeHead.Get(content.Id).Versions.Length);

            ////------------------------ modify is allowed (but version will be 42.1.D)
            //content.Version = new VersionNumber(42, 0);
            //content["HTMLFragment"] = "<h1>Testcontent (modified)</h1>";
            //content.Save();
            //versionCount++;
            //Assert.AreEqual(VersionNumber.Parse("V42.1.D"), content.Version);
            //Assert.AreEqual(versionCount, NodeHead.Get(content.Id).Versions.Length);

            ////------------------------ downgrade is forbidden
            //var thrown = false;
            //try
            //{
            //    content.Version = new VersionNumber(41, 0);
            //    content.Save();
            //    thrown = false;
            //}
            //catch (InvalidContentActionException)
            //{
            //    thrown = true;
            //}
            //Assert.IsTrue(thrown);

            //thrown = false;
            //try
            //{
            //    content.Version = new VersionNumber(45, 0);
            //    content.Save(SavingMode.KeepVersion);
            //}
            //catch (InvalidContentActionException)
            //{
            //    thrown = true;
            //}
            //Assert.IsTrue(thrown);
            //Assert.AreEqual(versionCount, NodeHead.Get(content.Id).Versions.Length);
        }
        [TestMethod]
        public void GC_SaveExplicitVersion_CheckedOut_Pending_Rejected()
        {
            Assert.Inconclusive();

            //var container = new SystemFolder(TestRoot)
            //{
            //    Name = Guid.NewGuid().ToString(),
            //    InheritableVersioningMode = InheritableVersioningType.MajorAndMinor,
            //    InheritableApprovingMode = ApprovingType.True
            //};
            //container.Save();
            //var content = new GenericContent(container, "HTMLContent") { Name = Guid.NewGuid().ToString() };
            //content["HTMLFragment"] = "<h1>Testcontent</h1>";
            //content.Save();
            //Assert.AreEqual(VersionNumber.Parse("V0.1.D"), content.Version);

            //var thrown = false;

            //// cannot save explicit version if the content is locked
            //content.CheckOut();
            //content.Version = new VersionNumber(42, 0);
            //try
            //{
            //    content.Save();
            //}
            //catch (InvalidContentActionException)
            //{
            //    thrown = true;
            //}
            //Assert.IsTrue(thrown);

            //// cannot save explicit version if the content is in "pending for approval" state
            //content.Publish();
            //content.Version = new VersionNumber(42, 0);
            //try
            //{
            //    content.Save();
            //}
            //catch (InvalidContentActionException)
            //{
            //    thrown = true;
            //}
            //Assert.IsTrue(thrown);

            //// cannot save explicit version if the content is in "rejected" state
            //content = Node.Load<GenericContent>(content.Id);
            //content.Reject();
            //content.Version = new VersionNumber(42, 0);
            //try
            //{
            //    content.Save();
            //}
            //catch (InvalidContentActionException)
            //{
            //    thrown = true;
            //}
            //Assert.IsTrue(thrown);
        }
        //-------------------------------------------------------------------- Exception ----------------

        [TestMethod()]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void GC_Exception_VersionModeNone_ApprovingFalse_Test()
        {
            Assert.Inconclusive();

            //Page test = CreatePage("GCSaveTest");

            //test.VersioningMode = VersioningType.None;
            //test.ApprovingMode = ApprovingType.False;

            //test.Reject();
        }

        //-------------------------------------------------------------------- Others ----------------

        [TestMethod()]
        public void GC_MajorAndMinor_CheckoutSaveCheckin_BinaryData()
        {
            Assert.Inconclusive();

            ////--------------------------------------------- prepare

            //var folderContent = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString());
            //var folder = (Folder)folderContent.ContentHandler;
            //folder.InheritableVersioningMode = InheritableVersioningType.MajorAndMinor;
            //folder.Save();

            //var fileContent = Content.CreateNew("File", folder, null);
            //var file = (File)fileContent.ContentHandler;

            //var stream = RepositoryTools.GetStreamFromString("asdf qwer yxcv");
            //var binaryData = new BinaryData { ContentType = "text/plain", FileName = "1.txt", Size = stream.Length };
            //binaryData.SetStream(stream);

            //file.SetBinary("Binary", binaryData);
            //file.Save();

            //var fileId = file.Id;

            ////--------------------------------------------- operating

            //file = Node.Load<File>(fileId);
            //file.CheckOut();

            //file = Node.Load<File>(fileId);
            //file.Binary.SetStream(RepositoryTools.GetStreamFromString("asdf qwer yxcv 123"));
            //file.Save();

            //file = Node.Load<File>(fileId);
            //file.CheckIn();

            //file = Node.Load<File>(fileId);
            //var s = RepositoryTools.GetStreamString(file.Binary.GetStream());

            //Assert.IsTrue(s == "asdf qwer yxcv 123");
        }

        [TestMethod()]
        public void GC_None_CheckoutSaveCheckin_BinaryData()
        {
            Assert.Inconclusive();

            ////--------------------------------------------- prepare

            //var folderContent = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString());
            //var folder = (Folder)folderContent.ContentHandler;
            //folder.InheritableVersioningMode = InheritableVersioningType.None;
            //folder.Save();

            //var fileContent = Content.CreateNew("File", folder, null);
            //var file = (File)fileContent.ContentHandler;

            //var stream = RepositoryTools.GetStreamFromString("asdf qwer yxcv");
            //var binaryData = new BinaryData { ContentType = "text/plain", FileName = "1.txt", Size = stream.Length };
            //binaryData.SetStream(stream);

            //file.SetBinary("Binary", binaryData);
            //file.Save();

            //var fileId = file.Id;

            ////--------------------------------------------- operating

            //file = Node.Load<File>(fileId);
            //file.CheckOut();

            //file = Node.Load<File>(fileId);
            //file.Binary.SetStream(RepositoryTools.GetStreamFromString("asdf qwer yxcv 123"));
            //file.Save();

            //file = Node.Load<File>(fileId);
            //file.CheckIn();

            //file = Node.Load<File>(fileId);
            //var s = RepositoryTools.GetStreamString(file.Binary.GetStream());

            //Assert.IsTrue(s == "asdf qwer yxcv 123");
        }
        [TestMethod()]
        public void GC_None_CheckoutSaveCheckin_BinaryData_OfficeProtocolBug()
        {
            Assert.Inconclusive();

            ////--------------------------------------------- prepare

            //var folderContent = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString());
            //var folder = (Folder)folderContent.ContentHandler;
            //folder.InheritableVersioningMode = InheritableVersioningType.None;
            //folder.Save();

            //var fileContent = Content.CreateNew("File", folder, null);
            //var file = (File)fileContent.ContentHandler;

            //var stream2 = RepositoryTools.GetStreamFromString("asdf qwer yxcv");
            //var binaryData = new BinaryData { ContentType = "text/plain", FileName = "1.txt", Size = stream2.Length };
            //binaryData.SetStream(stream2);

            //file.SetBinary("Binary", binaryData);
            //file.Save();

            //var fileId = file.Id;

            ////--------------------------------------------- operating

            //var node = Node.LoadNode(fileId);
            //var gc = node as GenericContent;
            //gc.CheckOut();

            //// save
            //var node2 = Node.LoadNode(fileId);
            //using (var stream = new System.IO.MemoryStream())
            //{
            //    using (var streamwriter = new System.IO.StreamWriter(stream))
            //    {
            //        streamwriter.Write("asdf qwer yxcv 123");
            //        streamwriter.Flush();
            //        stream.Seek(0, System.IO.SeekOrigin.Begin);

            //        ((BinaryData)node2["Binary"]).SetStream(stream);

            //        node2.Save();
            //    }
            //}

            //// load
            //var node3 = Node.LoadNode(fileId);
            //var bdata = (BinaryData)node3["Binary"];
            //var expstream = bdata.GetStream();
            //Assert.IsTrue(expstream.Length > 0, "stream length is 0");

            //var s = RepositoryTools.GetStreamString(expstream);
            //Assert.IsTrue(s == "asdf qwer yxcv 123", String.Format("content is '{0}'. expected: 'asdf qwer yxcv 123'", s));
        }

        //[TestMethod]
        //public void LoggedDataProvider_RightWrappingAndRestoring()
        //{
        //    var dataProvider = DataProvider.Current;
        //    using (var loggedDataProvider = new LoggedDataProvider())
        //    {
        //        Assert.ReferenceEquals(loggedDataProvider, DataProvider.Current);
        //    }
        //    Assert.ReferenceEquals(dataProvider, DataProvider.Current);
        //}




































        /* -------------------------------------------------------------------- Helper methods ----------- */

        private GenericContent CreateTestRoot()
        {
            var node = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
            node.Save();
            return node;
        }

        /// <summary>
        /// Creates a file without binary. Name is a GUID if not passed. Parent is a newly created SystemFolder.
        /// </summary>
        public File CreateTestFile(string name = null, bool save = true)
        {
            return CreateTestFile(name ?? Guid.NewGuid().ToString(), CreateTestRoot(), save);
        }

        /// <summary>
        /// Creates a file without binary under the given parent node.
        /// </summary>
        public static File CreateTestFile(string name, Node parent, bool save = true)
        {
            var file = new File(parent) { Name = name };
            if(save)
                file.Save();
            return file;
        }

    }
}
