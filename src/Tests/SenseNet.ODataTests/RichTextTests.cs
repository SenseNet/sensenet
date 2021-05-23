using System;
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
        [DataRow("text", "{p1: 'xx', p2: 'yy'}")]
        [DataRow("text", null)]
        [DataRow(null, "{p1: 'xx', p2: 'yy'}")]
        [DataRow(null, null)]
        [DataTestMethod]
        public void Field_RichText_Export(string textValue, string editorValue)
        {
            Test(() =>
            {
                // ALIGN
                InstallContentType();
                var testRoot = CreateTestRoot("Folder1");

                //var textValue = "Chillwave flexitarian pork belly raw denim.";
                //var editorValue = "{property1: 'Asymmetrical trust fund', property2: 'Crucifix intelligentsia godard'}";

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
                    if (textValue == null && editorValue == null)
                        Assert.AreEqual(RemoveWhitespaces(
                            @$"<RichText1/>"), RemoveWhitespaces(xmlNode.OuterXml));
                    else if (textValue == null && editorValue != null)
                        Assert.AreEqual(RemoveWhitespaces(
                            @$"<RichText1>
                                <Editor><![CDATA[{editorValue}]]></Editor>
                            </RichText1>"), RemoveWhitespaces(xmlNode.OuterXml));
                    else if (textValue != null && editorValue == null)
                        Assert.AreEqual(RemoveWhitespaces(
                            @$"<RichText1><![CDATA[{textValue}]]></RichText1>"), RemoveWhitespaces(xmlNode.OuterXml));
                    else 
                        Assert.AreEqual(RemoveWhitespaces(
                            @$"<RichText1>
                                <Text><![CDATA[{textValue}]]></Text>
                                <Editor><![CDATA[{editorValue}]]></Editor>
                            </RichText1>"), RemoveWhitespaces(xmlNode.OuterXml));

                }
            });
        }
        [DataRow(0, "<RichText1><Text><![CDATA[textValue]]></Text><Editor><![CDATA[editorValue]]></Editor></RichText1>")]
        [DataRow(1, "<RichText1><Editor><![CDATA[editorValue]]></Editor></RichText1>")]
        [DataRow(2, "<RichText1><Text><![CDATA[textValue]]></Text></RichText1>")]
        [DataRow(3, "<RichText1>textValue</RichText1>")]
        [DataRow(4, "<RichText1>  </RichText1>")]
        [DataRow(5, "<RichText1/>")]
        [DataTestMethod]
        public void Field_RichText_Import(int testId, string richTextElement)
        {
            Test(() =>
            {
                // ALIGN
                InstallContentType();
                var testRoot = CreateTestRoot("Folder1");

                var content = Content.CreateNew("ContentType1", testRoot, "Content1");
                content.Save();

                var xml = new XmlDocument();
                xml.LoadXml($@"<ContentMetaData>
  <ContentType>ContentType1</ContentType>
  <ContentName>Content1</ContentName>
  <Fields>
    {richTextElement}
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
                    switch (testId)
                    {
                        case 0:
                            Assert.IsNotNull(rtf);
                            Assert.AreEqual("textValue", rtf.Text);
                            Assert.AreEqual("editorValue", rtf.Editor);
                            break;
                        case 1:
                            Assert.IsNotNull(rtf);
                            Assert.IsNull(rtf.Text);
                            Assert.AreEqual("editorValue", rtf.Editor);
                            break;
                        case 2:
                        case 3:
                            Assert.IsNotNull(rtf);
                            Assert.AreEqual("textValue", rtf.Text);
                            Assert.IsNull(rtf.Editor);
                            break;
                        case 4:
                        case 5:
                            Assert.IsNull(rtf);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            });
        }

        [DataRow(false, "Simple")]
        [DataRow(false, "SimpleExpander")]
        [DataRow(false, "Expander")]
        [DataRow(true, "Simple")]
        [DataRow(true, "SimpleExpander")]
        [DataRow(true, "Expander")]
        [DataTestMethod]
        public async Task OD_RichText_Get(bool expand, string projectorName)
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

                string queryString;
                switch (projectorName)
                {
                    case "Simple": queryString = "?metadata=no&$select=Id,RichText1,RichText2"; break;
                    case "SimpleExpander": queryString = "?metadata=no&$expand=ModifiedBy"; break;
                    case "Expander": queryString = "?metadata=no&$expand=ModifiedBy&$select=Id,RichText1,RichText2"; break;
                    default: throw new NotImplementedException();
                }

                if (expand)
                    queryString += "&richtexteditor=RichText1";

                // ACTION
                var response = await ODataGetAsync("/OData.svc/Root/Folder1('Content1')", queryString)
                    .ConfigureAwait(false);

                // ASSERT
                var entity = GetEntity(response);
                Assert.AreEqual(content.Id, entity.Id);
                if (expand)
                    Assert.AreEqual(RemoveWhitespaces(richTextValue), RemoveWhitespaces(entity.AllProperties["RichText1"].ToString()));
                else
                    Assert.AreEqual(textValue, entity.AllProperties["RichText1"].ToString());
                Assert.AreEqual(textValue, entity.AllProperties["RichText2"].ToString());
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
