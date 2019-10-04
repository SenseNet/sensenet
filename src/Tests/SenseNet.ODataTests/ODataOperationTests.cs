using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.OData;
using SenseNet.OData.Operations;
using SenseNet.Search;
using Task = System.Threading.Tasks.Task;
// ReSharper disable StringLiteralTypo

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataOperationTests : ODataTestBase
    {
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

        // 
        #region /* ===================================================================== ACTION RESOLVER */

        internal class ActionResolverSwindler : IDisposable
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

        internal class TestActionResolver : IActionResolver
        {
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
                    case "GetPermissions": return new GetPermissionsAction();
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
                    case "ODataError": return new ODataErrorAction();
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
        #endregion
    }
}
