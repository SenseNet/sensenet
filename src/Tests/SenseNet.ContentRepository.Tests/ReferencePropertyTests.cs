using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ReferencePropertyTests : TestBase
    {
        private readonly ContentFactory _factory = new ContentFactory();

        [TestMethod]
        public void ReferenceProperty_FIX_RemoveBrokenItemsWhenOverwrite()
        {
            Test(true, () =>
            {
                // Prerequisite
                Assert.AreEqual(User.Administrator.Id, User.Current.Id);

                // ARRANGE
                var group = new Group(Node.LoadNode("/Root/IMS/Public")) { Name = "G1" };
                var user1 = _factory.CreateUserAndSave("U1");
                var user2 = _factory.CreateUserAndSave("U2");
                group.AddReferences("Members", new[] {user1});
                group.Save();
                user1.ForceDelete();

                // ACTION
                group = Node.Load<Group>(group.Id);
                group.Members = new[] { user2 };
                group.Save();

                // ASSERT
                group = Node.Load<Group>(group.Id);
                var actual = string.Join("", group.Members.Select(x => x.Id.ToString()));
                Assert.AreEqual($"{user2.Id}", actual);
            });
        }
        [TestMethod]
        public void ReferenceProperty_FIX_ItemsWhenAddMember()
        {
            Test(true, () =>
            {
                // Prerequisite
                Assert.AreEqual(User.Administrator.Id, User.Current.Id);

                // ARRANGE
                var group = new Group(Node.LoadNode("/Root/IMS/Public")) { Name = "G1" };
                var user1 = _factory.CreateUserAndSave("U1");
                var user2 = _factory.CreateUserAndSave("U2");
                group.AddReferences("Members", new[] { user1 });
                group.Save();
                user1.ForceDelete();

                // ACTION
                group = Node.Load<Group>(group.Id);
                group.AddMember(user2);

                // ASSERT
                group = Node.Load<Group>(group.Id);
                var actual = string.Join("", group.Members.Select(x => x.Id.ToString()));
                Assert.AreEqual($"{user2.Id}", actual);
            });
        }
        [TestMethod]
        public void ReferenceProperty_FIX_RemoveReferences()
        {
            Test(true, () =>
            {
                // Prerequisite
                Assert.AreEqual(User.Administrator.Id, User.Current.Id);

                // ARRANGE
                var group = new Group(Node.LoadNode("/Root/IMS/Public")) { Name = "G1" };
                var user1 = _factory.CreateUserAndSave("U1");
                var user2 = _factory.CreateUserAndSave("U2");
                var user3 = _factory.CreateUserAndSave("U3");
                group.AddReferences("Members", new[] { user1, user2, user3 });
                group.Save();
                user2.ForceDelete();

                // ACTION
                group = Node.Load<Group>(group.Id);
                group.RemoveReference("Members",user3);
                group.Save();

                // ASSERT
                group = Node.Load<Group>(group.Id);
                var actual = string.Join("", group.Members.Select(x => x.Id.ToString()));
                Assert.AreEqual($"{user1.Id}", actual);
            });
        }
        [TestMethod]
        public void ReferenceProperty_FIX_RemoveMember()
        {
            Test(true, () =>
            {
                // Prerequisite
                Assert.AreEqual(User.Administrator.Id, User.Current.Id);

                // ARRANGE
                var group = new Group(Node.LoadNode("/Root/IMS/Public")) { Name = "G1" };
                var user1 = _factory.CreateUserAndSave("U1");
                var user2 = _factory.CreateUserAndSave("U2");
                var user3 = _factory.CreateUserAndSave("U3");
                group.AddReferences("Members", new[] { user1, user2, user3 });
                group.Save();
                user2.ForceDelete();

                // ACTION
                group = Node.Load<Group>(group.Id);
                group.RemoveMember(user3);

                // ASSERT
                group = Node.Load<Group>(group.Id);
                var actual = string.Join("", group.Members.Select(x => x.Id.ToString()));
                Assert.AreEqual($"{user1.Id}", actual);
            });
        }

    }
}
