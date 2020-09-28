using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
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
  </Fields>
</ContentType>";
        #endregion

        [TestMethod]
        public void ContentType_Simple_HeaderProperties()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CtdSimple);

                var cb = new ContentTypeBuilder();

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

                var cb = new ContentTypeBuilder();

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

                var cb = new ContentTypeBuilder();

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

                var cb = new ContentTypeBuilder();

                cb.Type("SimpleTestContent")
                    .Field("TestText", "ShortText")
                    .DefaultValue("default text");

                cb.Apply();

                var fs1 = ContentType.GetByName("SimpleTestContent").FieldSettings.First(fs => fs.Name == "TestText");

                Assert.AreEqual("default text", fs1.DefaultValue);
            });
        }

        [TestMethod]
        public void ContentType_Complex_FieldConfiguration()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CtdComplex);

                var cb = new ContentTypeBuilder();

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
    }
}
