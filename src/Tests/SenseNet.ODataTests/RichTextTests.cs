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

        [DataRow("all", "Simple")]
        [DataRow("all", "SimpleExpander")]
        [DataRow("all", "Expander")]
        [DataRow("aLL", "Simple")]
        [DataRow("aLL", "SimpleExpander")]
        [DataRow("aLL", "Expander")]
        [DataRow("*", "Simple")]
        [DataRow("*", "SimpleExpander")]
        [DataRow("*", "Expander")]
        [DataTestMethod]
        public async Task OD_RichText_Get_ExpandAll(string rteExpansion, string projectorName)
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

                queryString += "&richtexteditor=" + rteExpansion;

                // ACTION
                var response = await ODataGetAsync("/OData.svc/Root/Folder1('Content1')", queryString)
                    .ConfigureAwait(false);

                // ASSERT
                var entity = GetEntity(response);
                Assert.AreEqual(content.Id, entity.Id);

                Assert.AreEqual(RemoveWhitespaces(richTextValue), RemoveWhitespaces(entity.AllProperties["RichText1"].ToString()));
                Assert.AreEqual(RemoveWhitespaces(richTextValue), RemoveWhitespaces(entity.AllProperties["RichText2"].ToString()));
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
