using System;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Packaging.Tools;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ContentTypeBuilderTests : TestBase
    {
        #region Test CTDs
        private const string CtdSimple = @"<ContentType name=""SimpleTestContent"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
<DisplayName>Simple Test Content</DisplayName>
  <Description>Simple Test Description</Description>
  <Fields>
    <Field name=""TestCount"" type=""Integer""></Field>
  </Fields>
</ContentType>";
        private const string CtdComplex = @"<ContentType name=""ComplexTestContent"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <DisplayName>Complex Test Content</DisplayName>
  <Description>Test Description</Description>
  <Icon>Content</Icon>
  <Fields>
    <Field name=""TestCount"" type=""Integer"">
		<Indexing>
			<Store>Yes</Store>
		</Indexing>
        <Configuration>
            <DefaultValue>123</DefaultValue>
			<VisibleBrowse>Hide</VisibleBrowse>
      </Configuration>
    </Field>
    <Field name=""TestText"" type=""ShortText"">
		<DisplayName>TestText-DisplayName</DisplayName>
		<Description>TestText-Description</Description>
		<Configuration>
			<VisibleBrowse>Hide</VisibleBrowse>
			<VisibleEdit>Hide</VisibleEdit>
			<VisibleNew>Hide</VisibleNew>
      </Configuration>
    </Field>
    <Field name=""ValidFrom"" type=""DateTime"">
      <DisplayName>Test ValidFrom-DisplayName</DisplayName>
      <Description>Test ValidFrom-Description</Description>
      <Configuration>
        <DateTimeMode>DateAndTime</DateTimeMode>
        <DefaultValue>@@currenttime@@</DefaultValue>
      </Configuration>
    </Field>
   <Field name=""ConfigField"" type=""ShortText"">
      <Configuration>
        <DefaultValue>default</DefaultValue>
      </Configuration>
    </Field>
  </Fields>
</ContentType>";
        private const string CtdTextEditor = @"<ContentType name=""TextEditor"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
<DisplayName>Editor Test Content</DisplayName>
  <Fields>
    <Field name=""LongText1"" type=""LongText"">
        <Description>Preserved desc</Description>
        <Configuration>
            <ControlHint>abc</ControlHint>
        </Configuration>
    </Field>
  </Fields>
</ContentType>";
        private const string CtdTextEditorSibling = @"<ContentType name=""TextEditorSibling"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
<DisplayName>Editor Test Content</DisplayName>
  <Fields>
    <Field name=""LongText1"" type=""LongText"">
        <Description>Sibling desc</Description>
        <Configuration>
            <ControlHint>sibling</ControlHint>
        </Configuration>
    </Field>
  </Fields>
</ContentType>";
        private const string CtdTextEditorChild1 = @"<ContentType name=""TextEditorChild1"" parentType=""TextEditor"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
<DisplayName>Editor Test Content</DisplayName>
  <Fields>
  </Fields>
</ContentType>";
        private const string CtdTextEditorChild2 = @"<ContentType name=""TextEditorChild2"" parentType=""TextEditorChild1"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
<DisplayName>Editor Test Content</DisplayName>
  <Fields>
    <Field name=""LongText1"" type=""LongText"">
        <Description>TextEditorChild2 desc</Description>
        <Configuration>
            <ControlHint>def</ControlHint>
        </Configuration>
    </Field>
  </Fields>
</ContentType>";
        private const string CtdTextEditorChild3 = @"<ContentType name=""TextEditorChild3"" parentType=""TextEditorChild2"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
<DisplayName>Editor Test Content</DisplayName>
  <Fields>
        <Field name=""LongText1"" type=""LongText"">
            <Configuration>
                <ControlHint>ghi</ControlHint>
            </Configuration>
        </Field>
  </Fields>
</ContentType>";

        private const string CtdTextEditorName = "TextEditor";
        private const string CtdTextEditorSiblingName = "TextEditorSibling";
        private const string CtdTextEditorChild1Name = "TextEditorChild1";
        private const string CtdTextEditorChild2Name = "TextEditorChild2";
        private const string CtdTextEditorChild3Name = "TextEditorChild3";
        #endregion

        [TestMethod]
        public void ContentType_Simple_HeaderProperties()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CtdSimple);

                var cb = new ContentTypeBuilder(null);

                cb.Type("SimpleTestContent")
                    .DisplayName("Test SimpleTestContent x")
                    .Description("Test SimpleTestContent Description x");

                cb.Apply();

                var ct1 = ContentType.GetByName("SimpleTestContent");

                Assert.AreEqual("Test SimpleTestContent x", ct1.DisplayName);
                Assert.AreEqual("Test SimpleTestContent Description x", ct1.Description);
            });
        }
        [TestMethod]
        public void ContentType_Simple_FieldProperties()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CtdSimple);

                var cb = new ContentTypeBuilder(null);

                cb.Type("SimpleTestContent")
                    .Field("TestCount")
                    .DisplayName("Test TestCount DisplayName")
                    .Description("Test TestCount Description");

                cb.Apply();

                var fs1 = ContentType.GetByName("SimpleTestContent").FieldSettings.First(fs => fs.Name == "TestCount");

                Assert.AreEqual("Test TestCount DisplayName", fs1.DisplayNameStoredValue);
                Assert.AreEqual("Test TestCount Description", fs1.DescriptionStoredValue);
            });
        }
        [TestMethod]
        public void ContentType_Simple_FieldConfiguration()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CtdSimple);

                var cb = new ContentTypeBuilder(null);

                cb.Type("SimpleTestContent")
                    .Field("TestCount")
                    .DefaultValue("64326");

                cb.Apply();

                var fs1 = ContentType.GetByName("SimpleTestContent").FieldSettings.First(fs => fs.Name == "TestCount");

                Assert.AreEqual("64326", fs1.DefaultValue);
            });
        }

        [TestMethod]
        public void ContentType_Simple_AddField()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CtdSimple);

                var cb = new ContentTypeBuilder(null);

                cb.Type("SimpleTestContent")
                    .Field("TestText", "ShortText")
                    .DefaultValue("default text");

                cb.Apply();

                var fs1 = ContentType.GetByName("SimpleTestContent").FieldSettings.First(fs => fs.Name == "TestText");

                Assert.AreEqual("default text", fs1.DefaultValue);
            });
        }

        [TestMethod]
        public void ContentType_Simple_RemoveField_Fluent()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CtdSimple);

                var fs1 = ContentType.GetByName("SimpleTestContent").FieldSettings.Single(fs => fs.Name == "TestCount");
                Assert.IsNotNull(fs1);

                var cb = new ContentTypeBuilder(null);

                cb.Type("SimpleTestContent")
                    .Field("TestCount")
                    .Delete();

                cb.Apply();

                fs1 = ContentType.GetByName("SimpleTestContent").FieldSettings.FirstOrDefault(fs => fs.Name == "TestCount");
                Assert.IsNull(fs1);
            });
        }
        [TestMethod]
        public void ContentType_Simple_RemoveField_Standalone()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CtdSimple);

                var fs1 = ContentType.GetByName("SimpleTestContent").FieldSettings.First(fs => fs.Name == "TestCount");
                Assert.IsNotNull(fs1);

                var cb = new ContentTypeBuilder(null);

                cb.Type("SimpleTestContent")
                    .RemoveField("TestCount");

                cb.Apply();

                fs1 = ContentType.GetByName("SimpleTestContent").FieldSettings.FirstOrDefault(fs => fs.Name == "TestCount");
                Assert.IsNull(fs1);
            });
        }
        [TestMethod]
        public void ContentType_Complex_RemoveConfig_Empty()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CtdComplex);

                var cb = new ContentTypeBuilder(null);

                cb.Type("ComplexTestContent")
                    .Field("TestCount")
                    .RemoveConfiguration("VisibleBrowse")
                    .RemoveConfiguration("DefaultValue");

                cb.Apply();

                AssertContentTypeXml("ComplexTestContent", (document, manager) =>
                {
                    // the field should remain in the ctd
                    var field = document.SelectSingleNode("ns:ContentType/ns:Fields/ns:Field[@name='TestCount']", manager);
                    Assert.IsNotNull(field);

                    var configNode = field.SelectSingleNode("ns:Configuration", manager);

                    // the config node should have been removed
                    Assert.IsNull(configNode, "The Configuration node should not be there anymore.");
                });
            });
        }
        [TestMethod]
        public void ContentType_Complex_RemoveProperty()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CtdComplex);

                var cb = new ContentTypeBuilder(null);

                cb.Type("ComplexTestContent")
                    .Field("TestCount")
                    .RemoveProperty("Indexing");

                cb.Apply();

                AssertContentTypeXml("ComplexTestContent", (document, manager) =>
                {
                    // the field should remain in the ctd
                    var field = document.SelectSingleNode("ns:ContentType/ns:Fields/ns:Field[@name='TestCount']", manager);
                    Assert.IsNotNull(field);

                    var childNode = field.SelectSingleNode("ns:Indexing", manager);

                    // the Indexing node should have been removed
                    Assert.IsNull(childNode, "The Indexing node should not be there anymore.");
                });
            });
        }
        [TestMethod]
        public void ContentType_Complex_RemoveConfig()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CtdComplex);

                var fs1 = ContentType.GetByName("ComplexTestContent").FieldSettings.Single(fs => fs.Name == "TestCount");
                Assert.AreEqual("123", fs1.DefaultValue);

                var cb = new ContentTypeBuilder(null);

                cb.Type("ComplexTestContent")
                    .Field("TestCount")
                    .RemoveConfiguration("Unknown")
                    .RemoveConfiguration("DefaultValue");

                cb.Apply();

                fs1 = ContentType.GetByName("ComplexTestContent").FieldSettings.Single(fs => fs.Name == "TestCount");
                Assert.AreEqual(null, fs1.DefaultValue);

                // the other config value is intact
                Assert.AreEqual(FieldVisibility.Hide, fs1.VisibleBrowse);
            });
        }

        [TestMethod]
        public void ContentType_Complex_FieldConfiguration()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CtdComplex);

                var cb = new ContentTypeBuilder(null);

                cb.Type("ComplexTestContent")
                    .Field("TestCount")
                    .DefaultValue("64326")
                    .VisibleBrowse(FieldVisibility.Show)
                    .VisibleEdit(FieldVisibility.Advanced)
                    .VisibleNew(FieldVisibility.Hide)
                    .ReadOnly()
                    .Field("ValidFrom")
                    .Configure("DateTimeMode", "Date")
                    .FieldIndex(567)
                    .Compulsory()
                    .ControlHint("mycustomcontrol");

                cb.Apply();

                var ct = ContentType.GetByName("ComplexTestContent");
                var fs1 = ct.FieldSettings.First(fs => fs.Name == "TestCount");
                var fs2 = ct.FieldSettings.First(fs => fs.Name == "ValidFrom") as DateTimeFieldSetting;

                Assert.AreEqual("64326", fs1.DefaultValue);
                Assert.AreEqual(FieldVisibility.Show, fs1.VisibleBrowse);
                Assert.AreEqual(FieldVisibility.Advanced, fs1.VisibleEdit);
                Assert.AreEqual(FieldVisibility.Hide, fs1.VisibleNew);
                Assert.AreEqual(true, fs1.ReadOnly);

                Assert.AreEqual(DateTimeMode.Date, fs2.DateTimeMode);
                Assert.AreEqual(567, fs2.FieldIndex);
                Assert.AreEqual(true, fs2.Compulsory);
                Assert.AreEqual("mycustomcontrol", fs2.ControlHint);
            });
        }

        [TestMethod]
        public void ContentType_Complex_InsertFieldProperty()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CtdComplex);

                var cb = new ContentTypeBuilder(null);

                // define properties in reverse order
                cb.Type("ComplexTestContent")
                    .Field("ConfigField")
                    .Description("desc")
                    .DisplayName("disp");

                cb.Apply();

                var fs1 = ContentType.GetByName("ComplexTestContent").FieldSettings.Single(fs => fs.Name == "ConfigField");

                Assert.AreEqual("disp", fs1.DisplayName);
                Assert.AreEqual("desc", fs1.Description);
                Assert.AreEqual("default", fs1.DefaultValue);
            });
        }
      
        [TestMethod]
        public void ContentType_ChangeFieldType_LongText()
        {
            Content CreateTestContent(string contentTypeName, Node parent)
            {
                var testContent = Content.CreateNew(contentTypeName, parent, Guid.NewGuid().ToString());
                testContent["LongText1"] = $"{contentTypeName} longtext value";
                testContent.Save();
                return testContent;
            }

            void AssertProperties(FieldSetting fs, string displayName, string description,
                bool compulsory, FieldVisibility visibleEdit, string controlHint)
            {
                Assert.AreEqual("RichText", fs.ShortName);
                Assert.AreEqual(displayName, fs.DisplayName);
                Assert.AreEqual(description, fs.Description);
                Assert.AreEqual(compulsory, fs.Compulsory);
                Assert.AreEqual(visibleEdit, fs.VisibleEdit);
                Assert.AreEqual(controlHint, fs.ControlHint);
            }

            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CtdTextEditor);
                ContentTypeInstaller.InstallContentType(CtdTextEditorSibling);
                ContentTypeInstaller.InstallContentType(CtdTextEditorChild1);
                ContentTypeInstaller.InstallContentType(CtdTextEditorChild2);
                ContentTypeInstaller.InstallContentType(CtdTextEditorChild3);

                var fs1 = ContentType.GetByName(CtdTextEditorName).FieldSettings.First(fs => fs.Name == "LongText1");
                Assert.AreEqual("LongText", fs1.ShortName);

                // create text content and fill the field
                var parent = new SystemFolder(Repository.Root);
                parent.Save();

                var testContent1 = CreateTestContent(CtdTextEditorName, parent);
                var testContent2 = CreateTestContent(CtdTextEditorSiblingName, parent);
                var testContent3 = CreateTestContent(CtdTextEditorChild1Name, parent);
                var testContent4 = CreateTestContent(CtdTextEditorChild2Name, parent);
                var testContent5 = CreateTestContent(CtdTextEditorChild3Name, parent);

                var cb = new ContentTypeBuilder(null);

                // change field type and set a couple of additional properties
                cb.ChangeFieldType("LongText1", "RichText")
                    .Type(CtdTextEditorName)
                    .Field("LongText1")
                    .DisplayName("My rich text field")
                    .Compulsory()
                    .VisibleEdit(FieldVisibility.Hide);

                cb.Apply();

                fs1 = ContentType.GetByName(CtdTextEditorName).FieldSettings.Single(fs => fs.Name == "LongText1");
                var fs2 = ContentType.GetByName(CtdTextEditorSiblingName).FieldSettings.Single(fs => fs.Name == "LongText1");
                var fs3 = ContentType.GetByName(CtdTextEditorChild1Name).FieldSettings.Single(fs => fs.Name == "LongText1");
                var fs4 = ContentType.GetByName(CtdTextEditorChild2Name).FieldSettings.Single(fs => fs.Name == "LongText1");
                var fs5 = ContentType.GetByName(CtdTextEditorChild3Name).FieldSettings.Single(fs => fs.Name == "LongText1");

                // check new and preserved properties
                AssertProperties(fs1, "My rich text field", "Preserved desc", 
                    true, FieldVisibility.Hide, "abc");
                AssertProperties(fs2, null, "Sibling desc",
                    false, FieldVisibility.Show, "sibling");
                AssertProperties(fs3, "My rich text field", "Preserved desc",
                    true, FieldVisibility.Hide, "abc");
                AssertProperties(fs4, "My rich text field", "TextEditorChild2 desc",
                    true, FieldVisibility.Hide, "def");
                AssertProperties(fs5, "My rich text field", "TextEditorChild2 desc",
                    true, FieldVisibility.Hide, "ghi");

                testContent1 = Content.Load(testContent1.Id);
                testContent2 = Content.Load(testContent2.Id);
                testContent3 = Content.Load(testContent3.Id);
                testContent4 = Content.Load(testContent4.Id);
                testContent5 = Content.Load(testContent5.Id);

                Assert.AreEqual($"{CtdTextEditorName} longtext value", ((RichTextFieldValue)testContent1["LongText1"]).Text);
                Assert.AreEqual($"{CtdTextEditorSiblingName} longtext value", ((RichTextFieldValue)testContent2["LongText1"]).Text);
                Assert.AreEqual($"{CtdTextEditorChild1Name} longtext value", ((RichTextFieldValue)testContent3["LongText1"]).Text);
                Assert.AreEqual($"{CtdTextEditorChild2Name} longtext value", ((RichTextFieldValue)testContent4["LongText1"]).Text);
                Assert.AreEqual($"{CtdTextEditorChild3Name} longtext value", ((RichTextFieldValue)testContent5["LongText1"]).Text);
            });
        }

        private static void AssertContentTypeXml(string contentTypeName, Action<XmlDocument, XmlNamespaceManager> checkXml)
        {
            var ct = ContentType.GetByName(contentTypeName);
            using (var ctStream = ct.Binary.GetStream())
            {
                var document = new XmlDocument();
                document.Load(ctStream);

                var m = new XmlNamespaceManager(document.NameTable);
                m.AddNamespace("ns", "http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition");

                checkXml(document, m);
            }
        }
    }
}
