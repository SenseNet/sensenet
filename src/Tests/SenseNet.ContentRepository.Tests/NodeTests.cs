using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.Tests;
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
    }
}
