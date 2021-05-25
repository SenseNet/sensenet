using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.Search.Indexing;
using SenseNet.Testing;
using Task = System.Threading.Tasks.Task ;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class RichTextTests : ODataTestBase
    {
        [DataRow("text", "{p1: 'xx', p2: 'yy'}")]
        [DataRow("text", null)]
        [DataRow(null, "{p1: 'xx', p2: 'yy'}")]
        [DataRow(null, null)]
        [DataTestMethod]
        public void Field_RichText_Export(string textValue, string editorValue)
        {
            var field = CreateRichTextField("RichText1");
            field.SetData(new RichTextFieldValue{Text = textValue, Editor = editorValue });

            var sb = new StringBuilder();
            using (var stringWriter = new StringWriter(sb))
            using (var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings {OmitXmlDeclaration = true}))
            {
                writer.WriteStartElement("Root");

                // ACTION
                field.Export(writer, null);

                // finish action
                writer.WriteEndElement();
            }

            // ASSERT
            var xml = new XmlDocument();
            xml.LoadXml($"{sb}");
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

        [DataRow(0, "<RichText1><Text><![CDATA[textValue]]></Text><Editor><![CDATA[editorValue]]></Editor></RichText1>")]
        [DataRow(1, "<RichText1><Editor><![CDATA[editorValue]]></Editor></RichText1>")]
        [DataRow(2, "<RichText1><Text><![CDATA[textValue]]></Text></RichText1>")]
        [DataRow(3, "<RichText1>textValue</RichText1>")]
        [DataRow(4, "<RichText1>  </RichText1>")]
        [DataRow(5, "<RichText1/>")]
        [DataTestMethod]
        public void Field_RichText_Import(int testId, string richTextElement)
        {
            var field = CreateRichTextField("RichText1");

            var xml = new XmlDocument();
            // Valid part of an import material
            xml.LoadXml($@"<Fields>{richTextElement}</Fields>");
            var fieldNode = xml.SelectSingleNode("/Fields/RichText1");

            // ACTION
            field.Import(fieldNode);

            // ASSERT
            var rtf = (RichTextFieldValue)field.GetData();
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

        [DataRow("textValue1", "textValue1", null)]
        [DataRow("{\"text\": \"textValue2\", \"editor\": \"editorValue2\"}", "textValue2", "editorValue2")]
        [DataRow("{\"text\": null, \"editor\": \"editorValue3\"}", null, "editorValue3")]
        [DataRow("{\"text\": \"textValue3\", \"editor\": null}", "textValue3", null)]
        [DataRow("{\"text\": null, \"editor\": null}", null, null)]
        [DataRow(typeof(RichTextFieldValue), "textValue4", "editorValue4")]
        [DataTestMethod]
        public void Field_RichText_ConvertFromPropertyToOutput(object propertyValue, string expectedText, string expectedEditor)
        {
            var slotIndex = 1;
            if (propertyValue is Type type && type == typeof(RichTextFieldValue))
            {
                slotIndex = 0;
                propertyValue = new RichTextFieldValue {Text = expectedText, Editor = expectedEditor };
            }

            var field = CreateRichTextField("RichText1", slotIndex);

            // ACTION
            var result = field.ConvertFromPropertyToOutput(new object[] {propertyValue});

            // ASSERT
            var rtfValue = result as RichTextFieldValue;
            Assert.IsNotNull(rtfValue);
            Assert.AreEqual(expectedText, rtfValue.Text);
            Assert.AreEqual(expectedEditor, rtfValue.Editor);
        }

        [DataRow("textValue", "editorValue", "{\"text\": \"textValue\", \"editor\": \"editorValue\"}")]
        [DataRow(null, "editorValue", "{\"text\": null, \"editor\": \"editorValue\"}")]
        [DataRow("textValue", null, "{\"text\": \"textValue\", \"editor\": null}")]
        [DataRow(null, null, "{\"text\": null, \"editor\": null}")]
        [DataRow("textValue", "editorValue", typeof(RichTextFieldValue))]
        [DataTestMethod]
        public void Field_RichText_ConvertFromInputToProperty(string textValue, string editorValue, object expectedPropertyValue)
        {
            var slotIndex = 1;
            if (expectedPropertyValue is Type type && type == typeof(RichTextFieldValue))
                slotIndex = 0;

            var field = CreateRichTextField("RichText1", slotIndex);
            var inputData = new RichTextFieldValue {Text = textValue, Editor = editorValue};

            // ACTION
            var result = field.ConvertFromInputToProperty(inputData);
            if (slotIndex == 0)
            {
                var propertyValue = result[0] as RichTextFieldValue;
                Assert.IsNotNull(propertyValue);
                Assert.AreEqual(propertyValue.Text, textValue);
                Assert.AreEqual(propertyValue.Editor, editorValue);
            }
            else
            {
                var propertyValue = result[0] as string;
                Assert.IsNotNull(propertyValue);
                Assert.AreEqual(RemoveWhitespaces(propertyValue), RemoveWhitespaces((string)expectedPropertyValue));
            }
        }

        private RichTextField CreateRichTextField(string fieldName, int handlerSlotIndex = 0)
        {
            var fDesc = new FieldDescriptor
            {
                FieldName = fieldName,
                Analyzer = IndexFieldAnalyzer.Standard,
                Bindings = new List<string> { fieldName },
                DataTypes = new[] { RepositoryDataType.Text },
                FieldTypeName = typeof(RichTextField).FullName,
                FieldTypeShortName = "RichText"
            };

            var fieldSetting = FieldSetting.Create(fDesc);
            fieldSetting.HandlerSlotIndices[0] = handlerSlotIndex;
            var field = Field.Create(null, fieldSetting);
            //field.Name = fieldName;

            return (RichTextField)field;
        }



        /* =================================================================================== */

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

        [DataRow("models=[{'RichText1':{'text': 'ttt', 'editor': 'eee'}}]", "ttt", "eee")]
        [DataRow("models=[{'RichText1':{'editor': 'eee'}}]", null, "eee")]
        [DataRow("models=[{'RichText1':{'text': 'ttt'}}]", "ttt", null)]
        [DataRow("models=[{'RichText1':'ttt'}]", "ttt", null)]
        [DataRow("models=[{'RichText1':null}]", null, null)]
        [DataTestMethod]
        public async Task OD_RichText_Modify(string requestBody, string expectedText, string expectedEditor)
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
                var response = await ODataPutAsync(
                        "/OData.svc/Root/Folder1/Content1",
                        "?metadata=no&$select=Id,RichText1,RichText2&richtexteditor=RichText1",
                        requestBody)
                    .ConfigureAwait(false);

                // ASSERT
                var loadedContent = Content.Load("/Root/Folder1/Content1");
                var rtf = (RichTextFieldValue)loadedContent["RichText1"];
                if (expectedText == null && expectedEditor == null)
                {
                    Assert.IsNull(rtf);
                }
                else
                {
                    Assert.IsNotNull(rtf);
                    Assert.AreEqual(expectedText, rtf.Text);
                    Assert.AreEqual(expectedEditor, rtf.Editor);
                }

                var entity = GetEntity(response);
                string expectedRichTextValue;
                if (expectedText != null && expectedEditor != null)
                    expectedRichTextValue = $"{{\"text\": \"{expectedText}\", \"editor\": \"{expectedEditor}\"}}";
                else if (expectedText == null && expectedEditor != null)
                    expectedRichTextValue = $"{{\"text\": null, \"editor\": \"{expectedEditor}\"}}";
                else if (expectedText != null && expectedEditor == null)
                    expectedRichTextValue = $"{{\"text\": \"{expectedText}\", \"editor\": null}}";
                else
                    expectedRichTextValue = "";
                Assert.AreEqual(content.Id, entity.Id);
                Assert.AreEqual(RemoveWhitespaces(expectedRichTextValue), RemoveWhitespaces(entity.AllProperties["RichText1"].ToString()));
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
