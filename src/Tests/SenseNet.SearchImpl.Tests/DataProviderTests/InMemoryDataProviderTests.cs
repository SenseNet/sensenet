using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Tests;
using SenseNet.ContentRepository.Tests.Implementations;

namespace SenseNet.SearchImpl.Tests.DataProviderTests
{
    [TestClass]
    public class InMemoryDataProviderTests : TestBase
    {
        [TestMethod]
        public void InMemDb_LoadRootById()
        {
            var node = Test(() => Node.LoadNode(Identifiers.PortalRootId));

            Assert.AreEqual(Identifiers.PortalRootId, node.Id);
            Assert.AreEqual(Identifiers.RootPath, node.Path);
        }
        [TestMethod]
        public void InMemDb_LoadRootByPath()
        {
            var node = Test(() => Node.LoadNode(Identifiers.RootPath));

            Assert.AreEqual(Identifiers.PortalRootId, node.Id);
            Assert.AreEqual(Identifiers.RootPath, node.Path);
        }
        [TestMethod]
        public void InMemDb_Create()
        {
            Node node;
            var result = Test(() =>
            {
                var lastNodeId = ((InMemoryDataProvider)DataProvider.Current).LastNodeId;

                var root = Node.LoadNode(Identifiers.RootPath);
                node = new SystemFolder(root)
                {
                    Name = "Node1",
                    DisplayName = "Node 1"
                };

                node.Save();

                node = Node.Load<SystemFolder>(node.Id);
                return new Tuple<int, Node>(lastNodeId, node);

            });
            var lastId = result.Item1;
            node = result.Item2;
            Assert.AreEqual(lastId + 1, node.Id);
            Assert.AreEqual("/Root/Node1", node.Path);
        }

        [TestMethod]
        public void InMemDb_FlatPropertyLoaded()
        {
            Test(() =>
            {
                var user = User.Somebody;
                Assert.AreEqual("BuiltIn", user.Domain);
                return 0;
            });
        }
        [TestMethod]
        public void InMemDb_FlatPropertyWrite()
        {
            Test(() =>
            {
                // ARRANGE
                var admin = Node.Load<User>(Identifiers.AdministratorUserId);
                var testValue = "Administrator 22";
                Assert.AreNotEqual(testValue, admin.FullName);

                // ACTION
                admin.FullName = testValue;
                admin.Save();

                // ASSERT
                admin = Node.Load<User>(Identifiers.AdministratorUserId);
                Assert.AreEqual(testValue, admin.FullName);

                return 0;
            });
        }

        [TestMethod]
        public void InMemDb_TextPropertyWrite()
        {
            Test(() =>
            {
                // ARRANGE
                var propertyName = "Education";
                var testValue = "High school 46";
                var admin = Node.Load<User>(Identifiers.AdministratorUserId);
                Assert.AreEqual(DataType.Text, PropertyType.GetByName(propertyName).DataType);
                Assert.AreNotEqual(testValue, admin.GetProperty<string>(propertyName));

                // ACTION
                admin.SetProperty(propertyName, testValue);
                admin.Save();

                // ASSERT
                admin = Node.Load<User>(Identifiers.AdministratorUserId);
                Assert.AreEqual(testValue, admin.GetProperty<string>(propertyName));

                return 0;
            });
        }

        [TestMethod]
        public void InMemDb_ReferencePropertyLoaded()
        {
            Test(() =>
            {
                var group = Group.Administrators;
                Assert.IsTrue(group.Members.Any());
                Assert.IsTrue(group.HasReference(PropertyType.GetByName("Members"), User.Administrator));
                return 0;
            });
        }

        [TestMethod]
        public void InMemDb_ReferencePropertyWrite()
        {
            Test(() =>
            {
                // ARRANGE
                var editors = Node.Load<Group>("/Root/IMS/BuiltIn/Portal/Editors");
                var developers = Node.Load<Group>("/Root/IMS/BuiltIn/Portal/Developers");
                var developersGroupId = developers.Id;

                var editorMembersBefore = editors.Members.Select(n => n.Id).OrderBy(i => i).ToArray();
                Assert.IsFalse(editorMembersBefore.Contains(developersGroupId));

                // ACTION
                editors.AddMember(developers);

                // ASSERT
                editors = Node.Load<Group>("/Root/IMS/BuiltIn/Portal/Editors"); // reload
                var editorMembersAfter = editors.Members.Select(n => n.Id).OrderBy(i => i).ToArray();
                Assert.IsTrue(editorMembersAfter.Contains(developersGroupId));

                return 0;
            });
        }
    }
}
