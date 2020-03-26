using System;
using IO = System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Tests.Implementations;
using BlobStorage = SenseNet.ContentRepository.Storage.Data.BlobStorage;

namespace SenseNet.Tests.SelfTest
{
    [TestClass]
    public class InMemoryDataProviderTests : TestBase
    {
        [TestMethod]
        public void InMemDb_LoadRootById()
        {
            Test(() =>
            {
                var node = Node.LoadNode(Identifiers.PortalRootId);
                Assert.AreEqual(Identifiers.PortalRootId, node.Id);
                Assert.AreEqual(Identifiers.RootPath, node.Path);
            }
        );

        }
        [TestMethod]
        public void InMemDb_LoadRootByPath()
        {
            Test(() =>
            {
                var node = Node.LoadNode(Identifiers.RootPath);
                Assert.AreEqual(Identifiers.PortalRootId, node.Id);
                Assert.AreEqual(Identifiers.RootPath, node.Path);
            });
        }
        [TestMethod]
        public void InMemDb_Create()
        {
            Node node;
            Test(() =>
            {
                var lastNodeId =
                    DataStore.GetDataProviderExtension<ITestingDataProviderExtension>().GetLastNodeIdAsync()
                    .GetAwaiter().GetResult();

                var root = Node.LoadNode(Identifiers.RootPath);
                node = new SystemFolder(root)
                {
                    Name = "Node1",
                    DisplayName = "Node 1"
                };

                node.Save();

                node = Node.Load<SystemFolder>(node.Id);

                Assert.AreEqual(lastNodeId + 1, node.Id);
                Assert.AreEqual("/Root/Node1", node.Path);
            });
        }

        [TestMethod]
        public void InMemDb_FlatPropertyLoaded()
        {
            Test(() =>
            {
                var user = User.Somebody;
                Assert.AreEqual("BuiltIn", user.Domain);
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

                Retrier.Retry(3, 200, typeof(NodeIsOutOfDateException), () =>
                {
                    var admin = Node.Load<User>(Identifiers.AdministratorUserId);

                    Assert.AreEqual(DataType.Text, PropertyType.GetByName(propertyName).DataType);
                    Assert.AreNotEqual(testValue, admin.GetProperty<string>(propertyName));

                    // ACTION
                    admin.SetProperty(propertyName, testValue);
                    admin.Save();

                    // ASSERT
                    admin = Node.Load<User>(Identifiers.AdministratorUserId);
                    Assert.AreEqual(testValue, admin.GetProperty<string>(propertyName));
                });
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
                ((BinaryData)content[binaryFieldName]).SetStream(new IO.MemoryStream(buffer));
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
            });
        }

        [TestMethod]
        public void InMemDb_ChunkUpload_NewFile()
        {
            Test(async () =>
            {
                var root = CreateTestRoot();
                var file = new File(root) {Name = "File1.txt"};
                file.Binary.ContentType = "application/octet-stream";
                //file.Binary.FileName = "File1.txt";
                file.Save();

                var chunks = new[]
                {
                    new byte[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                    new byte[] {2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2},
                    new byte[] {3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3},
                    new byte[] {4, 4 }
                };
                var chunkSize = chunks[0].Length;

                // START CHUNK
                var versionId = file.VersionId;
                var propertyTypeId = PropertyType.GetByName("Binary").Id;
                var fullSize = 50L;
                var token = await BlobStorage.StartChunkAsync(versionId, propertyTypeId, fullSize, CancellationToken.None)
                    .ConfigureAwait(false);

                // WRITE CHUNKS
                for (int i = 0; i < chunks.Length; i++)
                {
                    var offset = i * chunkSize;
                    var chunk = chunks[i];
                    await BlobStorage.WriteChunkAsync(versionId, token, chunk, offset, fullSize,
                        CancellationToken.None).ConfigureAwait(false);
                }

                // COMMIT CHUNK
                await BlobStorage.CommitChunkAsync(versionId, propertyTypeId, token, fullSize, null, CancellationToken.None)
                    .ConfigureAwait(false);

                // ASSERT
                Cache.Reset();
                file = Node.Load<File>(file.Id);
                var length = Convert.ToInt32(file.Binary.Size);
                var buffer = new byte[length];
                using (var stream = file.Binary.GetStream())
                    stream.Read(buffer, 0, length);
                Assert.AreEqual(
                    "11111111111111112222222222222222333333333333333344",
                    new string(buffer.Select(b=>(char)(b+'0')).ToArray()));
            }).GetAwaiter().GetResult();
        }

        private SystemFolder CreateTestRoot()
        {
            var node = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
            node.Save();
            return node;
        }
    }
}
