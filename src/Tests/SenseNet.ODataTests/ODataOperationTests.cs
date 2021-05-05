using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.OData;
using SenseNet.Portal.ApplicationModel;
using SenseNet.Search;
using Task = System.Threading.Tasks.Task;
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataOperationTests : ODataTestBase
    {
        [ODataFunction]
        public static string Function0(Content content)
        {
            return null;
        }
        [ODataFunction]
        public static Task Function0VoidAsync(Content content)
        {
            return Task.CompletedTask;
        }
        [ODataFunction]
        public static Task<object> Function0ObjectAsync(Content content)
        {
            return Task.FromResult((object)null);
        }

        [ODataFunction]
        public static string Function1(Content content, string param1)
        {
            return "## Function1 called." +
                   $" Path: {(content?.Path ?? "[null]")}." +
                   $" Param1: {param1}.";
        }
        [ODataFunction]
        public static string Function2(Content content, HttpContext httpContext, ODataRequest request, string param1)
        {
            return "## Function2 called." +
                   $" Query: {httpContext.Request.QueryString}." +
                   $" Format: {request.Format}." +
                   $" Path: {(content?.Path ?? "[null]")}." +
                   $" Param1: {param1}.";
        }
        [ODataFunction]
        public static string Function22(Content content, IConfiguration configuration, string param1)
        {
            return "## Function22 called." +
                   $" Config: {configuration?.GetType().Name ?? "(null)"}." +
                   $" Param1: {param1}.";
        }
        [ODataFunction]
        public static string Function3(Content content, string param1, string param2, string param3 = null)
        {
            return "## Function3 called." +
                   $" Path: {(content?.Path ?? "[null]")}," +
                   $" Param1: {param1}," +
                   $" Param2: {param2}," +
                   $" Param3: {(param3 ?? "[null]")}";
        }

        [Flags] public enum CustomEnum { Zero = 0, One = 1, Two = 2, Four = 4, Eight = 8, All = 15 }
        [ODataFunction]
        public static string Function4Enums(Content content, MetadataFormat metadataFormat, CustomEnum customEnum = default)
        {
            return "## Function4Enums called." +
                   $" metadataFormat: {metadataFormat}," +
                   $" customEnum: {customEnum}.";
        }

        public class CustomTypeForFunction4
        {
            public int Id { get; set; }
            public string Value { get; set; }
        }
        [ODataFunction]
        public static Task<CustomTypeForFunction4> Function4CustomTypeAsync(Content content, int id, string value)
        {
            return Task.FromResult(new CustomTypeForFunction4 {Id = id, Value = value});
        }
        [ODataFunction]
        public static Task<Content> Function4ContentAsync(Content content)
        {
            return Task.FromResult(content);
        }
        [ODataFunction]
        public static Task<string> Function4StringAsync(Content content, string input)
        {
            return Task.FromResult(input);
        }

        /* ============================================================= METHOD BASED OPERATION TESTS */

        [TestMethod]
        public async Task OD_MBOP_Invoke_Null_String()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var httpContext = await ODataProcessRequestEmptyResponseAsync(
                        "/OData.svc/Root('IMS')/Function0", null, null,
                        "POST", null)
                    .ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual(204, httpContext.Response.StatusCode);

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_MBOP_Invoke_Null_VoidAsync()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var httpContext = await ODataProcessRequestEmptyResponseAsync(
                        "/OData.svc/Root('IMS')/Function0VoidAsync", null, null,
                        "POST", null)
                    .ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual(204, httpContext.Response.StatusCode);

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_MBOP_Invoke_Null_ObjectAsync()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var httpContext = await ODataProcessRequestEmptyResponseAsync(
                        "/OData.svc/Root('IMS')/Function0ObjectAsync", null, null,
                        "POST", null)
                    .ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual(204, httpContext.Response.StatusCode);

            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_MBOP_Invoke_Async_Custom()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync(
                    "/OData.svc/Root('IMS')/Function4CustomTypeAsync",
                    null,
                    "{id:\"42\", value:\"value42\"}").ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var customResult = JsonSerializer.Create()
                    .Deserialize<CustomTypeForFunction4>(new JsonTextReader(new StringReader(response.Result)));
                Assert.AreEqual(42, customResult.Id);
                Assert.AreEqual("value42", customResult.Value);

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_MBOP_Invoke_Async_Content()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync("/OData.svc/Root('IMS')/Function4ContentAsync",
                    null, null).ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var expectedContent = Content.Load("/Root/IMS");
                var clientContent = (JObject)JsonSerializer.Create()
                    .Deserialize(new JsonTextReader(new StringReader(response.Result)));
                Assert.AreEqual(expectedContent.Id, clientContent["d"]["Id"].Value<int>());
                Assert.AreEqual(expectedContent.Path, clientContent["d"]["Path"].Value<string>());

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_MBOP_Invoke_Async_String()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync(
                    "/OData.svc/Root('IMS')/Function4StringAsync",
                    null,
                    "{input:\"input42\"}").ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual("input42", response.Result);

            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_MBOP_Invoke()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync(
                    "/OData.svc/Root('IMS')/Function1",
                    "?param2=value2",
                    "{param1:\"asdf\"}").ConfigureAwait(false);

                // ASSERT
                var expected = "## Function1 called. Path: /Root/IMS. Param1: asdf.";
                var actual = response.Result;
                var raw = actual.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expected.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(exp, raw);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_MBOP_Invoke_FormEncoded()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync(
                    "/OData.svc/Root('IMS')/Function3",
                    "?param2=value2",
                    "param1=value1&param3=value3").ConfigureAwait(false);

                // ASSERT
                var expected = "## Function3 called. Path: /Root/IMS, Param1: value1, Param2: value2, Param3: value3";
                var actual = response.Result;
                var raw = actual.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expected.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(exp, raw);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_MBOP_Invoke_SystemParameters()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync(
                    "/OData.svc/Root('IMS')/Function2",
                    "?param2=value2",
                    "{param1:\"asdf\"}").ConfigureAwait(false);

                // ASSERT
                var expected = "## Function2 called. Query: ?param2=value2. Format: json. Path: /Root/IMS. Param1: asdf.";
                var actual = response.Result;
                var raw = actual.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expected.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(exp, raw);
            }).ConfigureAwait(false);
        }

        private class TestConfigForSystemParametersTest : IConfiguration
        {
            public IConfigurationSection GetSection(string key) { throw new NotImplementedException(); }
            public IEnumerable<IConfigurationSection> GetChildren() { throw new NotImplementedException(); }
            public IChangeToken GetReloadToken() { throw new NotImplementedException(); }
            public string this[string key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        }
        [TestMethod]
        public async Task OD_MBOP_Invoke_SystemParameters_IConfig_Get()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataGetAsync(
                    "/OData.svc/Root('IMS')/Function22", "?param1=asdf", null,
                    new TestConfigForSystemParametersTest()).ConfigureAwait(false);

                // ASSERT
                var expected = $"## Function22 called. Config: {nameof(TestConfigForSystemParametersTest)}. Param1: asdf.";
                var actual = response.Result;
                var raw = actual.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expected.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(exp, raw);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_MBOP_Invoke_SystemParameters_IConfig_Post()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync(
                    "/OData.svc/Root('IMS')/Function22", "",
                    "{param1:\"asdf\"}", null, new TestConfigForSystemParametersTest())
                    .ConfigureAwait(false);

                // ASSERT
                var expected = $"## Function22 called. Config: {nameof(TestConfigForSystemParametersTest)}. Param1: asdf.";
                var actual = response.Result;
                var raw = actual.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expected.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(exp, raw);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_MBOP_Invoke_EnumParams_Get()
        {
            await ODataTestAsync(async () =>
            {
                // MetadataFormat: None, Minimal, Full

                // ACTION
                var response = await ODataGetAsync(
                    "/OData.svc/Root('IMS')/Function4Enums",
                    "?metadataFormat=mInImal&customEnum=0").ConfigureAwait(false);

                // ASSERT
                var expected = $"## Function4Enums called. metadataFormat: Minimal, customEnum: Zero.";
                var actual = response.Result;
                var raw = actual.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expected.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(exp, raw);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_MBOP_Invoke_EnumParams_Post_Strings()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync(
                    "/OData.svc/Root('IMS')/Function4Enums", "",
                    "{metadataFormat:\"Full\",customEnum:\"6\"}").ConfigureAwait(false);

                // ASSERT
                var expected = $"## Function4Enums called. metadataFormat: Full, customEnum:{CustomEnum.Four | CustomEnum.Two}.";
                var actual = response.Result;
                var raw = actual.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expected.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(exp, raw);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_MBOP_Invoke_EnumParams_Post_Integers()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync(
                    "/OData.svc/Root('IMS')/Function4Enums", "",
                    "{metadataFormat:1,customEnum:6}").ConfigureAwait(false);

                // ASSERT
                var expected = $"## Function4Enums called. metadataFormat: Minimal, customEnum:{CustomEnum.Four | CustomEnum.Two}.";
                var actual = response.Result;
                var raw = actual.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expected.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(exp, raw);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_MBOP_Invoke_WithApp()
        {
            await ODataTestAsync(async () =>
            {
                if (ContentType.GetByName("GenericODataApplication") == null)
                {
                    ContentTypeInstaller.InstallContentType(@"<?xml version=""1.0"" encoding=""utf-8""?>
<ContentType name=""GenericODataApplication"" parentType=""Application"" handler=""SenseNet.Portal.ApplicationModel.GenericODataApplication"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <DisplayName>$Ctd-GenericODataApplication,DisplayName</DisplayName>
  <Description>$Ctd-GenericODataApplication,Description</Description>
  <Icon>Application</Icon>
  <Fields>
    <Field name=""ClassName"" type=""ShortText"">
      <DisplayName>$Ctd-GenericODataApplication,ClassName-DisplayName</DisplayName>
      <Description>$Ctd-GenericODataApplication,ClassName-Description</Description>
    </Field>
    <Field name=""MethodName"" type=""ShortText"">
      <DisplayName>$Ctd-GenericODataApplication,MethodName-DisplayName</DisplayName>
      <Description>$Ctd-GenericODataApplication,MethodName-Description</Description>
    </Field>
    <Field name=""Parameters"" type=""LongText"">
      <DisplayName>$Ctd-GenericODataApplication,Parameters-DisplayName</DisplayName>
      <Description>$Ctd-GenericODataApplication,Parameters-Description</Description>
    </Field>
  </Fields>
</ContentType>");
                }

                var apps = Node.LoadNode("/Root/(apps)");
                if (apps == null)
                {
                    apps = new SystemFolder(Repository.Root) {Name = "(apps)"};
                    apps.Save();
                }
                var appRoot = Node.Load<GenericContent>("/Root/(apps)/GenericContent");
                if (appRoot == null)
                {
                    appRoot = new Folder(Repository.Root) { Name = "GenericContent" };
                    appRoot.Save();
                }

                //appRoot.AllowChildType(ContentType.GetByName("GenericODataApplication"), true);

                var app = new GenericODataApplication(appRoot)
                {
                    Name = "Function2",
                    ActionTypeName = typeof(GenericODataOperation).FullName,
                    ClassName = this.GetType().FullName,
                    MethodName = "Function2",
                    Parameters = "string param1, string param2"
                };
                app.Save();

                // ACTION
                var response = await ODataPostAsync(
                    "/OData.svc/Root('IMS')/Function3",
                    "?param2=value2",
                    "{param1:\"asdf\",param2:\"qwer\"}").ConfigureAwait(false);

                // ASSERT
                var expected = "## Function3 called. Path: /Root/IMS, Param1: asdf, Param2: value2, Param3:[null]";
                var actual = response.Result;
                var raw = actual.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expected.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(exp, raw);
            }).ConfigureAwait(false);
        }

        /* ============================================================= METHOD BASED OPERATION METADATA */

        [TestMethod]
        public async Task OD_MBOM_FullMetadata()
        {
            await ODataTestAsync(async () =>
            {

                // ACTION
                var response = await ODataGetAsync(
                    "/OData.svc/Root('IMS')",
                    "?metadata=full&$select=Name").ConfigureAwait(false);

                // ASSERT
                var expected =
                    "{\"title\":\"Function2\",\"name\":\"Function2\",\"target\":\"/odata.svc/Root('IMS')/Function2\",\"forbidden\":false,\"parameters\":[{\"name\":\"param1\",\"type\":\"string\",\"required\":true}]}," +
                    "{\"title\":\"Function22\",\"name\":\"Function22\",\"target\":\"/odata.svc/Root('IMS')/Function22\",\"forbidden\":false,\"parameters\":[{\"name\":\"param1\",\"type\":\"string\",\"required\":true}]}";
                var actual = response.Result;
                var raw = actual.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var p0 = raw.IndexOf("{\"title\":\"Function2\"", StringComparison.Ordinal);
                var p1 = raw.IndexOf(",{\"title\":\"", p0, StringComparison.Ordinal);
                var function2 = raw.Substring(p0, p1 - p0);
                p0 = raw.IndexOf("{\"title\":\"Function22\"", StringComparison.Ordinal);
                p1 = raw.IndexOf(",{\"title\":\"", p0, StringComparison.Ordinal);
                var function22 = raw.Substring(p0, p1 - p0);
                Assert.AreEqual(expected, function2 + "," + function22);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_MBOM_ActionsProperty()
        {
            await ODataTestAsync(async () =>
            {

                // ACTION
                var response = await ODataGetAsync(
                    "/OData.svc/Root('IMS')",
                    "?metadata=no&$expand=Actions&$select=Name,Actions").ConfigureAwait(false);

                // ASSERT
                var expected =
                    "{\"Name\":\"Function2\",\"DisplayName\":\"Function2\",\"Index\":0,\"Icon\":\"Application\",\"Url\":\"/odata.svc/Root('IMS')/Function2\",\"IsODataAction\":true,\"ActionParameters\":[\"param1\"],\"Scenario\":null,\"Forbidden\":false}," +
                    "{\"Name\":\"Function22\",\"DisplayName\":\"Function22\",\"Index\":0,\"Icon\":\"Application\",\"Url\":\"/odata.svc/Root('IMS')/Function22\",\"IsODataAction\":true,\"ActionParameters\":[\"param1\"],\"Scenario\":null,\"Forbidden\":false}";
                var exp = expected.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var actual = response.Result;
                var raw = actual.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var p0 = raw.IndexOf("{\"Name\":\"Function2\"", StringComparison.Ordinal);
                var p1 = raw.IndexOf(",{\"Name\":\"", p0, StringComparison.Ordinal);
                var function2 = raw.Substring(p0, p1 - p0);
                p0 = raw.IndexOf("{\"Name\":\"Function22\"", StringComparison.Ordinal);
                p1 = raw.IndexOf(",{\"Name\":\"", p0, StringComparison.Ordinal);
                var function22 = raw.Substring(p0, p1 - p0);
                Assert.AreEqual(exp, function2 + "," + function22);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_MBOM_ActionsFunction()
        {
            await ODataTestAsync(async () =>
            {

                // ACTION
                var response = await ODataGetAsync(
                    "/OData.svc/Root('IMS')/Actions",
                    "?metadata=no").ConfigureAwait(false);

                // ASSERT
                var expected =
                    "{\"Name\":\"Function2\",\"DisplayName\":\"Function2\",\"Index\":0,\"Icon\":\"Application\",\"Url\":\"/odata.svc/Root('IMS')/Function2\",\"IsODataAction\":true,\"ActionParameters\":[\"param1\"],\"Scenario\":null,\"Forbidden\":false}," +
                    "{\"Name\":\"Function22\",\"DisplayName\":\"Function22\",\"Index\":0,\"Icon\":\"Application\",\"Url\":\"/odata.svc/Root('IMS')/Function22\",\"IsODataAction\":true,\"ActionParameters\":[\"param1\"],\"Scenario\":null,\"Forbidden\":false}";
                var exp = expected.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var actual = response.Result;
                var raw = actual.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var p0 = raw.IndexOf("{\"Name\":\"Function2\"", StringComparison.Ordinal);
                var p1 = raw.IndexOf(",{\"Name\":\"", p0, StringComparison.Ordinal);
                var function2 = raw.Substring(p0, p1 - p0);
                p0 = raw.IndexOf("{\"Name\":\"Function22\"", StringComparison.Ordinal);
                p1 = raw.IndexOf(",{\"Name\":\"", p0, StringComparison.Ordinal);
                var function22 = raw.Substring(p0, p1 - p0);
                Assert.AreEqual(exp, function2 + "," + function22);
            }).ConfigureAwait(false);
        }

        /* ============================================================= OPERATION RESULT TESTS */

        [TestMethod]
        public async Task OD_OP_GetSchema()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataGetAsync(
                    "/OData.svc/('Root')/GetSchema",
                    "").ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var serializerSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    Formatting = Formatting.Indented
                };
                object deserialized;
                using (var reader = new StringReader(response.Result))
                    deserialized = JsonSerializer.Create(serializerSettings).Deserialize(new JsonTextReader(reader));
                var items = deserialized as JArray;
                if (items == null)
                    Assert.Fail("Result is not an array");

                var names = items.ToArray().Select(x => x["ContentTypeName"].ToString()).ToList();
                Assert.IsTrue(names.Contains("GenericContent"));
                Assert.IsTrue(names.Contains("Folder"));
                Assert.IsTrue(names.Contains("File"));
            }).ConfigureAwait(false);
        }

        /* ============================================================= OPERATION RESULT TESTS */
        /*                 (these tests use old style app resolution via mocked ActionResolver) */

        [TestMethod]
        public async Task OD_OP_InvokeAction()
        {
            await ODataTestAsync(async () =>
            {
                using (new ActionResolverSwindler(new TestActionResolver()))
                {
                    var expectedJson = @"
                        {
                          ""d"": {
                            ""message"":""Action3 executed""
                          }
                        }";

                    // ACTION
                    var response = await ODataPostAsync(
                        "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Action3",
                        "",
                        null).ConfigureAwait(false);

                    // ASSERT
                    var jsonText = response.Result;
                    var raw = jsonText.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                    var exp = expectedJson.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                    Assert.IsTrue(raw == exp);
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_OP_InvokeAction_NoContent()
        {
            await ODataTestAsync(async () =>
            {
                using (new ActionResolverSwindler(new TestActionResolver()))
                {
                    // ACTION
                    var response = await ODataPostAsync(
                        "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Action4",
                        "",
                        null).ConfigureAwait(false);

                    // ASSERT
                    Assert.IsTrue(response.StatusCode == 204);
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_OP_InvokeAction_Post_GetPutMergePatchDelete()
        {
            await ODataTestAsync(async () =>
            {
                using (new ActionResolverSwindler(new TestActionResolver()))
                {
                    // ACTION POST
                    var response = await ODataPostAsync(
                        "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/ODataAction",
                        "",
                        null).ConfigureAwait(false);

                    // ASSERT: POST Ok
                    Assert.AreEqual("ODataAction executed.", response.Result);

                    var verbs = new[] {"GET", "PUT", "MERGE", "PATCH", "DELETE"};
                    foreach (var verb in verbs)
                    {
                        // ACTION: GET PUT MERGE PATCH DELETE: error
                        response = await ODataCallAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/ODataAction",
                            "",
                            null,
                            verb).ConfigureAwait(false);

                        // ASSERT: error
                        var error = GetError(response);
                        Assert.AreEqual(ODataExceptionCode.IllegalInvoke, error.Code);
                    }
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_OP_InvokeFunction_PostGet_PutMergePatchDelete()
        {
            await ODataTestAsync(async () =>
            {
                using (new ActionResolverSwindler(new TestActionResolver()))
                {
                    // ACTION: POST
                    var response = await ODataPostAsync(
                        "/OData.svc/Root('IMS')/ODataFunction",
                        "",
                        null).ConfigureAwait(false);

                    // ASSERT: POST ok
                    Assert.AreEqual("ODataFunction executed.", response.Result);

                    // ACTION: GET
                    response = await ODataGetAsync(
                        "/OData.svc/Root('IMS')/ODataFunction",
                        "")
                        .ConfigureAwait(false);

                    // ASSERT: GET ok
                    Assert.AreEqual("ODataFunction executed.", response.Result);

                    //------------------------------------------------------------ GET PUT MERGE PATCH DELETE: error
                    var verbs = new[] {"PUT", "MERGE", "PATCH", "DELETE"};
                    foreach (var verb in verbs)
                    {
                        // ACTION: PUT MERGE PATCH DELETE
                        response = await ODataCallAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/ODataAction",
                            "",
                            null,
                            verb).ConfigureAwait(false);

                        // ASSERT: error
                        var error = GetError(response);
                        Assert.AreEqual(ODataExceptionCode.IllegalInvoke, error.Code);
                    }
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_OP_InvokeFunction_DictionaryHandler()
        {
            await ODataTestAsync(async () =>
            {
                using (new ActionResolverSwindler(new TestActionResolver()))
                {
                    // ACTION
                    var response = await ODataPostAsync(
                        "/OData.svc/Root/System/Schema/ContentTypes/GenericContent('FieldSettingContent')/ODataGetParentChainAction",
                        "?metadata=no&$select=Id,Name&$top=2&$inlinecount=allpages",
                        null)
                        .ConfigureAwait(false);

                    // ASSERT: POST ok
                    var entities = GetEntities(response);
                    Assert.AreEqual(6, entities.TotalCount);
                    Assert.AreEqual(2, entities.Length);
                }
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_OP_InvokeAction_Errors()
        {
            await ODataTestAsync(async () =>
            {
                using (new ActionResolverSwindler(new TestActionResolver()))
                {
                    var testCases = new[]
                    {
                        new { request = "ContentNotFound", errorCode = ODataExceptionCode.ResourceNotFound },
                        new { request = "SenseNetSecurityException", errorCode = ODataExceptionCode.NotSpecified },
                        new { request = "InvalidContentActionException", errorCode = ODataExceptionCode.NotSpecified },
                        new { request = "NodeAlreadyExistsException", errorCode = ODataExceptionCode.ContentAlreadyExists },
                        new { request = "UnknownError", errorCode = ODataExceptionCode.NotSpecified },
                    };

                    foreach (var testCase in testCases)
                    {
                        // ACTION
                        var response = await ODataPostAsync(
                                "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/ODataError",
                                "",
                                $@"{{""errorType"":""{testCase.request}""}}")
                            .ConfigureAwait(false);

                        // ASSERT
                        var error = GetError(response);
                        Assert.AreEqual(testCase.errorCode, error.Code);

                    }
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_OP_InvokeAction_SecurityErrorVisitor()
        {
            await ODataTestAsync(new TestUser("Visitor", Identifiers.VisitorUserId), async () =>
            {
                using (new ActionResolverSwindler(new TestActionResolver()))
                {
                    // ACTION
                    var response = await ODataPostAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/ODataError",
                            "",
                            $@"{{""errorType"":""SenseNetSecurityException""}}")
                        .ConfigureAwait(false);

                    // ASSERT
                    AssertNoError(response);
                    Assert.AreEqual(404, response.StatusCode);
                }
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_OP_FilteringAndPartitioningOperationResult_ChildrenDefinition()
        {
            await ODataTestAsync(async () =>
            {
                using (new ActionResolverSwindler(new TestActionResolver()))
                {
                    // ACTION
                    var response = await ODataPostAsync(
                            "/OData.svc/Root('IMS')/ChildrenDefinitionFilteringTest",
                            "",
                            null)
                        .ConfigureAwait(false);

                    // ASSERT
                    AssertNoError(response);
                    var entities = GetEntities(response);
                    var ids = String.Join(", ", entities.Select(e => e.Id));
                    var expids = String.Join(", ",
                        CreateSafeContentQuery(
                                "InFolder:/Root/IMS/BuiltIn/Portal .AUTOFILTERS:OFF .REVERSESORT:Name .SKIP:2 .TOP:3")
                            .Execute().Identifiers);
                    // 8, 9, 7
                    Assert.AreEqual(expids, ids);
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_OP_FilteringAndPartitioningOperationResult_ContentCollection()
        {
            await ODataTestAsync(async () =>
            {
                using (new ActionResolverSwindler(new TestActionResolver()))
                {
                    // ACTION
                    var response = await ODataPostAsync(
                            "/OData.svc/Root('IMS')/CollectionFilteringTest",
                            "?$skip=1&$top=3&$orderby=Name desc&$select=Id,Name&$filter=Id ne 10&metadata=no",
                            null)
                        .ConfigureAwait(false);

                    // ASSERT
                    AssertNoError(response);
                    var entities = GetEntities(response);
                    var ids = String.Join(", ", entities.Select(e => e.Id));
                    var expids = String.Join(", ",
                        CreateSafeContentQuery(
                                "+InFolder:/Root/IMS/BuiltIn/Portal -Id:10 .AUTOFILTERS:OFF .REVERSESORT:Name .SKIP:1 .TOP:3")
                            .Execute().Identifiers);
                    // 8, 9, 7
                    Assert.AreEqual(expids, ids);
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_OP_FilteringCollection_IsOf()
        {
            await ODataTestAsync(async () =>
            {
                using (new ActionResolverSwindler(new TestActionResolver()))
                {
                    // ACTION 1: Select users
                    var response = await ODataPostAsync(
                            "/OData.svc/Root('IMS')/CollectionFilteringTest",
                            "?&$select=Id,Name&metadata=no&$filter=isof('User')",
                            null)
                        .ConfigureAwait(false);

                    // ASSERT 1: Ids: 1, 6, 10, 12, 1205
                    AssertNoError(response);
                    var entities = GetEntities(response);
                    var ids = String.Join(", ",
                        entities.Select(e => e.Id).OrderBy(x => x).Select(x => x.ToString()));
                    var expids = String.Join(", ",
                        CreateSafeContentQuery("+InFolder:/Root/IMS/BuiltIn/Portal +TypeIs:User .AUTOFILTERS:OFF")
                            .Execute().Identifiers.OrderBy(x => x).Select(x => x.ToString()));
                    Assert.AreEqual(expids, ids);

                    // ACTION 2: Select not users
                    response = await ODataPostAsync(
                            "/OData.svc/Root('IMS')/CollectionFilteringTest",
                            "?$select=Id,Name&metadata=no&$filter=not isof('User')",
                            null)
                        .ConfigureAwait(false);

                    // ASSERT 2: Ids: 7, 8, 9, 11, 1197, 1198, 1199, 1200, 1201, 1202, 1203, 1204
                    AssertNoError(response);
                    entities = GetEntities(response);
                    ids = String.Join(", ", entities.Select(e => e.Id).OrderBy(x => x).Select(x => x.ToString()));
                    expids = String.Join(", ",
                        CreateSafeContentQuery("+InFolder:/Root/IMS/BuiltIn/Portal -TypeIs:User .AUTOFILTERS:OFF")
                            .Execute().Identifiers.OrderBy(x => x).Select(x => x.ToString()));
                    Assert.AreEqual(expids, ids);
                }
            }).ConfigureAwait(false);
        }


        [TestMethod]
        public async Task OD_OP_QueryString_Func_Get()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataGetAsync(
                        "/OData.svc/Root('IMS')/FunctionForQueryStringTest",
                        "?a=stringValue&b=42&rnd=123456789")
                    .ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual("FunctionForQueryStringTest-stringValue-42", response.Result);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_OP_QueryString_Func_Post_Override()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync(
                        "/OData.svc/Root('IMS')/FunctionForQueryStringTest",
                        "?b=442&rnd=123456789",
                        "{a:\"stringValue\",b:42}")
                    .ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual("FunctionForQueryStringTest-stringValue-442", response.Result);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_OP_QueryString_StringArray()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataGetAsync(
                        "/OData.svc/Root('IMS')/Array_String",
                        "?a=ppp&a=qqq&a=rrr&b=1&b=2")
                    .ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual("[\"ppp\",\"qqq\",\"rrr\"]", RemoveWhitespaces(response.Result));
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public void OD_OP_RepositoryApps_ClientApp()
        {
            Application CreateApp(Node parent, string name, string scenario)
            {
                var tempApp1 = new ClientApplication(parent)
                {
                    Name = name,
                    Scenario = scenario,
                    Parameters = "string p1, int p2"
                };
                tempApp1.Save(SavingMode.KeepVersion);

                return tempApp1;
            }

            // this needs to be isolated so that test content can be created freely
            ODataTest(() =>
            {
                // create at least one app with a scenario
                var parentPath1 = "/Root/(apps)/GenericContent";
                var parent = RepositoryTools.CreateStructure(parentPath1, "SystemFolder") 
                             ?? Content.Load(parentPath1);

                CreateApp(parent.ContentHandler, "app1", "SC1,SC2");
                CreateApp(parent.ContentHandler, "app2", "SC2,SC3");
                ApplicationStorage.Invalidate();

                var dar = new DefaultActionResolver();

                // check if the default action list contains the app-based client action
                var action = dar.GetActions(Content.Create(Repository.Root), null, null, 
                    new DefaultHttpContext()).Single(a => a.Name == "app1");

                Assert.AreEqual(2, action.ActionParameters.Length);
                Assert.AreEqual("p1", action.ActionParameters[0].Name);
                Assert.AreEqual("p2", action.ActionParameters[1].Name);

                // check if scenario 'SC1' contains the action
                var actions = dar.GetActions(Content.Create(Repository.Root), "SC1", null,
                    new DefaultHttpContext()).ToArray();

                Assert.AreEqual(1, actions.Length);
                Assert.IsNotNull(actions.Single(a => a.Name == "app1"));

                // check if scenario 'SC2' contains both actions
                actions = dar.GetActions(Content.Create(Repository.Root), "SC2", null,
                    new DefaultHttpContext()).ToArray();

                Assert.AreEqual(2, actions.Length);
                Assert.IsNotNull(actions.Single(a => a.Name == "app1"));
                Assert.IsNotNull(actions.Single(a => a.Name == "app2"));
            });
        }

        #region /* ===================================================================== ACTION RESOLVER MOCK */

        internal class ActionResolverSwindler : IDisposable
        {
            private readonly IActionResolver _original;
            public ActionResolverSwindler(IActionResolver actionResolver)
            {
                _original = ODataMiddleware.ActionResolver;
                Providers.Instance.SetProvider(typeof(IActionResolver), actionResolver);
            }

            public void Dispose()
            {
                Providers.Instance.SetProvider(typeof(IActionResolver), _original);
            }
        }

        internal class TestActionResolver : IActionResolver
        {
            #region Nested classes

            internal class Action1 : ActionBase
            {
                public override string Icon { get => "ActionIcon1"; set { } }
                public override string Name { get => "Action1"; set { } }
                public override string Uri => "ActionIcon1_URI";
                public override bool IsHtmlOperation => true;
                public override bool IsODataOperation => false;
                public override bool CausesStateChange => true;

                public override object Execute(Content content, params object[] parameters)
                {
                    return new Dictionary<string, object> { { "d", new Dictionary<string, object> { { "message", "Action1 executed" } } } };
                }
            }
            internal class Action2 : ActionBase
            {
                public override string Icon { get => "ActionIcon2"; set { } }
                public override string Name { get => "Action2"; set { } }
                public override string Uri => "ActionIcon2_URI";
                public override bool IsHtmlOperation => true;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => false;

                public override object Execute(Content content, params object[] parameters)
                {
                    return new Dictionary<string, object> { { "d", new Dictionary<string, object> { { "message", "Action2 executed" } } } };
                }
            }
            internal class Action3 : ActionBase
            {
                public override string Icon { get => "ActionIcon3"; set { } }
                public override string Name { get => "Action3"; set { } }
                public override string Uri => "ActionIcon3_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => true;

                public override object Execute(Content content, params object[] parameters)
                {
                    return new Dictionary<string, object> { { "d", new Dictionary<string, object> { { "message", "Action3 executed" } } } };
                }
            }
            internal class Action4 : ActionBase
            {
                public override string Icon { get => "ActionIcon4"; set { } }
                public override string Name { get => "Action4"; set { } }
                public override string Uri => "ActionIcon4_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => true;

                public override object Execute(Content content, params object[] parameters)
                {
                    return null;
                }
            }

            internal class ChildrenDefinitionFilteringTestAction : ActionBase
            {
                public override string Icon { get => "ChildrenDefinitionFilteringTestAction"; set { } }
                public override string Name { get => "ChildrenDefinitionFilteringTestAction"; set { } }
                public override string Uri => "ChildrenDefinitionFilteringTestAction_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => true;

                public override object Execute(Content content, params object[] parameters)
                {
                    return new ChildrenDefinition
                    {
                        ContentQuery = "InFolder:/Root/IMS/BuiltIn/Portal",
                        EnableAutofilters = FilterStatus.Disabled,
                        PathUsage = PathUsageMode.NotUsed,
                        Sort = new[] { new SortInfo("Name", true) },
                        Skip = 2,
                        Top = 3
                    };
                }
            }
            internal class CollectionFilteringTestAction : ActionBase
            {
                public override string Icon { get => "ActionIcon4"; set { } }
                public override string Name { get => "Action4"; set { } }
                public override string Uri => "ActionIcon4_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => true;

                public override object Execute(Content content, params object[] parameters)
                {
                    return CreateSafeContentQuery("InFolder:/Root/IMS/BuiltIn/Portal .AUTOFILTERS:OFF")
                        .Execute().Nodes.Select(Content.Create);
                }
            }

            internal class ODataActionAction : ActionBase
            {
                public override string Icon { get => "ODataActionAction"; set { } }
                public override string Name { get => "ODataActionAction"; set { } }
                public override string Uri => "ODataActionAction_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => true;

                public override object Execute(Content content, params object[] parameters)
                {
                    return "ODataAction executed.";
                }
            }
            internal class ODataFunctionAction : ActionBase
            {
                public override string Icon { get => "ODataFunctionAction"; set { } }
                public override string Name { get => "ODataFunctionAction"; set { } }
                public override string Uri => "ODataFunctionAction_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => false;

                public override object Execute(Content content, params object[] parameters)
                {
                    return "ODataFunction executed.";
                }
            }
            internal class ODataErrorAction : ActionBase
            {
                public override string Icon { get => "ODataErrorAction"; set { } }
                public override string Name { get => "ODataErrorAction"; set { } }
                public override string Uri => "ODataErrorAction_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => false;

                public override ActionParameter[] ActionParameters { get; } = {
                    new ActionParameter("errorType", typeof (string)),
                };

                public override object Execute(Content content, params object[] parameters)
                {
                    var errorType = parameters.FirstOrDefault()?.ToString();
                    switch (errorType)
                    {
                        case null:
                            return null;
                        case "ContentNotFound":
                            throw new SenseNet.ContentRepository.Storage.ContentNotFoundException("42");
                        case "SenseNetSecurityException":
                            throw new SenseNet.ContentRepository.Storage.Security.SenseNetSecurityException("");
                        case "InvalidContentActionException":
                            throw new SenseNet.ContentRepository.InvalidContentActionException("");
                        case "NodeAlreadyExistsException":
                            throw new SenseNet.ContentRepository.Storage.Data.NodeAlreadyExistsException("");
                        case "UnknownError":
                            throw new DivideByZeroException("");
                    }
                    return "ODataFunction executed.";
                }
            }
            internal class ODataGetParentChainAction : ActionBase
            {
                public override string Icon { get => ""; set { } }
                public override string Name { get => "ODataGetParentChainAction"; set { } }
                public override string Uri => "ODataContentDictionaryFunctionAction_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => false;

                public override object Execute(Content content, params object[] parameters)
                {
                    var result = new List<Content>();
                    Content c = content;
                    while (true)
                    {
                        result.Add(c);
                        var n = c.ContentHandler.Parent;
                        if (n == null)
                            break;
                        c = Content.Create(n);
                    }
                    return result;
                }
            }

            internal class ParameterEchoAction : ActionBase
            {
                public override string Icon { get => ""; set { } }
                public override string Name { get => "ParameterEchoAction"; set { } }
                public override string Uri => "ParameterEchoAction_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => false;

                public override ActionParameter[] ActionParameters { get; } = {
                    new ActionParameter("testString", typeof (string)),
                };

                public override object Execute(Content content, params object[] parameters)
                {
                    return parameters[0].ToString();
                }
            }

            internal class TestCopyToAction : ActionBase
            {
                public override string Icon { get => ""; set { } }
                public override string Name { get => "TestCopyToAction"; set { } }
                public override string Uri => "TestCopyToAction_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => false;

                public override ActionParameter[] ActionParameters { get; } = {
                    new ActionParameter("targetPath", typeof (string)),
                };

                public override object Execute(Content content, params object[] parameters)
                {
                    Node.Copy(content.Path, (string)parameters[0]);
                    return null;
                }
            }
            internal class TestMoveToAction : ActionBase
            {
                public override string Icon { get => ""; set { } }
                public override string Name { get => "TestMoveToAction"; set { } }
                public override string Uri => "TestMoveToAction_URI";
                public override bool IsHtmlOperation => false;
                public override bool IsODataOperation => true;
                public override bool CausesStateChange => false;

                public override ActionParameter[] ActionParameters { get; } = {
                    new ActionParameter("targetPath", typeof (string)),
                };

                public override object Execute(Content content, params object[] parameters)
                {
                    Node.Move(content.Path, (string)parameters[0]);
                    return null;
                }
            }

            #endregion

            public GenericScenario GetScenario(string name, string parameters, HttpContext httpContext)
            {
                return null;
            }
            public IEnumerable<ActionBase> GetActions(Content context, string scenario, string backUri, HttpContext httpContext)
            {
                return new ActionBase[] { new Action1(), new Action2(), new Action3(), new Action4() };
            }
            public ActionBase GetAction(Content context, string scenario, string actionName, string backUri, object parameters, HttpContext httpContext, IConfiguration appConfig)
            {
                switch (actionName)
                {
                    default: return null;
                    case "Action1": return new Action1();
                    case "Action2": return new Action2();
                    case "Action3": return new Action3();
                    case "Action4": return new Action4();
                    //case "GetPermissions": return new GetPermissionsAction();
                    //case "SetPermissions": return new SetPermissionsAction();
                    //case "HasPermission": return new HasPermissionAction();
                    //case "AddAspects": return new SenseNet.ApplicationModel.AspectActions.AddAspectsAction();
                    //case "RemoveAspects": return new SenseNet.ApplicationModel.AspectActions.RemoveAspectsAction();
                    //case "RemoveAllAspects": return new SenseNet.ApplicationModel.AspectActions.RemoveAllAspectsAction();
                    //case "AddFields": return new SenseNet.ApplicationModel.AspectActions.AddFieldsAction();
                    //case "RemoveFields": return new SenseNet.ApplicationModel.AspectActions.RemoveFieldsAction();
                    //case "RemoveAllFields": return new SenseNet.ApplicationModel.AspectActions.RemoveAllFieldsAction();

                    case "ChildrenDefinitionFilteringTest": return new ChildrenDefinitionFilteringTestAction();
                    case "CollectionFilteringTest": return new CollectionFilteringTestAction();

                    case "ODataAction": return new ODataActionAction();
                    case "ODataFunction": return new ODataFunctionAction();
                    case "ODataError": return new ODataErrorAction();
                    case "ODataGetParentChainAction": return new ODataGetParentChainAction();

                    case "ParameterEcho": return new ParameterEchoAction();

                    case "TestCopyTo": return new TestCopyToAction();
                    case "TestMoveTo": return new TestMoveToAction();
                }
            }
        }
        /*
        ActionBase
            Action1
            Action2
            Action3
            Action4
            PortalAction
                ClientAction
                    OpenPickerAction
                        CopyToAction
                            CopyBatchAction
                        ContentLinkBatchAction
                        MoveToAction
                            MoveBatchAction
                    ShareAction
                    DeleteBatchAction
                        DeleteAction
                    WebdavOpenAction
                    WebdavBrowseAction
                UrlAction
                    SetAsDefaultViewAction
                    PurgeFromProxyAction
                    ExpenseClaimPublishAction
                    WorkflowsAction
                    OpenLinkAction
                    BinarySpecialAction
                    AbortWorkflowAction
                    UploadAction
                    ManageViewsAction
                    ContentTypeAction
                    SetNotificationAction
                ServiceAction
                    CopyAppLocalAction
                    LogoutAction
                    UserProfileAction
                    CopyViewLocalAction
                DeleteLocalAppAction
                ExploreAction
        */
        #endregion
    }
}
