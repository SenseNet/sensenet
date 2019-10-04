using Compatibility.SenseNet.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataSecurityTests : ODataTestBase
    {
        [TestMethod]
        public async Task OD_Security_1()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_GetPermissions_ACL()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    //Result: {
                    //    "id": 42,
                    //    "path": "/Root/....",
                    //    "inherits": true,
                    //    "entries": [
                    //        {
                    //            "identity": { "path": "/Root/...", ...
                    //            "permissions": {
                    //                "See": {...
                    SnAclEditor.Create(new SnSecurityContext(User.Current))
                        .Allow(2, 1, false, PermissionType.Custom01)
                        .Apply();

                    // ACTION
                    var response = await ODataPostAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/GetPermissions",
                            "",
                            null)
                        .ConfigureAwait(false);

                    // ASSERT
                    AssertNoError(response);
                    var json = Deserialize(response.Result);
                    var entries = json["entries"];
                    Assert.IsNotNull(entries);

                    var expected = "Custom01:{value:allow,from:/Root,identity:/Root/IMS/BuiltIn/Portal/Admin}";
                    var actual = response.Result
                        .Replace("\r", "")
                        .Replace("\n", "")
                        .Replace("\t", "")
                        .Replace(" ", "")
                        .Replace("\"", "");
                    Assert.IsTrue(actual.Contains(expected));
                }
            }).ConfigureAwait(false);
        }
        /*[TestMethod]*/
        /*public async Task OD_Security_GetPermissions_ACE()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
                  //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/GetPermissions
                    //Stream: {identity:"/root/ims/builtin/portal/visitor"}
                    //Result: {
                    //    "identity": { "id:": 7,  "path": "/Root/IMS/BuiltIn/Portal/Administrators",…},
                    //    "permissions": {
                    //        "See": { "value": "allow", "from": "/root" }
                    //       ...

                    var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    JContainer json;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/GetPermissions", "", output);
                        var handler = new ODataHandler();
                        var stream = CreateRequestStream("{identity:\"/root/ims/builtin/portal/visitor\"}");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        json = Deserialize(output);
                    }
                    var identity = json[0]["identity"];
                    var permissions = json[0]["permissions"];
                    Assert.IsTrue(identity != null);
                    Assert.IsTrue(permissions != null);
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            }).ConfigureAwait(false);
        }*/

        /*[TestMethod]*/
        /*public async Task OD_Security_HasPermission_Administrator()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
                    //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
                    //Stream: {user:"/root/ims/builtin/portal/admin", permissions:["Open","Save"] }
                    //result: true

                    SecurityHandler.CreateAclEditor()
                    .Allow(Repository.Root.Id, Group.Administrators.Id, false, PermissionType.Open)
                    .Allow(Repository.Root.Id, Group.Administrators.Id, false, PermissionType.Save)
                    .Apply();

                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                var hasPermission = SecurityHandler.HasPermission(
                    User.Administrator, Group.Administrators, PermissionType.Open, PermissionType.Save);
                Assert.IsTrue(hasPermission);

                CreateTestSite();
                try
                {
                    string result;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
                        var handler = new ODataHandler();
                        var stream = CreateRequestStream(String.Concat("{user:\"", User.Administrator.Path,
                            "\", permissions:[\"Open\",\"Save\"] }"));
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        result = GetStringResult(output);
                    }
                    Assert.AreEqual("true", result);
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_HasPermission_Visitor()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
                    //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
                    //Stream: {user:"/root/ims/builtin/portal/visitor", permissions:["Open","Save"] }
                    //result: false

                    var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    string result;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
                        var handler = new ODataHandler();
                        var stream =
                            CreateRequestStream(
                                "{user:\"/root/ims/builtin/portal/visitor\", permissions:[\"Open\",\"Save\"] }");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        result = GetStringResult(output);
                    }
                    Assert.IsTrue(result == "false");
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_HasPermission_NullUser()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
                    //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
                    //Stream: {user:null, permissions:["Open","Save"] }
                    //result: true

                    var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    string result;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
                        var handler = new ODataHandler();
                        var stream = CreateRequestStream("{user:null, permissions:[\"Open\",\"Save\"] }");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        result = GetStringResult(output);
                    }
                    Assert.IsTrue(result == "true");
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_HasPermission_WithoutUser()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
                    //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
                    //Stream: {permissions:["Open","Save"] }
                    //result: true

                    var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    string result;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
                        var handler = new ODataHandler();
                        var stream = CreateRequestStream("{permissions:[\"Open\",\"Save\"] }");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        result = GetStringResult(output);
                    }
                    Assert.IsTrue(result == "true");
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_HasPermission_Error_IdentityNotFound()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
                    //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
                    //Stream: {user:"/root/ims/builtin/portal/nobody", permissions:["Open","Save"] }
                    //result: ERROR: ODataException: Content not found: /root/ims/builtin/portal/nobody

                    var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    ODataError error;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
                        var handler = new ODataHandler();
                        var stream =
                            CreateRequestStream(
                                "{user:\"/root/ims/builtin/portal/nobody\", permissions:[\"Open\",\"Save\"] }");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        error = GetError(output);
                    }
                    Assert.IsTrue(error.Code == ODataExceptionCode.ResourceNotFound);
                    Assert.IsTrue(error.Message == "Identity not found: /root/ims/builtin/portal/nobody");
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_HasPermission_Error_UnknownPermission()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
                    //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
                    //Stream: {permissions:["Open","Save1"] }
                    //result: ERROR: ODataException: Unknown permission: Save1

                    var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    ODataError error;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
                        var handler = new ODataHandler();
                        var stream = CreateRequestStream("{permissions:[\"Open\",\"Save1\"] }");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        error = GetError(output);
                    }
                    Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified);
                    Assert.IsTrue(error.Message == "Unknown permission: Save1");
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_HasPermission_Error_MissingParameter()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
                    //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
                    //Stream:
                    //result: ERROR: "ODataException: Value cannot be null.\\nParameter name: permissions

                    var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    ODataError error;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
                        var handler = new ODataHandler();
                            //var stream = CreateRequestStream("{user:\"/root/ims/builtin/portal/nobody\", permissions:[\"Open\",\"Save\"] }");
                            handler.ProcessRequest(pc.OwnerHttpContext, "POST", null);
                        error = GetError(output);
                    }
                    Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified);
                    Assert.IsTrue(error.Message == "Value cannot be null.\\nParameter name: permissions");
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            }).ConfigureAwait(false);
        }*/

        /*[TestMethod]*/
        /*public async Task OD_Security_SetPermissions()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
                    //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/SetPermission
                    //Stream: {r:[{identity:"/Root/IMS/BuiltIn/Portal/Visitor", OpenMinor:"allow", Save:"deny"},{identity:"/Root/IMS/BuiltIn/Portal/Creators", Custom16:"A", Custom17:"1"}]}
                    //result: (nothing)

                    InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.Save();
                var resourcePath = ODataHandler.GetEntityUrl(content.Path);

                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    string result;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
                            output);
                        var handler = new ODataHandler();
                        var stream =
                            CreateRequestStream(
                                "{r:[{identity:\"/Root/IMS/BuiltIn/Portal/Visitor\", OpenMinor:\"allow\", Save:\"deny\"},{identity:\"/Root/IMS/BuiltIn/Portal/Owners\", Custom16:\"A\", Custom17:\"1\"}]}");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        result = GetStringResult(output);
                    }
                    Assert.IsTrue(result.Length == 0);
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                    content.DeletePhysical();
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_SetPermissions_NotPropagates()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
                    //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/SetPermission
                    //Stream: {r:[{identity:"/Root/IMS/BuiltIn/Portal/Visitor", OpenMinor:"allow", Save:"deny"},{identity:"/Root/IMS/BuiltIn/Portal/Creators", Custom16:"A", Custom17:"1"}]}
                    //result: (nothing)

                    InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var content = Content.CreateNew("Folder", testRoot, Guid.NewGuid().ToString());
                content.Save();
                var folderPath = ODataHandler.GetEntityUrl(content.Path);
                var folderRepoPath = content.Path;
                content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.Save();
                var carRepoPath = content.Path;

                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    string result;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(string.Concat("/OData.svc/", folderPath, "/SetPermissions"), "",
                            output);
                        var handler = new ODataHandler();
                        var stream =
                            CreateRequestStream(
                                "{r:[{identity:\"/Root/IMS/BuiltIn/Portal/Visitor\", OpenMinor:\"allow\", propagates:false}]}");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        result = GetStringResult(output);
                    }
                    Assert.IsTrue(result.Length == 0);
                    var folder = Node.LoadNode(folderRepoPath);
                    var car = Node.LoadNode(carRepoPath);

                    Assert.IsTrue(folder.Security.HasPermission((IUser)User.Visitor, PermissionType.OpenMinor));
                    Assert.IsFalse(car.Security.HasPermission((IUser)User.Visitor, PermissionType.OpenMinor));
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                    content.DeletePhysical();
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_Break()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.Save();
                var resourcePath = ODataHandler.GetEntityUrl(content.Path);

                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    string result;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
                            output);
                        var handler = new ODataHandler();
                        var stream = CreateRequestStream("{inheritance:\"break\"}");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        result = GetStringResult(output);
                    }
                    Assert.IsTrue(result.Length == 0);
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                    content.DeletePhysical();
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_Unbreak()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.Save();
                var resourcePath = ODataHandler.GetEntityUrl(content.Path);

                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    string result;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
                            output);
                        var handler = new ODataHandler();
                        var stream = CreateRequestStream("{inheritance:\"unbreak\"}");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        result = GetStringResult(output);
                    }
                    Assert.IsTrue(result.Length == 0);
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                    content.DeletePhysical();
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_Error_MissingStream()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.Save();
                var resourcePath = ODataHandler.GetEntityUrl(content.Path);

                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    ODataError error;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
                            output);
                        var handler = new ODataHandler();
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", null);
                        error = GetError(output);
                    }
                    var expectedMessage = "Value cannot be null.\\nParameter name: stream";
                    Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified);
                    Assert.IsTrue(error.Message == expectedMessage);
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_Error_BothParameters()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.Save();
                var resourcePath = ODataHandler.GetEntityUrl(content.Path);

                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    ODataError error;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
                            output);
                        var handler = new ODataHandler();
                        var stream =
                            CreateRequestStream(
                                "{r:[{identity:\"/Root/IMS/BuiltIn/Portal/Visitor\", OpenMinor:\"allow\"}], inheritance:\"break\"}");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        error = GetError(output);
                    }
                    var expectedMessage = "Cannot use  r  and  inheritance  parameters at the same time.";
                    Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified);
                    Assert.IsTrue(error.Message == expectedMessage);
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                    content.DeletePhysical();
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_Error_InvalidInheritanceParam()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                }
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.Save();
                var resourcePath = ODataHandler.GetEntityUrl(content.Path);

                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    ODataError error;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
                            output);
                        var handler = new ODataHandler();
                        var stream = CreateRequestStream("{inheritance:\"dance\"}");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        error = GetError(output);
                    }
                    var expectedMessage = "The value of the  inheritance  must be  break  or  unbreak .";
                    Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified);
                    Assert.IsTrue(error.Message == expectedMessage);
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                    content.DeletePhysical();
                }
            }).ConfigureAwait(false);
        }*/

    }
}
