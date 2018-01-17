using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Tests;
using SenseNet.ContentRepository.Tests.Implementations;

namespace SenseNet.Search.Lucene29.Tests.DataProviderTests
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

        [TestMethod]
        public void InMemDb_SaveAndLoadNewNodeWithAllDynamicDataTypes()
        {
            Test(() =>
            {
                // ARRANGE
                var stringFieldName = "StringField1";
                var intFieldName = "IntField1";
                var dateTimeFieldName = "DateTimeField1";
                var decimalFieldName = "DecimalField1";
                var textFieldName = "TextField1";
                var referenceFieldName = "ReferenceField1";
                var binaryFieldName = "BinaryField1";

                var stringValue = Guid.NewGuid().ToString();
                var intValue = 34562;
                var dateTimeValue = DateTime.UtcNow;
                var decimalValue = Convert.ToDecimal(9287456.5432);
                var textValue = "Lorem ipsum...";
                var referenceValue = User.Visitor;
                var buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

                /**/ContentTypeInstaller.InstallContentType($@"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='DataTestNode' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
	<Fields>
		<Field name='{stringFieldName}' type='ShortText'></Field>
		<Field name='{intFieldName}' type='Integer'></Field>
		<Field name='{dateTimeFieldName}' type='DateTime'></Field>
		<Field name='{decimalFieldName}' type='Currency'></Field>
		<Field name='{textFieldName}' type='LongText'></Field>
		<Field name='{referenceFieldName}' type='Reference'></Field>
		<Field name='{binaryFieldName}' type='Binary'></Field>
	</Fields>
</ContentType>
");

                var root = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
                root.Save();

                // ACTION
                var content = Content.CreateNew("DataTestNode", root, Guid.NewGuid().ToString());
                content[stringFieldName] = stringValue;
                content[intFieldName] = intValue;
                content[dateTimeFieldName] = dateTimeValue;
                content[decimalFieldName] = decimalValue;
                content[textFieldName] = textValue;
                content.ContentHandler.SetReference(referenceFieldName, referenceValue);
                ((BinaryData)content[binaryFieldName]).SetStream(new MemoryStream(buffer));
                content.Save();

                // ASSERT
                content = Content.Load(content.Id);
                Assert.AreEqual(stringValue, content[stringFieldName]);
                Assert.AreEqual(intValue, content[intFieldName]);
                Assert.AreEqual(dateTimeValue, content[dateTimeFieldName]);
                Assert.AreEqual(decimalValue, content[decimalFieldName]);
                Assert.AreEqual(textValue, content[textFieldName]);

                var referred = content.ContentHandler.GetReference<Node>(referenceFieldName);
                Assert.AreEqual(referenceValue.Id, referred.Id);

                var stream = ((BinaryData) content[binaryFieldName]).GetStream();
                var b = new byte[stream.Length];
                stream.Read(b, 0, b.Length);
                var expected = string.Join(",", buffer.Select(x => x.ToString()));
                var actual = string.Join(",", b.Select(x => x.ToString()));
                Assert.AreEqual(expected, actual);

                return 0;
            });
        }
    }
}
