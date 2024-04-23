using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Schema.Metadata;
using SenseNet.OData.Metadata;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ClientMetadataProviderTests : TestBase
    {
        [TestMethod]
        [TestCategory("Metadata")]
        public void ClientMetadataProvider_ExistingType()
        {
            Test2(services =>
            {
                services.AddSingleton<IClientMetadataProvider, ClientMetadataProvider>();
            }, () =>
            {
                var fileClass = new Class(ContentType.GetByName("File"));
                var fileTypeObject = ClientMetadataProvider.Instance.GetClientMetaClass(fileClass) as JObject;

                Assert.IsNotNull(fileTypeObject);
                Assert.AreEqual("File", fileTypeObject["ContentTypeName"]?.Value<string>());
                Assert.IsNotNull(fileTypeObject["DisplayName"]);
                Assert.IsNotNull(fileTypeObject["Description"]);
                Assert.IsNotNull(fileTypeObject["Icon"]);
                Assert.IsNotNull(fileTypeObject["ParentTypeName"]);
                Assert.IsNotNull(fileTypeObject["AllowIndexing"]);
                Assert.IsNotNull(fileTypeObject["AllowIncrementalNaming"]);
                Assert.IsNotNull(fileTypeObject["AllowedChildTypes"]);
                Assert.IsNotNull(fileTypeObject["Categories"]);
                Assert.IsNotNull(fileTypeObject["FieldSettings"]);
            });
        }

        [TestMethod]
        [TestCategory("Metadata")]
        public void ClientMetadataProvider_NewType()
        {
            var contentTypeName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var fieldName1 = "Field123456";

            Test2(services =>
            {
                services.AddSingleton<IClientMetadataProvider, ClientMetadataProvider>();
            }, () =>
            {
                var myType = ContentType.GetByName(contentTypeName);

                Assert.IsNull(myType);

                // register a new content type
                ContentTypeInstaller.InstallContentType($@"<?xml version='1.0' encoding='utf-8'?>
                <ContentType name='{contentTypeName}' parentType='GenericContent'
                         handler='{typeof(GenericContent).FullName}' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
                    <Fields>
                        <Field name='{fieldName1}' type='ShortText'></Field>
                    </Fields>
                </ContentType>
                ");

                // this should return the new type now
                myType = ContentType.GetByName(contentTypeName);

                var myClass = new Class(myType);
                var myTypeObject = ClientMetadataProvider.Instance.GetClientMetaClass(myClass) as JObject;

                Assert.IsNotNull(myTypeObject);
                Assert.AreEqual(contentTypeName, myTypeObject["ContentTypeName"].Value<string>());

                var field = myTypeObject["FieldSettings"].Values<JToken>().First(f => f["Name"].Value<string>() == fieldName1);

                Assert.AreEqual("ShortTextFieldSetting", field["Type"].Value<string>());
            });
        }

        [TestMethod]
        [TestCategory("Metadata")]
        public void ClientMetadataProvider_Configuration_Default()
        {
            var contentTypeName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var fieldName = "Field123456";
            var configuration = string.Empty;
            FieldSettingConfigurationTest(contentTypeName, fieldName, configuration, field =>
            {
                Assert.AreEqual("ShortTextFieldSetting", field["Type"].Value<string>());
                Assert.AreEqual(fieldName, field["Name"].Value<string>());
                Assert.IsNull(field["Regex"]);
                Assert.IsNull(field["MinLength"]);
                Assert.IsNull(field["MaxLength"]);
                Assert.IsNull(field["Customization"]);
            });
        }
        [TestMethod]
        [TestCategory("Metadata")]
        public void ClientMetadataProvider_Configuration_Regular()
        {
            var contentTypeName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var fieldName = "Field123456";
            var configuration = "<Regex>[a-zA-Z]</Regex><MinLength>7</MinLength><MaxLength>42</MaxLength>";
            FieldSettingConfigurationTest(contentTypeName, fieldName, configuration, field =>
            {
                Assert.AreEqual("ShortTextFieldSetting", field["Type"].Value<string>());
                Assert.AreEqual(fieldName, field["Name"].Value<string>());
                Assert.AreEqual("[a-zA-Z]", field["Regex"]);
                Assert.AreEqual(7, field["MinLength"]);
                Assert.AreEqual(42, field["MaxLength"]);
                Assert.IsNull(field["Customization"]);
            });
        }
        [TestMethod]
        [TestCategory("Metadata")]
        public void ClientMetadataProvider_Configuration_Customized()
        {
            var contentTypeName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var fieldName = "Field123456";
            var configuration = "<MaxLength>42</MaxLength><CustomString>Value1</CustomString><CustomInt>142</CustomInt>";
            FieldSettingConfigurationTest(contentTypeName, fieldName, configuration, field =>
            {
                Assert.AreEqual("ShortTextFieldSetting", field["Type"].Value<string>());
                Assert.AreEqual(fieldName, field["Name"].Value<string>());
                Assert.IsNull(field["Regex"]);
                Assert.IsNull(field["MinLength"]);
                Assert.AreEqual(42, field["MaxLength"]);
                var customization = field["Customization"];
                Assert.IsNotNull(customization);
                Assert.AreEqual("Value1", customization["CustomString"]);
                Assert.AreEqual("142", customization["CustomInt"]);
            });
        }
        private void FieldSettingConfigurationTest(string contentTypeName, string fieldName,
            string shortTextConfiguration, Action<JToken> assertCallback)
        {
            Test2(services =>
            {
                services.AddSingleton<IClientMetadataProvider, ClientMetadataProvider>();
            }, () =>
            {

                // register a new content type
                var ctd = ($@"<?xml version='1.0' encoding='utf-8'?>
                <ContentType name='{contentTypeName}' parentType='GenericContent'
                         handler='{typeof(GenericContent).FullName}' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
                    <Fields>
                        <Field name='{fieldName}' type='ShortText'>
                            <Configuration>{shortTextConfiguration}</Configuration>
                        </Field>
                    </Fields>
                </ContentType>
                ");

                ContentTypeInstaller.InstallContentType(ctd);

                var myType = ContentType.GetByName(contentTypeName);
                var myClass = new Class(myType);
                var myTypeObject = ClientMetadataProvider.Instance.GetClientMetaClass(myClass) as JObject;

                Assert.IsNotNull(myTypeObject);
                Assert.AreEqual(contentTypeName, myTypeObject["ContentTypeName"].Value<string>());

                var field = myTypeObject["FieldSettings"].Values<JToken>()
                    .First(f => f["Name"].Value<string>() == fieldName);

                assertCallback(field);
            });
        }

        [TestMethod]
        [TestCategory("Metadata")]
        public void ClientMetadataProvider_Categories()
        {
            var contentTypeName = System.Reflection.MethodBase.GetCurrentMethod().Name;

            Test2(services =>
            {
                services.AddSingleton<IClientMetadataProvider, ClientMetadataProvider>();
            }, () =>
            {
                ContentTypeInstaller.InstallContentType($@"<?xml version='1.0' encoding='utf-8'?>
                <ContentType name='{contentTypeName}' parentType='GenericContent'
                         handler='{typeof(GenericContent).FullName}' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
                    <Categories>Cat1 Cat2</Categories>
                    <Fields/>
                </ContentType>
                ");

                var myType = ContentType.GetByName(contentTypeName);
                AssertSequenceEqual(new[] {"Cat1", "Cat2"}, myType.Categories);

                var myClass = new Class(myType);
                var myTypeObject = ClientMetadataProvider.Instance.GetClientMetaClass(myClass) as JObject;

                Assert.IsNotNull(myTypeObject);
                Assert.AreEqual(contentTypeName, myTypeObject["ContentTypeName"].Value<string>());

                var categories = myTypeObject["Categories"]?.Values<string>() ?? Array.Empty<string>();
                AssertSequenceEqual(new[] { "Cat1", "Cat2" }, categories.ToArray());
            });
        }

        [TestMethod]
        [TestCategory("Metadata")]
        public void ClientMetadataProvider_GetSchema_AllTypes()
        {
            Test2(services =>
            {
                services.AddSingleton<IClientMetadataProvider, ClientMetadataProvider>();
            }, () =>
            {
                var allTypeObjects = ClientMetadataProvider.GetSchema(null) as object[];

                Assert.IsNotNull(allTypeObjects);
                Assert.IsTrue(allTypeObjects.Length > 1);

                // check a few unrelated types
                Assert.IsTrue(allTypeObjects.Cast<JObject>().Any(jo => jo["ContentTypeName"].Value<string>() == "File"));
                Assert.IsTrue(allTypeObjects.Cast<JObject>().Any(jo => jo["ContentTypeName"].Value<string>() == "Folder"));
            });
        }

        [TestMethod]
        [TestCategory("Metadata")]
        public void ClientMetadataProvider_GetSchema_SingleType()
        {
            Test2(services =>
            {
                services.AddSingleton<IClientMetadataProvider, ClientMetadataProvider>();
            }, () =>
            {
                var typeObjects = ClientMetadataProvider.GetSchema(null, "File") as object[];

                Assert.IsNotNull(typeObjects);
                Assert.IsTrue(typeObjects.Length == 1);

                var tpyeJObject = typeObjects[0] as JObject;

                Assert.IsNotNull(tpyeJObject);
                Assert.AreEqual("File", tpyeJObject["ContentTypeName"].Value<string>());
            });
        }

        [TestMethod]
        [TestCategory("Metadata")]
        public void ClientMetadataProvider_GetSchema_NoNull()
        {
            // make sure that null values are not rendered to save time and bandwidth

            Test2(services =>
            {
                services.AddSingleton<IClientMetadataProvider, ClientMetadataProvider>();
            }, () =>
            {
                var allTypeObjects = ClientMetadataProvider.GetSchema(null) as object[];

                Assert.IsNotNull(allTypeObjects);

                foreach (var typeObject in allTypeObjects.Cast<JObject>())
                {
                    foreach (var property in typeObject)
                    {
                        AssertNullValue(property.Value, property.Key);
                    }
                }
            });

            // check property values recursively for null
            void AssertNullValue(JToken token, string propertyName = null)
            {
                Assert.IsNotNull(token, $"Schema property must not be null. Property: {propertyName}");

                if (token is JValue jValue)
                {
                    Assert.IsNotNull(jValue.Value, $"Schema property value must not be null. Property: {propertyName}");
                }
                
                if (token.Type == JTokenType.Object)
                {
                    foreach (var child in token.Children<JProperty>())
                    {
                        AssertNullValue(child.Value, child.Name);
                    }
                }
                else if (token.Type == JTokenType.Array)
                {
                    foreach (var childToken in token.Children())
                    {
                        AssertNullValue(childToken);
                    }
                }
            }
        }
    }
}
