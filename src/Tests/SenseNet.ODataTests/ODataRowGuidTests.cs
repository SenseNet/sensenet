using System;
using System.Linq;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Versioning;
using SenseNet.OData.Metadata;
using SenseNet.ODataTests.Responses;
using SenseNet.Search.Indexing;
using SenseNet.Security;
using File = SenseNet.ContentRepository.File;
using Settings = SenseNet.ContentRepository.Settings;

namespace SenseNet.ODataTests;

[TestClass]
public class ODataRowGuidTests : ODataTestBase
{
    private static readonly string[] GuidFields = { "NodeGuid", "VersionGuid" };

    private sealed class GuidTextResult
    {
        public string NodeGuidText { get; set; }
        public string VersionGuidText { get; set; }
    }

    [TestMethod]
    public async Task OD_RowGuid_DoesNotChangeWritableRuntimeFields()
    {
        await ODataTestAsync(() =>
        {
            const string ctd = """
                <ContentType name="GuidTextResult" xmlns="http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition">
                  <Fields>
                    <Field name="NodeGuidText" type="ShortText" />
                    <Field name="VersionGuidText" type="ShortText" />
                  </Fields>
                </ContentType>
                """;
            var content = Content.Create(new GuidTextResult
                { NodeGuidText = "original-node", VersionGuidText = "original-version" }, ctd);
            content["NodeGuidText"] = "edited-node";
            content["VersionGuidText"] = "edited-version";
            Assert.AreEqual("edited-node", content["NodeGuidText"]);
            Assert.AreEqual("edited-version", content["VersionGuidText"]);
            return Task.CompletedTask;
        });
    }

    [TestMethod]
    public async Task OD_RowGuid_FieldsAreReadOnlyShortTextAndNotIndexed()
    {
        await ODataTestAsync(async () =>
        {
            var content = Content.CreateNew("Folder", CreateTestRoot(), "GuidFields");
            foreach (var name in GuidFields)
            {
                var field = content.Fields[name];
                Assert.IsInstanceOfType<ShortTextField>(field);
                Assert.IsTrue(field.ReadOnly);
                Assert.AreEqual(IndexingMode.No, field.FieldSetting.IndexingInfo.IndexingMode);
                Assert.AreEqual(IndexStoringMode.No, field.FieldSetting.IndexingInfo.IndexStoringMode);
                Assert.AreEqual(Guid.Empty.ToString("D"), content[name]);
                content[name] = Guid.NewGuid().ToString("D");
                Assert.AreEqual(Guid.Empty.ToString("D"), content[name]);
            }

            await content.SaveAsync(CancellationToken.None);
            AssertContentGuids(content);
            var handler = (GenericContent)content.ContentHandler;
            Assert.ThrowsException<InvalidOperationException>(() => handler.SetProperty("NodeGuidText", "changed"));
            Assert.ThrowsException<InvalidOperationException>(() => handler.SetProperty("VersionGuidText", "changed"));
            Assert.IsFalse(handler.PropertyTypes.Any(p => p.Name is "NodeGuidText" or "VersionGuidText"));

            var document = handler.GetIndexDocument();
            Assert.IsTrue(document.Fields.ContainsKey("Id"));
            foreach (var name in GuidFields)
                Assert.IsFalse(document.Fields.ContainsKey(name), name);
        });
    }

    [DataTestMethod]
    [DataRow("?metadata=no")]
    [DataRow("?metadata=no&$select=Id,NodeGuid,VersionGuid")]
    [DataRow("?metadata=no&$expand=CreatedBy")]
    [DataRow("?metadata=no&$expand=CreatedBy&$select=Id,NodeGuid,VersionGuid,CreatedBy/NodeGuid,CreatedBy/VersionGuid")]
    public async Task OD_RowGuid_GetAndExpand(string query)
    {
        await ODataTestAsync(async () =>
        {
            var node = CreateTestRoot();
            var response = await ODataGetAsync(EntityUrl(node), query);
            AssertNoError(response);
            var entity = GetEntity(response);
            AssertEntityGuids(entity, node);
            if (query.Contains("$expand"))
                AssertEntityGuids(entity.CreatedBy, node.CreatedBy);
        });
    }

    [DataTestMethod]
    [DataRow("?metadata=no")]
    [DataRow("?metadata=no&$select=Id,NodeGuid,VersionGuid")]
    public async Task OD_RowGuid_Children(string query)
    {
        await ODataTestAsync(async () =>
        {
            var parent = CreateTestRoot();
            var child = new Folder(parent) { Name = "Child" };
            await child.SaveAsync(CancellationToken.None);
            var response = await ODataGetAsync(EntityUrl(parent) + "/Children", query);
            AssertNoError(response);
            AssertEntityGuids(GetEntities(response).Single(), child);
        });
    }

    [TestMethod]
    public async Task OD_RowGuid_PropertyAndRawValue()
    {
        await ODataTestAsync(async () =>
        {
            var node = CreateTestRoot();
            foreach (var name in GuidFields)
            {
                var expected = ((Guid)node[name]).ToString("D");
                var response = await ODataGetAsync(EntityUrl(node) + "/" + name, "?metadata=no");
                AssertNoError(response);
                Assert.AreEqual(expected, GetEntity(response).AllProperties[name].ToString());
                var raw = await ODataGetAsync(EntityUrl(node) + "/" + name + "/$value", "");
                Assert.AreEqual(200, raw.StatusCode, raw.Result);
                Assert.AreEqual(expected, raw.Result);
            }
        });
    }

    [TestMethod]
    public async Task OD_RowGuid_MetadataIsString()
    {
        await ODataTestAsync(async () =>
        {
            var response = await ODataGetAsync("/OData.svc/$metadata", "");
            Assert.AreEqual(200, response.StatusCode, response.Result);
            var xml = XDocument.Parse(response.Result);
            var genericType = xml.Descendants().Single(e =>
                e.Name.LocalName == "EntityType" && (string)e.Attribute("Name") == "GenericContent");
            var schemaClass = new ContentRepository.Schema.Metadata.Class(ContentType.GetByName("GenericContent"));
            var clientSchema = (JObject)ClientMetadataProvider.Instance.GetClientMetaClass(schemaClass);
            foreach (var name in GuidFields)
            {
                var property = genericType.Elements().Single(e => (string)e.Attribute("Name") == name);
                Assert.AreEqual("Edm.String", (string)property.Attribute("Type"));
                var setting = clientSchema["FieldSettings"].Single(f => f["Name"].Value<string>() == name);
                Assert.AreEqual("ShortTextFieldSetting", setting["Type"].Value<string>());
                Assert.IsTrue(setting["ReadOnly"].Value<bool>());
            }
        });
    }

    [TestMethod]
    public async Task OD_RowGuid_VersionChangesRefreshExistingFields()
    {
        await ODataTestAsync(async () =>
        {
            var node = new File(CreateTestRoot()) { Name = "Versions.txt", VersioningMode = VersioningType.MajorAndMinor };
            node.Binary.SetStream(RepositoryTools.GetStreamFromString("original"));
            var content = Content.Create(node);
            Assert.AreEqual(Guid.Empty.ToString("D"), content["VersionGuid"]);
            await content.SaveAsync(CancellationToken.None);
            await node.PublishAsync(CancellationToken.None);
            AssertContentGuids(content);
            var originalGuid = node.VersionGuid;
            var originalVersion = node.Version;
            var nodeGuid = node.NodeGuid;

            await node.CheckOutAsync(CancellationToken.None);
            node.Binary.SetStream(RepositoryTools.GetStreamFromString("changed"));
            await content.SaveAsync(SavingMode.KeepVersionAndLock, CancellationToken.None);
            await node.CheckInAsync(CancellationToken.None);
            Assert.AreEqual(nodeGuid, node.NodeGuid);
            Assert.AreNotEqual(originalGuid, node.VersionGuid);
            AssertContentGuids(content);
            var versionGuid = node.VersionGuid;
            content.DisplayName = "Updated";
            await content.SaveSameVersionAsync(CancellationToken.None);
            Assert.AreEqual(versionGuid, node.VersionGuid);
            AssertContentGuids(content);

            var original = await Node.LoadNodeAsync(nodeGuid, originalVersion, CancellationToken.None);
            Assert.AreEqual(originalGuid, original.VersionGuid);
            AssertContentGuids(Content.Create(original));
            var response = await ODataGetAsync(EntityUrl(node), "?metadata=no&$select=NodeGuid,VersionGuid");
            AssertNoError(response);
            AssertEntityGuids(GetEntity(response), node);
        });
    }

    [DataTestMethod]
    [DataRow("PATCH")]
    [DataRow("PUT")]
    public async Task OD_RowGuid_UpdateIgnoresInput(string method)
    {
        await ODataTestAsync(async () =>
        {
            var node = CreateTestRoot();
            foreach (var input in new[] { Guid.NewGuid(), Guid.Empty })
            {
                var body = "models=" + JsonConvert.SerializeObject(new[] { new
                {
                    Name = node.Name, DisplayName = "Updated", NodeGuid = input, VersionGuid = input
                }});
                var response = await ODataCallAsync(EntityUrl(node), "", body, method);
                AssertNoError(response);
                var reloaded = Node.LoadNode(node.Id);
                Assert.AreEqual(node.NodeGuid, reloaded.NodeGuid);
                Assert.AreEqual(node.VersionGuid, reloaded.VersionGuid);
                AssertEntityGuids(GetEntity(response), reloaded);
            }
        });
    }

    [TestMethod]
    public async Task OD_RowGuid_CreateIgnoresInput()
    {
        await ODataTestAsync(async () =>
        {
            var parent = CreateTestRoot();
            foreach (var input in new[] { Guid.NewGuid(), Guid.Empty })
            {
                var body = "models=" + JsonConvert.SerializeObject(new[] { new
                {
                    __ContentType = "Folder", Name = "Child" + input.ToString("N"), NodeGuid = input, VersionGuid = input
                }});
                var response = await ODataPostAsync("/OData.svc" + parent.Path, "", body);
                AssertNoError(response);
                var entity = GetEntity(response);
                var created = Node.LoadNode(entity.Id);
                Assert.AreNotEqual(input, created.NodeGuid);
                Assert.AreNotEqual(input, created.VersionGuid);
                AssertEntityGuids(entity, created);
            }
        });
    }

    [TestMethod]
    public async Task OD_RowGuid_ContentLinkUsesOwnIdentifiers()
    {
        await ODataTestAsync(async () =>
        {
            var parent = CreateTestRoot();
            var linked = new Folder(parent) { Name = "Target" };
            await linked.SaveAsync(CancellationToken.None);
            var link = new ContentLink(parent) { Name = "Link", Link = linked };
            await link.SaveAsync(CancellationToken.None);
            Assert.AreNotEqual(linked.NodeGuid, link.NodeGuid);
            Assert.AreNotEqual(linked.VersionGuid, link.VersionGuid);
            AssertContentGuids(Content.Create(link));
            var response = await ODataGetAsync(EntityUrl(link), "?metadata=no&$select=NodeGuid,VersionGuid");
            AssertNoError(response);
            AssertEntityGuids(GetEntity(response), link);
        });
    }

    [TestMethod]
    public async Task OD_RowGuid_SettingsUseComputedBindings()
    {
        await ODataTestAsync(async () =>
        {
            var folder = new SystemFolder(CreateTestRoot()) { Name = "Settings" };
            await folder.SaveAsync(CancellationToken.None);
            var handler = new Settings(folder) { Name = "Guid.settings" };
            handler.Binary.SetStream(RepositoryTools.GetStreamFromString("{}"));
            await handler.SaveAsync(CancellationToken.None);
            var settings = Content.Create(handler);
            AssertContentGuids(settings);
            var response = await ODataGetAsync(EntityUrl(settings.ContentHandler), "?metadata=no&$select=NodeGuid,VersionGuid");
            AssertNoError(response);
            AssertEntityGuids(GetEntity(response), settings.ContentHandler);
        });
    }

    [TestMethod]
    public async Task OD_RowGuid_SeeOnlyDoesNotExposeIdentifiers()
    {
        await ODataTestAsync(async () =>
        {
            var node = CreateTestRoot();
            var user = new User(Node.LoadNode("/Root/IMS/Public"))
                { Name = "GuidReader", Enabled = true, Email = "guidreader@example.com" };
            await user.SaveAsync(CancellationToken.None);
            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .BreakInheritance(node.Id, Array.Empty<EntryType>())
                .Allow(node.Id, user.Id, false, PermissionType.See).ApplyAsync(CancellationToken.None);
            using (new CurrentUserBlock(user))
            {
                Assert.IsTrue(Node.LoadNode(node.Id).IsHeadOnly);
                foreach (var query in new[] { "?metadata=no&$select=NodeGuid,VersionGuid", "?metadata=no&$expand=CreatedBy&$select=*" })
                {
                    var response = await ODataGetAsync(EntityUrl(node), query);
                    AssertNoError(response);
                    var entity = GetEntity(response);
                    foreach (var name in GuidFields)
                        Assert.AreEqual(JTokenType.Null, ((JToken)entity.AllProperties[name]).Type);
                }
                foreach (var name in GuidFields)
                {
                    var response = await ODataGetAsync(EntityUrl(node) + "/" + name + "/$value", "");
                    Assert.AreNotEqual(200, response.StatusCode);
                    Assert.IsFalse(response.Result.Contains(((Guid)node[name]).ToString("D")));
                }
            }
        });
    }

    [TestMethod]
    public async Task OD_RowGuid_ImportAndCopyKeepRowIdentity()
    {
        await ODataTestAsync(async () =>
        {
            var parent = CreateTestRoot();
            var path = parent.Path + "/Imported";
            var input = Guid.NewGuid();
            var model = new { path, data = new
            {
                ContentName = "Imported", ContentType = "Folder",
                Fields = new { NodeGuid = input, VersionGuid = input, DisplayName = "Imported" }
            }};
            var request = "models=" + JsonConvert.SerializeObject(new[] { model });
            var response = await ODataPostAsync("/OData.svc/('Root')/Import", "", request);
            AssertNoError(response);
            var original = Node.LoadNode(path);
            Assert.AreNotEqual(input, original.NodeGuid);
            Assert.AreNotEqual(input, original.VersionGuid);
            response = await ODataPostAsync("/OData.svc/('Root')/Import", "", request);
            AssertNoError(response);
            var reloaded = Node.LoadNode(path);
            Assert.AreEqual(original.NodeGuid, reloaded.NodeGuid);
            Assert.AreEqual(original.VersionGuid, reloaded.VersionGuid);
            var destination = new Folder(parent) { Name = "Copies" };
            await destination.SaveAsync(CancellationToken.None);
            Node.Copy(path, destination.Path);
            var copy = Content.Load(destination.Path + "/Imported");
            Assert.AreNotEqual(original.NodeGuid, copy.ContentHandler.NodeGuid);
            Assert.AreNotEqual(original.VersionGuid, copy.ContentHandler.VersionGuid);
            AssertContentGuids(copy);
        });
    }

    [TestMethod]
    public async Task OD_RowGuid_UpgradePreservesOtherFieldsAndIsIdempotent()
    {
        await ODataTestAsync(() =>
        {
            var type = ContentType.GetByName("GenericContent");
            using var stream = type.Binary.GetStream();
            var xml = XDocument.Load(stream);
            var ns = xml.Root.Name.Namespace;
            var fields = xml.Root.Element(ns + "Fields");
            fields.Elements().Where(f => GuidFields.Contains((string)f.Attribute("name"))).Remove();
            fields.Add(new XElement(ns + "Field", new XAttribute("name", "CustomField"), new XAttribute("type", "ShortText")));
            ContentTypeInstaller.InstallContentType(xml.ToString());
            ServicesComponent.AddRowGuidFields();
            var updated = ContentType.GetByName("GenericContent");
            Assert.IsNotNull(updated.FieldSettings.SingleOrDefault(f => f.Name == "CustomField"));
            foreach (var name in GuidFields)
            {
                var setting = updated.FieldSettings.Single(f => f.Name == name);
                Assert.AreEqual("ShortText", setting.ShortName);
                Assert.IsTrue(setting.ReadOnly);
                Assert.AreEqual(IndexingMode.No, setting.IndexingInfo.IndexingMode);
                Assert.AreEqual(IndexStoringMode.No, setting.IndexingInfo.IndexStoringMode);
            }
            var timestamp = updated.NodeTimestamp;
            ServicesComponent.AddRowGuidFields();
            Assert.AreEqual(timestamp, ContentType.GetByName("GenericContent").NodeTimestamp);
            return Task.CompletedTask;
        });
    }

    private static string EntityUrl(Node node) => $"/OData.svc{node.ParentPath}('{node.Name}')";

    private static void AssertContentGuids(Content content)
    {
        Assert.AreEqual(content.ContentHandler.NodeGuid.ToString("D"), content["NodeGuid"]);
        Assert.AreEqual(content.ContentHandler.VersionGuid.ToString("D"), content["VersionGuid"]);
    }

    private static void AssertEntityGuids(ODataEntityResponse entity, Node node)
    {
        foreach (var name in GuidFields)
        {
            var value = (JToken)entity.AllProperties[name];
            Assert.AreEqual(JTokenType.String, value.Type);
            Assert.AreEqual(((Guid)node[name]).ToString("D"), value.Value<string>());
        }
    }
}
