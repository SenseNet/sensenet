using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.Testing;
using Task = System.Threading.Tasks.Task ;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class RichTextTests:ODataTestBase
    {
        [TestMethod]
        public void Field_RichText_Export()
        {
            Test(() =>
            {
                // ALIGN
                InstallContentType();
                var testRoot = CreateTestRoot("Folder1");

                var textValue = "Chillwave flexitarian pork belly raw denim.";
                var editorValue = "{property1: 'Asymmetrical trust fund', property2: 'Crucifix intelligentsia godard'}";

                var content = Content.CreateNew("ContentType1", testRoot, "Content1");
                content["RichText1"] = new RichTextFieldValue {Text = textValue, Editor = editorValue};
                content.Save();

                var sb = new StringBuilder();
                var writer = new XmlTextWriter(new StringWriter(sb));

                using (new Swindler<RepositoryEnvironment.WorkingModeFlags>(
                    new RepositoryEnvironment.WorkingModeFlags {Exporting = true},
                    () => RepositoryEnvironment.WorkingMode,
                    value => RepositoryEnvironment.WorkingMode = value))
                {
                    // ACTION
                    content = Content.Load(content.Id);
                    content.ExportFieldData(writer, null);

                    // ASSERT
                    var xml = new XmlDocument();
                    xml.LoadXml($"<r>{sb}</r>");
                    var xmlNode = xml.SelectSingleNode("//RichText1");
                    Assert.IsNotNull(xmlNode);
                    Assert.AreEqual(RemoveWhitespaces($"<RichText1>" +
                                                      $"<Text><![CDATA[{textValue}]]></Text>" +
                                                      $"<Editor><![CDATA[{editorValue}]]></Editor>" +
                                                      "</RichText1>"), RemoveWhitespaces(xmlNode.OuterXml));
                }
            });
        }
        [TestMethod]
        public void Field_RichText_Import()
        {
            Test(() =>
            {
                // ALIGN
                InstallContentType();
                var testRoot = CreateTestRoot("Folder1");

                var textValue = "Chillwave flexitarian pork belly raw denim.";
                var editorValue = "{property1: 'Asymmetrical trust fund', property2: 'Crucifix intelligentsia godard'}";

                var content = Content.CreateNew("ContentType1", testRoot, "Content1");
                content.Save();

                var xml = new XmlDocument();
                xml.LoadXml($@"<ContentMetaData>
  <ContentType>ContentType1</ContentType>
  <ContentName>Content1</ContentName>
  <Fields>
    <RichText1>
      <Text><![CDATA[{textValue}]]></Text>
      <Editor><![CDATA[{editorValue}]]></Editor>
    </RichText1>
  </Fields>
</ContentMetaData>");

                var importContext = new ImportContext(
                    xml.SelectNodes("/ContentMetaData/Fields/*"), "", false, true, false);

                using (new Swindler<RepositoryEnvironment.WorkingModeFlags>(
                    new RepositoryEnvironment.WorkingModeFlags {Importing = true},
                    () => RepositoryEnvironment.WorkingMode,
                    value => RepositoryEnvironment.WorkingMode = value))
                {
                    // ACTION
                    content = Content.Load(content.Id);
                    content.ImportFieldData(importContext);

                    // ASSERT
                    var rtf = (RichTextFieldValue)content["RichText1"];
                    Assert.IsNotNull(rtf);
                    Assert.AreEqual(textValue, rtf.Text);
                    Assert.AreEqual(editorValue, rtf.Editor);
                }
            });
        }

        [TestMethod]
        public async Task OD_RichText_Get()
        {
            await ODataTestAsync(async () =>
            {
                // ALIGN
                InstallContentType();
                var testRoot = CreateTestRoot("Folder1");

                var textValue = "Chillwave flexitarian pork belly raw denim.";
                var editorValue = "{property1: 'Asymmetrical trust fund', property2: 'Crucifix intelligentsia godard'}";
                var richTextValue = $"{{\"text\": \"{textValue}\", \"editor\": \"{editorValue}\"}}";

                var content = Content.CreateNew("ContentType1", testRoot, "Content1");
                content["RichText1"] = new RichTextFieldValue { Text = textValue, Editor = editorValue };
                content.Save();

                // ACTION
                var response = await ODataGetAsync(
                        "/OData.svc/Root/Folder1('Content1')",
                        "?metadata=no&$select=Id,RichText1")
                    .ConfigureAwait(false);

                // ASSERT
                var entity = GetEntity(response);
                Assert.AreEqual(content.Id, entity.Id);
                Assert.AreEqual(textValue, entity.AllProperties["RichText1"].ToString());
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_RichText_Get_EditorMode()
        {
            await ODataTestAsync(async () =>
            {
                // ALIGN
                InstallContentType();
                var testRoot = CreateTestRoot("Folder1");

                var textValue = "Chillwave flexitarian pork belly raw denim.";
                var editorValue = "{property1: 'Asymmetrical trust fund', property2: 'Crucifix intelligentsia godard'}";
                var richTextValue = $"{{\"text\": \"{textValue}\", \"editor\": \"{editorValue}\"}}";

                var content = Content.CreateNew("ContentType1", testRoot, "Content1");
                content["RichText1"] = new RichTextFieldValue { Text = textValue, Editor = editorValue };
                content["RichText2"] = new RichTextFieldValue { Text = textValue, Editor = editorValue };
                content.Save();

                // ACTION
                var response = await ODataGetAsync(
                        "/OData.svc/Root/Folder1('Content1')",
                        "?metadata=no&$select=Id,RichText1,RichText2&richtexteditor=RichText1")
                    .ConfigureAwait(false);

                // ASSERT
                var entity = GetEntity(response);
                Assert.AreEqual(content.Id, entity.Id);
                Assert.AreEqual(RemoveWhitespaces(richTextValue), RemoveWhitespaces(entity.AllProperties["RichText1"].ToString()));
                Assert.AreEqual(RemoveWhitespaces(textValue), RemoveWhitespaces(entity.AllProperties["RichText2"].ToString()));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_RichText_Create()
        {
            await ODataTestAsync(async () =>
            {
                // ALIGN
                InstallContentType();
                var testRoot = CreateTestRoot("Folder1");

                var textValue = "Chillwave flexitarian pork belly raw denim.";
                var editorValue = "{property1: 'Asymmetrical trust fund', property2: 'Crucifix intelligentsia godard'}";
                var richTextValue = $"{{\"text\": \"{textValue}\", \"editor\": \"{editorValue}\"}}";

                // ACTION
                var response = await ODataPostAsync(
                        "/OData.svc/Root/Folder1",
                        "?metadata=no&richtexteditor=RichText1",
                        $@"models=[{{""__ContentType"":""ContentType1"",""Name"":""Content1"",""RichText1"":{richTextValue},""Index"":42}}]")
                    .ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var content = Content.Load("/Root/Folder1/Content1");
                var rtf = (RichTextFieldValue) content["RichText1"];
                Assert.IsNotNull(rtf);
                Assert.AreEqual(textValue, rtf.Text);
                Assert.AreEqual(editorValue, rtf.Editor);
                var entity = GetEntity(response);
                Assert.AreEqual(content.Id, entity.Id);
                Assert.AreEqual(RemoveWhitespaces(richTextValue), RemoveWhitespaces(entity.AllProperties["RichText1"].ToString()));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_RichText_Modify()
        {
            await ODataTestAsync(async () =>
            {
                // ALIGN
                InstallContentType();
                var testRoot = CreateTestRoot("Folder1");

                var textValue = "Chillwave flexitarian pork belly raw denim.";
                var editorValue = "{property1: 'Asymmetrical trust fund', property2: 'Crucifix intelligentsia godard'}";
                var richTextValue = $"{{\"text\": \"{textValue}\", \"editor\": \"{editorValue}\"}}";

                var content = Content.CreateNew("ContentType1", testRoot, "Content1");
                content["RichText1"] = new RichTextFieldValue { Text = textValue, Editor = editorValue };
                content.Save();

                // ACTION
                var textValue2 = "Tilde ennui heirloom narwhal.";
                var editorValue2 = "{property1: 'Tilde ennui', property2: 'heirloom narwhal'}";
                var richTextValue2 = $"{{\"text\": \"{textValue2}\", \"editor\": \"{editorValue2}\"}}";
                var response = await ODataPutAsync(
                        "/OData.svc/Root/Folder1/Content1",
                        "?metadata=no&richtexteditor=RichText1",
                        $@"models=[{{""RichText1"":{richTextValue2}}}]")
                    .ConfigureAwait(false);

                // ASSERT
                var loadedContent = Content.Load("/Root/Folder1/Content1");
                var rtf = (RichTextFieldValue)loadedContent["RichText1"];
                Assert.AreEqual(textValue2, rtf.Text);
                Assert.AreEqual(editorValue2, rtf.Editor);
                var entity = GetEntity(response);
                Assert.AreEqual(content.Id, entity.Id);
                Assert.AreEqual(RemoveWhitespaces(richTextValue2), RemoveWhitespaces(entity.AllProperties["RichText1"].ToString()));
            }).ConfigureAwait(false);
        }

        private void InstallContentType()
        {
            ContentTypeInstaller.InstallContentType($@"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='ContentType1' parentType='GenericContent' handler='{typeof(GenericContent).FullName}'
             xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='RichText1' type='RichText'>
            <Indexing>
                <Analyzer>Standard</Analyzer>
            </Indexing>
        </Field>
        <Field name='RichText2' type='RichText'>
            <Indexing>
                <Analyzer>Standard</Analyzer>
            </Indexing>
        </Field>
    </Fields>
</ContentType>");

        }
    }
}
