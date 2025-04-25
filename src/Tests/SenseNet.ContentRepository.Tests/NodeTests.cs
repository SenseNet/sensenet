using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
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
        readonly CancellationToken _cancel = new CancellationTokenSource().Token;

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
                group.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                Providers.Instance.SecurityHandler.CreateAclEditor()
                    .Allow(u3.Id, u1.Id, false, PermissionType.See)
                    .Allow(u4.Id, u1.Id, false, PermissionType.See)
                    .Allow(group.Id, u1.Id, false, PermissionType.Save)
                    .ApplyAsync(CancellationToken.None).GetAwaiter().GetResult();

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
                    loadedGroup.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                }

                // ASSERT
                var reloaded = Node.Load<Group>(group.Id);
                var actual = string.Join(", ", reloaded.Members.Select(x => x.Name));
                Assert.AreEqual("U2, U4", actual);
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
                target0.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                var target1 = new Folder(root) { Name = "folder2" };
                target1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                var link = new ContentLink(root) { Name = "Link1", Link = target0 };
                link.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                Providers.Instance.SecurityHandler.CreateAclEditor()
                    .Allow(target1.Id, u1.Id, false, PermissionType.See)
                    .Allow(link.Id, u1.Id, false, PermissionType.Save)
                    .ApplyAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.IsFalse(target0.Security.HasPermission(u1, PermissionType.See));
                Assert.IsTrue(target1.Security.HasPermission(u1, PermissionType.See));
                Assert.IsTrue(link.Security.HasPermission(u1, PermissionType.Save));

                // ACTION
                using (new CurrentUserBlock(u1))
                {
                    var loadedLink = Node.Load<ContentLink>(link.Id);

                    // Restrictive user cannot see/access the current target.
                    Assert.IsNull(loadedLink.Link);

                    // Set new target
                    var loadedTarget = Node.LoadNode(target1.Id);
                    loadedLink.Link = loadedTarget;
                    loadedLink.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                }

                // ASSERT
                var reloaded = Node.Load<ContentLink>(link.Id);
                Assert.AreEqual(reloaded.Link.Id, target1.Id);
            });
        }

        [TestMethod, TestCategory("NODE, REFERENCE")] //Fix2202 Deleted reference target can cause nullrefex in the nodelist
        public async STT.Task Node_Reference_GetReference_Single_Deleted()
        {
            await Test(async () =>
            {
                var u1 = CreateUser("U1");
                var u2 = CreateUser("U2");

                u1.SetReference(ManagerPropertyName, u2);
                await u1.SaveAsync(_cancel);

                u1 = await Node.LoadAsync<User>(u1.Id, _cancel);

                Assert.AreEqual(u2.Id, u1.GetReference<User>(ManagerPropertyName)?.Id ?? 0);

                // ACT
                await Node.ForceDeleteAsync(u2.Id, _cancel);

                // ASSERT
                var loaded = await Node.LoadAsync<User>(u1.Id, _cancel);
                var manager = loaded.GetReference<User>(ManagerPropertyName);
                Assert.IsNull(manager);

            }).ConfigureAwait(false);
        }
        [TestMethod, TestCategory("NODE, REFERENCE")] //Fix2202 Deleted reference target can cause nullrefex in the nodelist
        public async STT.Task Node_Reference_GetReference_Single_FromMultiple_Deleted()
        {
            await Test(async () =>
            {
                //var root = CreateTestRoot();
                var u1 = CreateUser("U1");
                var u2 = CreateUser("U2");
                var u3 = CreateUser("U3");
                var u4 = CreateUser("U4");
                var u5 = CreateUser("U5");
                var group = new Group(await Node.LoadNodeAsync("/Root/IMS/Public", _cancel)) { Name = "G1" };
                group.AddReferences("Members", new[] { u1, u2, u3, u4, u5 });
                await group.SaveAsync(_cancel);

                group = await Node.LoadAsync<Group>(group.Id, _cancel);

                var memberIds = group.GetReferences("Members").Select(x => x.Id).ToArray();
                Assert.AreEqual($"{u1.Id}, {u2.Id}, {u3.Id}, {u4.Id}, {u5.Id}", string.Join(", ", memberIds));

                // ACT
                await Node.ForceDeleteAsync(u1.Id, _cancel);
                await Node.ForceDeleteAsync(u2.Id, _cancel);
                await Node.ForceDeleteAsync(u3.Id, _cancel);
                var singleMember = group.GetReference<User>("Members");

                // ASSERT
                Assert.AreEqual(u4.Id, singleMember.Id);

            }).ConfigureAwait(false);
        }

        [TestMethod, TestCategory("NODE, REFERENCE")] //Fix2202 Deleted reference target can cause nullrefex in the nodelist
        public async STT.Task Node_Reference_GetReference_Multiple_Deleted()
        {
            await Test(async () =>
            {
                //var root = CreateTestRoot();
                var u1 = CreateUser("U1");
                var u2 = CreateUser("U2");
                var u3 = CreateUser("U3");
                var group = new Group(Node.LoadNode("/Root/IMS/Public")) { Name = "G1" };
                group.AddReferences("Members", new[] { u1, u2, u3 });
                await group.SaveAsync(_cancel);

                group = await Node.LoadAsync<Group>(group.Id, _cancel);

                var memberIds = group.GetReferences("Members").Select(x => x.Id).ToArray();
                Assert.AreEqual($"{u1.Id}, {u2.Id}, {u3.Id}", string.Join(", ", memberIds));

                // ACT
                await Node.ForceDeleteAsync(u2.Id, _cancel);

                // ASSERT
                var loaded = await Node.LoadAsync<Group>(group.Id, _cancel);
                memberIds = loaded.GetReferences("Members").Select(x => x.Id).ToArray();
                Assert.AreEqual($"{u1.Id}, {u3.Id}", string.Join(", ", memberIds));

            }).ConfigureAwait(false);
        }
        [TestMethod, TestCategory("NODE, REFERENCE")] //Fix2202 Deleted reference target can cause nullrefex in the nodelist
        public async STT.Task Node_Reference_GetReference_Multiple_Deleted_WithoutReload()
        {
            await Test(async () =>
            {
                //var root = CreateTestRoot();
                var u1 = CreateUser("U1");
                var u2 = CreateUser("U2");
                var u3 = CreateUser("U3");
                var group = new Group(Node.LoadNode("/Root/IMS/Public")) { Name = "G1" };
                group.AddReferences("Members", new[] { u1, u2, u3 });
                await group.SaveAsync(_cancel);

                var memberIds = group.GetReferences("Members").Select(x => x.Id).ToArray();
                Assert.AreEqual($"{u1.Id}, {u2.Id}, {u3.Id}", string.Join(", ", memberIds));

                // ACT
                await Node.ForceDeleteAsync(u2.Id, _cancel);

                // ASSERT
                memberIds = group.GetReferences("Members").Select(x => x.Id).ToArray();
                Assert.AreEqual($"{u1.Id}, {u3.Id}", string.Join(", ", memberIds));

            }).ConfigureAwait(false);
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
            node.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
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
            node.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            return node;
        }
    }
}
