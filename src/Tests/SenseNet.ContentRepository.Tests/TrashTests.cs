﻿using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tests.Core;
using STT = System.Threading.Tasks;

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
                    Providers.Instance.SecurityHandler.CreateAclEditor()
                        .Allow(file.Id, Identifiers.VisitorUserId, false, PermissionType.OpenMinor)
                        .Allow(TrashBin.Instance.Id, Identifiers.VisitorUserId, false, PermissionType.Open)
                        .Allow(Identifiers.VisitorUserId, Identifiers.VisitorUserId, false, PermissionType.Open)
                        .ApplyAsync(CancellationToken.None).GetAwaiter().GetResult();
                }

                var thrown = false;

                try
                {
                    AccessProvider.Current.SetCurrentUser(User.Visitor);

                    // action: try to trash the file as Visitor
                    TrashBin.DeleteNodeAsync(file, CancellationToken.None).GetAwaiter().GetResult();
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
                    Providers.Instance.SecurityHandler.CreateAclEditor()
                        .Allow(file.Id, Identifiers.VisitorUserId, false, 
                            PermissionType.OpenMinor, PermissionType.Delete)
                        .Allow(TrashBin.Instance.Id, Identifiers.VisitorUserId, false, PermissionType.Open)
                        .Allow(Identifiers.VisitorUserId, Identifiers.VisitorUserId, false, PermissionType.Open)
                        .ApplyAsync(CancellationToken.None).GetAwaiter().GetResult();
                }

                try
                {
                    AccessProvider.Current.SetCurrentUser(User.Visitor);

                    // action: try to trash the file as Visitor - it should succeed
                    TrashBin.DeleteNodeAsync(file, CancellationToken.None).GetAwaiter().GetResult();
                }
                finally
                {
                    AccessProvider.Current.SetCurrentUser(originalUser);
                }
            });
        }

        [TestMethod]
        public async STT.Task Delete_Trash_AdminDeletesWhenOwnerIsVisitor()
        {
            await Test(true, async () =>
            {
                var file = CreateTestFile();
                // The Visitor cannot be owner of a content. Set OwnerId with a deeper API.
                file.MakePrivateData();
                file.Data.OwnerId = User.Visitor.Id;
                await file.SaveAsync(CancellationToken.None);

                using (new CurrentUserBlock(User.Administrator))
                {
                    // ACT
                    await TrashBin.DeleteNodeAsync(file, CancellationToken.None);
                }
            }).ConfigureAwait(false);
        }

        #region Helper methods

        protected GenericContent CreateTestRoot(bool save = true)
        {
            var node = new SystemFolder(Repository.Root)
            {
                Name = Guid.NewGuid().ToString(),
                TrashDisabled = false
            };
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
            if (save)
                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            return file;
        }

        #endregion
    }
}
