using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.Search.Indexing;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class FieldTests : TestBase
    {
        [DataRow("text", "{p1: 'xx', p2: 'yy'}")]
        [DataRow("text", null)]
        [DataRow(null, "{p1: 'xx', p2: 'yy'}")]
        [DataRow(null, null)]
        [DataTestMethod]
        public void UT_Field_RichText_Export(string textValue, string editorValue)
        {
            var field = CreateRichTextField("RichText1");
            field.SetData(new RichTextFieldValue { Text = textValue, Editor = editorValue });

            var sb = new StringBuilder();
            using (var stringWriter = new StringWriter(sb))
            using (var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings { OmitXmlDeclaration = true }))
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
        public void UT_Field_RichText_Import(int testId, string richTextElement)
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
        public void UT_Field_RichText_ConvertFromPropertyToOutput(object propertyValue, string expectedText, string expectedEditor)
        {
            var slotIndex = 1;
            if (propertyValue is Type type && type == typeof(RichTextFieldValue))
            {
                slotIndex = 0;
                propertyValue = new RichTextFieldValue { Text = expectedText, Editor = expectedEditor };
            }

            var field = CreateRichTextField("RichText1", slotIndex);

            // ACTION
            var result = field.ConvertFromPropertyToOutput(new object[] { propertyValue });

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
        public void UT_Field_RichText_ConvertFromInputToProperty(string textValue, string editorValue, object expectedPropertyValue)
        {
            var slotIndex = 1;
            if (expectedPropertyValue is Type type && type == typeof(RichTextFieldValue))
                slotIndex = 0;

            var field = CreateRichTextField("RichText1", slotIndex);
            var inputData = new RichTextFieldValue { Text = textValue, Editor = editorValue };

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
    }
}
