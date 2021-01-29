using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class GroupTests : TestBase
    {
        [TestMethod]
        public void Group_Fix1291_SetMembersDoNotRemovesHiddenItems()
        {
            Test(() =>
            {
                var root = CreateTestRoot();
                var u1 = CreateUser("U1");
                var u2 = CreateUser("U2");
                var u3 = CreateUser("U3");
                var u4 = CreateUser("U4");
                var group = new Group(Node.LoadNode("/Root/IMS/Public"))
                {
                    Name = "Group1",
                    Members = new[] {u2, u3}
                };
                group.Save();

                SecurityHandler.CreateAclEditor()
                    .Allow(u3.Id, u1.Id, false, PermissionType.See)
                    .Allow(u4.Id, u1.Id, false, PermissionType.See)
                    .Allow(group.Id, u1.Id, false, PermissionType.Save)
                    .Apply();

                Assert.IsFalse(u2.Security.HasPermission(u1, PermissionType.See));
                Assert.IsTrue(u3.Security.HasPermission(u1, PermissionType.See));
                Assert.IsTrue(u4.Security.HasPermission(u1, PermissionType.See));
                Assert.IsTrue(group.Security.HasPermission(u1, PermissionType.Save));

                // ACTION
                using (new CurrentUserBlock(u1))
                {
                    var loadedGroup = Node.Load<Group>(group.Id);

                    // Restrictive user does not see the user "U2".
                    var loadedGroupMembers = loadedGroup.Members.ToArray();
                    Assert.AreEqual(1, loadedGroupMembers.Length);
                    Assert.AreEqual(u3.Id, loadedGroupMembers[0].Id);

                    // Set new membership
                    var loadedUser = Node.Load<User>(u4.Id);
                    loadedGroup.Members = new[] {u4};
                    loadedGroup.Save();
                }

                // ASSERT
                var reloaded = Node.Load<Group>(group.Id);
                var actual = string.Join(", ", reloaded.Members.Select(x => x.Name));
                Assert.AreEqual("U1, U4", actual);
            });
        }
        private GenericContent CreateTestRoot()
        {
            var node = new SystemFolder(Repository.Root) { Name = "_GroupTests" };
            node.Save();
            return node;
        }
        private User CreateUser(string name)
        {
            var node = new User(Node.LoadNode("/Root/IMS/Public"))
            {
                Name = name,
                Email = $"{name}@example.com",
                Enabled = true
            };
            node.Save();
            return node;
        }
    }
}
