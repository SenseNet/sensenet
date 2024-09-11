using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Tests.Core;
using System;
using System.Linq;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.Security;
using System.Threading.Channels;
using System.Threading.Tasks;

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

        /* ============================================================================= Update */

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
        [TestMethod]
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

                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

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
                parent.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var file = CreateTestFile(parent);

                if (originalVersion != file.Version.ToString())
                    Assert.Inconclusive();

                file.VersioningMode = VersioningType.Inherited;
                file.ApprovingMode = approving;

                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
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
                parent.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var file = CreateTestFile(parent);

                Assert.AreEqual(originalVersion, file.Version.ToString());

                file.VersioningMode = versioning;
                file.ApprovingMode = ApprovingType.Inherited;

                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                Assert.AreEqual(expectedVersion, file.Version.ToString());

                var file1 = Node.Load<File>(file.Id);
                Assert.AreNotSame(file, file1);
                Assert.AreEqual(file.Version.ToString(), file1.Version.ToString());

            });
        }

        /* ============================================================================= SaveSameVersion */

        [TestMethod]
        public void GC_SaveSameVersion_NoneFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                var originalVersion = file.Version;
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.False;

                file.Index = 42;
                file.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual(originalVersion.ToString(), file.Version.ToString());
                Assert.AreEqual(VersionStatus.Approved, file.Version.Status);
            });
        }
        [TestMethod]
        public void GC_SaveSameVersion_MajorFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                var originalVersion = file.Version;
                file.VersioningMode = VersioningType.MajorOnly;
                file.ApprovingMode = ApprovingType.False;

                file.Index = 42;
                file.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual(originalVersion.ToString(), file.Version.ToString());
                Assert.AreEqual(VersionStatus.Approved, file.Version.Status);
            });
        }
        [TestMethod]
        public void GC_SaveSameVersion_FullFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                var originalVersion = file.Version;
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.False;

                file.Index = 42;
                file.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual(originalVersion.ToString(), file.Version.ToString());
                Assert.AreEqual(VersionStatus.Approved, file.Version.Status);
            });
        }
        [TestMethod]
        public void GC_SaveSameVersion_NoneTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                var originalVersion = file.Version;
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.True;

                file.Index = 42;
                file.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual(originalVersion.ToString(), file.Version.ToString());
                Assert.AreEqual(VersionStatus.Approved, file.Version.Status);
            });
        }
        [TestMethod]
        public void GC_SaveSameVersion_MajorTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                var originalVersion = file.Version;
                file.VersioningMode = VersioningType.MajorOnly;
                file.ApprovingMode = ApprovingType.True;

                file.Index = 42;
                file.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual(originalVersion.ToString(), file.Version.ToString());
                Assert.AreEqual(VersionStatus.Approved, file.Version.Status);
            });
        }
        [TestMethod]
        public void GC_SaveSameVersion_FullTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                var originalVersion = file.Version;
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.True;

                file.Index = 42;
                file.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual(originalVersion.ToString(), file.Version.ToString());
                Assert.AreEqual(VersionStatus.Approved, file.Version.Status);
            });
        }

        /* ============================================================================= CreateWithLock */

        [TestMethod]
        public void GC_CreateWithLock_IsLocked()
        {
            Test(() =>
            {
                var file = CreateTestFile(save: false);
                file.SaveAsync(SavingMode.KeepVersionAndLock, CancellationToken.None).GetAwaiter().GetResult();
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

                file.SaveAsync(SavingMode.KeepVersionAndLock, CancellationToken.None).GetAwaiter().GetResult();

                var fileId = file.Id;
                file = Node.Load<File>(fileId);
                Assert.AreEqual("V1.0.L", file.Version.ToString());
                Assert.AreEqual(1, file.Versions.Count());

                file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

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

                file.SaveAsync(SavingMode.KeepVersionAndLock, CancellationToken.None).GetAwaiter().GetResult();

                var fileId = file.Id;
                file = Node.Load<File>(fileId);
                Assert.AreEqual("V0.1.L", file.Version.ToString());
                Assert.AreEqual(1, file.Versions.Count());

                file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

                file = Node.Load<File>(fileId);
                Assert.AreEqual("V0.1.D", file.Version.ToString());
                Assert.AreEqual(1, file.Versions.Count());

                file.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();

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

                file.SaveAsync(SavingMode.KeepVersionAndLock, CancellationToken.None).GetAwaiter().GetResult();

                var fileId = file.Id;
                file = Node.Load<File>(fileId);
                Assert.AreEqual("V0.1.L", file.Version.ToString(), "Version number is not correct after locked save.");
                Assert.AreEqual(1, file.Versions.Count(), "Version count is not correct after locked save.");

                file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

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
                file.SaveAsync(SavingMode.KeepVersionAndLock, CancellationToken.None).GetAwaiter().GetResult();
                file.UndoCheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
            });
        }

        /* ============================================================================= CheckOut */

        [TestMethod]
        public void GC_CheckOut_None_False()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.False;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

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

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

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

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V1.1.L", file.Version.ToString());
            });
        }
        [TestMethod]
        public void GC_CheckOut_None_True()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.True;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V2.0.L", file.Version.ToString());
            });
        }
        [TestMethod]
        public void GC_CheckOut_MajorOnly_True()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorOnly;
                file.ApprovingMode = ApprovingType.True;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V2.0.L", file.Version.ToString());
            });
        }
        [TestMethod]
        public void GC_CheckOut_MajorAndMinor_True()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.True;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

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
                    file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual(User.LoggedInUser.Id, file.LockedById);
                Assert.AreEqual("V2.0.L", file.Version.ToString());
            });
        }

        /* ============================================================================= CheckIn */

        [TestMethod]
        public void GC_CheckIn_NoneFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.False;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

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

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

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

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

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

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

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

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

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

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V1.1.D", file.Version.ToString());
                Assert.AreEqual(2, Node.GetVersionNumbers(file.Id).Count);
            });
        }

        /* ============================================================================= UndoCheckOut */

        [TestMethod]
        public void GC_UndoCheckOut_NoneFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.False;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.UndoCheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V1.0.A", file.Version.ToString());
                Assert.AreEqual(1, Node.GetVersionNumbers(file.Id).Count);
            });
        }
        [TestMethod]
        public void GC_UndoCheckOut_MajorFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorOnly;
                file.ApprovingMode = ApprovingType.False;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.UndoCheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V1.0.A", file.Version.ToString());
                Assert.AreEqual(1, Node.GetVersionNumbers(file.Id).Count);
            });
        }
        [TestMethod]
        public void GC_UndoCheckOut_FullFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.False;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.UndoCheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V1.0.A", file.Version.ToString());
                Assert.AreEqual(1, Node.GetVersionNumbers(file.Id).Count);
            });
        }
        [TestMethod]
        public void GC_UndoCheckOut_NoneTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.True;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.UndoCheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V1.0.A", file.Version.ToString());
                Assert.AreEqual(1, Node.GetVersionNumbers(file.Id).Count);
            });
        }
        [TestMethod]
        public void GC_UndoCheckOut_MajorTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorOnly;
                file.ApprovingMode = ApprovingType.True;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.UndoCheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V1.0.A", file.Version.ToString());
                Assert.AreEqual(1, Node.GetVersionNumbers(file.Id).Count);
            });
        }
        [TestMethod]
        public void GC_UndoCheckOut_FullTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.True;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.UndoCheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V1.0.A", file.Version.ToString());
                Assert.AreEqual(1, Node.GetVersionNumbers(file.Id).Count);
            });
        }

        /* ============================================================================= Publish */

        [TestMethod]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void GC_Publish_NoneFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.False;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void GC_Publish_MajorFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorOnly;
                file.ApprovingMode = ApprovingType.False;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();
            });
        }
        [TestMethod]
        public void GC_Publish_FullFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.False;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V2.0.A", file.Version.ToString());
                Assert.AreEqual(2, Node.GetVersionNumbers(file.Id).Count);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void GC_Publish_NoneTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.True;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void GC_Publish_MajorTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorOnly;
                file.ApprovingMode = ApprovingType.True;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();
            });
        }
        [TestMethod]
        public void GC_Publish_FullTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.True;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V1.1.P", file.Version.ToString());
                Assert.AreEqual(2, Node.GetVersionNumbers(file.Id).Count);
            });
        }

        /* ============================================================================= Approve */

        [TestMethod]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void GC_Approve_NoneFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.False;

                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.ApproveAsync(CancellationToken.None).GetAwaiter().GetResult();
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void GC_Approve_MajorFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorOnly;
                file.ApprovingMode = ApprovingType.False;

                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.ApproveAsync(CancellationToken.None).GetAwaiter().GetResult();
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void GC_Approve_FullFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.False;

                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.ApproveAsync(CancellationToken.None).GetAwaiter().GetResult();
            });
        }
        [TestMethod]
        public void GC_Approve_NoneTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.True;

                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.ApproveAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V1.0.A", file.Version.ToString());
                Assert.AreEqual(1, Node.GetVersionNumbers(file.Id).Count);
            });
        }
        [TestMethod]
        public void GC_Approve_MajorTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorOnly;
                file.ApprovingMode = ApprovingType.True;

                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.ApproveAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V2.0.A", file.Version.ToString());
                Assert.AreEqual(2, Node.GetVersionNumbers(file.Id).Count);
            });
        }
        [TestMethod]
        public void GC_Approve_FullTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.True;

                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.ApproveAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V2.0.A", file.Version.ToString());
                Assert.AreEqual(2, Node.GetVersionNumbers(file.Id).Count);
            });
        }

        /* ============================================================================= Reject */

        [TestMethod]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void GC_Reject_NoneFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.False;

                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.RejectAsync(CancellationToken.None).GetAwaiter().GetResult();
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void GC_Reject_MajorFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorOnly;
                file.ApprovingMode = ApprovingType.False;

                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.RejectAsync(CancellationToken.None).GetAwaiter().GetResult();
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void GC_Reject_FullFalse()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.False;

                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.RejectAsync(CancellationToken.None).GetAwaiter().GetResult();
            });
        }
        [TestMethod]
        public void GC_Reject_NoneTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.True;

                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.RejectAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V2.0.R", file.Version.ToString());
                Assert.AreEqual(2, Node.GetVersionNumbers(file.Id).Count);
            });
        }
        [TestMethod]
        public void GC_Reject_MajorTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorOnly;
                file.ApprovingMode = ApprovingType.True;

                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.RejectAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V2.0.R", file.Version.ToString());
                Assert.AreEqual(2, Node.GetVersionNumbers(file.Id).Count);
            });
        }
        [TestMethod]
        public void GC_Reject_FullTrue()
        {
            Test(() =>
            {
                var file = CreateTestFile();
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.ApprovingMode = ApprovingType.True;

                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.RejectAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V1.1.R", file.Version.ToString());
                Assert.AreEqual(2, Node.GetVersionNumbers(file.Id).Count);
            });
        }

        /* ============================================================================= Explicit version */

        [TestMethod]
        public void GC_SaveExplicitVersion()
        {
            Test(() =>
            {
                var container = CreateTestRoot();
                container.InheritableVersioningMode = InheritableVersioningType.MajorAndMinor;
                container.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var content = CreateTestFile(container);
                Assert.AreEqual(VersionNumber.Parse("V0.1.D"), content.Version);

                var versionCount = 0;
                content = CreateTestFile(container, save: false);
                content.Version = new VersionNumber(2, 0);
                content.SaveExplicitVersionAsync(CancellationToken.None).GetAwaiter().GetResult(); // must be called
                versionCount++;
                content.Version = new VersionNumber(2, 1);
                content.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                versionCount++;
                content.Version = new VersionNumber(2, 2);
                content.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                versionCount++;
                content.Version = new VersionNumber(3, 0);
                content.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                versionCount++;
                content.Version = new VersionNumber(6, 3);
                content.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                versionCount++;
                content.Version = new VersionNumber(42, 0);
                content.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                versionCount++;

                var expected = "V2.0.A, V2.1.D, V2.2.D, V3.0.A, V6.3.D, V42.0.A";
                var actual = String.Join(", ", content.Versions.Select(x => x.Version));
                Assert.AreEqual(expected, actual);
                Assert.AreEqual(versionCount, NodeHead.Get(content.Id).Versions.Length);

                //------------------------ modify is allowed
                content.Version = new VersionNumber(42, 0);
                content.SaveExplicitVersionAsync(CancellationToken.None).GetAwaiter().GetResult();
                Assert.AreEqual(VersionNumber.Parse("V42.0.A"), content.Version);
                Assert.AreEqual(versionCount, NodeHead.Get(content.Id).Versions.Length);

                //------------------------ modify is allowed (but version will be 42.1.D)
                content.Version = new VersionNumber(42, 0);
                content.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                versionCount++;
                Assert.AreEqual(VersionNumber.Parse("V42.1.D"), content.Version);
                Assert.AreEqual(versionCount, NodeHead.Get(content.Id).Versions.Length);

                //------------------------ downgrade is forbidden
                var thrown = false;
                try
                {
                    content.Version = new VersionNumber(41, 0);
                    content.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (InvalidContentActionException)
                {
                    thrown = true;
                }
                Assert.IsTrue(thrown);

                thrown = false;
                try
                {
                    content.Version = new VersionNumber(45, 0);
                    content.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (InvalidContentActionException)
                {
                    thrown = true;
                }
                Assert.IsTrue(thrown);
                Assert.AreEqual(versionCount, NodeHead.Get(content.Id).Versions.Length);
            });
        }
        [TestMethod]
        public void GC_SaveExplicitVersion_CheckOut_Publish_Reject()
        {
            Test(() =>
            {
                var container = CreateTestRoot();
                container.InheritableVersioningMode = InheritableVersioningType.MajorAndMinor;
                container.InheritableApprovingMode = ApprovingType.True;
                container.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var file = CreateTestFile(container);
                Assert.AreEqual(VersionNumber.Parse("V0.1.D"), file.Version);

                var thrown = false;

                // cannot save explicit version if the content is locked
                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.Version = new VersionNumber(42, 0);
                try
                {
                    file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (InvalidContentActionException)
                {
                    thrown = true;
                }
                Assert.IsTrue(thrown);

                // cannot save explicit version if the content is in "pending for approval" state
                thrown = false;
                file.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.Version = new VersionNumber(42, 0);
                try
                {
                    file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (InvalidContentActionException)
                {
                    thrown = true;
                }
                Assert.IsTrue(thrown);

                // cannot save explicit version if the content is in "rejected" state
                thrown = false;
                file = Node.Load<File>(file.Id);
                file.RejectAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.Version = new VersionNumber(42, 0);
                try
                {
                    file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (InvalidContentActionException)
                {
                    thrown = true;
                }
                Assert.IsTrue(thrown);
            });
        }

        /* ============================================================================= other content workflows */

        [TestMethod]
        public void GC_ElevatedSaveWithLock_CheckedOutByAnotherUser()
        {
            Test(() =>
            {
                //============ Creating a test user
                var userName = "UserFor_GC_ElevatedSaveWithLock_CheckedOutByAnotherUser";
                var testUser = Node.Load<User>(RepositoryPath.Combine(User.Administrator.Parent.Path, userName));
                if (testUser == null)
                {
                    testUser = new User(User.Administrator.Parent)
                    {
                        Name = userName,
                        Email = "UserFor_GC_ElevatedSaveWithLock_CheckedOutByAnotherUser@example.com",
                        Enabled = true
                    };
                    testUser.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    testUser = Node.Load<User>(testUser.Id);
                }

                File testFile = null;
                var modifierIdBefore = 0;
                var modifierIdAfter = 0;
                var origUser = User.Current;
                try
                {
                    try
                    {
                        //============ Orig user creates a file
                        testFile = CreateTestFile();
                        Providers.Instance.SecurityHandler.CreateAclEditor()
                            .Allow(testFile.Id, testUser.Id, false, PermissionType.Save)
                            .Allow(testUser.Id, testUser.Id, false, PermissionType.Save)
                            .ApplyAsync(CancellationToken.None).GetAwaiter().GetResult();

                        Assert.IsTrue(testFile.Security.HasPermission((IUser)testUser, PermissionType.Save));

                        //============ Orig user modifies and locks the file
                        testFile.Index++;
                        testFile.SaveAsync(SavingMode.KeepVersionAndLock, CancellationToken.None).GetAwaiter().GetResult(); //(must work properly)

                        //============ Orig user makes free the file
                        testFile.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

                        //============ Test user locks the file
                        User.Current = testUser;
                        testFile.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                        //============ Test user tries to lock the file
                        User.Current = origUser;
                        testFile = Node.Load<File>(testFile.Id);
                        testFile.Index++;

                        //============ System user always can save
                        modifierIdBefore = testFile.ModifiedById;
                        using (new SystemAccount())
                            testFile.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();
                        modifierIdAfter = testFile.ModifiedById;
                    }
                    finally
                    {
                        //============ restoring content state
                        if (testFile.Locked)
                        {
                            User.Current = testFile.LockedBy;
                            testFile.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();
                        }
                    }
                }
                finally
                {
                    //============ restoring orig user
                    User.Current = origUser;
                }

                Assert.AreEqual(modifierIdBefore, modifierIdAfter);
            });
        }

        [TestMethod]
        public void GC_SaveWithLock_Exception()
        {
            Test(() =>
            {
                Assert.AreEqual(Identifiers.SystemUserId, User.Current.Id);

                //============ Create a test user
                var testUser = new User(User.Administrator.Parent)
                {
                    Name = "UserFor_GC_SaveWithLock_Exception",
                    Email = "UserFor_GC_SaveWithLock_Exception@example.com",
                    Enabled = true
                };
                testUser.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                testUser = Node.Load<User>(testUser.Id);
                File testFile = null;
                var origUser = User.Current;

                //============ Orig user creates a file
                testFile = CreateTestFile();
                Providers.Instance.SecurityHandler.CreateAclEditor()
                    .Allow(testFile.Id, testUser.Id, false, PermissionType.Save)
                    .Allow(testUser.Id, testUser.Id, false, PermissionType.Save)
                    .ApplyAsync(CancellationToken.None).GetAwaiter().GetResult();

                //============ Orig user modifies and locks the file
                testFile.Index++;
                testFile.SaveAsync(SavingMode.KeepVersionAndLock, CancellationToken.None).GetAwaiter().GetResult(); //(must work properly)

                //============ Orig user makes free thr file
                testFile.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

                //============ Test user locks the file
                User.Current = testUser;
                testFile.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                //============ Administrator tries to lock the file
                User.Current = User.Administrator;
                testFile = Node.Load<File>(testFile.Id);
                testFile.Index++;
                try
                {
                    testFile.SaveAsync(SavingMode.KeepVersionAndLock, CancellationToken.None).GetAwaiter().GetResult();

                    //============ forbidden code branch
                    Assert.Fail("InvalidContentActionException was not thrown");
                }
                catch (InvalidContentActionException)
                {
                }

                //============ System user locks the file
                User.Current = origUser;
                testFile = Node.Load<File>(testFile.Id);
                testFile.Index++;
                testFile.SaveAsync(SavingMode.KeepVersionAndLock, CancellationToken.None).GetAwaiter().GetResult();
            });
        }

        [TestMethod]
        public void GC_SaveWithLock_RestorePreviousVersion_Administrator()
        {
            Test(() =>
            {
                Providers.Instance.SecurityHandler.CreateAclEditor()
                    .Allow(Identifiers.PortalRootId, User.Administrator.Id, false,
                        PermissionType.AddNew, PermissionType.RecallOldVersion)
                    .ApplyAsync(CancellationToken.None).GetAwaiter().GetResult();

                User.Current = User.Administrator;

                var file = CreateTestFile(save: false);
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                var versionId = file.VersionId;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

                //lock by the current user
                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                var lockedVersion = file.Version;

                //restore original content
                file = (File) Node.LoadNodeByVersionId(versionId);
                file.SaveAsync(SavingMode.KeepVersionAndLock, CancellationToken.None).GetAwaiter().GetResult();

                Assert.IsTrue(file.Locked, "File is not locked after restore.");
                Assert.IsTrue(file.IsLatestVersion, "File version is not correct after restore.");
                Assert.AreEqual(lockedVersion.ToString(), file.Version.ToString());
            });
        }
        [TestMethod]
        public void GC_SaveWithLock_RestorePreviousVersion_SystemUser()
        {
            Test(() =>
            {
                //SecurityHandler.CreateAclEditor()
                //    .Allow(Identifiers.PortalRootId, User.Administrator.Id, false, PermissionType.AddNew,
                //        PermissionType.RecallOldVersion)
                //    .Apply();

                Assert.AreEqual(Identifiers.SystemUserId, User.Current.Id);

                var file = CreateTestFile(save: false);
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                var versionId = file.VersionId;

                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

                //lock by the current user
                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                var lockedVersion = file.Version;

                //restore original content
                file = (File)Node.LoadNodeByVersionId(versionId);
                file.SaveAsync(SavingMode.KeepVersionAndLock, CancellationToken.None).GetAwaiter().GetResult();

                Assert.IsTrue(file.Locked, "File is not locked after restore.");
                Assert.IsTrue(file.IsLatestVersion, "File version is not correct after restore.");
                Assert.AreEqual(lockedVersion.ToString(), file.Version.ToString());
            });
        }

        [TestMethod]
        public void GC_Save_CheckOutIn_None_ApprovingFalse_Bug3167()
        {
            Test(() =>
            {
                // action 1
                var file = CreateTestFile();

                Assert.AreEqual("V1.0.A", file.Version.ToString());

                var fileId = file.Id;
                file.VersioningMode = VersioningType.None;
                file.ApprovingMode = ApprovingType.False;

                // action 2
                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                file = Node.Load<File>(fileId);

                // action 3
                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V2.0.L", file.Version.ToString());

                // action 4
                file.Index++;
                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual("V2.0.L", file.Version.ToString());

                // action 5
                file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

                file = Node.Load<File>(fileId);
                Assert.AreEqual("V1.0.A", file.Version.ToString());

                Assert.AreEqual(1, Node.GetVersionNumbers(file.Id).Count);
            });
        }

        [TestMethod]
        public async System.Threading.Tasks.Task GC_RestoreVersion_MajorAndMinor_SystemUser()
        {
            await Test(async () =>
            {
                var cancel = new CancellationToken();
                Assert.AreEqual(Identifiers.SystemUserId, User.Current.Id);

                var file = CreateTestFile(save: false);
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.Description = "1.0";
                await file.SaveAsync(cancel); // V0.1.D
                await file.PublishAsync(cancel); // V1.0.A
                file = await CreateVersionsFor_RestoreVersion_MajorAndMinor(file, cancel);

                // ACT
                file = (File)await file.RestoreVersionAsync(new VersionNumber(1, 1), cancel);
                // versions before: V1.0.A, V1.1.D, V2.0.A, V2.1.D, V3.0.A, V3.1.D
                // versions after:  V1.0.A, V1.1.D, V2.0.A, V2.1.D, V3.0.A, V3.1.D, V3.2.D

                // ASSERT
                Assert.IsTrue(file.IsLatestVersion, "File version is not correct after restore.");
                Assert.AreEqual("V3.2.D", file.Version.ToString());
                Assert.AreEqual("1.1", file.Description);
            });
        }
        [TestMethod]
        public async System.Threading.Tasks.Task GC_RestoreVersion_MajorAndMinor_Admin()
        {
            await Test(true, async () =>
            {
                var cancel = new CancellationToken();
                Assert.AreEqual(Identifiers.AdministratorUserId, User.Current.Id);

                var file = CreateTestFile(save: false);
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.Description = "1.0";
                await file.SaveAsync(cancel); // V0.1.D
                await file.PublishAsync(cancel); // V1.0.A
                file = await CreateVersionsFor_RestoreVersion_MajorAndMinor(file, cancel);

                // ACT
                file = (File)await file.RestoreVersionAsync(new VersionNumber(1, 1), cancel);
                // versions before: V1.0.A, V1.1.D, V2.0.A, V2.1.D, V3.0.A, V3.1.D
                // versions after:  V1.0.A, V1.1.D, V2.0.A, V2.1.D, V3.0.A, V3.1.D, V3.2.D

                // ASSERT
                Assert.IsTrue(file.IsLatestVersion, "File version is not correct after restore.");
                Assert.AreEqual("V3.2.D", file.Version.ToString());
                Assert.AreEqual("1.1", file.Description);
            });
        }
        [TestMethod]
        public async System.Threading.Tasks.Task GC_RestoreVersion_MajorAndMinor_TestUser_Error()
        {
            await Test(true, async () =>
            {
                var cancel = new CancellationToken();
                var user = new User(await Node.LoadNodeAsync(Identifiers.PortalOrgUnitId, cancel))
                {
                    Name = "testUsr124",
                    LoginName = "testUsr124",
                    Email = "testusr124@example.com",
                    Enabled = true
                };
                user.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.AreEqual(Identifiers.AdministratorUserId, User.Current.Id);

                var file = CreateTestFile(save: false);
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.Description = "1.0";
                await file.SaveAsync(cancel); // V0.1.D
                await file.PublishAsync(cancel); // V1.0.A
                file = await CreateVersionsFor_RestoreVersion_MajorAndMinor(file, cancel);

                string errorMessage = null;
                try
                {
                    using (new CurrentUserBlock(user))
                    {
                        // ACT
                        file = (File)await file.RestoreVersionAsync(new VersionNumber(1, 1), cancel);
                    }
                    Assert.Fail("The expected exception was not thrown.");
                }
                catch (SenseNetSecurityException e)
                {
                    errorMessage = e.Message;
                }

                // ASSERT
                Assert.IsNotNull(errorMessage);
                Assert.IsTrue(errorMessage.StartsWith("Not enough permission to restore an older version"));
                Assert.IsTrue(file.IsLatestVersion, "File version is not correct after restore.");
                Assert.AreEqual("V3.1.D", file.Version.ToString());
                Assert.AreEqual("3.1", file.Description);
            });
        }
        [TestMethod]
        public async System.Threading.Tasks.Task GC_RestoreVersion_MajorAndMinor_MissingVersion_Error()
        {
            await Test(async () =>
            {
                var cancel = new CancellationToken();
                Assert.AreEqual(Identifiers.SystemUserId, User.Current.Id);

                var file = CreateTestFile(save: false);
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.Description = "1.0";
                await file.SaveAsync(cancel); // V0.1.D
                await file.PublishAsync(cancel); // V1.0.A
                file = await CreateVersionsFor_RestoreVersion_MajorAndMinor(file, cancel);

                string errorMessage = null;
                try
                {
                    // ACT
                    file = (File)await file.RestoreVersionAsync(new VersionNumber(4, 2), cancel);
                    Assert.Fail("The expected exception was not thrown.");
                }
                catch (InvalidContentActionException e)
                {
                    errorMessage = e.Message;
                }

                // ASSERT
                Assert.IsNotNull(errorMessage);
                Assert.IsTrue(errorMessage.StartsWith("Cannot restore the version 'V4.2.D' because it does not exist on content"));
                Assert.IsTrue(file.IsLatestVersion, "File version is not correct after restore.");
                Assert.AreEqual("V3.1.D", file.Version.ToString());
                Assert.AreEqual("3.1", file.Description);
            });
        }
        private async Task<File> CreateVersionsFor_RestoreVersion_MajorAndMinor(File file, CancellationToken cancel)
        {
            await file.CheckOutAsync(cancel); // V1.1.L
            file.Description = "1.1";
            await file.SaveAsync(cancel);
            await file.CheckInAsync(cancel); // V1.1.D

            await file.CheckOutAsync(cancel); // V1.2.L
            file.Description = "2.0";
            await file.SaveAsync(cancel);
            await file.CheckInAsync(cancel); // V1.2.D

            await file.PublishAsync(cancel); // V2.0.A

            await file.CheckOutAsync(cancel); // V2.1.L
            file.Description = "2.1";
            await file.SaveAsync(cancel);
            await file.CheckInAsync(cancel); // V2.1.D

            await file.CheckOutAsync(cancel); // V2.2.L
            file.Description = "3.0";
            await file.SaveAsync(cancel);
            await file.CheckInAsync(cancel); // V2.2.D

            await file.PublishAsync(cancel); // V3.0.A

            await file.CheckOutAsync(cancel); // V3.1.L
            file.Description = "3.1";
            await file.SaveAsync(cancel);
            await file.CheckInAsync(cancel); // V3.1.D

            return file;
        }

        [TestMethod]
        public async System.Threading.Tasks.Task GC_RestoreVersion_MajorAndMinor_ChangedVersioningMode()
        {
            await Test(async () =>
            {
                var cancel = new CancellationToken();
                Assert.AreEqual(Identifiers.SystemUserId, User.Current.Id);

                var file = CreateTestFile(save: false);
                var parent = (Folder)file.Parent;
                parent.InheritableVersioningMode = InheritableVersioningType.MajorAndMinor;
                await parent.SaveAsync(cancel);
                file.Description = "1.0";
                await file.SaveAsync(cancel); // V0.1.D
                await file.PublishAsync(cancel); // V1.0.A

                await file.CheckOutAsync(cancel); // V1.1.L
                file.Description = "1.1";
                await file.SaveAsync(cancel);
                await file.CheckInAsync(cancel); // V1.1.D

                await file.CheckOutAsync(cancel); // V1.2.L
                file.Description = "2.0";
                await file.SaveAsync(cancel);
                await file.CheckInAsync(cancel); // V1.2.D

                await file.PublishAsync(cancel); // V2.0.A

                await file.CheckOutAsync(cancel); // V2.1.L
                file.Description = "2.1";
                await file.SaveAsync(cancel);
                await file.CheckInAsync(cancel); // V2.1.D

                await file.CheckOutAsync(cancel); // V2.2.L
                // Change versioning modes
                parent.InheritableVersioningMode = InheritableVersioningType.Inherited;
                await parent.SaveAsync(cancel);
                file.VersioningMode = VersioningType.MajorAndMinor;
                file.Description = "3.0";
                await file.SaveAsync(cancel);
                await file.CheckInAsync(cancel); // V2.2.D

                await file.PublishAsync(cancel); // V3.0.A

                await file.CheckOutAsync(cancel); // V3.1.L
                file.Description = "3.1";
                await file.SaveAsync(cancel);
                await file.CheckInAsync(cancel); // V3.1.D

                // ACT
                file = (File)await file.RestoreVersionAsync(new VersionNumber(1, 1), cancel);
                // versions before: V1.0.A, V1.1.D, V2.0.A, V2.1.D, V3.0.A, V3.1.D
                // versions after:  V1.0.A, V1.1.D, V2.0.A, V2.1.D, V3.0.A, V3.1.D, V3.2.D

                // ASSERT
                Assert.IsTrue(file.IsLatestVersion, "File version is not correct after restore.");
                Assert.AreEqual("V3.2.D", file.Version.ToString());
                Assert.AreEqual("1.1", file.Description);
            });
        }

        /* ============================================================================= BinaryData */

        [TestMethod]
        public void GC_None_CheckoutSaveCheckin_BinaryData()
        {
            Test(() =>
            {
                var container = CreateTestRoot(save: false);
                container.InheritableVersioningMode = InheritableVersioningType.None;
                container.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var file = CreateTestFile(container, save: false);
                var stream = RepositoryTools.GetStreamFromString("asdf qwer yxcv");
                var binaryData = new BinaryData { ContentType = "text/plain", FileName = "1.txt", Size = stream.Length };
                binaryData.SetStream(stream);
                file.SetBinary("Binary", binaryData);
                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var fileId = file.Id;

                // operation

                file = Node.Load<File>(fileId);
                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                file = Node.Load<File>(fileId);
                file.Binary.SetStream(RepositoryTools.GetStreamFromString("asdf qwer yxcv 123"));
                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                file = Node.Load<File>(fileId);
                file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

                // assertion

                file = Node.Load<File>(fileId);
                var s = RepositoryTools.GetStreamString(file.Binary.GetStream());

                Assert.IsTrue(s == "asdf qwer yxcv 123");
            });
        }

        //[TestMethod]
        public void GC_MajorAndMinor_CheckoutSaveCheckin_BinaryData()
        {
            Assert.Inconclusive();

            ////--------------------------------------------- prepare

            //var folderContent = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString());
            //var folder = (Folder)folderContent.ContentHandler;
            //folder.InheritableVersioningMode = InheritableVersioningType.MajorAndMinor;
            //folder.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            //var fileContent = Content.CreateNew("File", folder, null);
            //var file = (File)fileContent.ContentHandler;

            //var stream = RepositoryTools.GetStreamFromString("asdf qwer yxcv");
            //var binaryData = new BinaryData { ContentType = "text/plain", FileName = "1.txt", Size = stream.Length };
            //binaryData.SetStream(stream);

            //file.SetBinary("Binary", binaryData);
            //file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            //var fileId = file.Id;

            ////--------------------------------------------- operating

            //file = Node.Load<File>(fileId);
            //file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

            //file = Node.Load<File>(fileId);
            //file.Binary.SetStream(RepositoryTools.GetStreamFromString("asdf qwer yxcv 123"));
            //file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            //file = Node.Load<File>(fileId);
            //file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

            //file = Node.Load<File>(fileId);
            //var s = RepositoryTools.GetStreamString(file.Binary.GetStream());

            //Assert.IsTrue(s == "asdf qwer yxcv 123");
        }

        //[TestMethod]
        public void GC_None_CheckoutSaveCheckin_BinaryData_OfficeProtocolBug()
        {
            Assert.Inconclusive();

            ////--------------------------------------------- prepare

            //var folderContent = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString());
            //var folder = (Folder)folderContent.ContentHandler;
            //folder.InheritableVersioningMode = InheritableVersioningType.None;
            //folder.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            //var fileContent = Content.CreateNew("File", folder, null);
            //var file = (File)fileContent.ContentHandler;

            //var stream2 = RepositoryTools.GetStreamFromString("asdf qwer yxcv");
            //var binaryData = new BinaryData { ContentType = "text/plain", FileName = "1.txt", Size = stream2.Length };
            //binaryData.SetStream(stream2);

            //file.SetBinary("Binary", binaryData);
            //file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            //var fileId = file.Id;

            ////--------------------------------------------- operating

            //var node = Node.LoadNode(fileId);
            //var gc = node as GenericContent;
            //gc.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

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

            //        node2.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
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

        //-------------------------------------------------------------------- Start multistep save -----------------

        //[TestMethod]
        public void GC_MultistepSave_FullFalse()
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

            //test.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

            //vn = test.Version;

            //test.Save(SavingMode.StartMultistepSave);

            //Assert.AreEqual(vn, test.Version, "#6 version is not correct");
            //Assert.AreEqual(ContentSavingState.ModifyingLocked, test.SavingState, string.Format("#7 saving state is incorrect."));

            //test.FinalizeContent();

            //Assert.AreEqual(vn, test.Version, "#8 version is not correct");
            //Assert.AreEqual(ContentSavingState.Finalized, test.SavingState, string.Format("#9 saving state is incorrect."));
        }









        /* ============================================================================= helpers */

        protected GenericContent CreateTestRoot(bool save = true)
        {
            var node = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
            if (save)
                node.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            return node;
        }

        /// <summary>
        /// Creates a file without binary. Name is a GUID if not passed. Parent is a newly created SystemFolder.
        /// </summary>
        protected File CreateTestFile(string name = null, bool save = true)
        {
            return CreateTestFile(CreateTestRoot(), name ?? Guid.NewGuid().ToString(), save);
        }

        /// <summary>
        /// Creates a file without binary under the given parent node.
        /// </summary>
        protected static File CreateTestFile(Node parent, string name = null, bool save = true)
        {
            var file = new File(parent) { Name = name ?? Guid.NewGuid().ToString() };
            if(save)
                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            return file;
        }

    }
}
