using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.Packaging.Tools;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ContentTypeBuilderTests : TestBase
    {
        [TestMethod]
        public void ContentType_SetBasicHeaderProperties()
        {
            Test(() =>
            {
                var cb = new ContentTypeBuilder();

                cb.Type("GenericContent")
                    .DisplayName("Test GenericContent")
                    .Description("Test GenericContent Description");

                cb.Apply();

                var ct1 = ContentType.GetByName("GenericContent");

                Assert.AreEqual("Test GenericContent", ct1.DisplayName);
                Assert.AreEqual("Test GenericContent Description", ct1.Description);
            });
        }

        [TestMethod]
        public void ContentType_SetBasicFieldProperties()
        {
            Test(() =>
            {
                var cb = new ContentTypeBuilder();

                cb.Type("GenericContent")
                    .Field("ValidFrom")
                    .DisplayName("Test ValidFrom DisplayName")
                    .Description("Test ValidFrom Description");

                cb.Apply();

                var fs1 = ContentType.GetByName("GenericContent").FieldSettings.First(fs => fs.Name == "ValidFrom");

                Assert.AreEqual("Test ValidFrom DisplayName", fs1.DisplayNameStoredValue);
                Assert.AreEqual("Test ValidFrom Description", fs1.DescriptionStoredValue);
            });
        }
        [TestMethod]
        public void ContentType_SetFieldConfiguration()
        {
            Test(() =>
            {
                var cb = new ContentTypeBuilder();

                cb.Type("GenericContent")
                    .Field("ValidFrom")
                    .DefaultValue("test default value");

                cb.Apply();

                var fs1 = ContentType.GetByName("GenericContent").FieldSettings.First(fs => fs.Name == "ValidFrom");

                Assert.AreEqual("test default value", fs1.DefaultValue);
            });
        }
    }
}
