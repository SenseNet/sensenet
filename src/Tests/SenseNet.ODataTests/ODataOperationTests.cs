using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.OData;
using SenseNet.Search;
using SenseNet.Tests.Accessors;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataOperationTests : ODataTestBase
    {
        private class ActionResolverSwindler : IDisposable
        {
            private readonly IActionResolver _original;
            public ActionResolverSwindler(IActionResolver actionResolver)
            {
                _original = ODataMiddleware.ActionResolver;
                ODataMiddleware.ActionResolver = actionResolver;
            }

            public void Dispose()
            {
                ODataMiddleware.ActionResolver = _original;
            }
        }

        [TestMethod]
        public async Task OD_OP_Invoke_Action()
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
        /*[TestMethod]*/
        /*public void OData_Invoking_Actions_NoContent()
        {
            Test(() =>
            {
                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    HttpResponse response;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Action4", "",
                            output);
                        var handler = new ODataHandler();
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", Stream.Null);
                        response = pc.OwnerHttpContext.Response;
                    }
                    Assert.IsTrue(response.StatusCode == 204);
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            });
        }*/

        /*[TestMethod]*/
        /*public void OData_InvokeAction_Post_GetPutMergePatchDelete()
        {
            Test(() =>
            {
                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    string result = null;
                    ODataError error;

                        //------------------------------------------------------------ POST: ok
                        using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/ODataAction", "", output);
                        var handler = new ODataHandler();
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                        result = GetStringResult(output);
                    }
                    Assert.AreEqual("ODataAction executed.", result);

                        //------------------------------------------------------------ GET PUT MERGE PATCH DELETE: error
                        var verbs = new[] { "GET", "PUT", "MERGE", "PATCH", "DELETE" };
                    foreach (var verb in verbs)
                    {
                        using (var output = new StringWriter())
                        {
                            var pc = CreatePortalContext("/OData.svc/Root('IMS')/ODataAction", "", output);
                            var handler = new ODataHandler();
                            handler.ProcessRequest(pc.OwnerHttpContext, verb, MemoryStream.Null);
                            error = GetError(output);
                            if (error == null)
                                Assert.Fail("Exception was not thrown: " + verb);
                        }
                        Assert.AreEqual(ODataExceptionCode.IllegalInvoke, error.Code, String.Format(
                            "Error code is {0}, expected: {1}, verb: {2}"
                            , error.Code, ODataExceptionCode.IllegalInvoke, verb));
                    }
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_InvokeFunction_PostGet_PutMergePatchDelete()
        {
            Test(() =>
            {
                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    string result = null;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/ODataFunction", "", output);
                        var handler = new ODataHandler();
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                        result = GetStringResult(output);
                    }
                    Assert.AreEqual("ODataFunction executed.", result);

                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/ODataFunction", "", output);
                        var handler = new ODataHandler();
                        handler.ProcessRequest(pc.OwnerHttpContext, "GET", MemoryStream.Null);
                        result = GetStringResult(output);
                    }
                    Assert.AreEqual("ODataFunction executed.", result);

                        //------------------------------------------------------------ GET PUT MERGE PATCH DELETE: error
                        var verbs = new[] { "PUT", "MERGE", "PATCH", "DELETE" };
                    foreach (var verb in verbs)
                    {
                        ODataError error = null;
                        using (var output = new StringWriter())
                        {
                            var pc = CreatePortalContext("/OData.svc/Root('IMS')/ODataAction", "", output);
                            var handler = new ODataHandler();
                            handler.ProcessRequest(pc.OwnerHttpContext, verb, MemoryStream.Null);
                            error = GetError(output);
                            if (error == null)
                                Assert.Fail("Exception was not thrown: " + verb);
                        }
                        Assert.AreEqual(ODataExceptionCode.IllegalInvoke, error.Code, String.Format(
                            "Error code is {0}, expected: {1}, verb: {2}"
                            , error.Code, ODataExceptionCode.IllegalInvoke, verb));
                    }
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_InvokeDictionaryHandlerFunction()
        {
            Test(() =>
            {
                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    ODataEntities result = null;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(
                            "/OData.svc/Root/System/Schema/ContentTypes/GenericContent('FieldSettingContent')/ODataGetParentChainAction"
                            , "metadata=no&$select=Id,Name&$top=2&$inlinecount=allpages", output);
                        var handler = new ODataHandler();
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                        result = GetEntities(output);
                    }
                    Assert.AreEqual(6, result.TotalCount);
                    Assert.AreEqual(2, result.Length);
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            });
        }*/

        /* =========================================================================== ACTION RESOLVER */

        internal class TestActionResolver : IActionResolver
        {
            internal class Action1 : ActionBase
            {
                public override string Icon { get { return "ActionIcon1"; } set { } }
                public override string Name { get { return "Action1"; } set { } }
                public override string Uri { get { return "ActionIcon1_URI"; } }
                public override bool IsHtmlOperation { get { return true; } }
                public override bool IsODataOperation { get { return false; } }
                public override bool CausesStateChange { get { return true; } }
                public override object Execute(Content content, params object[] parameters)
                {
                    return new Dictionary<string, object> { { "d", new Dictionary<string, object> { { "message", "Action1 executed" } } } };
                }
            }
            internal class Action2 : ActionBase
            {
                public override string Icon { get { return "ActionIcon2"; } set { } }
                public override string Name { get { return "Action2"; } set { } }
                public override string Uri { get { return "ActionIcon2_URI"; } }
                public override bool IsHtmlOperation { get { return true; } }
                public override bool IsODataOperation { get { return true; } }
                public override bool CausesStateChange { get { return false; } }
                public override object Execute(Content content, params object[] parameters)
                {
                    return new Dictionary<string, object> { { "d", new Dictionary<string, object> { { "message", "Action2 executed" } } } };
                }
            }
            internal class Action3 : ActionBase
            {
                public override string Icon { get { return "ActionIcon3"; } set { } }
                public override string Name { get { return "Action3"; } set { } }
                public override string Uri { get { return "ActionIcon3_URI"; } }
                public override bool IsHtmlOperation { get { return false; } }
                public override bool IsODataOperation { get { return true; } }
                public override bool CausesStateChange { get { return true; } }
                public override object Execute(Content content, params object[] parameters)
                {
                    return new Dictionary<string, object> { { "d", new Dictionary<string, object> { { "message", "Action3 executed" } } } };
                }
            }
            internal class Action4 : ActionBase
            {
                public override string Icon { get { return "ActionIcon4"; } set { } }
                public override string Name { get { return "Action4"; } set { } }
                public override string Uri { get { return "ActionIcon4_URI"; } }
                public override bool IsHtmlOperation { get { return false; } }
                public override bool IsODataOperation { get { return true; } }
                public override bool CausesStateChange { get { return true; } }
                public override object Execute(Content content, params object[] parameters)
                {
                    return null;
                }
            }

            internal class ChildrenDefinitionFilteringTestAction : ActionBase
            {
                public override string Icon { get { return "ChildrenDefinitionFilteringTestAction"; } set { } }
                public override string Name { get { return "ChildrenDefinitionFilteringTestAction"; } set { } }
                public override string Uri { get { return "ChildrenDefinitionFilteringTestAction_URI"; } }
                public override bool IsHtmlOperation { get { return false; } }
                public override bool IsODataOperation { get { return true; } }
                public override bool CausesStateChange { get { return true; } }
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
                public override string Icon { get { return "ActionIcon4"; } set { } }
                public override string Name { get { return "Action4"; } set { } }
                public override string Uri { get { return "ActionIcon4_URI"; } }
                public override bool IsHtmlOperation { get { return false; } }
                public override bool IsODataOperation { get { return true; } }
                public override bool CausesStateChange { get { return true; } }
                public override object Execute(Content content, params object[] parameters)
                {
                    return CreateSafeContentQuery("InFolder:/Root/IMS/BuiltIn/Portal .AUTOFILTERS:OFF")
                        .Execute().Nodes.Select(Content.Create);
                }
            }

            internal class ODataActionAction : ActionBase
            {
                public override string Icon { get { return "ODataActionAction"; } set { } }
                public override string Name { get { return "ODataActionAction"; } set { } }
                public override string Uri { get { return "ODataActionAction_URI"; } }
                public override bool IsHtmlOperation { get { return false; } }
                public override bool IsODataOperation { get { return true; } }
                public override bool CausesStateChange { get { return true; } }
                public override object Execute(Content content, params object[] parameters)
                {
                    return "ODataAction executed.";
                }
            }
            internal class ODataFunctionAction : ActionBase
            {
                public override string Icon { get { return "ODataFunctionAction"; } set { } }
                public override string Name { get { return "ODataFunctionAction"; } set { } }
                public override string Uri { get { return "ODataFunctionAction_URI"; } }
                public override bool IsHtmlOperation { get { return false; } }
                public override bool IsODataOperation { get { return true; } }
                public override bool CausesStateChange { get { return false; } }
                public override object Execute(Content content, params object[] parameters)
                {
                    return "ODataFunction executed.";
                }
            }
            internal class ODataGetParentChainAction : ActionBase
            {
                public override string Icon { get { return ""; } set { } }
                public override string Name { get { return "ODataGetParentChainAction"; } set { } }
                public override string Uri { get { return "ODataContentDictionaryFunctionAction_URI"; } }
                public override bool IsHtmlOperation { get { return false; } }
                public override bool IsODataOperation { get { return true; } }
                public override bool CausesStateChange { get { return false; } }
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

            public GenericScenario GetScenario(string name, string parameters, HttpContext httpContext)
            {
                return null;
            }
            public IEnumerable<ActionBase> GetActions(Content context, string scenario, string backUri, HttpContext httpContext)
            {
                return new ActionBase[] { new Action1(), new Action2(), new Action3(), new Action4() };
            }
            public ActionBase GetAction(Content context, string scenario, string actionName, string backUri, object parameters, HttpContext httpContext)
            {
                switch (actionName)
                {
                    default: return null;
                    case "Action1": return new Action1();
                    case "Action2": return new Action2();
                    case "Action3": return new Action3();
                    case "Action4": return new Action4();
                    //case "GetPermissions": return new GetPermissionsAction();
                    //case "SetPermissions": return new SenseNet.Portal.ApplicationModel.SetPermissionsAction();
                    //case "HasPermission": return new SenseNet.Portal.ApplicationModel.HasPermissionAction();
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

                    case "ODataGetParentChainAction": return new ODataGetParentChainAction();

                    //case "CopyTo": return new CopyToAction();
                    //case "MoveTo": return new MoveToAction();
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
    }
}
