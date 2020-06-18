using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class TrashTests : TestBase
    {
        [TestMethod]
        public void Delete_Trash_WithoutDeletePermission()
        {
            Test(true, () =>
            {
                var originalUser = AccessProvider.Current.GetCurrentUser();

                File file;
                using (new SystemAccount())
                {
                    file = CreateTestFile();

                    // give Visitor only Open permission, not Delete
                    // (workaround: add permissions for Visitor to the user content and to the Trash to make this test work)
                    SecurityHandler.CreateAclEditor()
                        .Allow(file.Id, Identifiers.VisitorUserId, false, PermissionType.OpenMinor)
                        .Allow(TrashBin.Instance.Id, Identifiers.VisitorUserId, false, PermissionType.Open)
                        .Allow(Identifiers.VisitorUserId, Identifiers.VisitorUserId, false, PermissionType.Open)
                        .Apply();
                }

                var thrown = false;

                try
                {
                    AccessProvider.Current.SetCurrentUser(User.Visitor);

                    // action: try to trash the file as Visitor
                    TrashBin.DeleteNode(file);
                }
                catch (InvalidOperationException ex)
                {
                    if (ex.Message.Contains("You do not have enough permissions to delete this content"))
                        thrown = true;
                }
                finally
                {
                    AccessProvider.Current.SetCurrentUser(originalUser);
                }

                Assert.IsTrue(thrown, "The expected exception was not thrown.");
            });
        }

        [TestMethod]
        public void Delete_Trash_WithoutAddNewPermission()
        {
            Test(true, () =>
            {
                var originalUser = AccessProvider.Current.GetCurrentUser();

                File file;
                using (new SystemAccount())
                {
                    file = CreateTestFile();

                    // give Visitor Delete permission to the file, but not AddNew
                    // (workaround: add permissions for Visitor to the user content and to the Trash to make this test work)
                    SecurityHandler.CreateAclEditor()
                        .Allow(file.Id, Identifiers.VisitorUserId, false, 
                            PermissionType.OpenMinor, PermissionType.Delete)
                        .Allow(TrashBin.Instance.Id, Identifiers.VisitorUserId, false, PermissionType.Open)
                        .Allow(Identifiers.VisitorUserId, Identifiers.VisitorUserId, false, PermissionType.Open)
                        .Apply();
                }

                try
                {
                    AccessProvider.Current.SetCurrentUser(User.Visitor);

                    // action: try to trash the file as Visitor - it should succeed
                    TrashBin.DeleteNode(file);
                }
                finally
                {
                    AccessProvider.Current.SetCurrentUser(originalUser);
                }
            });
        }

        #region Helper methods

        protected GenericContent CreateTestRoot(bool save = true)
        {
            var node = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
            if (save)
                node.Save();
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
            if (save)
                file.Save();
            return file;
        }

        #endregion
    }
}
