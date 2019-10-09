using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Services.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Schema.Metadata;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataSchemaTests : ODataTestBase
    {
        [TestMethod]
        [TestCategory("Metadata")]
        public async Task OD_ClientMetadataProvider_ExistingType()
        {
            await ODataTestAsync(() =>
            {
                var fileClass = new Class(ContentType.GetByName("File"));
                var fileTypeObject = ClientMetadataProvider.Instance.GetClientMetaClass(fileClass) as JObject;

                Assert.IsNotNull(fileTypeObject);
                Assert.AreEqual("File", fileTypeObject["ContentTypeName"].Value<string>());
                Assert.IsNotNull(fileTypeObject["DisplayName"]);
                Assert.IsNotNull(fileTypeObject["Description"]);
                Assert.IsNotNull(fileTypeObject["Icon"]);
                Assert.IsNotNull(fileTypeObject["ParentTypeName"]);
                Assert.IsNotNull(fileTypeObject["AllowIndexing"]);
                Assert.IsNotNull(fileTypeObject["AllowIncrementalNaming"]);
                Assert.IsNotNull(fileTypeObject["AllowedChildTypes"]);
                Assert.IsNotNull(fileTypeObject["FieldSettings"]);

                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("Metadata")]
        public async Task OD_ClientMetadataProvider_NewType()
        {
            var contentTypeName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var fieldName1 = "Field123456";

            await IsolatedODataTestAsync(() =>
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

                var field = myTypeObject["FieldSettings"].Values<JToken>()
                    .First(f => f["Name"].Value<string>() == fieldName1);

                Assert.AreEqual("ShortTextFieldSetting", field["Type"].Value<string>());

                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("Metadata")]
        public async Task OD_ClientMetadataProvider_GetSchema_AllTypes()
        {
            await ODataTestAsync(() =>
            {
                var allTypeObjects = ClientMetadataProvider.GetSchema(null) as object[];

                Assert.IsNotNull(allTypeObjects);
                Assert.IsTrue(allTypeObjects.Length > 1);

                // check a few unrelated types
                Assert.IsTrue(allTypeObjects.Cast<JObject>()
                    .Any(jo => jo["ContentTypeName"].Value<string>() == "File"));
                Assert.IsTrue(allTypeObjects.Cast<JObject>()
                    .Any(jo => jo["ContentTypeName"].Value<string>() == "Folder"));

                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("Metadata")]
        public async Task OD_ClientMetadataProvider_GetSchema_SingleType()
        {
            await ODataTestAsync(() =>
            {
                var typeObjects = ClientMetadataProvider.GetSchema(null, "File") as object[];

                Assert.IsNotNull(typeObjects);
                Assert.IsTrue(typeObjects.Length == 1);

                var tpyeJObject = typeObjects[0] as JObject;

                Assert.IsNotNull(tpyeJObject);
                Assert.AreEqual("File", tpyeJObject["ContentTypeName"].Value<string>());

                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("Metadata")]
        public async Task OD_ClientMetadataProvider_GetSchema_NoNull()
        {
            // make sure that null values are not rendered to save time and bandwidth

            await ODataTestAsync(() =>
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

                return Task.CompletedTask;
            }).ConfigureAwait(false);

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

        /*[TestMethod]*/
        /*[TestCategory("Metadata")]*/
        public async Task OD_ClientMetadataProvider_GetSchema_Request()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataGetAsync("/OData.svc/('Root')/GetSchema", "")
                    .ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual(200, response.StatusCode);
            });
        }
    }
}
