using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Security;
using SenseNet.Tests.Core;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class NodeTests : TestBase
    {
        private const string ManagerPropertyName = "Manager";

        [TestMethod, TestCategory("NODE, REFERENCE")]
        public void Node_Reference_SetReference()
        {
            Test(() =>
            {
                var admin = User.Administrator;
                var visitor = User.Visitor;

                visitor.SetReference(ManagerPropertyName, admin);

                var manager = visitor.GetReferences(ManagerPropertyName).Single();

                Assert.IsNotNull(manager);
                Assert.AreEqual(admin.Id, manager.Id);
            });
        }
        [TestMethod, TestCategory("NODE, REFERENCE")]
        public void Node_Reference_SetReference_NotDistinct()
        {
            Test(() =>
            {
                var admin = User.Administrator;
                var visitor = User.Visitor;

                // list containing non-distinct elements
                visitor.SetReferences(ManagerPropertyName, new Node[] { admin, admin });

                var managers = visitor.GetReferences(ManagerPropertyName).ToArray();

                Assert.AreEqual(2, managers.Length);
                Assert.AreEqual(admin.Id, managers[0].Id);
                Assert.AreEqual(admin.Id, managers[1].Id);
            });
        }
        [TestMethod, TestCategory("NODE, REFERENCE")]
        public void Node_Reference_SetReference_NullOrEmpty()
        {
            Test(() =>
            {
                var admin = User.Administrator;
                var visitor = User.Visitor;

                // initial value
                visitor.SetReference(ManagerPropertyName, admin);
                
                // set null list
                visitor.SetReferences<Node>(ManagerPropertyName, null);
                Assert.IsFalse(visitor.GetReferences(ManagerPropertyName).Any());

                // initial value
                visitor.SetReference(ManagerPropertyName, admin);

                // set empty list
                visitor.SetReferences(ManagerPropertyName, new Node[0]);
                Assert.IsFalse(visitor.GetReferences(ManagerPropertyName).Any());
            });
        }

        [TestMethod, TestCategory("NODE, REFERENCE")]
        public void Node_Reference_AddReference_Distinct()
        {
            Test(() =>
            {
                var admin = User.Administrator;
                var visitor = User.Visitor;

                visitor.SetReference(ManagerPropertyName, admin);
                visitor.AddReferences(ManagerPropertyName, new Node[] { admin }, true);

                var manager = visitor.GetReferences(ManagerPropertyName).Single();

                Assert.IsNotNull(manager);
                Assert.AreEqual(admin.Id, manager.Id);
                
                visitor.AddReferences(ManagerPropertyName, new Node[] { admin }, false);

                var managers = visitor.GetReferences(ManagerPropertyName).ToArray();

                Assert.AreEqual(2, managers.Length);
                Assert.AreEqual(admin.Id, managers[0].Id);
                Assert.AreEqual(admin.Id, managers[1].Id);
            });
        }

        [TestMethod, TestCategory("NODE, REFERENCE, FIX")] //Fix1291_SetMembersDoNotRemovesHiddenItems
        public void Node_Reference_SetReference_Multiple_Invisible()
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
                    Members = new[] { u2, u3 }
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
                    loadedGroup.Members = new[] { u4 };
                    loadedGroup.Save();
                }

                // ASSERT
                var reloaded = Node.Load<Group>(group.Id);
                var actual = string.Join(", ", reloaded.Members.Select(x => x.Name));
                Assert.AreEqual("U1, U4", actual);
            });
        }

        [TestMethod, TestCategory("NODE, REFERENCE")]
        public void Node_Reference_SetReference_Simple_Invisible()
        {
            Test(() =>
            {
                var root = CreateTestRoot();
                var u1 = CreateUser("U1");
                var target0 = new Folder(root) { Name = "folder1" };
                target0.Save();
                var target1 = new Folder(root) { Name = "folder2" };
                target1.Save();
                var link = new ContentLink(root) { Name = "Link1", Link = target0 };
                link.Save();

                SecurityHandler.CreateAclEditor()
                    .Allow(target1.Id, u1.Id, false, PermissionType.See)
                    .Allow(link.Id, u1.Id, false, PermissionType.Save)
                    .Apply();

                Assert.IsFalse(target0.Security.HasPermission(u1, PermissionType.See));
                Assert.IsTrue(target1.Security.HasPermission(u1, PermissionType.See));
                Assert.IsTrue(link.Security.HasPermission(u1, PermissionType.Save));

                // ACTION
                using (new CurrentUserBlock(u1))
                {
                    var loadedLink = Node.Load<ContentLink>(link.Id);

                    // Restrictive user cannot access the current target.
                    //Assert.IsNull(loadedLink.Link);
                    try
                    {
                        var currentLint = loadedLink.Link;
                        Assert.Fail("The expected AccessDeniedException was not thrown.");
                    }
                    catch (SenseNetSecurityException)
                    {
                        // expected exception
                    }

                    // Set new target
                    var loadedTarget = Node.LoadNode(target1.Id);
                    loadedLink.Link = loadedTarget;
                    loadedLink.Save();
                }

                // ASSERT
                var reloaded = Node.Load<ContentLink>(link.Id);
                Assert.AreEqual(reloaded.Link.Id, target1.Id);
            });
        }



        [TestMethod, TestCategory("NODE, LOAD")]
        public async STT.Task Node_Load()
        {
            await Test(async () =>
            {
                var admin = User.Administrator;
                var visitor = User.Visitor;

                var a1 = await Content.LoadAsync(admin.Id, CancellationToken.None);
                Assert.AreEqual(admin.Path, a1.Path);
                var a2 = await Content.LoadAsync(admin.Path, CancellationToken.None);
                Assert.AreEqual(admin.Path, a2.Path);

                var a3 = await Content.LoadByIdOrPathAsync($"{admin.Id}", CancellationToken.None);
                Assert.AreEqual(admin.Path, a3.Path);
                var a4 = await Content.LoadByIdOrPathAsync(admin.Path, CancellationToken.None);
                Assert.AreEqual(admin.Path, a4.Path);

                var a5 = await Node.LoadNodeAsync(admin.Id, VersionNumber.LastFinalized, CancellationToken.None);
                Assert.AreEqual(admin.Path, a5.Path);

                var a6 = await Node.LoadAsync<User>(admin.Id, CancellationToken.None);
                Assert.AreEqual(admin.Path, a6.Path);
                var a7 = await Node.LoadAsync<User>(admin.Path, CancellationToken.None);
                Assert.AreEqual(admin.Path, a7.Path);

                var nodes1 = await Node.LoadNodesAsync(new [] {admin.Id, visitor.Id}, CancellationToken.None);
                Assert.AreEqual(admin.Id, nodes1[0].Id);
                Assert.AreEqual(visitor.Id, nodes1[1].Id);
            });
        }

        /* ============================================================================== */

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
