using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Caching;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.Virtualization;
using SenseNet.Security;
using SenseNet.Tests.Core;
// ReSharper disable UnusedVariable

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class UserTests : TestBase
    {
        //TODO: Rewrite this class when a MembershipExtender alternative is developing.
        //private class TestMembershipExtender : MembershipExtenderBase
        //{
        //    public override MembershipExtension GetExtension(IUser user)
        //    {
        //        if (!(user is User userContent))
        //            return EmptyExtension;
        //        if (userContent.DisplayName != "User_sensenet393_BugReproduction")
        //            return EmptyExtension;

        //        // this line can cause infinite recursion.
        //        var requestedNode = PortalContext.Current.ContextNode;

        //        var groupIds = ((User) requestedNode).GetDynamicGroups(2);

        //        var testGroup = (IGroup)Node.LoadNode("/Root/IMS/BuiltIn/Portal/TestGroup");
        //        return new MembershipExtension(new []{ testGroup });
        //    }
        //}

        //TODO: Rewrite this test when a MembershipExtender alternative is developing.
        //[TestMethod]
        //public void User_sensenet393_BugReproduction()
        //{
        //    Test(true, () =>
        //    {
        //        Providers.Instance.CacheProvider = new SnMemoryCache();

        //        Group group;
        //        User user;
        //        using (new SystemAccount())
        //        {
        //            var ed = SecurityHandler.CreateAclEditor();
        //            ed.Set(Repository.Root.Id, User.Administrator.Id, false, PermissionBitMask.AllAllowed);
        //            ed.Set(Repository.Root.Id, Group.Administrators.Id, false, PermissionBitMask.AllAllowed);
        //            ed.Apply();

        //            var portal = Node.LoadNode("/Root/IMS/BuiltIn/Portal");

        //            group = new Group(portal)
        //            {
        //                Name = "TestGroup"
        //            };
        //            group.Save();

        //            user = new User(portal)
        //            {
        //                Name = "TestUser",
        //                Enabled = true,
        //                Email = "mail@example.com",
        //                DisplayName = "User_sensenet393_BugReproduction"
        //            };
        //            user.Save();

        //            Group.Administrators.AddMember(user);
        //            User.Current = user;
        //        }

        //        Providers.Instance.MembershipExtender = new TestMembershipExtender();

        //        var simulatedOutput = new StringWriter();
        //        var simulatedWorkerRequest = new SimulatedHttpRequest(@"\", @"C:\Inetpub\wwwroot", user.Path, "",
        //            simulatedOutput, "localhost_forms");
        //        var simulatedHttpContext = new HttpContext(simulatedWorkerRequest);
        //        var portalContext = PortalContext.Create(simulatedHttpContext);
        //        HttpContext.Current = simulatedHttpContext;

        //        // This line caused StackOverflowException
        //        var additionalGroups = user.GetDynamicGroups(2);

        //        // The bug is fixed if the code can run up to this point
        //        // but we test the full feature.
        //        Assert.AreEqual(group.Id, additionalGroups.First());
        //    });
        //}

        [TestMethod]
        public void User_CreateByRegularUser()
        {
            Test(true, () =>
            {
                var parentPath = "/Root/IMS/BuiltIn/Temp-" + Guid.NewGuid();
                var originalUser = AccessProvider.Current.GetOriginalUser();

                try
                {
                    User user1;
                    Content parent;

                    using (new SystemAccount())
                    {
                        // create a test container
                        parent = RepositoryTools.CreateStructure(parentPath, "OrganizationalUnit");

                        // create a test user
                        user1 = new User(parent.ContentHandler)
                        {
                            Name = "sample-sam",
                            LoginName = "samplesam@example.com",
                            Email = "samplesam@example.com"
                        };
                        user1.Save();

                        // add permissions for this test user (local Add, but not TakeOwnership) and for Owners (everything)
                        var editor = SnSecurityContext.Create().CreateAclEditor();
                        editor
                            .Allow(parent.Id, user1.Id, true, PermissionType.AddNew)
                            .Allow(parent.Id, Identifiers.OwnersGroupId, false, PermissionType.BuiltInPermissionTypes)
                            // technical permission for content types
                            .Allow(Identifiers.PortalRootId, user1.Id, false, PermissionType.See);
                        editor.Apply();
                    }

                    AccessProvider.Current.SetCurrentUser(user1);

                    // create a new user in the name of the test user
                    var user2 = new User(parent.ContentHandler)
                    {
                        Name = "newser",
                        LoginName = "newser@example.com",
                        Email = "newser@example.com"
                    };

                    //ACTION
                    user2.Save();

                    using (new SystemAccount())
                    {
                        // user1 could create user2, but cannot open or modify it, because he only has AddNew permission on the parent
                        Assert.IsFalse(user2.Security.HasPermission(user1, PermissionType.Open));

                        // the new user should have permissions for herself because she is the owner
                        Assert.IsTrue(user2.Security.HasPermission(user2, PermissionType.Save));
                    }
                }
                finally
                {
                    AccessProvider.Current.SetCurrentUser(originalUser);

                    using (new SystemAccount())
                        if (Node.Exists(parentPath))
                            Node.ForceDelete(parentPath);
                }
            });
        }

        [TestMethod]
        public void User_Roles()
        {
            string GetNames(IEnumerable<Node> list)
            {
                return string.Join(", ", list.Select(x => x.Name));
            }

            Test(() =>
            {
                var adminContent = Content.Create(User.Administrator);
                var allAdminRoles = adminContent["AllRoles"];
                var directAdminRoles = adminContent["DirectRoles"];

                var group1 = new Group(OrganizationalUnit.Portal) { Name = "Group-1" };
                group1.Save();
                var group2 = new Group(OrganizationalUnit.Portal) { Name = "Group-2" };
                group2.Save();

                // ACTION-1: Create user
                var user = new User(OrganizationalUnit.Portal)
                {
                    Name = "User-1",
                    Email = "user1@example.com",
                    Enabled = true
                };
                user.Save();

                // ASSERT-1: There are no known roles
                var content = Content.Create(user);
                var allRoles = GetNames((IEnumerable<Node>) content["AllRoles"]);
                var directRoles = GetNames((IEnumerable<Node>)content["DirectRoles"]);
                Assert.IsFalse(allRoles.Contains("Group-1"));
                Assert.IsFalse(directRoles.Contains("Group-1"));
                Assert.IsFalse(allRoles.Contains("Group-2"));
                Assert.IsFalse(directRoles.Contains("Group-2"));

                // ACTION-2: Set direct member
                group1.AddMember(user);

                // ASSERT-2
                content = Content.Create(user);
                allRoles = GetNames((IEnumerable<Node>)content["AllRoles"]);
                directRoles = GetNames((IEnumerable<Node>)content["DirectRoles"]);
                Assert.IsTrue(allRoles.Contains("Group-1"));
                Assert.IsTrue(directRoles.Contains("Group-1"));
                Assert.IsFalse(allRoles.Contains("Group-2"));
                Assert.IsFalse(directRoles.Contains("Group-2"));

                // ACTION-3: Set indirect member
                group2.AddMember(group1);

                // ASSERT-3:
                content = Content.Create(user);
                allRoles = GetNames((IEnumerable<Node>)content["AllRoles"]);
                directRoles = GetNames((IEnumerable<Node>)content["DirectRoles"]);
                Assert.IsTrue(allRoles.Contains("Group-1"));
                Assert.IsTrue(directRoles.Contains("Group-1"));
                Assert.IsTrue(allRoles.Contains("Group-2"));
                Assert.IsFalse(directRoles.Contains("Group-2"));

                // ACTION-4: Remove direct member
                group1.RemoveMember(user);

                // ASSERT-4
                content = Content.Create(user);
                allRoles = GetNames((IEnumerable<Node>)content["AllRoles"]);
                directRoles = GetNames((IEnumerable<Node>)content["DirectRoles"]);
                Assert.IsFalse(allRoles.Contains("Group-1"));
                Assert.IsFalse(directRoles.Contains("Group-1"));
                Assert.IsFalse(allRoles.Contains("Group-2"));
                Assert.IsFalse(directRoles.Contains("Group-2"));

            });
        }

        [TestMethod]
        public void User_Roles_Inaccessible()
        {
            Test(() =>
            {
                var user1 = new User(OrganizationalUnit.Portal)
                {
                    Name = "User-1",
                    Email = "user1@example.com",
                    Enabled = true
                };
                user1.Save();

                var group1 = new Group(OrganizationalUnit.Portal) { Name = "Group-1" };
                group1.Save();
                group1.AddMember(user1);

                var allRolesWithAdmin = ((IEnumerable<Node>) Content.Create(user1)["AllRoles"]).ToArray();

                Assert.IsNotNull(allRolesWithAdmin.Single(r => r.Name == "Portal"));
                Assert.IsNotNull(allRolesWithAdmin.Single(r => r.Name == group1.Name));

                var originalUser = AccessProvider.Current.GetOriginalUser();
                try
                {
                    // switch to a user with few permissions
                    AccessProvider.Current.SetCurrentUser(user1);

                    var allRolesWithUser = ((IEnumerable<Node>)Content.Create(user1)["AllRoles"]).ToArray();

                    // the user does not have permission for these roles, but the field does not throw an exception
                    Assert.AreEqual(0, allRolesWithUser.Length);
                }
                finally
                {
                    AccessProvider.Current.SetCurrentUser(originalUser);
                }
            });
        }
    }
}
