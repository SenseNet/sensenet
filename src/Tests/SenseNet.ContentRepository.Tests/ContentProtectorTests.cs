using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
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
                node.Save();

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

            // CLEANUP: Restore the original list
            Providers.Instance.ContentProtector = new ContentProtector();

            // TEST-2: The "TestFolder" is deletable.
            Test(() =>
            {
                var node = new SystemFolder(Repository.Root) { Name = "TestFolder" };
                node.Save();

                node.ForceDelete();
            });
        }

        [TestMethod]
        public void ContentProtector_ParentAxis()
        {
            var originalList = new string[0];

            Test(builder =>
            {
                originalList = ContentProtector.GetProtectedPaths();

                // Add a deep path
                builder.ProtectContent("/Root/A/B/C");
            }, () =>
            {
                var actual = string.Join(" ", 
                    ContentProtector.GetProtectedPaths().Except(originalList));

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
                var originalList = ContentProtector.GetProtectedPaths();
                var expectedFirst = originalList[0];
                originalList[0] = null;

                var actualList = ContentProtector.GetProtectedPaths();
                var actualFirst = actualList[0];

                Assert.AreEqual(expectedFirst, actualFirst);
            });

        }

        [TestMethod]
        public void ContentProtector_Group_DeleteLastUser()
        {
            Test(builder =>
            {
                // protect groups
                builder.ProtectGroups("/Root/IMS/Public/group1", "/Root/IMS/Public/group2");
            }, () =>
            {
                CreateUserAndGroupStructure();

                // direct member of group1
                AssertProtectedGroup_DeleteUser("/Root/IMS/Public/TestOrg/user1");
                // direct member of group2
                AssertProtectedGroup_DeleteUser("/Root/IMS/Public/TestOrg/user2");
                
                // can be deleted, group3 is not protected
                Node.ForceDelete("/Root/IMS/Public/TestOrg/user3");
                // user without groups
                Node.ForceDelete("/Root/IMS/Public/TestOrg/user4");

                // CLEANUP: Restore the original list
                Providers.Instance.ContentProtector = new ContentProtector();

                // can delete anybody
                Node.ForceDelete("/Root/IMS/Public/TestOrg/user1");
                Node.ForceDelete("/Root/IMS/Public/TestOrg/user2");
            });
        }
        [TestMethod]
        public void ContentProtector_Group_DisableLastUser()
        {
            Test(builder =>
            {
                // protect groups
                builder.ProtectGroups("/Root/IMS/Public/group1", "/Root/IMS/Public/group2");
            }, () =>
            {
                CreateUserAndGroupStructure();

                // direct member of group1
                AssertProtectedGroup_DisableUser("/Root/IMS/Public/TestOrg/user1");
                // direct member of group2
                AssertProtectedGroup_DisableUser("/Root/IMS/Public/TestOrg/user2");

                // can be disabled, group3 is not protected
                DisableUser("/Root/IMS/Public/TestOrg/user3");
                // user without groups
                DisableUser("/Root/IMS/Public/TestOrg/user4");

                // CLEANUP: Restore the original list
                Providers.Instance.ContentProtector = new ContentProtector();

                // can disable anybody
                DisableUser("/Root/IMS/Public/TestOrg/user1");
                DisableUser("/Root/IMS/Public/TestOrg/user2");
            });
        }
        [TestMethod]
        public void ContentProtector_Group_RemoveLastUser()
        {
            Test(builder =>
            {
                // protect groups
                builder.ProtectGroups("/Root/IMS/Public/group1", "/Root/IMS/Public/group2");
            }, () =>
            {
                CreateUserAndGroupStructure();

                // direct member of group1
                AssertProtectedGroup_RemoveUser("/Root/IMS/Public/group1", "/Root/IMS/Public/TestOrg/user1");
                // direct member of group2
                AssertProtectedGroup_RemoveUser("/Root/IMS/Public/group2", "/Root/IMS/Public/TestOrg/user2");

                // can be removed, group3 is not protected
                RemoveUser("/Root/IMS/Public/group3", "/Root/IMS/Public/TestOrg/user3");

                // CLEANUP: Restore the original list
                Providers.Instance.ContentProtector = new ContentProtector();

                // can remove anything
                RemoveUser("/Root/IMS/Public/group1", "/Root/IMS/Public/TestOrg/user1");
                RemoveUser("/Root/IMS/Public/group2", "/Root/IMS/Public/TestOrg/user2");
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
            user.Save(SavingMode.KeepVersion);
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
             *          TestOrg
             *              user1
             *              user2
             *              user3
             *              user4
             *
             *      group1 members: user1
             *      group2 members: user2, group3   (--> and user3 transitively!)
             *      group3 members: user3
             */

            var publicDomain = Node.LoadNode("/Root/IMS/Public");

            var group1 = new Group(publicDomain) { Name = "group1" };
            group1.Save();
            var group2 = new Group(publicDomain) { Name = "group2" };
            group2.Save();
            var group3 = new Group(publicDomain) { Name = "group3" };
            group3.Save();

            var org = new OrganizationalUnit(publicDomain) { Name = "TestOrg" };
            org.Save();

            static User CreateUser(Node parent, string name)
            {
                var user = new User(parent)
                {
                    Name = name,
                    Email = name + "@example.com",
                    Enabled = true
                };
                user.Save();
                return user;
            }

            var user1 = CreateUser(org, "user1");
            var user2 = CreateUser(org, "user2");
            var user3 = CreateUser(org, "user3");

            // user without groups
            var _ = CreateUser(org, "user4");

            group1.AddMember(user1);
            group2.AddMember(user2);
            group2.AddMember(group3);
            group3.AddMember(user3);
        }
    }
}
