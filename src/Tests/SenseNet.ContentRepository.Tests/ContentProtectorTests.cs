﻿using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ContentProtectorTests : TestBase
    {
        [TestMethod]
        public void ContentProtector_ExtendTheList()
        {
            // TEST-1: The "TestFolder" is protected.
            Test(builder =>
            {
                // Protect a content with the white list extension
                builder.ProtectContent("/Root/TestFolder");
            }, () =>
            {
                var node = new SystemFolder(Repository.Root) { Name = "TestFolder" };
                node.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                try
                {
                    node.ForceDelete();
                    Assert.Fail("The expected exception was not thrown.");
                }
                catch (ApplicationException)
                {
                    // do nothing
                }
            });

            // TEST-2: The "TestFolder" is deletable.
            Test(() =>
            {
                var node = new SystemFolder(Repository.Root) { Name = "TestFolder" };
                node.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                node.ForceDelete();
            });
        }

        [TestMethod]
        public void ContentProtector_ParentAxis()
        {
            var originalList = new string[0];

            Test(builder =>
            {
                originalList = Providers.Instance.ContentProtector.GetProtectedPaths();

                // Add a deep path
                builder.ProtectContent("/Root/A/B/C");
            }, () =>
            {
                var actual = string.Join(" ",
                    Providers.Instance.ContentProtector.GetProtectedPaths().Except(originalList));

                // The whole parent axis added but the "Except" operation removes the "/Root".
                var expected = "/Root/A /Root/A/B /Root/A/B/C";

                Assert.AreEqual(expected, actual);
            });
        }

        [TestMethod]
        public void ContentProtector_ListIsImmutable()
        {
            Test(builder =>
            {
                // Additional path
                builder.ProtectContent("/Root/TestFolder");
            }, () =>
            {
                var originalList = Providers.Instance.ContentProtector.GetProtectedPaths();
                var expectedFirst = originalList[0];
                originalList[0] = null;

                var actualList = Providers.Instance.ContentProtector.GetProtectedPaths();
                var actualFirst = actualList[0];

                Assert.AreEqual(expectedFirst, actualFirst);
            });

        }

        [TestMethod, TestCategory("Services")]
        public void ContentProtector_Group_DeleteLastUser_CSrv()
        {
            Test(builder =>
            {
                // protect groups
                builder.ProtectGroups("/Root/IMS/Public/group1", "/Root/IMS/Public/group2", "/Root/IMS/Public/group5");
            }, () =>
            {
                CreateUserAndGroupStructure();

                // direct member of group1
                AssertProtectedGroup_DeleteUser("/Root/IMS/Public/TestOrg/user1");
                // direct member of group2
                AssertProtectedGroup_DeleteUser("/Root/IMS/Public/TestOrg/user2");
                // group5 with a single member except the public admin
                AssertProtectedGroup_DeleteUser("/Root/IMS/Public/TestOrg/user5");

                // try to delete the container
                AssertProtectedGroup_DeleteUser("/Root/IMS/Public/TestOrg");

                // can be deleted, group3 is not protected
                Node.ForceDelete("/Root/IMS/Public/TestOrg/user3");
                // user without groups
                Node.ForceDelete("/Root/IMS/Public/TestOrg/user4");
            });
        }
        [TestMethod]
        public void ContentProtector_Group_DisableLastUser()
        {
            Test(builder =>
            {
                // protect groups
                builder.ProtectGroups("/Root/IMS/Public/group1", "/Root/IMS/Public/group2", "/Root/IMS/Public/group5");
            }, () =>
            {
                CreateUserAndGroupStructure();

                // direct member of group1
                AssertProtectedGroup_DisableUser("/Root/IMS/Public/TestOrg/user1");
                // direct member of group2
                AssertProtectedGroup_DisableUser("/Root/IMS/Public/TestOrg/user2");
                // group5 with a single member except the public admin
                AssertProtectedGroup_DisableUser("/Root/IMS/Public/TestOrg/user5");

                // can be disabled, group3 is not protected
                DisableUser("/Root/IMS/Public/TestOrg/user3");
                // user without groups
                DisableUser("/Root/IMS/Public/TestOrg/user4");
            });
        }
        [TestMethod]
        public void ContentProtector_Group_RemoveLastUser()
        {
            Test(builder =>
            {
                // protect groups
                builder.ProtectGroups("/Root/IMS/Public/group1", "/Root/IMS/Public/group2", "/Root/IMS/Public/group5");
            }, () =>
            {
                CreateUserAndGroupStructure();

                // direct member of group1
                AssertProtectedGroup_RemoveUser("/Root/IMS/Public/group1", "/Root/IMS/Public/TestOrg/user1");
                // direct member of group2
                AssertProtectedGroup_RemoveUser("/Root/IMS/Public/group2", "/Root/IMS/Public/TestOrg/user2");
                // group5 with a single member except the public admin
                AssertProtectedGroup_RemoveUser("/Root/IMS/Public/group5", "/Root/IMS/Public/TestOrg/user5");

                // can be removed, group3 is not protected
                RemoveUser("/Root/IMS/Public/group3", "/Root/IMS/Public/TestOrg/user3");
            });
        }

        [TestMethod]
        public System.Threading.Tasks.Task ContentProtector_Group_Empty_AddInternalUser()
        {
            return Test(builder =>
            {
                // protect groups
                builder.ProtectGroups("/Root/IMS/Public/group6");
            }, async () =>
            {
                CreateUserAndGroupStructure();

                var group6 = await Node.LoadAsync<Group>("/Root/IMS/Public/group6", CancellationToken.None);

                // this should complete: adding an INTERNAL user to an EMPTY group
                group6.AddMember(User.PublicAdministrator);
            });
        }

        [TestMethod]
        public void ContentProtector_DeleteUser()
        {
            Test(() =>
            {
                CreateUserAndGroupStructure();

                // add permissions for the users to let them perform the Delete operation
                var user1 = Node.Load<User>("/Root/IMS/Public/TestOrg/user1");
                var user2 = Node.Load<User>("/Root/IMS/Public/TestOrg/user2");
                var testOrg = Node.Load<OrganizationalUnit>("/Root/IMS/Public/TestOrg");

                Providers.Instance.SecurityHandler.CreateAclEditor()
                    .Allow(testOrg.Id, user1.Id, false, PermissionType.Save, PermissionType.Delete)
                    .Allow(testOrg.Id, user2.Id, false, PermissionType.Save, PermissionType.Delete)
                    .Apply();

                var originalUser = AccessProvider.Current.GetOriginalUser();
                try
                {
                    AccessProvider.Current.SetCurrentUser(user1);

                    // delete user2 without a problem
                    user2.ForceDelete();
                }
                finally
                {
                    AccessProvider.Current.SetCurrentUser(originalUser);
                }

                var thrown = false;
                try
                {
                    AccessProvider.Current.SetCurrentUser(user1);

                    // try to delete themselves
                    user1.ForceDelete();
                }
                catch (ApplicationException ex)
                {
                    if (ex.Message.Contains("Users cannot delete"))
                        thrown = true;
                }
                finally
                {
                    AccessProvider.Current.SetCurrentUser(originalUser);
                }

                Assert.IsTrue(thrown, "Exception was not thrown.");

                thrown = false;
                try
                {
                    AccessProvider.Current.SetCurrentUser(user1);

                    // try to delete parent
                    testOrg.ForceDelete();
                }
                catch (ApplicationException ex)
                {
                    if (ex.Message.Contains("Users cannot delete"))
                        thrown = true;
                }
                finally
                {
                    AccessProvider.Current.SetCurrentUser(originalUser);
                }

                Assert.IsTrue(thrown, "Exception was not thrown.");
            });
        }

        private static void AssertProtectedGroup_DeleteUser(string userToDelete)
        {
            try
            {
                Node.ForceDelete(userToDelete);
                Assert.Fail("The expected exception was not thrown.");
            }
            catch (InvalidOperationException)
            {
                // do nothing, this is expected
            }
        }
        private static void AssertProtectedGroup_DisableUser(string userToDisable)
        {
            try
            {
                DisableUser(userToDisable);
                Assert.Fail("The expected exception was not thrown.");
            }
            catch (InvalidOperationException)
            {
                // do nothing, this is expected
            }
        }
        private static void AssertProtectedGroup_RemoveUser(string groupPath, string userToRemove)
        {
            try
            {
                RemoveUser(groupPath, userToRemove);
                Assert.Fail("The expected exception was not thrown.");
            }
            catch (InvalidOperationException)
            {
                // do nothing, this is expected
            }
        }

        private static void DisableUser(string userToDisable)
        {
            var user = Node.Load<User>(userToDisable);
            user.Enabled = false;
            user.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();
        }
        private static void RemoveUser(string groupPath, string userToRemove)
        {
            var group = Node.Load<Group>(groupPath);
            var user = Node.Load<User>(userToRemove);

            group.RemoveMember(user);
        }
        private static void CreateUserAndGroupStructure()
        {
            /*
             *      ../Public
             *          group1
             *          group2
             *          group3
             *          group5
             *          group6
             *          TestOrg
             *              user1
             *              user2
             *              user3
             *              user4
             *              user5
             *
             *      group1 members: user1
             *      group2 members: user2, group3   (--> and user3 transitively!)
             *      group3 members: user3
             *      group5 members: internal public admin user, user5
             *      group6 members: EMPTY group
             */

            var publicDomain = Node.LoadNode("/Root/IMS/Public");

            var group1 = new Group(publicDomain) { Name = "group1" };
            group1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            var group2 = new Group(publicDomain) { Name = "group2" };
            group2.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            var group3 = new Group(publicDomain) { Name = "group3" };
            group3.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            var group5 = new Group(publicDomain) { Name = "group5" };
            group5.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            var group6 = new Group(publicDomain) { Name = "group6" };
            group6.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            var org = new OrganizationalUnit(publicDomain) { Name = "TestOrg" };
            org.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            static User CreateUser(Node parent, string name)
            {
                var user = new User(parent)
                {
                    Name = name,
                    Email = name + "@example.com",
                    Enabled = true
                };
                user.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                return user;
            }

            var user1 = CreateUser(org, "user1");
            var user2 = CreateUser(org, "user2");
            var user3 = CreateUser(org, "user3");

            // user without groups
            var _ = CreateUser(org, "user4");

            var user5 = CreateUser(org, "user5");

            group1.AddMember(user1);
            group2.AddMember(user2);
            group2.AddMember(group3);
            group3.AddMember(user3);
            group5.AddMember(user5);
            group5.AddMember(User.PublicAdministrator);
        }
    }
}
