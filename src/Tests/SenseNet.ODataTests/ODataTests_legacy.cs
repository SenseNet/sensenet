using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.ApplicationModel;
using SenseNet.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using SenseNet.Configuration;
using SenseNet.OData;
using SenseNet.Security;

// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeThisQualifier

namespace SenseNet.ODataTests
{
    #region Additional classes

    public class ODataTestsCustomActions
    {
        [ODataAction]
        public static string ParameterEcho(Content content, string testString)
        {
            return testString;
        }
    }

    internal class ODataFilterTestHelper
    {
        public static string TestValue => "Administrators";

        internal class A
        {
            internal class B
            {
                // ReSharper disable once MemberHidesStaticFromOuterClass
                public static string TestValue { get; } = "Administrators";
            }
        }
    }

    [ContentHandler]
    public class OData_Filter_ThroughReference_ContentHandler : GenericContent
    {
        public const string CTD = @"<?xml version='1.0' encoding='utf-8'?>
    <ContentType name='OData_Filter_ThroughReference_ContentHandler' parentType='GenericContent' handler='SenseNet.ODataTests.OData_Filter_ThroughReference_ContentHandler' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
      <Fields>
        <Field name='References' type='Reference'>
          <Configuration>
            <AllowMultiple>true</AllowMultiple>
            <AllowedTypes>
              <Type>OData_Filter_ThroughReference_ContentHandler</Type>
            </AllowedTypes>
          </Configuration>
        </Field>
      </Fields>
    </ContentType>
    ";
        public OData_Filter_ThroughReference_ContentHandler(Node parent) : this(parent, null) { }
        public OData_Filter_ThroughReference_ContentHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected OData_Filter_ThroughReference_ContentHandler(NodeToken token) : base(token) { }

        public const string REFERENCES = "References";
        [RepositoryProperty(REFERENCES, RepositoryDataType.Reference)]
        public IEnumerable<Node> References
        {
            get { return this.GetReferences(REFERENCES); }
            set { this.SetReferences(REFERENCES, value); }
        }

    }
    [ContentHandler]
    public class OData_ReferenceTest_ContentHandler : GenericContent
    {
        public const string CTD = @"<?xml version='1.0' encoding='utf-8'?>
    <ContentType name='OData_ReferenceTest_ContentHandler' parentType='GenericContent' handler='SenseNet.ODataTests.OData_ReferenceTest_ContentHandler' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
      <Fields>
        <Field name='Reference' type='Reference'>
          <Configuration>
            <AllowMultiple>false</AllowMultiple>
          </Configuration>
        </Field>
        <Field name='References' type='Reference'>
          <Configuration>
            <AllowMultiple>true</AllowMultiple>
          </Configuration>
        </Field>
        <Field name='Reference2' type='Reference'>
          <Configuration>
            <AllowMultiple>false</AllowMultiple>
          </Configuration>
        </Field>
      </Fields>
    </ContentType>
    ";
        public OData_ReferenceTest_ContentHandler(Node parent) : this(parent, null) { }
        public OData_ReferenceTest_ContentHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected OData_ReferenceTest_ContentHandler(NodeToken token) : base(token) { }

        public const string REFERENCES = "References";
        [RepositoryProperty(REFERENCES, RepositoryDataType.Reference)]
        public IEnumerable<Node> References
        {
            get { return this.GetReferences(REFERENCES); }
            set { this.SetReferences(REFERENCES, value); }
        }

        public const string REFERENCE = "Reference";
        [RepositoryProperty(REFERENCE, RepositoryDataType.Reference)]
        public Node Reference
        {
            get { return this.GetReference<Node>(REFERENCE); }
            set { this.SetReference(REFERENCE, value); }
        }

        public const string REFERENCE2 = "Reference2";
        [RepositoryProperty(REFERENCE2, RepositoryDataType.Reference)]
        public Node Reference2
        {
            get { return this.GetReference<Node>(REFERENCE2); }
            set { this.SetReference(REFERENCE2, value); }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case REFERENCE: return this.Reference;
                case REFERENCE2: return this.Reference2;
                case REFERENCES: return this.References;
                default: return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case REFERENCE: this.Reference = (Node)value; break;
                case REFERENCE2: this.Reference2 = (Node)value; break;
                case REFERENCES: this.References = (IEnumerable<Node>)value; break;
                default: base.SetProperty(name, value); break;
            }
        }
    }
    #endregion

    [TestClass]
    public class ODataTests_legacy : ODataTestBase
    {
        #region Playground

        public static void CreateGlobalActions()
        {
            InitializePlayground();

            var rootAppsFolder = Node.Load<Folder>("/Root/(apps)");
            if (rootAppsFolder == null)
            {
                rootAppsFolder = new SystemFolder(Repository.Root);
                rootAppsFolder.Name = "(apps)";
                rootAppsFolder.Save();
            }
            var rootAppsGenericContentFolder = Node.Load<Folder>("/Root/(apps)/GenericContent");
            if (rootAppsGenericContentFolder == null)
            {
                rootAppsGenericContentFolder = new SystemFolder(rootAppsFolder);
                rootAppsGenericContentFolder.Name = "GenericContent";
                rootAppsGenericContentFolder.Save();
            }
            var rootAppsGenericContent_ParameterEcho = Node.Load<GenericODataApplication>("/Root/(apps)/GenericContent/ParameterEcho");
            if (rootAppsGenericContent_ParameterEcho == null)
            {
                rootAppsGenericContent_ParameterEcho = new GenericODataApplication(rootAppsGenericContentFolder);
                rootAppsGenericContent_ParameterEcho.Name = "ParameterEcho";
                rootAppsGenericContent_ParameterEcho.ClassName = "SenseNet.ODataTests.ODataTestsCustomActions";
                rootAppsGenericContent_ParameterEcho.MethodName = "ParameterEcho";
                rootAppsGenericContent_ParameterEcho.Parameters = "string testString";
                rootAppsGenericContent_ParameterEcho.Save();
            }

            ApplicationStorage.Invalidate();
        }

        private static void InitializePlayground()
        {
            //EnsureReferenceTestStructure();

            var content = Content.Create(User.Administrator);
            if (((IEnumerable<Node>)content["Manager"]).Any())
                return;
            content["Manager"] = User.Administrator;
            content["Email"] = "anybody@somewhere.com";
            content.Save();
        }


        #endregion

        #region [TestMethod] 2 SnJsonConverterTest_

        /*[TestMethod]*/
        /*public void SnJsonConverterTest_SimpleProjection()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                    // Create, save
                    var content = Content.CreateNew("Car", testRoot, "MyCar1");
                content["Make"] = "Citroen";
                content["Model"] = "C100";
                content["Price"] = 2399999.99;
                content.Save();

                    // Reload
                    content = Content.Load(content.Path);
                    // Generate JSON
                    var generatedJson = content.ToJson(new[] { "Id", "Path", "Name", "Make", "Model", "Price" }, null);

                    // Run assertions
                    var jobj = JObject.Parse(generatedJson);
                Assert.AreEqual(jobj["Id"], content.Id);
                Assert.AreEqual(jobj["Path"], content.Path);
                Assert.AreEqual(jobj["Name"], content.Name);
                Assert.AreEqual(jobj["Make"].Value<string>(), content["Make"]);
                Assert.AreEqual(jobj["Model"].Value<string>(), content["Model"]);
                Assert.AreEqual(jobj["Price"].Value<decimal>(), content["Price"]);
            });
        }*/
        /*[TestMethod]*/
        /*public void SnJsonConverterTest_WithExpand()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                    // Create, save
                    var content = Content.CreateNew("Car", testRoot, "MyCar2");
                content["Make"] = "Citroen";
                content["Model"] = "C101";
                content["Price"] = 4399999.99;
                content.Save();

                    // Reload
                    content = Content.Load(content.Path);
                    // Generate JSON
                    var generatedJson =
                    content.ToJson(
                        new[] { "Id", "Path", "Name", "Make", "Model", "Price", "CreatedBy/Id", "CreatedBy/Path" },
                        new[] { "CreatedBy" });

                    // Run assertions
                    var jobj = JObject.Parse(generatedJson);
                Assert.AreEqual(jobj["Id"], content.Id);
                Assert.AreEqual(jobj["Path"], content.Path);
                Assert.AreEqual(jobj["Name"], content.Name);
                Assert.AreEqual(jobj["Make"].Value<string>(), content["Make"]);
                Assert.AreEqual(jobj["Model"].Value<string>(), content["Model"]);
                Assert.AreEqual(jobj["Price"].Value<decimal>(), content["Price"]);
                Assert.AreEqual(jobj["CreatedBy"]["Id"], content.ContentHandler.CreatedBy.Id);
                Assert.AreEqual(jobj["CreatedBy"]["Path"], content.ContentHandler.CreatedBy.Path);
            });
        }*/
        #endregion


        /*[TestMethod]*/
        /*public void OData_Urls_CurrentSite()
        {
            Test(() =>
            {
                var site = CreateTestSite();
                var siteParentPath = RepositoryPath.GetParentPath(site.Path);
                var siteName = RepositoryPath.GetFileName(site.Path);

                string expectedJson = string.Concat(@"{""d"":{
                        ""__metadata"":{                    ""uri"":""/odata.svc", siteParentPath, @"('", siteName, @"')"",""type"":""Site""},
                        ""Manager"":{""__deferred"":{       ""uri"":""/odata.svc", siteParentPath, @"('", siteName, @"')/Manager""}},
                        ""CreatedBy"":{""__deferred"":{     ""uri"":""/odata.svc", siteParentPath, @"('", siteName, @"')/CreatedBy""}},
                        ""ModifiedBy"":{""__deferred"":{    ""uri"":""/odata.svc", siteParentPath, @"('", siteName, @"')/ModifiedBy""}}}}")
                    .Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
                string json;
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext(ODataTools.GetODataUrl(site.Path),
                        "$select=Manager,CreatedBy,ModifiedBy&metadata=minimal", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    json = GetStringResult(output);
                }
                var result = json.Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(expectedJson, result);
            });
        }*/

        /* --------------------------------------------------------------------------------------------------- */

        /*internal class TestActionResolver : IActionResolver
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

            public GenericScenario GetScenario(string name, string parameters)
            {
                return null;
            }
            public IEnumerable<ActionBase> GetActions(Content context, string scenario, string backUri)
            {
                return new ActionBase[] { new Action1(), new Action2(), new Action3(), new Action4() };
            }
            public ActionBase GetAction(Content context, string scenario, string actionName, string backUri, object parameters)
            {
                switch (actionName)
                {
                    default: return null;
                    case "Action1": return new Action1();
                    case "Action2": return new Action2();
                    case "Action3": return new Action3();
                    case "Action4": return new Action4();
                    case "GetPermissions": return new GetPermissionsAction();
                    case "SetPermissions": return new SenseNet.Portal.ApplicationModel.SetPermissionsAction();
                    case "HasPermission": return new SenseNet.Portal.ApplicationModel.HasPermissionAction();
                    case "AddAspects": return new SenseNet.ApplicationModel.AspectActions.AddAspectsAction();
                    case "RemoveAspects": return new SenseNet.ApplicationModel.AspectActions.RemoveAspectsAction();
                    case "RemoveAllAspects": return new SenseNet.ApplicationModel.AspectActions.RemoveAllAspectsAction();
                    case "AddFields": return new SenseNet.ApplicationModel.AspectActions.AddFieldsAction();
                    case "RemoveFields": return new SenseNet.ApplicationModel.AspectActions.RemoveFieldsAction();
                    case "RemoveAllFields": return new SenseNet.ApplicationModel.AspectActions.RemoveAllFieldsAction();

                    case "ChildrenDefinitionFilteringTest": return new ChildrenDefinitionFilteringTestAction();
                    case "CollectionFilteringTest": return new CollectionFilteringTestAction();

                    case "ODataAction": return new ODataActionAction();
                    case "ODataFunction": return new ODataFunctionAction();

                    case "ODataGetParentChainAction": return new ODataGetParentChainAction();

                    case "CopyTo": return new CopyToAction();
                    case "MoveTo": return new MoveToAction();
                }
            }
        }*/
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

        /* --------------------------------------------------------------------------------------------------- */

        /*[TestMethod]*/
        /*public void OData_Security_GetPermissions_ACL()
        {
            Test(() =>
            {
                    //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/GetPermissions
                    //Stream:
                    //Result: {
                    //    "id": 4108,
                    //    "path": "/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Document_Library",
                    //    "inherits": true,
                    //    "entries": [
                    //        {
                    //            "identity": { "path": "/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Groups/Owners", ...
                    //            "permissions": {
                    //                "See": {...

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
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", Stream.Null);
                        json = Deserialize(output);
                    }
                    var entries = json["entries"];
                    Assert.IsNotNull(entries);
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_Security_GetPermissions_ACE()
        {
            Test(() =>
            {
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
            });
        }*/

        /*[TestMethod]*/
        /*public void OData_Security_HasPermission_Administrator()
        {
            Test(() =>
            {
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
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_Security_HasPermission_Visitor()
        {
            Test(() =>
            {
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
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_Security_HasPermission_NullUser()
        {
            Test(() =>
            {
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
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_Security_HasPermission_WithoutUser()
        {
            Test(() =>
            {
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
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_Security_HasPermission_Error_IdentityNotFound()
        {
            Test(() =>
            {
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
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_Security_HasPermission_Error_UnknownPermission()
        {
            Test(() =>
            {
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
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_Security_HasPermission_Error_MissingParameter()
        {
            Test(() =>
            {
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
            });
        }*/

        /*[TestMethod]*/
        /*public void OData_Security_SetPermissions()
        {
            Test(() =>
            {
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
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_Security_SetPermissions_NotPropagates()
        {
            Test(() =>
            {
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
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_Security_Break()
        {
            Test(() =>
            {
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
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_Security_Unbreak()
        {
            Test(() =>
            {
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
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_Security_Error_MissingStream()
        {
            Test(() =>
            {
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
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_Security_Error_BothParameters()
        {
            Test(() =>
            {
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
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_Security_Error_InvalidInheritanceParam()
        {
            Test(() =>
            {
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
            });
        }*/

        /* --------------------------------------------------------------------------------------------------- */

        //TODO: Remove inconclusive test result and implement this test.
        /*//[TestMethod]
        /*public void OData_Metadata_Instance_Entity()
        {
            Assert.Inconclusive("InMemorySchemaWriter.CreatePropertyType is partially implemented.");

            Test(() =>
            {
                string listDef = @"<?xml version='1.0' encoding='utf-8'?>
    <ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'>
    	<Fields>
    		<ContentListField name='#ListField1' type='ShortText'>
    			<Configuration>
    				<MaxLength>42</MaxLength>
    			</Configuration>
    		</ContentListField>
    	</Fields>
    </ContentListDefinition>
    ";
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var listContent = Content.CreateNew("ContentList", testRoot, Guid.NewGuid().ToString());
                var list = (ContentList)listContent.ContentHandler;
                list.AllowChildTypes(new[]
                    {ContentType.GetByName("Folder"), ContentType.GetByName("File"), ContentType.GetByName("Car")});
                list.ContentListDefinition = listDef;
                listContent.Save();

                var itemFolder = Content.CreateNew("Folder", listContent.ContentHandler, Guid.NewGuid().ToString());
                itemFolder.Save();
                var itemContent = Content.CreateNew("Car", itemFolder.ContentHandler, Guid.NewGuid().ToString());
                itemContent.Save();

                CreateTestSite();

                XmlNamespaceManager nsmgr;
                XmlDocument metaXml;
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext(
                        String.Concat("/OData.svc", ODataHandler.GetEntityUrl(itemContent.Path), "/$metadata"), "",
                        output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    output.Flush();
                    var src = GetStringResult(output);
                    metaXml = GetMetadataXml(src, out nsmgr);
                }
                var allTypes = metaXml.SelectNodes("//x:EntityType", nsmgr);
                Assert.IsTrue(allTypes.Count == 1);
                var listProps = metaXml.SelectNodes("//x:EntityType/x:Property[@Name='#ListField1']", nsmgr);
                Assert.IsTrue(listProps.Count == 1);
            });
        }*/

        //TODO: Remove inconclusive test result and implement this test.
        /*//[TestMethod]
        /*public void OData_Metadata_Instance_Collection()
        {
            Assert.Inconclusive("InMemorySchemaWriter.CreatePropertyType is partially implemented.");

            Test(() =>
            {
                string listDef = @"<?xml version='1.0' encoding='utf-8'?>
    <ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'>
    	<Fields>
    		<ContentListField name='#ListField1' type='ShortText'>
    			<Configuration>
    				<MaxLength>42</MaxLength>
    			</Configuration>
    		</ContentListField>
    	</Fields>
    </ContentListDefinition>
    ";
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var listContent = Content.CreateNew("ContentList", testRoot, Guid.NewGuid().ToString());
                var list = (ContentList)listContent.ContentHandler;
                list.AllowChildTypes(new[]
                    {ContentType.GetByName("Folder"), ContentType.GetByName("File"), ContentType.GetByName("Car")});
                list.ContentListDefinition = listDef;
                listContent.Save();

                var itemFolder = Content.CreateNew("Folder", listContent.ContentHandler, Guid.NewGuid().ToString());
                itemFolder.Save();

                CreateTestSite();

                XmlNamespaceManager nsmgr;
                XmlDocument metaXml;
                string src = null;
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext(String.Concat("/OData.svc", listContent.Path, "/$metadata"), "",
                        output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    output.Flush();
                    src = GetStringResult(output);
                    metaXml = GetMetadataXml(src, out nsmgr);
                }
                var allTypes = metaXml.SelectNodes("//x:EntityType", nsmgr);
                Assert.IsTrue(allTypes.Count > 1);
                var listProps = metaXml.SelectNodes("//x:EntityType/x:Property[@Name='#ListField1']", nsmgr);
                Assert.IsTrue(listProps.Count == allTypes.Count);
            });
        }*/

        /* --------------------------------------------------------------------------------------------------- */

        /*[TestMethod]*/
        /*public void OData_FilteringAndPartitioningOperationResult_ChildrenDefinition()
        {
            Test(() =>
            {
                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    ODataEntities entities;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/ChildrenDefinitionFilteringTest", "",
                            output);
                        var handler = new ODataHandler();
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                        entities = GetEntities(output);
                    }
                    var ids = String.Join(", ", entities.Select(e => e.Id));
                    var expids = String.Join(", ", CreateSafeContentQuery("InFolder:/Root/IMS/BuiltIn/Portal .AUTOFILTERS:OFF .REVERSESORT:Name .SKIP:2 .TOP:3")
                        .Execute().Identifiers);
                        // 8, 9, 7
                        Assert.AreEqual(expids, ids);
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_FilteringAndPartitioningOperationResult_ContentCollection()
        {
            Test(() =>
            {
                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    ODataEntities entities;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/CollectionFilteringTest",
                            "$skip=1&$top=3&$orderby=Name desc&$select=Id,Name&$filter=Id ne 10&metadata=no", output);
                        var handler = new ODataHandler();
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                        entities = GetEntities(output);
                    }
                    var ids = String.Join(", ", entities.Select(e => e.Id));
                    var expids = String.Join(", ",
                        CreateSafeContentQuery("+InFolder:/Root/IMS/BuiltIn/Portal -Id:10 .AUTOFILTERS:OFF .REVERSESORT:Name .SKIP:1 .TOP:3")
                            .Execute().Identifiers);
                        // 8, 9, 7
                        Assert.AreEqual(expids, ids);
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            });
        }*/

        /*[TestMethod]*/
        /*public void OData_FilteringCollection_IsOf()
        {
            Test(() =>
            {
                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                    ODataEntities entities;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/CollectionFilteringTest",
                            "&$select=Id,Name&metadata=no&$filter=isof('User')", output);
                        var handler = new ODataHandler();
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                        entities = GetEntities(output);
                    }
                    var ids = String.Join(", ", entities.Select(e => e.Id).OrderBy(x => x).Select(x => x.ToString()));
                    var expids = String.Join(", ",
                        CreateSafeContentQuery("+InFolder:/Root/IMS/BuiltIn/Portal +TypeIs:User .AUTOFILTERS:OFF")
                            .Execute().Identifiers.OrderBy(x => x).Select(x => x.ToString()));
                        // 6, 1
                        Assert.AreEqual(expids, ids);

                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/CollectionFilteringTest",
                            "&$select=Id,Name&metadata=no&$filter=not isof('User')", output);
                        var handler = new ODataHandler();
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                        entities = GetEntities(output);
                    }
                    ids = String.Join(", ", entities.Select(e => e.Id).OrderBy(x => x).Select(x => x.ToString()));
                    expids = String.Join(", ",
                        CreateSafeContentQuery("+InFolder:/Root/IMS/BuiltIn/Portal -TypeIs:User .AUTOFILTERS:OFF")
                            .Execute().Identifiers.OrderBy(x => x).Select(x => x.ToString()));
                        // 8, 9, 7
                        Assert.AreEqual(expids, ids);

                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            });
        }*/


        /* ============================================================================== Bug reproductions */

        /*[TestMethod]*/
        /*public void OData_SortingByMappedDateTimeAspectField()
        {
            Test(() =>
            {
                InstallCarContentType();
                CreateTestSite();
                var testRoot = CreateTestRoot("ODataTestRoot");

                    // Create an aspect with date field that is mapped to CreationDate
                    var aspect1Name = "OData_SortingByMappedDateTimeAspectField";
                var aspect1Definition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
    <Fields>
        <AspectField name='Field1' type='DateTime'>
            <!-- not bound -->
        </AspectField>
        <AspectField name='Field2' type='DateTime'>
            <Bind property=""CreationDate""></Bind>
        </AspectField>
        <AspectField name='Field3' type='DateTime'>
            <Bind property=""ModificationDate""></Bind>
        </AspectField>
        </Fields>
    </AspectDefinition>";

                var aspect1 = new Aspect(Repository.AspectsFolder)
                {
                    Name = aspect1Name,
                    AspectDefinition = aspect1Definition
                };
                aspect1.Save();

                var field1Name = String.Concat(aspect1Name, Aspect.ASPECTFIELDSEPARATOR, "Field1");
                var field2Name = String.Concat(aspect1Name, Aspect.ASPECTFIELDSEPARATOR, "Field2");
                var field3Name = String.Concat(aspect1Name, Aspect.ASPECTFIELDSEPARATOR, "Field3");

                var container = new SystemFolder(testRoot) { Name = Guid.NewGuid().ToString() };
                container.Save();

                var today = DateTime.Now;
                (new[] { 3, 1, 5, 2, 4 }).Select(i =>
                      {
                    var content = Content.CreateNew("Car", container, "Car-" + i + "-" + Guid.NewGuid());
                    content.AddAspects(aspect1);

                    content[field1Name] = today.AddDays(-5 + i);
                        //content[field2Name] = today.AddDays(-i);
                        //content[field3Name] = today.AddDays(-i);
                        content.CreationDate = today.AddDays(-i);
                    content.ModificationDate = today.AddDays(-i);

                    content.Save();
                    return i;
                }).ToArray();

                    // check prerequisits

                    var r1 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
                    .OrderBy(c => c[field1Name]).ToArray();
                var result1 = String.Join(",", r1.Select(x => x.Name[4]));
                Assert.AreEqual("1,2,3,4,5", result1);
                var r2 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
                    .OrderByDescending(c => c[field1Name]).ToArray();
                var result2 = String.Join(",", r2.Select(x => x.Name[4]));
                Assert.AreEqual("5,4,3,2,1", result2);
                var r3 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
                    .OrderBy(c => c[field2Name]).ToArray();
                var result3 = String.Join(",", r3.Select(x => x.Name[4]));
                Assert.AreEqual("5,4,3,2,1", result3);
                var r4 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
                    .OrderByDescending(c => c[field2Name]).ToArray();
                var result4 = String.Join(",", r4.Select(x => x.Name[4]));
                Assert.AreEqual("1,2,3,4,5", result4);
                var r5 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
                    .OrderBy(c => c[field3Name]).ToArray();
                var result5 = String.Join(",", r5.Select(x => x.Name[4]));
                Assert.AreEqual("5,4,3,2,1", result5);
                var r6 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
                    .OrderByDescending(c => c[field3Name]).ToArray();
                var result6 = String.Join(",", r6.Select(x => x.Name[4]));
                Assert.AreEqual("1,2,3,4,5", result6);

                    //------------------------------------------

                    ODataEntities entities;

                    // Field1 ASC
                    using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/" + container.Path, "$orderby=" + field1Name + " asc",
                        output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                Assert.AreEqual(5, entities.Length);
                Assert.AreEqual("1,2,3,4,5", string.Join(",", entities.Select(e => e.Name[4])));

                    // Field1 DESC
                    using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/" + container.Path, "$orderby=" + field1Name + " desc",
                        output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                Assert.AreEqual(5, entities.Length);
                Assert.AreEqual("5,4,3,2,1", string.Join(",", entities.Select(e => e.Name[4])));



                    // Field2 ASC
                    using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/" + container.Path, "$orderby=" + field2Name + " asc",
                        output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                Assert.AreEqual(5, entities.Length);
                Assert.AreEqual("5,4,3,2,1", string.Join(",", entities.Select(e => e.Name[4])));

                    // Field2 DESC
                    using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/" + container.Path, "$orderby=" + field2Name + " desc",
                        output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                Assert.AreEqual(5, entities.Length);
                Assert.AreEqual("1,2,3,4,5", string.Join(",", entities.Select(e => e.Name[4])));



                    // Field3 ASC
                    using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/" + container.Path, "$orderby=" + field2Name + " asc",
                        output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                Assert.AreEqual(5, entities.Length);
                Assert.AreEqual("5,4,3,2,1", string.Join(",", entities.Select(e => e.Name[4])));

                    // Field3 DESC
                    using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/" + container.Path, "$orderby=" + field2Name + " desc",
                        output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                Assert.AreEqual(5, entities.Length);
                Assert.AreEqual("1,2,3,4,5", string.Join(",", entities.Select(e => e.Name[4])));
            });
        }*/

        /* ============================================================================== Bug reproductions 2 */

        /*[TestMethod]*/
        /*public void OData_FIX_DoNotUrlDecodeTheRequestStream()
        {
            Test(() =>
            {
                    //var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                    //var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                    //odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());
                    CreateGlobalActions();
                CreateTestSite();

                var testString = "a&b c+d%20e";
                string result = null;
                    //------------------------------------------------------------ POST: ok
                    using (var output = new StringWriter())
                {
                        //"{iii: 42, sss: 'asdf' }"
                        var json = $"{{testString: \'{testString}\' }}";
                    var stream = CreateRequestStream(json);
                    var pc = CreatePortalContext("/OData.svc/Root('IMS')/ParameterEcho", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = GetStringResult(output);
                }
                Assert.AreEqual(testString, result);
            });
        }*/

        //TODO: Remove inconclusive test result and implement this test.
        /*//[TestMethod]
        /*public void OData_FIX_Move_RightExceptionIfTargetExists()
        {
            Assert.Inconclusive("InMemoryDataProvider.LoadChildTypesToAllow method is not implemented.");

            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                CreateStructureFor_RightExceptionIfTargetExistsTests(testRoot, out var sourcePath, out var targetContainerPath);

                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                        //------------------------------------------------------------------------------------------------------------------------ test 1
                        using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(
                            string.Format("/OData.svc/{0}('{1}')/MoveTo", RepositoryPath.GetParentPath(sourcePath),
                                RepositoryPath.GetFileName(sourcePath)), "", output);
                        var handler = new ODataHandler();
                        var stream = CreateRequestStream("{\"targetPath\":\"" + targetContainerPath + "\"}");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

                        var error = GetError(output);
                        Assert.AreEqual(ODataExceptionCode.ContentAlreadyExists, error.Code);
                        Assert.IsTrue(error.Message.ToLowerInvariant().Contains("cannot move the content"));
                    }
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            });
        }*/
        /*[TestMethod]*/
        /*public void OData_FIX_Copy_RightExceptionIfTargetExists()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                CreateStructureFor_RightExceptionIfTargetExistsTests(testRoot, out var sourcePath, out var targetContainerPath);

                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

                CreateTestSite();
                try
                {
                        //------------------------------------------------------------------------------------------------------------------------ test 1
                        using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(
                            string.Format("/OData.svc/{0}('{1}')/CopyTo", RepositoryPath.GetParentPath(sourcePath),
                                RepositoryPath.GetFileName(sourcePath)), "", output);
                        var handler = new ODataHandler();
                        var stream = CreateRequestStream("{\"targetPath\":\"" + targetContainerPath + "\"}");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

                        var error = GetError(output);
                        Assert.AreEqual(ODataExceptionCode.ContentAlreadyExists, error.Code);
                        Assert.IsTrue(error.Message.ToLowerInvariant().Contains("cannot copy the content"));
                    }
                }
                finally
                {
                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                }
            });
        }*/
        public void CreateStructureFor_RightExceptionIfTargetExistsTests(Node testRoot, out string sourcePath, out string targetContainerPath)
        {
            var sourceFolder = new SystemFolder(testRoot) { Name = Guid.NewGuid().ToString() };
            sourceFolder.Save();
            var targetFolder = new SystemFolder(testRoot) { Name = Guid.NewGuid().ToString() };
            targetFolder.Save();

            var sourceContent = new GenericContent(sourceFolder, "Car") { Name = "DemoContent" };
            sourceContent.Save();

            var targetContent = new GenericContent(targetFolder, "Car") { Name = sourceContent.Name };
            targetContent.Save();

            sourcePath = sourceContent.Path;
            targetContainerPath = targetFolder.Path;
        }

        /* ============================================================================== */

        //internal static JValue GetSimpleResult(StringWriter output)
        //{
        //    var result = new Dictionary<string, object>();
        //    var jo = (JObject)Deserialize(output);
        //    var value = jo["d"]["result"];
        //    return (JValue)value;
        //}

        private XmlDocument GetMetadataXml(string src, out XmlNamespaceManager nsmgr)
        {
            var xml = new XmlDocument();
            nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("edmx", "http://schemas.microsoft.com/ado/2007/06/edmx");
            nsmgr.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
            nsmgr.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");
            nsmgr.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
            nsmgr.AddNamespace("x", "http://schemas.microsoft.com/ado/2007/05/edm");
            xml.LoadXml(src);
            return xml;
        }

    }
}
