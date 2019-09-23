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
using SenseNet.Security;

// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeThisQualifier

namespace SenseNet.ODataTests
{
    //    #region Additional classes

    //    public class ODataTestsCustomActions
    //    {
    //        [ODataAction]
    //        public static string ParameterEcho(Content content, string testString)
    //        {
    //            return testString;
    //        }
    //    }

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

    //    [ContentHandler]
    //    public class OData_Filter_ThroughReference_ContentHandler : GenericContent
    //    {
    //        public const string CTD = @"<?xml version='1.0' encoding='utf-8'?>
    //<ContentType name='OData_Filter_ThroughReference_ContentHandler' parentType='GenericContent' handler='SenseNet.Services.OData.Tests.OData_Filter_ThroughReference_ContentHandler' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    //  <Fields>
    //    <Field name='References' type='Reference'>
    //      <Configuration>
    //        <AllowMultiple>true</AllowMultiple>
    //        <AllowedTypes>
    //          <Type>OData_Filter_ThroughReference_ContentHandler</Type>
    //        </AllowedTypes>
    //      </Configuration>
    //    </Field>
    //  </Fields>
    //</ContentType>
    //";
    //        public OData_Filter_ThroughReference_ContentHandler(Node parent) : this(parent, null) { }
    //        public OData_Filter_ThroughReference_ContentHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
    //        protected OData_Filter_ThroughReference_ContentHandler(NodeToken token) : base(token) { }

    //        public const string REFERENCES = "References";
    //        [RepositoryProperty(REFERENCES, RepositoryDataType.Reference)]
    //        public IEnumerable<Node> References
    //        {
    //            get { return this.GetReferences(REFERENCES); }
    //            set { this.SetReferences(REFERENCES, value); }
    //        }

    //    }
    //    [ContentHandler]
    //    public class OData_ReferenceTest_ContentHandler : GenericContent
    //    {
    //        public const string CTD = @"<?xml version='1.0' encoding='utf-8'?>
    //<ContentType name='OData_ReferenceTest_ContentHandler' parentType='GenericContent' handler='SenseNet.Services.OData.Tests.OData_ReferenceTest_ContentHandler' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    //  <Fields>
    //    <Field name='Reference' type='Reference'>
    //      <Configuration>
    //        <AllowMultiple>false</AllowMultiple>
    //      </Configuration>
    //    </Field>
    //    <Field name='References' type='Reference'>
    //      <Configuration>
    //        <AllowMultiple>true</AllowMultiple>
    //      </Configuration>
    //    </Field>
    //    <Field name='Reference2' type='Reference'>
    //      <Configuration>
    //        <AllowMultiple>false</AllowMultiple>
    //      </Configuration>
    //    </Field>
    //  </Fields>
    //</ContentType>
    //";
    //        public OData_ReferenceTest_ContentHandler(Node parent) : this(parent, null) { }
    //        public OData_ReferenceTest_ContentHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
    //        protected OData_ReferenceTest_ContentHandler(NodeToken token) : base(token) { }

    //        public const string REFERENCES = "References";
    //        [RepositoryProperty(REFERENCES, RepositoryDataType.Reference)]
    //        public IEnumerable<Node> References
    //        {
    //            get { return this.GetReferences(REFERENCES); }
    //            set { this.SetReferences(REFERENCES, value); }
    //        }

    //        public const string REFERENCE = "Reference";
    //        [RepositoryProperty(REFERENCE, RepositoryDataType.Reference)]
    //        public Node Reference
    //        {
    //            get { return this.GetReference<Node>(REFERENCE); }
    //            set { this.SetReference(REFERENCE, value); }
    //        }

    //        public const string REFERENCE2 = "Reference2";
    //        [RepositoryProperty(REFERENCE2, RepositoryDataType.Reference)]
    //        public Node Reference2
    //        {
    //            get { return this.GetReference<Node>(REFERENCE2); }
    //            set { this.SetReference(REFERENCE2, value); }
    //        }

    //        public override object GetProperty(string name)
    //        {
    //            switch (name)
    //            {
    //                case REFERENCE: return this.Reference;
    //                case REFERENCE2: return this.Reference2;
    //                case REFERENCES: return this.References;
    //                default: return base.GetProperty(name);
    //            }
    //        }
    //        public override void SetProperty(string name, object value)
    //        {
    //            switch (name)
    //            {
    //                case REFERENCE: this.Reference = (Node)value; break;
    //                case REFERENCE2: this.Reference2 = (Node)value; break;
    //                case REFERENCES: this.References = (IEnumerable<Node>)value; break;
    //                default: base.SetProperty(name, value); break;
    //            }
    //        }
    //    }
    //    #endregion

    //    [TestClass]
    //    public class ODataTests_legacy : ODataTestBase
    //    {
    //        #region Playground

    //        public static void CreateGlobalActions()
    //        {
    //            InitializePlayground();

    //            var rootAppsFolder = Node.Load<Folder>("/Root/(apps)");
    //            if (rootAppsFolder == null)
    //            {
    //                rootAppsFolder = new SystemFolder(Repository.Root);
    //                rootAppsFolder.Name = "(apps)";
    //                rootAppsFolder.Save();
    //            }
    //            var rootAppsGenericContentFolder = Node.Load<Folder>("/Root/(apps)/GenericContent");
    //            if (rootAppsGenericContentFolder == null)
    //            {
    //                rootAppsGenericContentFolder = new SystemFolder(rootAppsFolder);
    //                rootAppsGenericContentFolder.Name = "GenericContent";
    //                rootAppsGenericContentFolder.Save();
    //            }
    //            var rootAppsGenericContent_ParameterEcho = Node.Load<GenericODataApplication>("/Root/(apps)/GenericContent/ParameterEcho");
    //            if (rootAppsGenericContent_ParameterEcho == null)
    //            {
    //                rootAppsGenericContent_ParameterEcho = new GenericODataApplication(rootAppsGenericContentFolder);
    //                rootAppsGenericContent_ParameterEcho.Name = "ParameterEcho";
    //                rootAppsGenericContent_ParameterEcho.ClassName = "SenseNet.Services.OData.Tests.ODataTestsCustomActions";
    //                rootAppsGenericContent_ParameterEcho.MethodName = "ParameterEcho";
    //                rootAppsGenericContent_ParameterEcho.Parameters = "string testString";
    //                rootAppsGenericContent_ParameterEcho.Save();
    //            }

    //            ApplicationStorage.Invalidate();
    //        }

    //        private static void InitializePlayground()
    //        {
    //            //EnsureReferenceTestStructure();

    //            var content = Content.Create(User.Administrator);
    //            if (((IEnumerable<Node>)content["Manager"]).Any())
    //                return;
    //            content["Manager"] = User.Administrator;
    //            content["Email"] = "anybody@somewhere.com";
    //            content.Save();
    //        }

    //        private static void EnsureReferenceTestStructure(Node testRoot)
    //        {
    //            if (ContentType.GetByName(typeof(OData_ReferenceTest_ContentHandler).Name) == null)
    //                ContentTypeInstaller.InstallContentType(OData_ReferenceTest_ContentHandler.CTD);

    //            if (ContentType.GetByName(typeof(OData_Filter_ThroughReference_ContentHandler).Name) == null)
    //                ContentTypeInstaller.InstallContentType(OData_Filter_ThroughReference_ContentHandler.CTD);

    //            var referrercontent = Content.Load(RepositoryPath.Combine(testRoot.Path, "Referrer"));
    //            if (referrercontent == null)
    //            {
    //                var nodes = new Node[5];
    //                for (int i = 0; i < nodes.Length; i++)
    //                {
    //                    var content = Content.CreateNew("OData_Filter_ThroughReference_ContentHandler", testRoot, "Referenced" + i);
    //                    content.Index = i + 1;
    //                    content.Save();
    //                    nodes[i] = content.ContentHandler;
    //                }

    //                referrercontent = Content.CreateNew("OData_Filter_ThroughReference_ContentHandler", testRoot, "Referrer");
    //                var referrer = (OData_Filter_ThroughReference_ContentHandler)referrercontent.ContentHandler;
    //                referrer.References = nodes;
    //                referrercontent.Save();
    //            }
    //        }

    //        #endregion

    //        #region [TestMethod] 10 public void OData_Parsing_

    //        [TestMethod]
    //        public void OData_Parsing_TopSkip()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();

    //                PortalContext pc;
    //                ODataHandler handler;
    //                //---------------------------------------- without top, without skip
    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    Assert.AreEqual(0, handler.ODataRequest.Top);
    //                    Assert.AreEqual(0, handler.ODataRequest.Skip);
    //                    Assert.IsTrue(!handler.ODataRequest.HasTop);
    //                    Assert.IsTrue(!handler.ODataRequest.HasSkip);
    //                }

    //                //---------------------------------------- top 3, without skip
    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "$top=3", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    Assert.AreEqual(3, handler.ODataRequest.Top);
    //                    Assert.AreEqual(0, handler.ODataRequest.Skip);
    //                    Assert.IsTrue(handler.ODataRequest.HasTop);
    //                    Assert.IsTrue(!handler.ODataRequest.HasSkip);
    //                }

    //                //---------------------------------------- without top, skip 4
    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "$skip=4", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    Assert.AreEqual(0, handler.ODataRequest.Top);
    //                    Assert.AreEqual(4, handler.ODataRequest.Skip);
    //                    Assert.IsTrue(!handler.ODataRequest.HasTop);
    //                    Assert.IsTrue(handler.ODataRequest.HasSkip);
    //                }

    //                //---------------------------------------- top 3, skip 4
    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "$top=3&$skip=4", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    Assert.AreEqual(3, handler.ODataRequest.Top);
    //                    Assert.AreEqual(4, handler.ODataRequest.Skip);
    //                    Assert.IsTrue(handler.ODataRequest.HasTop);
    //                    Assert.IsTrue(handler.ODataRequest.HasSkip);
    //                }

    //                //---------------------------------------- top 0, skip 0
    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "$top=0&$skip=0", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    Assert.AreEqual(0, handler.ODataRequest.Top);
    //                    Assert.AreEqual(0, handler.ODataRequest.Skip);
    //                    Assert.IsTrue(!handler.ODataRequest.HasTop);
    //                    Assert.IsTrue(!handler.ODataRequest.HasSkip);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Parsing_InvalidTop()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();

    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/Root", "$top=-3", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var code = GetExceptionCode(output);
    //                    Assert.AreEqual(ODataExceptionCode.NegativeTopParameter, code);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Parsing_InvalidSkip()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();

    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/Root", "$skip=-4", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var code = GetExceptionCode(output);
    //                    Assert.AreEqual(ODataExceptionCode.NegativeSkipParameter, code);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Parsing_InlineCount()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();

    //                PortalContext pc;
    //                ODataHandler handler;

    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "$inlinecount=none", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    Assert.AreEqual(InlineCount.None, handler.ODataRequest.InlineCount);
    //                    Assert.IsTrue(!handler.ODataRequest.HasInlineCount);
    //                }

    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "$inlinecount=0", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    Assert.AreEqual(InlineCount.None, handler.ODataRequest.InlineCount);
    //                    Assert.IsTrue(!handler.ODataRequest.HasInlineCount);
    //                }

    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "$inlinecount=allpages", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    Assert.AreEqual(InlineCount.AllPages, handler.ODataRequest.InlineCount);
    //                    Assert.IsTrue(handler.ODataRequest.HasInlineCount);
    //                }

    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "$inlinecount=1", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    Assert.AreEqual(InlineCount.AllPages, handler.ODataRequest.InlineCount);
    //                    Assert.IsTrue(handler.ODataRequest.HasInlineCount);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Parsing_InvalidInlineCount()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();

    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/Root", "$inlinecount=asdf", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var code = GetExceptionCode(output);
    //                    Assert.AreEqual(ODataExceptionCode.InvalidInlineCountParameter, code);
    //                }
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/Root", "$inlinecount=2", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var code = GetExceptionCode(output);
    //                    Assert.AreEqual(ODataExceptionCode.InvalidInlineCountParameter, code);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Parsing_OrderBy()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();

    //                PortalContext pc;
    //                ODataHandler handler;

    //                //----------------------------------------------------------------------------- sorting: -
    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var sort = handler.ODataRequest.Sort.ToArray();
    //                    Assert.IsFalse(handler.ODataRequest.HasSort);
    //                    Assert.AreEqual(0, sort.Length);
    //                }

    //                //----------------------------------------------------------------------------- sorting: Id
    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "$orderby=Id", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var sort = handler.ODataRequest.Sort.ToArray();
    //                    Assert.IsTrue(handler.ODataRequest.HasSort);
    //                    Assert.AreEqual(1, sort.Length);
    //                    Assert.AreEqual("Id", sort[0].FieldName);
    //                    Assert.IsFalse(sort[0].Reverse);
    //                }

    //                //----------------------------------------------------------------------------- sorting: Name asc
    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "$orderby=Name asc", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var sort = handler.ODataRequest.Sort.ToArray();
    //                    Assert.IsTrue(handler.ODataRequest.HasSort);
    //                    Assert.IsTrue(sort.Length == 1);
    //                    Assert.IsTrue(sort[0].FieldName == "Name");
    //                    Assert.IsTrue(sort[0].Reverse == false);
    //                }

    //                //----------------------------------------------------------------------------- sorting: DisplayName desc
    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "$orderby=DisplayName desc", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var sort = handler.ODataRequest.Sort.ToArray();
    //                    Assert.IsTrue(handler.ODataRequest.HasSort);
    //                    Assert.AreEqual(1, sort.Length);
    //                    Assert.AreEqual("DisplayName", sort[0].FieldName);
    //                    Assert.IsTrue(sort[0].Reverse);
    //                }

    //                //----------------------------------------------------------------------------- sorting: ModificationDate desc, Category, Name
    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "$orderby=   ModificationDate desc    ,   Category   ,    Name", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var sort = handler.ODataRequest.Sort.ToArray();
    //                    Assert.IsTrue(handler.ODataRequest.HasSort);
    //                    Assert.AreEqual(3, sort.Length);
    //                    Assert.AreEqual("ModificationDate", sort[0].FieldName);
    //                    Assert.IsTrue(sort[0].Reverse);
    //                    Assert.AreEqual("Category", sort[1].FieldName);
    //                    Assert.IsFalse(sort[1].Reverse);
    //                    Assert.AreEqual("Name", sort[2].FieldName);
    //                    Assert.IsFalse(sort[2].Reverse);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Parsing_InvalidOrderBy()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();

    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/Root", "$orderby=asdf asd", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var code = GetExceptionCode(output);
    //                    Assert.IsTrue(code == ODataExceptionCode.InvalidOrderByDirectionParameter);
    //                }
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/Root", "$orderby=asdf asc desc", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var code = GetExceptionCode(output);
    //                    Assert.IsTrue(code == ODataExceptionCode.InvalidOrderByParameter);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Parsing_Format()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();

    //                PortalContext pc;
    //                ODataHandler handler;

    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "$format=json", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    Assert.IsTrue(handler.ODataRequest.Format == "json");
    //                }

    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "$format=verbosejson", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    Assert.IsTrue(handler.ODataRequest.Format == "verbosejson");
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Parsing_InvalidFormat()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();

    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/Root", "$format=atom", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var code = GetExceptionCode(output);
    //                    Assert.IsTrue(code == ODataExceptionCode.InvalidFormatParameter);
    //                }

    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/Root", "$format=xxx", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var code = GetExceptionCode(output);
    //                    Assert.IsTrue(code == ODataExceptionCode.InvalidFormatParameter);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Parsing_Select()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();

    //                PortalContext pc;
    //                ODataHandler handler;

    //                //----------------------------------------------------------------------------- select: -
    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var select = handler.ODataRequest.Select;
    //                    Assert.IsTrue(handler.ODataRequest.HasSelect == false);
    //                    Assert.IsTrue(select.Count == 0);
    //                }

    //                //----------------------------------------------------------------------------- select: Id, DisplayName, ModificationDate
    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root",
    //                        "$select=    Id  ,\tDisplayName\r\n\t,   ModificationDate   ", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var select = handler.ODataRequest.Select;
    //                    Assert.IsTrue(handler.ODataRequest.HasSelect);
    //                    Assert.IsTrue(select.Count == 3);
    //                    Assert.IsTrue(select[0] == "Id");
    //                    Assert.IsTrue(select[1] == "DisplayName");
    //                    Assert.IsTrue(select[2] == "ModificationDate");
    //                }

    //                //----------------------------------------------------------------------------- select: *
    //                using (var output = new StringWriter())
    //                {
    //                    pc = CreatePortalContext("/OData.svc/Root", "$select=*", output);
    //                    handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var select = handler.ODataRequest.Select;
    //                    Assert.IsTrue(handler.ODataRequest.HasSelect == false);
    //                    Assert.IsTrue(select.Count == 0);
    //                }
    //            });
    //        }
    //        #endregion

    //        #region [TestMethod] 2 SnJsonConverterTest_

    //        [TestMethod]
    //        public void SnJsonConverterTest_SimpleProjection()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");

    //                // Create, save
    //                var content = Content.CreateNew("Car", testRoot, "MyCar1");
    //                content["Make"] = "Citroen";
    //                content["Model"] = "C100";
    //                content["Price"] = 2399999.99;
    //                content.Save();

    //                // Reload
    //                content = Content.Load(content.Path);
    //                // Generate JSON
    //                var generatedJson = content.ToJson(new[] { "Id", "Path", "Name", "Make", "Model", "Price" }, null);

    //                // Run assertions
    //                var jobj = JObject.Parse(generatedJson);
    //                Assert.AreEqual(jobj["Id"], content.Id);
    //                Assert.AreEqual(jobj["Path"], content.Path);
    //                Assert.AreEqual(jobj["Name"], content.Name);
    //                Assert.AreEqual(jobj["Make"].Value<string>(), content["Make"]);
    //                Assert.AreEqual(jobj["Model"].Value<string>(), content["Model"]);
    //                Assert.AreEqual(jobj["Price"].Value<decimal>(), content["Price"]);
    //            });
    //        }
    //        [TestMethod]
    //        public void SnJsonConverterTest_WithExpand()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");

    //                // Create, save
    //                var content = Content.CreateNew("Car", testRoot, "MyCar2");
    //                content["Make"] = "Citroen";
    //                content["Model"] = "C101";
    //                content["Price"] = 4399999.99;
    //                content.Save();

    //                // Reload
    //                content = Content.Load(content.Path);
    //                // Generate JSON
    //                var generatedJson =
    //                    content.ToJson(
    //                        new[] { "Id", "Path", "Name", "Make", "Model", "Price", "CreatedBy/Id", "CreatedBy/Path" },
    //                        new[] { "CreatedBy" });

    //                // Run assertions
    //                var jobj = JObject.Parse(generatedJson);
    //                Assert.AreEqual(jobj["Id"], content.Id);
    //                Assert.AreEqual(jobj["Path"], content.Path);
    //                Assert.AreEqual(jobj["Name"], content.Name);
    //                Assert.AreEqual(jobj["Make"].Value<string>(), content["Make"]);
    //                Assert.AreEqual(jobj["Model"].Value<string>(), content["Model"]);
    //                Assert.AreEqual(jobj["Price"].Value<decimal>(), content["Price"]);
    //                Assert.AreEqual(jobj["CreatedBy"]["Id"], content.ContentHandler.CreatedBy.Id);
    //                Assert.AreEqual(jobj["CreatedBy"]["Path"], content.ContentHandler.CreatedBy.Path);
    //            });
    //        }
    //        #endregion


    //        [TestMethod]
    //        public void OData_Urls_CurrentSite()
    //        {
    //            Test(() =>
    //            {
    //                var site = CreateTestSite();
    //                var siteParentPath = RepositoryPath.GetParentPath(site.Path);
    //                var siteName = RepositoryPath.GetFileName(site.Path);

    //                string expectedJson = string.Concat(@"{""d"":{
    //                    ""__metadata"":{                    ""uri"":""/odata.svc", siteParentPath, @"('", siteName, @"')"",""type"":""Site""},
    //                    ""Manager"":{""__deferred"":{       ""uri"":""/odata.svc", siteParentPath, @"('", siteName, @"')/Manager""}},
    //                    ""CreatedBy"":{""__deferred"":{     ""uri"":""/odata.svc", siteParentPath, @"('", siteName, @"')/CreatedBy""}},
    //                    ""ModifiedBy"":{""__deferred"":{    ""uri"":""/odata.svc", siteParentPath, @"('", siteName, @"')/ModifiedBy""}}}}")
    //                    .Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
    //                string json;
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext(ODataTools.GetODataUrl(site.Path),
    //                        "$select=Manager,CreatedBy,ModifiedBy&metadata=minimal", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    json = GetStringResult(output);
    //                }
    //                var result = json.Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
    //                Assert.AreEqual(expectedJson, result);
    //            });
    //        }

    //        //-----------------------------------------------------------------------------------------------------------------------------------------


    //        [TestMethod]
    //        public void OData_Getting_Collection()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();
    //                ODataEntities entities;
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal", "", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    entities = GetEntities(output);
    //                }
    //                var folder = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal");
    //                var origIds = string.Join(", ", folder.Children.Select(f => f.Id).OrderBy(x => x).Select(x=>x.ToString()).ToArray());
    //                var Ids = string.Join(", ", entities.Select(e => e.Id).OrderBy(x => x).Select(x => x.ToString()).ToArray());
    //                Assert.AreEqual(origIds, Ids);
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Getting_Entity()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();

    //                ODataEntity entity;
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/Root('IMS')", "", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    entity = GetEntity(output);
    //                }
    //                var nodeHead = NodeHead.Get(entity.Path);
    //                Assert.IsTrue(nodeHead.Id == entity.Id,
    //                    string.Format("nodeHead.Id ({0}) and entity.Id ({1}) are not equal", nodeHead.Id, entity.Id));
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Getting_NotExistentEntity()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();

    //                string responseStatus;
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/Root('AAAA')", "", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    responseStatus = pc.OwnerHttpContext.Response.Status;
    //                }
    //                Assert.IsTrue(responseStatus == "404 Not Found");
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Getting_NotExistentProperty()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();

    //                string errorCode;
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/Root('IMS')/aaaa", "", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    errorCode = GetExceptionCodeText(output);
    //                }

    //                Assert.AreEqual("UnknownAction", errorCode, "errorCode is not correct");
    //            });
    //        }

    //        //[TestMethod]
    //        //public void OData_Getting_SimplePropertyAndRaw()
    //        //{
    //        //    CreateTestSite();
    //        //    try
    //        //    {
    //        //        string json;
    //        //        using (var output = new StringWriter())
    //        //        {
    //        //            var pc = CreatePortalContext("/OData.svc/Root('IMS')/Id", "", output);
    //        //            var handler = new ODataHandler();
    //        //            handler.ProcessRequest(pc.OwnerHttpContext);
    //        //            json = GetStringResult(output);
    //        //        }
    //        //        Assert.IsTrue(json.Replace("\r\n", "").Replace("\t", "").Replace(" ", "") == "{\"d\":{\"Id\":3}}", "#1");
    //        //        using (var output = new StringWriter())
    //        //        {
    //        //            var pc = CreatePortalContext("/OData.svc/Root('IMS')/Id/$value", "", output);
    //        //            var handler = new ODataHandler();
    //        //            handler.ProcessRequest(pc.OwnerHttpContext);
    //        //            json = GetStringResult(output);
    //        //        }
    //        //        Assert.IsTrue(json == "3", "#2");
    //        //    }
    //        //    finally
    //        //    {
    //        //        CleanupTestSite();
    //        //    }
    //        //}
    //        //[TestMethod]
    //        //public void OData_Getting_CollectionProperty()
    //        //{
    //        //    CreateTestSite();
    //        //    try
    //        //    {
    //        //        Entities entities;
    //        //        using (var output = new StringWriter())
    //        //        {
    //        //            var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Members", "", output);
    //        //            var handler = new ODataHandler();
    //        //            handler.ProcessRequest(pc.OwnerHttpContext);
    //        //            entities = GetEntities(output);
    //        //        }
    //        //        var group = Node.Load<Group>("/Root/IMS/BuiltIn/Portal/Administrators");
    //        //        var origIds = group.Members.Select(f => f.Id);
    //        //        var Ids = entities.Select(e => e.Id);
    //        //        Assert.IsTrue(origIds.Except(Ids).Count() == 0, "#1");
    //        //        Assert.IsTrue(Ids.Except(origIds).Count() == 0, "#2");
    //        //    }
    //        //    finally
    //        //    {
    //        //        CleanupTestSite();
    //        //    }
    //        //}
    //        //        [TestMethod]
    //        //        public void OData_Getting_Collection_Projection()
    //        //        {
    //        //            CreateTestSite();
    //        //            try
    //        //            {
    //        //                ODataEntities entities;
    //        //                using (var output = new StringWriter())
    //        //                {
    //        //                    //var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Members", "", output);
    //        //                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal", "$select=Id,Name", output);
    //        //                    var handler = new ODataHandler();
    //        //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //        //                    entities = GetEntities(output);
    //        //                }

    //        //                var itemIndex = 0;
    //        //                foreach (var entity in entities)
    //        //                {
    //        //                    var props = entity.AllProperties.ToArray();
    //        //                    Assert.IsTrue(props.Length == 3, string.Format("Item#{0}: AllProperties.Count id ({1}), expected: 3", itemIndex, props.Length));
    //        //                    Assert.IsTrue(props[0].Key == "__metadata", string.Format("Item#{0}: AllProperties[0] is ({1}), expected: '__metadata'", itemIndex, props[0].Key));
    //        //                    Assert.IsTrue(props[1].Key == "Id", string.Format("Item#{0}: AllProperties[1] is ({1}), expected: 'Id'", itemIndex, props[1].Key));
    //        //                    Assert.IsTrue(props[2].Key == "Name", string.Format("Item#{0}: AllProperties[2] is ({1}), expected: 'Name'", itemIndex, props[2].Key));
    //        //                    itemIndex++;
    //        //                }
    //        //            }
    //        //            finally
    //        //            {
    //        //                CleanupTestSite();
    //        //            }
    //        //        }

    //        //        [TestMethod]
    //        //        public void OData_Getting_Entity_Projection()
    //        //        {
    //        //            CreateTestSite();
    //        //            try
    //        //            {
    //        //                ODataEntity entity;
    //        //                using (var output = new StringWriter())
    //        //                {
    //        //                    var pc = CreatePortalContext("/OData.svc/Root('IMS')", "$select=Id,Name", output);
    //        //                    var handler = new ODataHandler();
    //        //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //        //                    entity = GetEntity(output);
    //        //                }
    //        //                var props = entity.AllProperties.ToArray();
    //        //                Assert.IsTrue(props.Length == 3, string.Format("AllProperties.Count id ({0}), expected: 3", props.Length));
    //        //                Assert.IsTrue(props[0].Key == "__metadata", string.Format("AllProperties[0] is ({0}), expected: '__metadata'", props[0].Key));
    //        //                Assert.IsTrue(props[1].Key == "Id", string.Format("AllProperties[1] is ({0}), expected: 'Id'", props[1].Key));
    //        //                Assert.IsTrue(props[2].Key == "Name", string.Format("AllProperties[2] is ({0}), expected: 'Name'", props[2].Key));
    //        //            }
    //        //            finally
    //        //            {
    //        //                CleanupTestSite();
    //        //            }
    //        //        }
    //        //        [TestMethod]
    //        //        public void OData_Getting_Entity_NoProjection()
    //        //        {
    //        //            CreateTestSite();
    //        //            try
    //        //            {
    //        //                ODataEntity entity;
    //        //                using (var output = new StringWriter())
    //        //                {
    //        //                    var pc = CreatePortalContext("/OData.svc/Root('IMS')", "", output);
    //        //                    var handler = new ODataHandler();
    //        //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //        //                    entity = GetEntity(output);
    //        //                }
    //        //                var allowedFieldNames = new List<string>();
    //        //                var c = Content.Load("/Root/IMS");
    //        //                var ct = c.ContentType;
    //        //                var fieldNames = ct.FieldSettings.Select(f => f.Name);
    //        //                allowedFieldNames.AddRange(fieldNames);
    //        //                allowedFieldNames.AddRange(new[] { "__metadata", "IsFile", "Actions", "IsFolder" });

    //        //                var entityPropNames = entity.AllProperties.Select(y => y.Key).ToArray();

    //        //                var a = entityPropNames.Except(allowedFieldNames).ToArray();
    //        //                var b = allowedFieldNames.Except(entityPropNames).ToArray();

    //        //                Assert.IsTrue(a.Length == 0, String.Format("Expected empty but contains: '{0}'", string.Join("', '", a)));
    //        //                Assert.IsTrue(b.Length == 0, String.Format("Expected empty but contains: '{0}'", string.Join("', '", b)));
    //        //            }
    //        //            finally
    //        //            {
    //        //                CleanupTestSite();
    //        //            }
    //        //        }
    //        //        [TestMethod]
    //        //        public void OData_Getting_ContentList_NoProjection()
    //        //        {
    //        //            string listDef = @"<?xml version='1.0' encoding='utf-8'?>
    //        //<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'>
    //        //	<Fields>
    //        //		<ContentListField name='#ListField1' type='ShortText'/>
    //        //		<ContentListField name='#ListField2' type='Integer'/>
    //        //		<ContentListField name='#ListField3' type='Reference'/>
    //        //	</Fields>
    //        //</ContentListDefinition>
    //        //";
    //        //            string path = RepositoryPath.Combine(testRoot.Path, "Cars");
    //        //            if (Node.Exists(path))
    //        //                Node.ForceDelete(path);
    //        //            ContentList list = new ContentList(testRoot);
    //        //            list.Name = "Cars";
    //        //            list.ContentListDefinition = listDef;
    //        //            list.AllowedChildTypes = new ContentType[] { ContentType.GetByName("Car") };
    //        //            list.Save();

    //        //            var car = Content.CreateNew("Car", list, "Car1");
    //        //            car.Save();
    //        //            car = Content.CreateNew("Car", list, "Car2");
    //        //            car.Save();

    //        //            CreateTestSite();
    //        //            try
    //        //            {
    //        //                ODataEntities entities;
    //        //                using (var output = new StringWriter())
    //        //                {
    //        //                    //var odataPath = ODataHandler.GetODataPath(list.Path, car.Name);
    //        //                    var pc = CreatePortalContext("/OData.svc" + list.Path, "", output);
    //        //                    var handler = new ODataHandler();
    //        //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //        //                    entities = GetEntities(output);
    //        //                }
    //        //                var entity = entities.First();
    //        //                var entityPropNames = entity.AllProperties.Select(y => y.Key).ToArray();

    //        //                var allowedFieldNames = new List<string>();
    //        //                allowedFieldNames.AddRange(ContentType.GetByName("Car").FieldSettings.Select(f => f.Name));
    //        //                allowedFieldNames.AddRange(ContentType.GetByName("File").FieldSettings.Select(f => f.Name));
    //        //                allowedFieldNames.AddRange(list.ListFieldNames);
    //        //                allowedFieldNames.AddRange(new[] { "__metadata", "IsFile", "Actions", "IsFolder" });
    //        //                allowedFieldNames = allowedFieldNames.Distinct().ToList();

    //        //                var a = entityPropNames.Except(allowedFieldNames).ToArray();
    //        //                var b = allowedFieldNames.Except(entityPropNames).ToArray();

    //        //                Assert.IsTrue(a.Length == 0, String.Format("Expected empty but contains: '{0}'", string.Join("', '", a)));
    //        //                Assert.IsTrue(b.Length == 0, String.Format("Expected empty but contains: '{0}'", string.Join("', '", b)));
    //        //            }
    //        //            finally
    //        //            {
    //        //                CleanupTestSite();
    //        //            }
    //        //        }

    //        //        [TestMethod]
    //        //        public void OData_ContentQuery()
    //        //        {
    //        //            var folderName = "OData_ContentQuery";
    //        //            var site = CreateTestSite();
    //        //            try
    //        //            {
    //        //                var folder = Node.Load<Folder>(RepositoryPath.Combine(site.Path, folderName));
    //        //                if (folder == null)
    //        //                {
    //        //                    var f = Content.CreateNew("Folder", site, folderName);
    //        //                    f.Save();
    //        //                    folder = (Folder)f.ContentHandler;
    //        //                }

    //        //                Content.CreateNewAndParse("Car", site, null, new Dictionary<string, string> { { "Make", "asdf" }, { "Model", "asdf" } }).Save();
    //        //                Content.CreateNewAndParse("Car", site, null, new Dictionary<string, string> { { "Make", "asdf" }, { "Model", "qwer" } }).Save();
    //        //                Content.CreateNewAndParse("Car", site, null, new Dictionary<string, string> { { "Make", "qwer" }, { "Model", "asdf" } }).Save();
    //        //                Content.CreateNewAndParse("Car", site, null, new Dictionary<string, string> { { "Make", "qwer" }, { "Model", "qwer" } }).Save();

    //        //                Content.CreateNewAndParse("Car", folder, null, new Dictionary<string, string> { { "Make", "asdf" }, { "Model", "asdf" } }).Save();
    //        //                Content.CreateNewAndParse("Car", folder, null, new Dictionary<string, string> { { "Make", "asdf" }, { "Model", "qwer" } }).Save();
    //        //                Content.CreateNewAndParse("Car", folder, null, new Dictionary<string, string> { { "Make", "qwer" }, { "Model", "asdf" } }).Save();
    //        //                Content.CreateNewAndParse("Car", folder, null, new Dictionary<string, string> { { "Make", "qwer" }, { "Model", "qwer" } }).Save();

    //        //                var expectedQueryGlobal = "asdf AND Type:Car .SORT:Path .AUTOFILTERS:OFF";
    //        //                var expectedGlobal = string.Join(", ", ContentQuery.Query(expectedQueryGlobal).Nodes.Select(n => n.Id.ToString()));

    //        //                var expectedQueryLocal = String.Format("asdf AND Type:Car AND InTree:'{0}' .SORT:Path .AUTOFILTERS:OFF", folder.Path);
    //        //                var expectedLocal = string.Join(", ", ContentQuery.Query(expectedQueryLocal).Nodes.Select(n => n.Id.ToString()));

    //        //                ODataEntities entities;
    //        //                using (var output = new StringWriter())
    //        //                {
    //        //                    var pc = CreatePortalContext("/OData.svc/Root", "$select=Id,Path&query=asdf+AND+Type%3aCar+.SORT%3aPath+.AUTOFILTERS%3aOFF", output);
    //        //                    var handler = new ODataHandler();
    //        //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //        //                    entities = GetEntities(output);
    //        //                }
    //        //                var realGlobal = String.Join(", ", entities.Select(e => e.Id));
    //        //                Assert.IsTrue(expectedGlobal == realGlobal, String.Format("Local: The result is {0}. Expected: {1}", realGlobal, expectedGlobal));

    //        //                using (var output = new StringWriter())
    //        //                {
    //        //                    var pc = CreatePortalContext("/OData.svc/" + folderName, "$select=Id,Path&query=asdf+AND+Type%3aCar+.SORT%3aPath+.AUTOFILTERS%3aOFF", output);
    //        //                    var handler = new ODataHandler();
    //        //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //        //                    entities = GetEntities(output);
    //        //                }
    //        //                var realLocal = String.Join(", ", entities.Select(e => e.Id));
    //        //                Assert.IsTrue(expectedLocal == realLocal, String.Format("Local: The result is {0}. Expected: {1}", realLocal, expectedLocal));
    //        //            }
    //        //            finally
    //        //            {
    //        //                CleanupTestSite();
    //        //            }
    //        //        }

    //        //        [TestMethod]
    //        //        public void OData_Getting_Collection_OrderTopSkipCount()
    //        //        {
    //        //            CreateTestSite();
    //        //            try
    //        //            {
    //        //                ODataEntities entities;
    //        //                using (var output = new StringWriter())
    //        //                {
    //        //                    var pc = CreatePortalContext("/OData.svc/Root/System/Schema/ContentTypes/GenericContent", "$orderby=Name desc&$skip=4&$top=3&$inlinecount=allpages", output);
    //        //                    var handler = new ODataHandler();
    //        //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //        //                    entities = GetEntities(output);
    //        //                }
    //        //                var Ids = entities.Select(e => e.Id);
    //        //                var origIds = ContentQuery.Query("+InFolder:/Root/System/Schema/ContentTypes/GenericContent .REVERSESORT:Name .SKIP:4 .TOP:3 .AUTOFILTERS:OFF").Nodes.Select(n => n.Id);
    //        //                var expected = String.Join(", ", origIds);
    //        //                var actual = String.Join(", ", Ids);
    //        //                Assert.AreEqual(expected, actual);
    //        //                Assert.AreEqual(ContentType.GetByName("GenericContent").ChildTypes.Count, entities.TotalCount);
    //        //            }
    //        //            finally
    //        //            {
    //        //                CleanupTestSite();
    //        //            }
    //        //        }

    //        //        [TestMethod]
    //        //        public void OData_Getting_Collection_Count()
    //        //        {
    //        //            CreateTestSite();
    //        //            try
    //        //            {
    //        //                string result;
    //        //                using (var output = new StringWriter())
    //        //                {
    //        //                    //var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Members", "", output);
    //        //                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal/$count", "", output);
    //        //                    var handler = new ODataHandler();
    //        //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //        //                    result = GetStringResult(output);
    //        //                }
    //        //                var folder = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal");
    //        //                Assert.AreEqual(folder.Children.Count().ToString(), result);
    //        //            }
    //        //            finally
    //        //            {
    //        //                CleanupTestSite();
    //        //            }
    //        //        }
    //        //        [TestMethod]
    //        //        public void OData_Getting_Collection_CountTop()
    //        //        {
    //        //            CreateTestSite();
    //        //            try
    //        //            {
    //        //                string result;
    //        //                using (var output = new StringWriter())
    //        //                {
    //        //                    //var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Members", "", output);
    //        //                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal/$count", "$top=3", output);
    //        //                    var handler = new ODataHandler();
    //        //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //        //                    result = GetStringResult(output);
    //        //                }
    //        //                var folder = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal");
    //        //                Assert.AreEqual("3", result);
    //        //            }
    //        //            finally
    //        //            {
    //        //                CleanupTestSite();
    //        //            }
    //        //        }

    //        //-----------------------------------------------------------------------------------------------------------------------------------------

    //        //        [TestMethod]
    //        //        public void OData_Expand()
    //        //        {
    //        //            EnsureCleanAdministratorsGroup();

    //        //            var count = ContentQuery.Query("InFolder:/Root/IMS/BuiltIn/Portal .COUNTONLY").Count;
    //        //            var expectedJson = String.Concat(@"
    //        //{
    //        //  ""d"": {
    //        //    ""__metadata"": {
    //        //      ""uri"": ""/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')"",
    //        //      ""type"": ""Group""
    //        //    },
    //        //    ""Id"": 7,
    //        //    ""Members"": [
    //        //      {
    //        //        ""__metadata"": {
    //        //          ""uri"": ""/OData.svc/Root/IMS/BuiltIn/Portal('Admin')"",
    //        //          ""type"": ""User""
    //        //        },
    //        //        ""Id"": 1,
    //        //        ""Name"": ""Admin""
    //        //      }
    //        //    ],
    //        //    ""Name"": ""Administrators""
    //        //  }
    //        //}");

    //        //            CreateTestSite();
    //        //            try
    //        //            {
    //        //                string jsonText;
    //        //                using (var output = new StringWriter())
    //        //                {
    //        //                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')", "$expand=Members,ModifiedBy&$select=Id,Members/Id,Name,Members/Name&metadata=minimal", output);
    //        //                    var handler = new ODataHandler();
    //        //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //        //                    jsonText = GetStringResult(output);
    //        //                }
    //        //                var raw = jsonText.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
    //        //                var exp = expectedJson.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
    //        //                Assert.IsTrue(raw == exp, String.Format("Result and expected are not equal. Result: {0}, expected: {1}", raw, exp));
    //        //            }
    //        //            finally
    //        //            {
    //        //                CleanupTestSite();
    //        //            }
    //        //        }

    //        //        [TestMethod]
    //        //        public void OData_Expand_Level2_Noselect()
    //        //        {
    //        //            CreateTestSite();
    //        //            try
    //        //            {
    //        //                ODataEntity entity;
    //        //                string json;
    //        //                using (var output = new StringWriter())
    //        //                {
    //        //                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn('Portal')", "$expand=CreatedBy/Manager", output);
    //        //                    var handler = new ODataHandler();
    //        //                    handler.ProcessRequest(pc.OwnerHttpContext);

    //        //                    json = GetStringResult(output);
    //        //                    entity = GetEntity(output);
    //        //                }
    //        //                var createdBy = entity.CreatedBy;
    //        //                var createdBy_manager = createdBy.Manager;
    //        //                var msg = "Property count of '{0}' is {1}, expected: more than 20";
    //        //                Assert.IsTrue(entity.AllPropertiesSelected, string.Format(msg, "entity", entity.AllProperties.Count));
    //        //                Assert.IsTrue(createdBy.AllPropertiesSelected, string.Format(msg, "createdBy", createdBy.AllProperties.Count));
    //        //                Assert.IsTrue(createdBy_manager.AllPropertiesSelected, string.Format(msg, "createdBy.Manager", createdBy_manager.AllProperties.Count));
    //        //                Assert.IsTrue(createdBy.Manager.CreatedBy.IsDeferred, "'createdBy.Manager.CreatedBy' is not deferred");
    //        //                Assert.IsTrue(createdBy.Manager.Manager.IsDeferred, "'createdBy.Manager.Manager' is not deferred");
    //        //            }
    //        //            finally
    //        //            {
    //        //                CleanupTestSite();
    //        //            }
    //        //        }
    //        //        [TestMethod]
    //        //        public void OData_Expand_Level2_Select_Level1()
    //        //        {
    //        //            CreateTestSite();
    //        //            try
    //        //            {
    //        //                ODataEntity entity;
    //        //                string json;
    //        //                using (var output = new StringWriter())
    //        //                {
    //        //                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn('Portal')", "$expand=CreatedBy/Manager&$select=CreatedBy", output);
    //        //                    var handler = new ODataHandler();
    //        //                    handler.ProcessRequest(pc.OwnerHttpContext);

    //        //                    json = GetStringResult(output);
    //        //                    entity = GetEntity(output);
    //        //                }
    //        //                Assert.IsTrue(!entity.AllPropertiesSelected, "'entity' is not expanded.");
    //        //                Assert.IsTrue(entity.CreatedBy.AllPropertiesSelected, "'entity.CreatedBy' is not expanded.");
    //        //                Assert.IsTrue(entity.CreatedBy.CreatedBy.IsDeferred, "'entity.CreatedBy.CreatedBy' is not deferred");
    //        //                Assert.IsTrue(entity.CreatedBy.Manager.AllPropertiesSelected, "'entity.CreatedBy.Manager' is not expanded.");
    //        //                Assert.IsTrue(entity.CreatedBy.Manager.CreatedBy.IsDeferred, "'entity.CreatedBy.Manager.CreatedBy' is not deferred");
    //        //                Assert.IsTrue(entity.CreatedBy.Manager.Manager.IsDeferred, "'entity.CreatedBy.Manager.Manager' is not deferred");
    //        //            }
    //        //            finally
    //        //            {
    //        //                CleanupTestSite();
    //        //            }
    //        //        }
    //        //        [TestMethod]
    //        //        public void OData_Expand_Level2_Select_Level2()
    //        //        {
    //        //            CreateTestSite();
    //        //            try
    //        //            {
    //        //                ODataEntity entity;
    //        //                string json;
    //        //                using (var output = new StringWriter())
    //        //                {
    //        //                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn('Portal')", "$expand=CreatedBy/Manager&$select=CreatedBy/Manager", output);
    //        //                    var handler = new ODataHandler();
    //        //                    handler.ProcessRequest(pc.OwnerHttpContext);

    //        //                    json = GetStringResult(output);
    //        //                    entity = GetEntity(output);
    //        //                }
    //        //                Assert.IsTrue(!entity.AllPropertiesSelected, "'entity' is not expanded.");
    //        //                Assert.IsTrue(!entity.CreatedBy.AllPropertiesSelected, "'entity.CreatedBy' is not expanded.");
    //        //                Assert.IsTrue(entity.CreatedBy.CreatedBy == null, "'entity.CreatedBy.CreatedBy' is not null");
    //        //                Assert.IsTrue(entity.CreatedBy.Manager.AllPropertiesSelected, "'entity.CreatedBy.Manager' is not expanded.");
    //        //                Assert.IsTrue(entity.CreatedBy.Manager.CreatedBy.IsDeferred, "'entity.CreatedBy.Manager.CreatedBy' is not deferred");
    //        //                Assert.IsTrue(entity.CreatedBy.Manager.Manager.IsDeferred, "'entity.CreatedBy.Manager.Manager' is not deferred");
    //        //            }
    //        //            finally
    //        //            {
    //        //                CleanupTestSite();
    //        //            }
    //        //        }
    //        //        [TestMethod]
    //        //        public void OData_Expand_Level2_Select_Level3()
    //        //        {
    //        //            CreateTestSite();
    //        //            try
    //        //            {
    //        //                ODataEntity entity;
    //        //                string json;
    //        //                using (var output = new StringWriter())
    //        //                {
    //        //                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn('Portal')", "$expand=CreatedBy/Manager&$select=CreatedBy/Manager/Id", output);
    //        //                    var handler = new ODataHandler();
    //        //                    handler.ProcessRequest(pc.OwnerHttpContext);

    //        //                    json = GetStringResult(output);
    //        //                    entity = GetEntity(output);
    //        //                }
    //        //                var id = entity.CreatedBy.Manager.Id;
    //        //                Assert.IsTrue(!entity.AllPropertiesSelected, "'entity' is not expanded.");
    //        //                Assert.IsTrue(!entity.CreatedBy.AllPropertiesSelected, "'entity.CreatedBy' is not expanded.");
    //        //                Assert.IsTrue(entity.CreatedBy.CreatedBy == null, "'entity.CreatedBy.CreatedBy' is not null");
    //        //                Assert.IsTrue(!entity.CreatedBy.Manager.AllPropertiesSelected, "'entity.CreatedBy.Manager' is not expanded.");
    //        //                Assert.IsTrue(entity.CreatedBy.Manager.Id > 0, String.Format("'entity.CreatedBy.Manager.Id' is {0}, expected: > 0", entity.CreatedBy.Manager.Id));
    //        //                Assert.IsTrue(entity.CreatedBy.Manager.Path == null, "'entity.CreatedBy.Manager.Path' is not null");
    //        //            }
    //        //            finally
    //        //            {
    //        //                CleanupTestSite();
    //        //            }
    //        //        }

    //        [TestMethod]
    //        public void OData_ExpandErrors()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();

    //                //------------------------------------------------------------------------------------------------------------------------ test 1
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal",
    //                        "$expand=Members&$select=Members1/Id", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var error = GetError(output);
    //                    Assert.IsTrue(error.Code == ODataExceptionCode.InvalidSelectParameter);
    //                    Assert.IsTrue(error.Message == "Bad item in $select: Members1/Id");
    //                }

    //                //------------------------------------------------------------------------------------------------------------------------ test 2
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal", "&$select=Members/Id", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    var error = GetError(output);
    //                    Assert.IsTrue(error.Code == ODataExceptionCode.InvalidSelectParameter);
    //                    Assert.IsTrue(error.Message == "Bad item in $select: Members/Id");
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Expand_Actions()
    //        {
    //            Test(() =>
    //            {
    //                EnsureCleanAdministratorsGroup();

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    ODataEntity entity;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')",
    //                            "metadata=no&$expand=Members/Actions,ModifiedBy&$select=Id,Name,Actions,Members/Id,Members/Name,Members/Actions",
    //                            output);
    //                        var handler = new ODataHandler();
    //                        handler.ProcessRequest(pc.OwnerHttpContext);
    //                        entity = GetEntity(output);
    //                    }
    //                    var members = entity.AllProperties["Members"] as JArray;
    //                    Assert.IsNotNull(members);
    //                    var member = members.FirstOrDefault() as JObject;
    //                    Assert.IsNotNull(member);
    //                    var actionsProperty = member.Property("Actions");
    //                    var actions = actionsProperty.Value as JArray;
    //                    Assert.IsNotNull(actions);
    //                    Assert.IsTrue(actions.Any());
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }

    //        //-----------------------------------------------------------------------------------------------------------------------------------------

    //        [TestMethod]
    //        public void OData_Invoking_Actions()
    //        {
    //            Test(() =>
    //            {
    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                var expectedJson = @"
    //{
    //  ""d"": {
    //    ""message"":""Action3 executed""
    //  }
    //}";

    //                CreateTestSite();
    //                try
    //                {
    //                    string jsonText;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Action3", "",
    //                            output);
    //                        var handler = new ODataHandler();
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", Stream.Null);
    //                        jsonText = GetStringResult(output);
    //                    }
    //                    var raw = jsonText.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
    //                    var exp = expectedJson.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
    //                    Assert.IsTrue(raw == exp);
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Invoking_Actions_NoContent()
    //        {
    //            Test(() =>
    //            {
    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    HttpResponse response;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Action4", "",
    //                            output);
    //                        var handler = new ODataHandler();
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", Stream.Null);
    //                        response = pc.OwnerHttpContext.Response;
    //                    }
    //                    Assert.IsTrue(response.StatusCode == 204);
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }

    //        internal class TestActionResolver : IActionResolver
    //        {
    //            internal class Action1 : ActionBase
    //            {
    //                public override string Icon { get { return "ActionIcon1"; } set { } }
    //                public override string Name { get { return "Action1"; } set { } }
    //                public override string Uri { get { return "ActionIcon1_URI"; } }
    //                public override bool IsHtmlOperation { get { return true; } }
    //                public override bool IsODataOperation { get { return false; } }
    //                public override bool CausesStateChange { get { return true; } }
    //                public override object Execute(Content content, params object[] parameters)
    //                {
    //                    return new Dictionary<string, object> { { "d", new Dictionary<string, object> { { "message", "Action1 executed" } } } };
    //                }
    //            }
    //            internal class Action2 : ActionBase
    //            {
    //                public override string Icon { get { return "ActionIcon2"; } set { } }
    //                public override string Name { get { return "Action2"; } set { } }
    //                public override string Uri { get { return "ActionIcon2_URI"; } }
    //                public override bool IsHtmlOperation { get { return true; } }
    //                public override bool IsODataOperation { get { return true; } }
    //                public override bool CausesStateChange { get { return false; } }
    //                public override object Execute(Content content, params object[] parameters)
    //                {
    //                    return new Dictionary<string, object> { { "d", new Dictionary<string, object> { { "message", "Action2 executed" } } } };
    //                }
    //            }
    //            internal class Action3 : ActionBase
    //            {
    //                public override string Icon { get { return "ActionIcon3"; } set { } }
    //                public override string Name { get { return "Action3"; } set { } }
    //                public override string Uri { get { return "ActionIcon3_URI"; } }
    //                public override bool IsHtmlOperation { get { return false; } }
    //                public override bool IsODataOperation { get { return true; } }
    //                public override bool CausesStateChange { get { return true; } }
    //                public override object Execute(Content content, params object[] parameters)
    //                {
    //                    return new Dictionary<string, object> { { "d", new Dictionary<string, object> { { "message", "Action3 executed" } } } };
    //                }
    //            }
    //            internal class Action4 : ActionBase
    //            {
    //                public override string Icon { get { return "ActionIcon4"; } set { } }
    //                public override string Name { get { return "Action4"; } set { } }
    //                public override string Uri { get { return "ActionIcon4_URI"; } }
    //                public override bool IsHtmlOperation { get { return false; } }
    //                public override bool IsODataOperation { get { return true; } }
    //                public override bool CausesStateChange { get { return true; } }
    //                public override object Execute(Content content, params object[] parameters)
    //                {
    //                    return null;
    //                }
    //            }

    //            internal class ChildrenDefinitionFilteringTestAction : ActionBase
    //            {
    //                public override string Icon { get { return "ChildrenDefinitionFilteringTestAction"; } set { } }
    //                public override string Name { get { return "ChildrenDefinitionFilteringTestAction"; } set { } }
    //                public override string Uri { get { return "ChildrenDefinitionFilteringTestAction_URI"; } }
    //                public override bool IsHtmlOperation { get { return false; } }
    //                public override bool IsODataOperation { get { return true; } }
    //                public override bool CausesStateChange { get { return true; } }
    //                public override object Execute(Content content, params object[] parameters)
    //                {
    //                    return new ChildrenDefinition
    //                    {
    //                        ContentQuery = "InFolder:/Root/IMS/BuiltIn/Portal",
    //                        EnableAutofilters = FilterStatus.Disabled,
    //                        PathUsage = PathUsageMode.NotUsed,
    //                        Sort = new[] { new SortInfo("Name", true) },
    //                        Skip = 2,
    //                        Top = 3
    //                    };
    //                }
    //            }
    //            internal class CollectionFilteringTestAction : ActionBase
    //            {
    //                public override string Icon { get { return "ActionIcon4"; } set { } }
    //                public override string Name { get { return "Action4"; } set { } }
    //                public override string Uri { get { return "ActionIcon4_URI"; } }
    //                public override bool IsHtmlOperation { get { return false; } }
    //                public override bool IsODataOperation { get { return true; } }
    //                public override bool CausesStateChange { get { return true; } }
    //                public override object Execute(Content content, params object[] parameters)
    //                {
    //                    return CreateSafeContentQuery("InFolder:/Root/IMS/BuiltIn/Portal .AUTOFILTERS:OFF")
    //                        .Execute().Nodes.Select(Content.Create);
    //                }
    //            }

    //            internal class ODataActionAction : ActionBase
    //            {
    //                public override string Icon { get { return "ODataActionAction"; } set { } }
    //                public override string Name { get { return "ODataActionAction"; } set { } }
    //                public override string Uri { get { return "ODataActionAction_URI"; } }
    //                public override bool IsHtmlOperation { get { return false; } }
    //                public override bool IsODataOperation { get { return true; } }
    //                public override bool CausesStateChange { get { return true; } }
    //                public override object Execute(Content content, params object[] parameters)
    //                {
    //                    return "ODataAction executed.";
    //                }
    //            }
    //            internal class ODataFunctionAction : ActionBase
    //            {
    //                public override string Icon { get { return "ODataFunctionAction"; } set { } }
    //                public override string Name { get { return "ODataFunctionAction"; } set { } }
    //                public override string Uri { get { return "ODataFunctionAction_URI"; } }
    //                public override bool IsHtmlOperation { get { return false; } }
    //                public override bool IsODataOperation { get { return true; } }
    //                public override bool CausesStateChange { get { return false; } }
    //                public override object Execute(Content content, params object[] parameters)
    //                {
    //                    return "ODataFunction executed.";
    //                }
    //            }
    //            internal class ODataGetParentChainAction : ActionBase
    //            {
    //                public override string Icon { get { return ""; } set { } }
    //                public override string Name { get { return "ODataGetParentChainAction"; } set { } }
    //                public override string Uri { get { return "ODataContentDictionaryFunctionAction_URI"; } }
    //                public override bool IsHtmlOperation { get { return false; } }
    //                public override bool IsODataOperation { get { return true; } }
    //                public override bool CausesStateChange { get { return false; } }
    //                public override object Execute(Content content, params object[] parameters)
    //                {
    //                    var result = new List<Content>();
    //                    Content c = content;
    //                    while (true)
    //                    {
    //                        result.Add(c);
    //                        var n = c.ContentHandler.Parent;
    //                        if (n == null)
    //                            break;
    //                        c = Content.Create(n);
    //                    }
    //                    return result;
    //                }
    //            }

    //            public GenericScenario GetScenario(string name, string parameters)
    //            {
    //                return null;
    //            }
    //            public IEnumerable<ActionBase> GetActions(Content context, string scenario, string backUri)
    //            {
    //                return new ActionBase[] { new Action1(), new Action2(), new Action3(), new Action4() };
    //            }
    //            public ActionBase GetAction(Content context, string scenario, string actionName, string backUri, object parameters)
    //            {
    //                switch (actionName)
    //                {
    //                    default: return null;
    //                    case "Action1": return new Action1();
    //                    case "Action2": return new Action2();
    //                    case "Action3": return new Action3();
    //                    case "Action4": return new Action4();
    //                    case "GetPermissions": return new GetPermissionsAction();
    //                    case "SetPermissions": return new SenseNet.Portal.ApplicationModel.SetPermissionsAction();
    //                    case "HasPermission": return new SenseNet.Portal.ApplicationModel.HasPermissionAction();
    //                    case "AddAspects": return new SenseNet.ApplicationModel.AspectActions.AddAspectsAction();
    //                    case "RemoveAspects": return new SenseNet.ApplicationModel.AspectActions.RemoveAspectsAction();
    //                    case "RemoveAllAspects": return new SenseNet.ApplicationModel.AspectActions.RemoveAllAspectsAction();
    //                    case "AddFields": return new SenseNet.ApplicationModel.AspectActions.AddFieldsAction();
    //                    case "RemoveFields": return new SenseNet.ApplicationModel.AspectActions.RemoveFieldsAction();
    //                    case "RemoveAllFields": return new SenseNet.ApplicationModel.AspectActions.RemoveAllFieldsAction();

    //                    case "ChildrenDefinitionFilteringTest": return new ChildrenDefinitionFilteringTestAction();
    //                    case "CollectionFilteringTest": return new CollectionFilteringTestAction();

    //                    case "ODataAction": return new ODataActionAction();
    //                    case "ODataFunction": return new ODataFunctionAction();

    //                    case "ODataGetParentChainAction": return new ODataGetParentChainAction();

    //                    case "CopyTo": return new CopyToAction();
    //                    case "MoveTo": return new MoveToAction();
    //                }
    //            }
    //        }
    //        /*
    //        ActionBase
    //	        Action1
    //	        Action2
    //	        Action3
    //	        Action4
    //	        PortalAction
    //		        ClientAction
    //			        OpenPickerAction
    //				        CopyToAction
    //					        CopyBatchAction
    //				        ContentLinkBatchAction
    //				        MoveToAction
    //					        MoveBatchAction
    //			        ShareAction
    //			        DeleteBatchAction
    //				        DeleteAction
    //			        WebdavOpenAction
    //			        WebdavBrowseAction
    //		        UrlAction
    //			        SetAsDefaultViewAction
    //			        PurgeFromProxyAction
    //			        ExpenseClaimPublishAction
    //			        WorkflowsAction
    //			        OpenLinkAction
    //			        BinarySpecialAction
    //			        AbortWorkflowAction
    //			        UploadAction
    //			        ManageViewsAction
    //			        ContentTypeAction
    //			        SetNotificationAction
    //		        ServiceAction
    //			        CopyAppLocalAction
    //			        LogoutAction
    //			        UserProfileAction
    //			        CopyViewLocalAction
    //		        DeleteLocalAppAction
    //		        ExploreAction
    //        */

    //        /*-----------------------------------------------------------------------------------------------------------------------------------------*/

    //        //[TestMethod]
    //        //public void OData_Select_FieldMoreThanOnce()
    //        //{
    //        //    var path = User.Administrator.Parent.Path;
    //        //    var nodecount = ContentQuery.Query(String.Format("InFolder:{0} .AUTOFILTERS:OFF .COUNTONLY", path)).Count;

    //        //    CreateTestSite();
    //        //    try
    //        //    {
    //        //        ODataEntities entities;
    //        //        using (var output = new StringWriter())
    //        //        {
    //        //            var pc = CreatePortalContext("/OData.svc" + path, "$orderby=Name asc&$select=Id,Id,Name,Name,Path", output);
    //        //            var handler = new ODataHandler();
    //        //            handler.ProcessRequest(pc.OwnerHttpContext);
    //        //            CheckError(output);
    //        //            entities = GetEntities(output);
    //        //        }
    //        //        Assert.AreEqual(nodecount, entities.Count());
    //        //    }
    //        //    finally
    //        //    {
    //        //        CleanupTestSite();
    //        //    }

    //        //}

    //        //        [TestMethod]
    //        //        public void OData_Select_AspectField()
    //        //        {
    //        //            var aspect1 = EnsureAspect("Aspect1");
    //        //            aspect1.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
    //        //<Fields>
    //        //    <AspectField name='Field1' type='ShortText' />
    //        //  </Fields>
    //        //</AspectDefinition>";
    //        //            aspect1.Save();

    //        //            var folder = new Folder(testRoot) { Name = Guid.NewGuid().ToString() };
    //        //            folder.Save();

    //        //            var content1 = Content.CreateNew("Car", folder, "Car1");
    //        //            content1.AddAspects(aspect1);
    //        //            content1["Aspect1.Field1"] = "asdf";
    //        //            content1.Save();

    //        //            var content2 = Content.CreateNew("Car", folder, "Car2");
    //        //            content2.AddAspects(aspect1);
    //        //            content2["Aspect1.Field1"] = "qwer";
    //        //            content2.Save();

    //        //            CreateTestSite();
    //        //            try
    //        //            {
    //        //                ODataEntities entities;
    //        //                using (var output = new StringWriter())
    //        //                {
    //        //                    var pc = CreatePortalContext("/OData.svc" + folder.Path, "$orderby=Name asc&$select=Name,Aspect1.Field1", output);
    //        //                    var handler = new ODataHandler();
    //        //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //        //                    entities = GetEntities(output);
    //        //                }
    //        //                Assert.IsTrue(entities.Count() == 2, string.Format("entities.Count is ({0}), expected: 2", entities.Count()));
    //        //                Assert.IsTrue(entities[0].Name == "Car1", string.Format("entities[0].Name is ({0}), expected: 'Car1'", entities[0].Name));
    //        //                Assert.IsTrue(entities[1].Name == "Car2", string.Format("entities[1].Name is ({0}), expected: 'Car2'", entities[0].Name));
    //        //                Assert.IsTrue(entities[0].AllProperties.ContainsKey("Aspect1.Field1"), "entities[0] does not contain 'Aspect1.Field1'");
    //        //                Assert.IsTrue(entities[1].AllProperties.ContainsKey("Aspect1.Field1"), "entities[1] does not contain 'Aspect1.Field1'");
    //        //                var value1 = (string)((JValue)entities[0].AllProperties["Aspect1.Field1"]).Value;
    //        //                var value2 = (string)((JValue)entities[1].AllProperties["Aspect1.Field1"]).Value;
    //        //                Assert.IsTrue(value1 == "asdf", string.Format("entities[0].AllProperties[\"Aspect1.Field1\"] is ({0}), expected: 'asdf'", value1));
    //        //                Assert.IsTrue(value2 == "qwer", string.Format("entities[0].AllProperties[\"Aspect1.Field1\"] is ({0}), expected: 'qwer'", value2));
    //        //            }
    //        //            finally
    //        //            {
    //        //                CleanupTestSite();
    //        //            }

    //        //        }
    //        //        private Aspect EnsureAspect(string name)
    //        //        {
    //        //            //var r = ContentQuery.Query(String.Concat("Name:", name, " .AUTOFILTERS:OFF"));
    //        //            //if (r.Count > 0)
    //        //            //    return (Aspect)r.Nodes.First();
    //        //            //var aspectContent = Content.CreateNew("Aspect", testRoot, name);
    //        //            //aspectContent.Save();
    //        //            //return (Aspect)aspectContent.ContentHandler;

    //        //            var aspect = Aspect.LoadAspectByName(name);
    //        //            if (aspect == null)
    //        //            {
    //        //                aspect = new Aspect(Repository.AspectsFolder) { Name = name };
    //        //                aspect.Save();
    //        //            }
    //        //            return aspect;
    //        //        }

    //        [TestMethod]
    //        public void OData_Rename_PUT()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");

    //                var content = Content.CreateNew("Car", testRoot, "ORIG_" + Guid.NewGuid());
    //                content.DisplayName = "Initial DisplayName";
    //                content.Save();
    //                var id = content.Id;
    //                var path = content.Path;

    //                var newName = "NEW_" + Guid.NewGuid();
    //                var newDisplayName = "New DisplayName";

    //                CreateTestSite();
    //                using (var output = new StringWriter())
    //                {
    //                    var json = String.Concat(@"models=[{
    //                      ""Name"": """, newName, @""",
    //                      ""DisplayName"": """, newDisplayName, @"""
    //                    }]");
    //                    var stream = CreateRequestStream(json);
    //                    var pc = CreatePortalContext("/OData.svc" + content.Path, "", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext, "PUT", stream);
    //                    var entity = GetEntity(output);
    //                }
    //                var content1 = Content.Load(id);
    //                Assert.AreEqual(newName, content1.Name);
    //                Assert.AreEqual(newDisplayName, content1.DisplayName);
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Rename_PATCH()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");

    //                var content = Content.CreateNew("Car", testRoot, "ORIG_" + Guid.NewGuid().ToString());
    //                content.DisplayName = "Initial DisplayName";
    //                content.Save();
    //                var id = content.Id;
    //                var path = content.Path;

    //                var newName = "NEW_" + Guid.NewGuid().ToString();
    //                var newDisplayName = "New DisplayName";

    //                CreateTestSite();

    //                using (var output = new StringWriter())
    //                {
    //                    var json = String.Concat(@"models=[{
    //                      ""Name"": """, newName, @""",
    //                      ""DisplayName"": """, newDisplayName, @"""
    //                    }]");
    //                    var stream = CreateRequestStream(json);
    //                    var pc = CreatePortalContext("/OData.svc" + content.Path, "", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext, "PATCH", stream);
    //                    var entity = GetEntity(output);
    //                }
    //                var content1 = Content.Load(id);
    //                Assert.AreEqual(newName, content1.Name);
    //                Assert.AreEqual(newDisplayName, content1.DisplayName);
    //            });
    //        }

    //        //-----------------------------------------------------------------------------------------------------------------------------------------

    //        [TestMethod]
    //        public void OData_TemplatedCreation()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();
    //                EnsureTemplateStructure();

    //                var name = Guid.NewGuid().ToString();
    //                var path = RepositoryPath.Combine(testRoot.Path, name);
    //                var json = string.Concat(
    //                    @"models=[{""__ContentType"":""Car"",""__ContentTemplate"":""Template3"",""Name"":""", name,
    //                    @""",""EngineSize"":""3.5 l""}]");

    //                var output = new StringWriter();
    //                var pc = CreatePortalContext("/OData.svc/" + testRoot.Path, "", output);
    //                var handler = new ODataHandler();
    //                var stream = CreateRequestStream(json);

    //                handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

    //                var content = Content.Load(path);
    //                Assert.AreEqual("Car", content.ContentType.Name);
    //                Assert.AreEqual(name, content.Name);
    //                Assert.AreEqual("TestCar3", content["Make"]);
    //                Assert.AreEqual("Template3", content["Model"]);
    //                Assert.AreEqual("3.5 l", content["EngineSize"]);
    //            });
    //        }
    //        private void EnsureTemplateStructure()
    //        {
    //            //global template folder
    //            var ctfGlobal = Node.LoadNode(RepositoryStructure.ContentTemplateFolderPath);
    //            if (ctfGlobal == null)
    //            {
    //                ctfGlobal = new SystemFolder(Node.LoadNode("/Root")) { Name = Repository.ContentTemplatesFolderName };
    //                ctfGlobal.Save();
    //            }

    //            //create content template type folders
    //            var folderGlobalCtCar = Node.Load<Folder>(RepositoryPath.Combine(ctfGlobal.Path, "Car"));
    //            if (folderGlobalCtCar == null)
    //            {
    //                folderGlobalCtCar = new Folder(ctfGlobal) { Name = "Car" };
    //                folderGlobalCtCar.Save();
    //            }

    //            //create content templates
    //            for (int i = 0; i < 4; i++)
    //            {
    //                var index = i + 1;
    //                var templateName = "Template" + index;
    //                if (Node.Load<ContentRepository.File>(RepositoryPath.Combine(folderGlobalCtCar.Path, templateName)) == null)
    //                {
    //                    var template = Content.CreateNew("Car", folderGlobalCtCar, templateName);
    //                    template["Make"] = "TestCar" + index;
    //                    template["Model"] = templateName;
    //                    template.Save();
    //                }
    //            }

    //        }


    //        //-----------------------------------------------------------------------------------------------------------------------------------------

    //        [TestMethod]
    //        public void OData_Put_Modifying()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();

    //                var name = Guid.NewGuid().ToString();
    //                var content = Content.CreateNew("Car", testRoot, name);
    //                content.DisplayName = "vadalma";
    //                var defaultMake = (string) content["Make"];
    //                content["Make"] = "Not default";
    //                content.Save();
    //                var id = content.Id;
    //                var path = content.Path;
    //                var url = GetUrl(content.Path);

    //                var newDisplayName = "szelídgesztenye";

    //                var json = String.Concat(@"models=[{
    //  ""DisplayName"": """, newDisplayName, @""",
    //  ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
    //  ""Index"": 42
    //}]");

    //                var output = new StringWriter();
    //                var pc = CreatePortalContext("/OData.svc/" + path, "", output);
    //                var handler = new ODataHandler();
    //                var stream = CreateRequestStream(json);

    //                handler.ProcessRequest(pc.OwnerHttpContext, "PUT", stream);

    //                var c = Content.Load(id);
    //                var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
    //                var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

    //                Assert.IsTrue(c.DisplayName == newDisplayName);
    //                Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370");
    //                Assert.IsTrue(c.ContentHandler.Index == 42);
    //                Assert.IsTrue((string)c["Make"] == null);
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Patch_Modifying()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();

    //                var name = Guid.NewGuid().ToString();
    //                var content = Content.CreateNew("Car", testRoot, name);
    //                content.DisplayName = "vadalma";
    //                var defaultMake = (string) content["Make"];
    //                content["Make"] = "Not default";
    //                content.Save();
    //                var id = content.Id;
    //                var path = content.Path;
    //                var url = GetUrl(content.Path);

    //                var newDisplayName = "szelídgesztenye";

    //                var json = String.Concat(@"models=[{
    //  ""DisplayName"": """, newDisplayName, @""",
    //  ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
    //  ""Index"": 42
    //}]");

    //                var output = new StringWriter();
    //                var pc = CreatePortalContext("/OData.svc/" + path, "", output);
    //                var handler = new ODataHandler();
    //                var stream = CreateRequestStream(json);

    //                handler.ProcessRequest(pc.OwnerHttpContext, "PATCH", stream);

    //                var c = Content.Load(id);
    //                var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
    //                var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

    //                Assert.IsTrue(c.DisplayName == newDisplayName);
    //                Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370");
    //                Assert.IsTrue(c.ContentHandler.Index == 42);
    //                Assert.IsTrue((string)c["Make"] == "Not default");
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Merge_Modifying()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();

    //                var name = Guid.NewGuid().ToString();
    //                var content = Content.CreateNew("Car", testRoot, name);
    //                content.DisplayName = "vadalma";
    //                var defaultMake = (string) content["Make"];
    //                content["Make"] = "Not default";
    //                content.Save();
    //                var id = content.Id;
    //                var path = content.Path;
    //                var url = GetUrl(content.Path);

    //                var newDisplayName = "szelídgesztenye";

    //                var json = String.Concat(@"models=[{
    //  ""DisplayName"": """, newDisplayName, @""",
    //  ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
    //  ""Index"": 42
    //}]");

    //                var output = new StringWriter();
    //                var pc = CreatePortalContext("/OData.svc/" + path, "", output);
    //                var handler = new ODataHandler();
    //                var stream = CreateRequestStream(json);

    //                handler.ProcessRequest(pc.OwnerHttpContext, "MERGE", stream);

    //                var c = Content.Load(id);
    //                var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
    //                var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

    //                Assert.IsTrue(c.DisplayName == newDisplayName);
    //                Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370");
    //                Assert.IsTrue(c.ContentHandler.Index == 42);
    //                Assert.IsTrue((string)c["Make"] == "Not default");
    //            });
    //        }

    //        [TestMethod]
    //        public void OData_Posting_Creating()
    //        {
    //            Test(() =>
    //            {
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();

    //                var name = Guid.NewGuid().ToString();
    //                var displayName = Guid.NewGuid().ToString();
    //                var path = RepositoryPath.Combine(testRoot.Path, name);
    //                //var json = string.Concat(@"models=[{""Id"":"""",""IsFile"":false,""Name"":"""",""DisplayName"":""", displayName, @""",""ModifiedBy"":null,""ModificationDate"":null,""CreationDate"":null,""Actions"":null}]");
    //                var json = string.Concat(@"models=[{""Name"":""", name, @""",""DisplayName"":""", displayName,
    //                    @""",""Index"":41}]");

    //                var output = new StringWriter();
    //                var pc = CreatePortalContext("/OData.svc/" + testRoot.Path, "", output);
    //                var handler = new ODataHandler();
    //                var stream = CreateRequestStream(json);

    //                handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

    //                var content = Content.Load(path);
    //                Assert.IsTrue(content.DisplayName == displayName);
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Posting_Creating_ExplicitType()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();

    //                var name = Guid.NewGuid().ToString();
    //                var path = RepositoryPath.Combine(testRoot.Path, name);
    //                //var json = string.Concat(@"models=[{""Id"":"""",""IsFile"":false,""Name"":"""",""DisplayName"":""", displayName, @""",""ModifiedBy"":null,""ModificationDate"":null,""CreationDate"":null,""Actions"":null}]");
    //                var json = string.Concat(@"models=[{""__ContentType"":""Car"",""Name"":""", name, @"""}]");

    //                var output = new StringWriter();
    //                var pc = CreatePortalContext("/OData.svc/" + testRoot.Path, "", output);
    //                var handler = new ODataHandler();
    //                var stream = CreateRequestStream(json);

    //                handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

    //                var content = Content.Load(path);
    //                Assert.IsTrue(content.ContentType.Name == "Car");
    //                Assert.IsTrue(content.Name == name);
    //            });
    //        }

    //        [TestMethod]
    //        public void OData_Deleting()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();

    //                var name = Guid.NewGuid().ToString();
    //                var content = Content.CreateNew("Car", testRoot, name);
    //                content.Save();
    //                var path = string.Concat("/OData.svc/", testRoot.Path, "('", name, "')");

    //                var output = new StringWriter();
    //                var pc = CreatePortalContext(path, "", output);
    //                var handler = new ODataHandler();
    //                handler.ProcessRequest(pc.OwnerHttpContext, "DELETE", null);

    //                var repoPath = string.Concat(testRoot.Path, "/", name);
    //                Assert.IsTrue(Node.Exists(repoPath) == false);
    //            });
    //        }

    //        [TestMethod]
    //        public void OData_Post_References()
    //        {
    //            Test(() =>
    //            {
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                EnsureReferenceTestStructure(testRoot);
    //                CreateTestSite();
    //                var refs = new[] {Repository.Root, Repository.ImsFolder};

    //                var name1 = Guid.NewGuid().ToString();
    //                var path1 = RepositoryPath.Combine(testRoot.Path, name1);

    //                var pathRefs = "[" + String.Join(",", refs.Select(n => "\"" + n.Path + "\"")) + "]";
    //                var idRefs = "[" + String.Join(",", refs.Select(n => n.Id)) + "]";
    //                var simpleRefNode = Node.LoadNode(4);

    //                var json1 = string.Concat(
    //                    @"models=[{""__ContentType"":""OData_ReferenceTest_ContentHandler"",""Name"":""", name1,
    //                    @""",""Reference"":", pathRefs, @",""References"":", pathRefs, @",""Reference2"":""",
    //                    simpleRefNode.Path, @"""}]");

    //                var output1 = new StringWriter();
    //                var pc1 = CreatePortalContext("/OData.svc/" + testRoot.Path, "", output1);
    //                var handler1 = new ODataHandler();
    //                var stream1 = CreateRequestStream(json1);

    //                handler1.ProcessRequest(pc1.OwnerHttpContext, "POST", stream1);
    //                CheckError(output1);

    //                var node1 = Node.Load<OData_ReferenceTest_ContentHandler>(path1);
    //                var reloadedRefs1 = "[" + String.Join(",", node1.References.Select(n => +n.Id)) + "]";
    //                Assert.AreEqual(idRefs, reloadedRefs1);
    //                Assert.AreEqual(refs[0].Id, node1.Reference.Id);
    //                Assert.AreEqual(simpleRefNode.Id, node1.Reference2.Id);

    //                /*--------------------------------------------------------------*/

    //                var name2 = Guid.NewGuid().ToString();
    //                var path2 = RepositoryPath.Combine(testRoot.Path, name2);

    //                var json2 = string.Concat(
    //                    @"models=[{""__ContentType"":""OData_ReferenceTest_ContentHandler"",""Name"":""", name2,
    //                    @""",""Reference"":", idRefs, @",""References"":", idRefs, @",""Reference2"":", simpleRefNode.Id,
    //                    @"}]");

    //                var output2 = new StringWriter();
    //                var pc2 = CreatePortalContext("/OData.svc/" + testRoot.Path, "", output2);
    //                var handler2 = new ODataHandler();
    //                var stream2 = CreateRequestStream(json2);

    //                handler2.ProcessRequest(pc2.OwnerHttpContext, "POST", stream2);
    //                CheckError(output2);

    //                var node2 = Node.Load<OData_ReferenceTest_ContentHandler>(path2);
    //                var reloadedRefs2 = "[" + String.Join(",", node2.References.Select(n => n.Id)) + "]";
    //                Assert.AreEqual(idRefs, reloadedRefs2);
    //                Assert.AreEqual(refs[0].Id, node2.Reference.Id);
    //                Assert.AreEqual(simpleRefNode.Id, node2.Reference2.Id);
    //            });
    //        }

    //        [TestMethod]
    //        public void OData_NameEncoding_CreateAndRename()
    //        {
    //            Test(() =>
    //            {
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();

    //                var guid = Guid.NewGuid().ToString().Replace("-", "");
    //                var name = "*_|" + guid;
    //                var encodedName = ContentNamingProvider.GetNameFromDisplayName(name);
    //                var newName = ContentNamingProvider.GetNameFromDisplayName("___" + guid);

    //                // creating

    //                var json = string.Concat(@"models=[{""Name"":""", name, @"""}]");

    //                var output = new StringWriter();
    //                var pc = CreatePortalContext("/OData.svc/" + testRoot.Path, "", output);
    //                var handler = new ODataHandler();
    //                var stream = CreateRequestStream(json);

    //                handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                CheckError(output);
    //                var entity = GetEntity(output);
    //                Assert.AreEqual(encodedName, entity.Name);

    //                // renaming

    //                json = string.Concat(@"models=[{""Name"":""", newName, @"""}]");

    //                output = new StringWriter();
    //                pc = CreatePortalContext("/OData.svc/" + entity.Path, "", output);
    //                handler = new ODataHandler();
    //                stream = CreateRequestStream(json);

    //                handler.ProcessRequest(pc.OwnerHttpContext, "PATCH", stream);
    //                CheckError(output);

    //                var node = Node.LoadNode(entity.Id);
    //                Assert.AreEqual(newName, node.Name);
    //            });
    //        }

    //        //-----------------------------------------------------------------------------------------------------------------------------------------

    //        [TestMethod]
    //        public void OData_Security_GetPermissions_ACL()
    //        {
    //            Test(() =>
    //            {
    //                //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/GetPermissions
    //                //Stream:
    //                //Result: {
    //                //    "id": 4108,
    //                //    "path": "/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Document_Library",
    //                //    "inherits": true,
    //                //    "entries": [
    //                //        {
    //                //            "identity": { "path": "/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Groups/Owners", ...
    //                //            "permissions": {
    //                //                "See": {...

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    JContainer json;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(
    //                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/GetPermissions", "", output);
    //                        var handler = new ODataHandler();
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", Stream.Null);
    //                        json = Deserialize(output);
    //                    }
    //                    var entries = json["entries"];
    //                    Assert.IsNotNull(entries);
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Security_GetPermissions_ACE()
    //        {
    //            Test(() =>
    //            {
    //                //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/GetPermissions
    //                //Stream: {identity:"/root/ims/builtin/portal/visitor"}
    //                //Result: {
    //                //    "identity": { "id:": 7,  "path": "/Root/IMS/BuiltIn/Portal/Administrators",…},
    //                //    "permissions": {
    //                //        "See": { "value": "allow", "from": "/root" }
    //                //       ...

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    JContainer json;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(
    //                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/GetPermissions", "", output);
    //                        var handler = new ODataHandler();
    //                        var stream = CreateRequestStream("{identity:\"/root/ims/builtin/portal/visitor\"}");
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                        json = Deserialize(output);
    //                    }
    //                    var identity = json[0]["identity"];
    //                    var permissions = json[0]["permissions"];
    //                    Assert.IsTrue(identity != null);
    //                    Assert.IsTrue(permissions != null);
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }

    //        [TestMethod]
    //        public void OData_Security_HasPermission_Administrator()
    //        {
    //            Test(() =>
    //            {
    //                //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
    //                //Stream: {user:"/root/ims/builtin/portal/admin", permissions:["Open","Save"] }
    //                //result: true

    //                SecurityHandler.CreateAclEditor()
    //                    .Allow(Repository.Root.Id, Group.Administrators.Id, false, PermissionType.Open)
    //                    .Allow(Repository.Root.Id, Group.Administrators.Id, false, PermissionType.Save)
    //                    .Apply();

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                var hasPermission = SecurityHandler.HasPermission(
    //                    User.Administrator, Group.Administrators, PermissionType.Open, PermissionType.Save);
    //                Assert.IsTrue(hasPermission);

    //                CreateTestSite();
    //                try
    //                {
    //                    string result;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(
    //                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
    //                        var handler = new ODataHandler();
    //                        var stream = CreateRequestStream(String.Concat("{user:\"", User.Administrator.Path,
    //                            "\", permissions:[\"Open\",\"Save\"] }"));
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                        result = GetStringResult(output);
    //                    }
    //                    Assert.AreEqual("true", result);
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Security_HasPermission_Visitor()
    //        {
    //            Test(() =>
    //            {
    //                //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
    //                //Stream: {user:"/root/ims/builtin/portal/visitor", permissions:["Open","Save"] }
    //                //result: false

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    string result;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(
    //                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
    //                        var handler = new ODataHandler();
    //                        var stream =
    //                            CreateRequestStream(
    //                                "{user:\"/root/ims/builtin/portal/visitor\", permissions:[\"Open\",\"Save\"] }");
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                        result = GetStringResult(output);
    //                    }
    //                    Assert.IsTrue(result == "false");
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Security_HasPermission_NullUser()
    //        {
    //            Test(() =>
    //            {
    //                //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
    //                //Stream: {user:null, permissions:["Open","Save"] }
    //                //result: true

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    string result;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(
    //                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
    //                        var handler = new ODataHandler();
    //                        var stream = CreateRequestStream("{user:null, permissions:[\"Open\",\"Save\"] }");
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                        result = GetStringResult(output);
    //                    }
    //                    Assert.IsTrue(result == "true");
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Security_HasPermission_WithoutUser()
    //        {
    //            Test(() =>
    //            {
    //                //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
    //                //Stream: {permissions:["Open","Save"] }
    //                //result: true

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    string result;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(
    //                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
    //                        var handler = new ODataHandler();
    //                        var stream = CreateRequestStream("{permissions:[\"Open\",\"Save\"] }");
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                        result = GetStringResult(output);
    //                    }
    //                    Assert.IsTrue(result == "true");
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Security_HasPermission_Error_IdentityNotFound()
    //        {
    //            Test(() =>
    //            {
    //                //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
    //                //Stream: {user:"/root/ims/builtin/portal/nobody", permissions:["Open","Save"] }
    //                //result: ERROR: ODataException: Content not found: /root/ims/builtin/portal/nobody

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    ODataError error;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(
    //                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
    //                        var handler = new ODataHandler();
    //                        var stream =
    //                            CreateRequestStream(
    //                                "{user:\"/root/ims/builtin/portal/nobody\", permissions:[\"Open\",\"Save\"] }");
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                        error = GetError(output);
    //                    }
    //                    Assert.IsTrue(error.Code == ODataExceptionCode.ResourceNotFound);
    //                    Assert.IsTrue(error.Message == "Identity not found: /root/ims/builtin/portal/nobody");
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Security_HasPermission_Error_UnknownPermission()
    //        {
    //            Test(() =>
    //            {
    //                //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
    //                //Stream: {permissions:["Open","Save1"] }
    //                //result: ERROR: ODataException: Unknown permission: Save1

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    ODataError error;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(
    //                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
    //                        var handler = new ODataHandler();
    //                        var stream = CreateRequestStream("{permissions:[\"Open\",\"Save1\"] }");
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                        error = GetError(output);
    //                    }
    //                    Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified);
    //                    Assert.IsTrue(error.Message == "Unknown permission: Save1");
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Security_HasPermission_Error_MissingParameter()
    //        {
    //            Test(() =>
    //            {
    //                //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
    //                //Stream:
    //                //result: ERROR: "ODataException: Value cannot be null.\\nParameter name: permissions

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    ODataError error;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(
    //                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
    //                        var handler = new ODataHandler();
    //                        //var stream = CreateRequestStream("{user:\"/root/ims/builtin/portal/nobody\", permissions:[\"Open\",\"Save\"] }");
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", null);
    //                        error = GetError(output);
    //                    }
    //                    Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified);
    //                    Assert.IsTrue(error.Message == "Value cannot be null.\\nParameter name: permissions");
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }

    //        [TestMethod]
    //        public void OData_Security_SetPermissions()
    //        {
    //            Test(() =>
    //            {
    //                //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/SetPermission
    //                //Stream: {r:[{identity:"/Root/IMS/BuiltIn/Portal/Visitor", OpenMinor:"allow", Save:"deny"},{identity:"/Root/IMS/BuiltIn/Portal/Creators", Custom16:"A", Custom17:"1"}]}
    //                //result: (nothing)

    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
    //                content.Save();
    //                var resourcePath = ODataHandler.GetEntityUrl(content.Path);

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    string result;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
    //                            output);
    //                        var handler = new ODataHandler();
    //                        var stream =
    //                            CreateRequestStream(
    //                                "{r:[{identity:\"/Root/IMS/BuiltIn/Portal/Visitor\", OpenMinor:\"allow\", Save:\"deny\"},{identity:\"/Root/IMS/BuiltIn/Portal/Owners\", Custom16:\"A\", Custom17:\"1\"}]}");
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                        result = GetStringResult(output);
    //                    }
    //                    Assert.IsTrue(result.Length == 0);
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                    content.DeletePhysical();
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Security_SetPermissions_NotPropagates()
    //        {
    //            Test(() =>
    //            {
    //                //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/SetPermission
    //                //Stream: {r:[{identity:"/Root/IMS/BuiltIn/Portal/Visitor", OpenMinor:"allow", Save:"deny"},{identity:"/Root/IMS/BuiltIn/Portal/Creators", Custom16:"A", Custom17:"1"}]}
    //                //result: (nothing)

    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                var content = Content.CreateNew("Folder", testRoot, Guid.NewGuid().ToString());
    //                content.Save();
    //                var folderPath = ODataHandler.GetEntityUrl(content.Path);
    //                var folderRepoPath = content.Path;
    //                content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
    //                content.Save();
    //                var carRepoPath = content.Path;

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    string result;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(string.Concat("/OData.svc/", folderPath, "/SetPermissions"), "",
    //                            output);
    //                        var handler = new ODataHandler();
    //                        var stream =
    //                            CreateRequestStream(
    //                                "{r:[{identity:\"/Root/IMS/BuiltIn/Portal/Visitor\", OpenMinor:\"allow\", propagates:false}]}");
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                        result = GetStringResult(output);
    //                    }
    //                    Assert.IsTrue(result.Length == 0);
    //                    var folder = Node.LoadNode(folderRepoPath);
    //                    var car = Node.LoadNode(carRepoPath);

    //                    Assert.IsTrue(folder.Security.HasPermission((IUser)User.Visitor, PermissionType.OpenMinor));
    //                    Assert.IsFalse(car.Security.HasPermission((IUser)User.Visitor, PermissionType.OpenMinor));
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                    content.DeletePhysical();
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Security_Break()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
    //                content.Save();
    //                var resourcePath = ODataHandler.GetEntityUrl(content.Path);

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    string result;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
    //                            output);
    //                        var handler = new ODataHandler();
    //                        var stream = CreateRequestStream("{inheritance:\"break\"}");
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                        result = GetStringResult(output);
    //                    }
    //                    Assert.IsTrue(result.Length == 0);
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                    content.DeletePhysical();
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Security_Unbreak()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
    //                content.Save();
    //                var resourcePath = ODataHandler.GetEntityUrl(content.Path);

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    string result;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
    //                            output);
    //                        var handler = new ODataHandler();
    //                        var stream = CreateRequestStream("{inheritance:\"unbreak\"}");
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                        result = GetStringResult(output);
    //                    }
    //                    Assert.IsTrue(result.Length == 0);
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                    content.DeletePhysical();
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Security_Error_MissingStream()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
    //                content.Save();
    //                var resourcePath = ODataHandler.GetEntityUrl(content.Path);

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    ODataError error;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
    //                            output);
    //                        var handler = new ODataHandler();
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", null);
    //                        error = GetError(output);
    //                    }
    //                    var expectedMessage = "Value cannot be null.\\nParameter name: stream";
    //                    Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified);
    //                    Assert.IsTrue(error.Message == expectedMessage);
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Security_Error_BothParameters()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
    //                content.Save();
    //                var resourcePath = ODataHandler.GetEntityUrl(content.Path);

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    ODataError error;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
    //                            output);
    //                        var handler = new ODataHandler();
    //                        var stream =
    //                            CreateRequestStream(
    //                                "{r:[{identity:\"/Root/IMS/BuiltIn/Portal/Visitor\", OpenMinor:\"allow\"}], inheritance:\"break\"}");
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                        error = GetError(output);
    //                    }
    //                    var expectedMessage = "Cannot use  r  and  inheritance  parameters at the same time.";
    //                    Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified);
    //                    Assert.IsTrue(error.Message == expectedMessage);
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                    content.DeletePhysical();
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Security_Error_InvalidInheritanceParam()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
    //                content.Save();
    //                var resourcePath = ODataHandler.GetEntityUrl(content.Path);

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    ODataError error;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
    //                            output);
    //                        var handler = new ODataHandler();
    //                        var stream = CreateRequestStream("{inheritance:\"dance\"}");
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                        error = GetError(output);
    //                    }
    //                    var expectedMessage = "The value of the  inheritance  must be  break  or  unbreak .";
    //                    Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified);
    //                    Assert.IsTrue(error.Message == expectedMessage);
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                    content.DeletePhysical();
    //                }
    //            });
    //        }

    //        //-----------------------------------------------------------------------------------------------------------------------------------------

    //        [TestMethod]
    //        public void OData_ServiceDocument()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var site = CreateTestSite();
    //                var allowedTypes = site.GetAllowedChildTypeNames().ToList();
    //                allowedTypes.Add("Car");
    //                allowedTypes.Add("File");
    //                site.AllowChildTypes(allowedTypes);
    //                site.Save();

    //                var folder1 = Content.CreateNew("Folder", site, "Folder_1");
    //                folder1.Save();
    //                var car1 = Content.CreateNew("Car", site, "Car_1");
    //                car1.Save();
    //                var file1 = Content.CreateNew("File", site, "File_1");
    //                file1.Save();

    //                var containers = new[] {folder1, car1, file1};
    //                var names = String.Join(",", containers.Select(c => String.Concat("\"", c.Name, "\"")));
    //                var expectedJson = String.Concat("{\"d\":{\"EntitySets\":[", names, "]}}");

    //                string json;
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc", "", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    json = GetStringResult(output);
    //                }
    //                var result = json.Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
    //                Assert.AreEqual(expectedJson, result);
    //            });
    //        }

    //        //-----------------------------------------------------------------------------------------------------------------------------------------

    //        [TestMethod]
    //        public void OData_Metadata_Global()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();

    //                XmlNamespaceManager nsmgr;
    //                XmlDocument metaXml;
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/$metadata", "", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    metaXml = GetMetadataXml(output.GetStringBuilder().ToString(), out nsmgr);
    //                }
    //                Assert.IsNotNull(metaXml);
    //                var allTypes = metaXml.SelectNodes("//x:EntityType", nsmgr);
    //                Assert.IsNotNull(allTypes);
    //                Assert.IsTrue(allTypes.Count == ContentType.GetContentTypes().Length);
    //                var rootTypes = metaXml.SelectNodes("//x:EntityType[not(@BaseType)]", nsmgr);
    //                Assert.IsNotNull(rootTypes);
    //                foreach (XmlElement node in rootTypes)
    //                {
    //                    var hasKey = node.SelectSingleNode("x:Key", nsmgr) != null;
    //                    var hasId = node.SelectSingleNode("x:Property[@Name = 'Id']", nsmgr) != null;
    //                    Assert.IsTrue(hasId == hasKey);
    //                }
    //                foreach (XmlElement node in metaXml.SelectNodes("//x:EntityType[@BaseType]", nsmgr))
    //                {
    //                    var hasKey = node.SelectSingleNode("x:Key", nsmgr) != null;
    //                    var hasId = node.SelectSingleNode("x:Property[@Name = 'Id']", nsmgr) != null;
    //                    Assert.IsFalse(hasKey);
    //                }
    //            });
    //        }

    //        //TODO: Remove inconclusive test result and implement this test.
    //        //[TestMethod]
    //        public void OData_Metadata_Instance_Entity()
    //        {
    //            Assert.Inconclusive("InMemorySchemaWriter.CreatePropertyType is partially implemented.");

    //            Test(() =>
    //            {
    //                string listDef = @"<?xml version='1.0' encoding='utf-8'?>
    //<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'>
    //	<Fields>
    //		<ContentListField name='#ListField1' type='ShortText'>
    //			<Configuration>
    //				<MaxLength>42</MaxLength>
    //			</Configuration>
    //		</ContentListField>
    //	</Fields>
    //</ContentListDefinition>
    //";
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                var listContent = Content.CreateNew("ContentList", testRoot, Guid.NewGuid().ToString());
    //                var list = (ContentList) listContent.ContentHandler;
    //                list.AllowChildTypes(new[]
    //                    {ContentType.GetByName("Folder"), ContentType.GetByName("File"), ContentType.GetByName("Car")});
    //                list.ContentListDefinition = listDef;
    //                listContent.Save();

    //                var itemFolder = Content.CreateNew("Folder", listContent.ContentHandler, Guid.NewGuid().ToString());
    //                itemFolder.Save();
    //                var itemContent = Content.CreateNew("Car", itemFolder.ContentHandler, Guid.NewGuid().ToString());
    //                itemContent.Save();

    //                CreateTestSite();

    //                XmlNamespaceManager nsmgr;
    //                XmlDocument metaXml;
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext(
    //                        String.Concat("/OData.svc", ODataHandler.GetEntityUrl(itemContent.Path), "/$metadata"), "",
    //                        output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    output.Flush();
    //                    var src = GetStringResult(output);
    //                    metaXml = GetMetadataXml(src, out nsmgr);
    //                }
    //                var allTypes = metaXml.SelectNodes("//x:EntityType", nsmgr);
    //                Assert.IsTrue(allTypes.Count == 1);
    //                var listProps = metaXml.SelectNodes("//x:EntityType/x:Property[@Name='#ListField1']", nsmgr);
    //                Assert.IsTrue(listProps.Count == 1);
    //            });
    //        }

    //        //TODO: Remove inconclusive test result and implement this test.
    //        //[TestMethod]
    //        public void OData_Metadata_Instance_Collection()
    //        {
    //            Assert.Inconclusive("InMemorySchemaWriter.CreatePropertyType is partially implemented.");

    //            Test(() =>
    //            {
    //                string listDef = @"<?xml version='1.0' encoding='utf-8'?>
    //<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'>
    //	<Fields>
    //		<ContentListField name='#ListField1' type='ShortText'>
    //			<Configuration>
    //				<MaxLength>42</MaxLength>
    //			</Configuration>
    //		</ContentListField>
    //	</Fields>
    //</ContentListDefinition>
    //";
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                var listContent = Content.CreateNew("ContentList", testRoot, Guid.NewGuid().ToString());
    //                var list = (ContentList) listContent.ContentHandler;
    //                list.AllowChildTypes(new[]
    //                    {ContentType.GetByName("Folder"), ContentType.GetByName("File"), ContentType.GetByName("Car")});
    //                list.ContentListDefinition = listDef;
    //                listContent.Save();

    //                var itemFolder = Content.CreateNew("Folder", listContent.ContentHandler, Guid.NewGuid().ToString());
    //                itemFolder.Save();

    //                CreateTestSite();

    //                XmlNamespaceManager nsmgr;
    //                XmlDocument metaXml;
    //                string src = null;
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext(String.Concat("/OData.svc", listContent.Path, "/$metadata"), "",
    //                        output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    output.Flush();
    //                    src = GetStringResult(output);
    //                    metaXml = GetMetadataXml(src, out nsmgr);
    //                }
    //                var allTypes = metaXml.SelectNodes("//x:EntityType", nsmgr);
    //                Assert.IsTrue(allTypes.Count > 1);
    //                var listProps = metaXml.SelectNodes("//x:EntityType/x:Property[@Name='#ListField1']", nsmgr);
    //                Assert.IsTrue(listProps.Count == allTypes.Count);
    //            });
    //        }

    //        /* ----------------------------------------------------------------------------------------------------------- */

    //        //[TestMethod]
    //        //public void OData_Filter_ContentField()
    //        //{
    //        //    var a = new[] { "Ferrari", "Porsche", "Ferrari", "Mercedes" };
    //        //    var names = new List<string>();
    //        //    foreach (var x in new[] { "Ferrari", "Porsche", "Ferrari", "Mercedes" })
    //        //    {
    //        //        var car = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
    //        //        car["Make"] = x;
    //        //        car.Save();
    //        //        names.Add(car.Name);
    //        //    }

    //        //    CreateTestSite();
    //        //    try
    //        //    {
    //        //        ODataEntities entities;
    //        //        using (var output = new StringWriter())
    //        //        {
    //        //            var pc = CreatePortalContext("/OData.svc" + testRoot.Path, "$filter=Make eq 'Ferrari'&enableautofilters=false", output);
    //        //            var handler = new ODataHandler();
    //        //            handler.ProcessRequest(pc.OwnerHttpContext);
    //        //            entities = GetEntities(output);
    //        //        }
    //        //        Assert.IsTrue(entities.Length == 2, String.Format("Count is {0}, expected: 2", entities.Length));
    //        //    }
    //        //    finally
    //        //    {
    //        //        CleanupTestSite();
    //        //    }
    //        //}

    //        //[TestMethod]
    //        //public void OData_Filter_InFolder()
    //        //{
    //        //    CreateTestSite();
    //        //    try
    //        //    {
    //        //        ODataEntities entities;
    //        //        using (var output = new StringWriter())
    //        //        {
    //        //            var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal", "$orderby=Id&$filter=Id lt (9 sub 2)", output);
    //        //            var handler = new ODataHandler();
    //        //            handler.ProcessRequest(pc.OwnerHttpContext);
    //        //            entities = GetEntities(output);
    //        //        }
    //        //        Assert.IsTrue(entities.Length == 2, String.Format("Count is {0}, expected: 2", entities.Length));
    //        //        Assert.IsTrue(entities[0].Id == 1, String.Format("entities[0].Id is {0}, expected: 1", entities[0].Id));
    //        //        Assert.IsTrue(entities[1].Id == 6, String.Format("entities[1].Id is {0}, expected: 6", entities[1].Id));
    //        //    }
    //        //    finally
    //        //    {
    //        //        CleanupTestSite();
    //        //    }
    //        //}

    //        [TestMethod]
    //        public void OData_Filter_ThroughReference()
    //        {
    //            Test(() =>
    //            {
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();
    //                EnsureReferenceTestStructure(testRoot);

    //                ODataEntities entities;
    //                using (var output = new StringWriter())
    //                {
    //                    var resourcePath = ODataHandler.GetEntityUrl(testRoot.Path + "/Referrer");
    //                    var url = String.Format("/OData.svc{0}/References", resourcePath);
    //                    var pc = CreatePortalContext(url, "$orderby=Index&$filter=Index lt 5 and Index gt 2", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    entities = GetEntities(output);
    //                }
    //                Assert.IsTrue(entities.Length == 2);
    //                Assert.IsTrue(entities[0].Index == 3);
    //                Assert.IsTrue(entities[1].Index == 4);

    //            });
    //        }

    //        [TestMethod]
    //        public void OData_Filter_ThroughReference_TopSkip()
    //        {
    //            Test(() =>
    //            {
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();
    //                EnsureReferenceTestStructure(testRoot);

    //                ODataEntities entities;
    //                using (var output = new StringWriter())
    //                {
    //                    var resourcePath = ODataHandler.GetEntityUrl(testRoot.Path + "/Referrer");
    //                    var url = String.Format("/OData.svc{0}/References", resourcePath);
    //                    var pc = CreatePortalContext(url, "$orderby=Index&$filter=Index lt 10&$top=3&$skip=1", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    entities = GetEntities(output);
    //                }
    //                var actual = String.Join(",", entities.Select(e => e.Index).ToArray());
    //                Assert.AreEqual("2,3,4", actual);
    //            });
    //        }
    //        //[TestMethod]
    //        //public void OData_Filter_IsFolder()
    //        //{
    //        //    var folder = new Folder(testRoot);
    //        //    folder.Name = Guid.NewGuid().ToString();
    //        //    folder.Save();

    //        //    var folder1 = new Folder(folder);
    //        //    folder1.Name = "Folder1";
    //        //    folder1.Save();

    //        //    var folder2 = new Folder(folder);
    //        //    folder2.Name = "Folder2";
    //        //    folder2.Save();

    //        //    var content = Content.CreateNew("Car", folder, null);
    //        //    content.Save();

    //        //    CreateTestSite();
    //        //    try
    //        //    {
    //        //        ODataEntities entities;
    //        //        using (var output = new StringWriter())
    //        //        {
    //        //            var pc = CreatePortalContext("/OData.svc" + folder.Path, "&$filter=IsFolder eq true", output);
    //        //            var handler = new ODataHandler();
    //        //            handler.ProcessRequest(pc.OwnerHttpContext);
    //        //            entities = GetEntities(output);
    //        //        }
    //        //        Assert.IsTrue(entities.Length == 2, String.Format("Count is {0}, expected: 2", entities.Length));
    //        //        Assert.IsTrue(entities[0].Id == folder1.Id, String.Format("entities[0].Id is {0}, expected: {1}", entities[0].Id, folder1.Id));
    //        //        Assert.IsTrue(entities[1].Id == folder2.Id, String.Format("entities[1].Id is {0}, expected: {1}", entities[1].Id, folder2.Id));
    //        //        using (var output = new StringWriter())
    //        //        {
    //        //            var pc = CreatePortalContext("/OData.svc" + folder.Path, "&$filter=IsFolder eq false", output);
    //        //            var handler = new ODataHandler();
    //        //            handler.ProcessRequest(pc.OwnerHttpContext);
    //        //            entities = GetEntities(output);
    //        //        }
    //        //        Assert.IsTrue(entities.Length == 1, String.Format("Count is {0}, expected: 1", entities.Length));
    //        //        Assert.IsTrue(entities[0].Id == content.Id, String.Format("entities[0].Id is {0}, expected: {1}", entities[0].Id, content.Id));
    //        //    }
    //        //    finally
    //        //    {
    //        //        CleanupTestSite();
    //        //    }
    //        //}

    //        [TestMethod]
    //        public void OData_FilteringAndPartitioningOperationResult_ChildrenDefinition()
    //        {
    //            Test(() =>
    //            {
    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    ODataEntities entities;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/ChildrenDefinitionFilteringTest", "",
    //                            output);
    //                        var handler = new ODataHandler();
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
    //                        entities = GetEntities(output);
    //                    }
    //                    var ids = String.Join(", ", entities.Select(e => e.Id));
    //                    var expids = String.Join(", ", CreateSafeContentQuery("InFolder:/Root/IMS/BuiltIn/Portal .AUTOFILTERS:OFF .REVERSESORT:Name .SKIP:2 .TOP:3")
    //                        .Execute().Identifiers);
    //                    // 8, 9, 7
    //                    Assert.AreEqual(expids, ids);
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_FilteringAndPartitioningOperationResult_ContentCollection()
    //        {
    //            Test(() =>
    //            {
    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    ODataEntities entities;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/CollectionFilteringTest",
    //                            "$skip=1&$top=3&$orderby=Name desc&$select=Id,Name&$filter=Id ne 10&metadata=no", output);
    //                        var handler = new ODataHandler();
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
    //                        entities = GetEntities(output);
    //                    }
    //                    var ids = String.Join(", ", entities.Select(e => e.Id));
    //                    var expids = String.Join(", ",
    //                        CreateSafeContentQuery("+InFolder:/Root/IMS/BuiltIn/Portal -Id:10 .AUTOFILTERS:OFF .REVERSESORT:Name .SKIP:1 .TOP:3")
    //                            .Execute().Identifiers);
    //                    // 8, 9, 7
    //                    Assert.AreEqual(expids, ids);
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }

    //        //[TestMethod]
    //        //public void OData_Filter_IsOf()
    //        //{
    //        //    var folder = new Folder(testRoot);
    //        //    folder.Name = Guid.NewGuid().ToString();
    //        //    folder.Save();

    //        //    var folder1 = new Folder(folder);
    //        //    folder1.Name = "Folder1";
    //        //    folder1.Save();

    //        //    var folder2 = new Folder(folder);
    //        //    folder2.Name = "Folder2";
    //        //    folder2.Save();

    //        //    var content = Content.CreateNew("Car", folder, null);
    //        //    content.Save();

    //        //    CreateTestSite();
    //        //    try
    //        //    {
    //        //        ODataEntities entities;
    //        //        using (var output = new StringWriter())
    //        //        {
    //        //            var pc = CreatePortalContext("/OData.svc" + folder.Path, "&$filter=isof('Folder')", output);
    //        //            var handler = new ODataHandler();
    //        //            handler.ProcessRequest(pc.OwnerHttpContext);
    //        //            entities = GetEntities(output);
    //        //        }
    //        //        Assert.IsTrue(entities.Length == 2, String.Format("Count is {0}, expected: 2", entities.Length));
    //        //        Assert.IsTrue(entities[0].Id == folder1.Id, String.Format("entities[0].Id is {0}, expected: {1}", entities[0].Id, folder1.Id));
    //        //        Assert.IsTrue(entities[1].Id == folder2.Id, String.Format("entities[1].Id is {0}, expected: {1}", entities[1].Id, folder2.Id));
    //        //        using (var output = new StringWriter())
    //        //        {
    //        //            var pc = CreatePortalContext("/OData.svc" + folder.Path, "&$filter=not isof('Folder')", output);
    //        //            var handler = new ODataHandler();
    //        //            handler.ProcessRequest(pc.OwnerHttpContext);
    //        //            entities = GetEntities(output);
    //        //        }
    //        //        Assert.IsTrue(entities.Length == 1, String.Format("Count is {0}, expected: 1", entities.Length));
    //        //        Assert.IsTrue(entities[0].Id == content.Id, String.Format("entities[0].Id is {0}, expected: {1}", entities[0].Id, content.Id));
    //        //    }
    //        //    finally
    //        //    {
    //        //        CleanupTestSite();
    //        //    }
    //        //}

    //        [TestMethod]
    //        public void OData_FilteringCollection_IsOf()
    //        {
    //            Test(() =>
    //            {
    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    ODataEntities entities;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/CollectionFilteringTest",
    //                            "&$select=Id,Name&metadata=no&$filter=isof('User')", output);
    //                        var handler = new ODataHandler();
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
    //                        entities = GetEntities(output);
    //                    }
    //                    var ids = String.Join(", ", entities.Select(e => e.Id).OrderBy(x => x).Select(x => x.ToString()));
    //                    var expids = String.Join(", ",
    //                        CreateSafeContentQuery("+InFolder:/Root/IMS/BuiltIn/Portal +TypeIs:User .AUTOFILTERS:OFF")
    //                            .Execute().Identifiers.OrderBy(x => x).Select(x => x.ToString()));
    //                    // 6, 1
    //                    Assert.AreEqual(expids, ids);

    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/CollectionFilteringTest",
    //                            "&$select=Id,Name&metadata=no&$filter=not isof('User')", output);
    //                        var handler = new ODataHandler();
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
    //                        entities = GetEntities(output);
    //                    }
    //                    ids = String.Join(", ", entities.Select(e => e.Id).OrderBy(x => x).Select(x => x.ToString()));
    //                    expids = String.Join(", ",
    //                        CreateSafeContentQuery("+InFolder:/Root/IMS/BuiltIn/Portal -TypeIs:User .AUTOFILTERS:OFF")
    //                            .Execute().Identifiers.OrderBy(x => x).Select(x => x.ToString()));
    //                    // 8, 9, 7
    //                    Assert.AreEqual(expids, ids);

    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }

    //        //[TestMethod]
    //        //public void OData_Filter_NamespaceAndMemberChain()
    //        //{
    //        //    CreateTestSite();
    //        //    try
    //        //    {
    //        //        ODataEntities entities;
    //        //        using (var output = new StringWriter())
    //        //        {
    //        //            var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal", "$filter=SenseNet.Services.OData.Tests.ODataFilterTestHelper/TestValue eq Name", output);
    //        //            var handler = new ODataHandler();
    //        //            handler.ProcessRequest(pc.OwnerHttpContext);
    //        //            entities = GetEntities(output);
    //        //        }
    //        //        var group = Node.Load<Group>("/Root/IMS/BuiltIn/Portal/Administrators");
    //        //        Assert.AreEqual(1, entities.Count());
    //        //        Assert.AreEqual(group.Path, entities.First().Path);
    //        //    }
    //        //    finally
    //        //    {
    //        //        CleanupTestSite();
    //        //    }
    //        //}

    //        [TestMethod]
    //        public void OData_Filter_AspectField()
    //        {
    //            Test(() =>
    //            {
    //                var aspectName = "Aspect" + Guid.NewGuid().ToString("N");
    //                var aspect = new Aspect(Repository.AspectsFolder) {Name = aspectName};
    //                aspect.AddFields(new FieldInfo
    //                {
    //                    Name = "Field1",
    //                    DisplayName = "Field1 DisplayName",
    //                    Description = "Field1 description",
    //                    Type = "ShortText",
    //                    Indexing = new IndexingInfo
    //                    {
    //                        IndexHandler = "SenseNet.Search.Indexing.LowerStringIndexHandler"
    //                    }
    //                });

    //                var aspectFieldName = aspectName + ".Field1";
    //                var aspectFieldODataName = aspectName + "/Field1";

    //                var site = CreateTestSite();
    //                var content1 = Content.CreateNew("SystemFolder", site, Guid.NewGuid().ToString());
    //                content1.Index = 1;
    //                content1.Save();
    //                var content2 = Content.CreateNew("SystemFolder", site, Guid.NewGuid().ToString());
    //                content2.Index = 2;
    //                content2.Save();
    //                var content3 = Content.CreateNew("SystemFolder", site, Guid.NewGuid().ToString());
    //                content3.Index = 3;
    //                content3.Save();
    //                var content4 = Content.CreateNew("SystemFolder", site, Guid.NewGuid().ToString());
    //                content4.Index = 4;
    //                content4.Save();

    //                content2.AddAspects(aspect);
    //                content2[aspectFieldName] = "Value2";
    //                content2.Save();
    //                content3.AddAspects(aspect);
    //                content3[aspectFieldName] = "Value3";
    //                content3.Save();
    //                content4.AddAspects(aspect);
    //                content4[aspectFieldName] = "Value2";
    //                content4.Save();

    //                ODataEntities entities;
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc" + site.Path,
    //                        "$orderby=Index&$filter=" + aspectFieldODataName + " eq 'Value2'", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    entities = GetEntities(output);
    //                }
    //                var expected = String.Join(", ", (new[] {content2.Name, content4.Name}));
    //                var names = String.Join(", ", entities.Select(e => e.Name));
    //                Assert.AreEqual(expected, names);

    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Filter_AspectField_FieldNotFound()
    //        {
    //            Test(() =>
    //            {
    //                var aspectName = "Aspect" + Guid.NewGuid().ToString("N");
    //                var aspect = new Aspect(Repository.AspectsFolder) {Name = aspectName};
    //                aspect.AddFields(new FieldInfo
    //                {
    //                    Name = "Field1",
    //                    DisplayName = "Field1 DisplayName",
    //                    Description = "Field1 description",
    //                    Type = "ShortText",
    //                    Indexing = new IndexingInfo
    //                    {
    //                        IndexHandler = "SenseNet.Search.Indexing.LowerStringIndexHandler"
    //                    }
    //                });

    //                var site = CreateTestSite();

    //                ODataError error;
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc" + site.Path,
    //                        "$orderby=Index&$filter=" + aspectName + "/Field2 eq 'Value2'", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    error = GetError(output);
    //                }
    //                Assert.IsTrue(error.Message.Contains("Field not found"));

    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Filter_AspectField_FieldNotFoundButAspectFound()
    //        {
    //            Test(() =>
    //            {
    //                var aspectName = "Aspect" + Guid.NewGuid().ToString("N");
    //                var aspect = new Aspect(Repository.AspectsFolder) {Name = aspectName};
    //                aspect.AddFields(new FieldInfo
    //                {
    //                    Name = "Field1",
    //                    DisplayName = "Field1 DisplayName",
    //                    Description = "Field1 description",
    //                    Type = "ShortText",
    //                    Indexing = new IndexingInfo
    //                    {
    //                        IndexHandler = "SenseNet.Search.Indexing.LowerStringIndexHandler"
    //                    }
    //                });

    //                var site = CreateTestSite();

    //                ODataError error;
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc" + site.Path,
    //                        "$orderby=Index&$filter=" + aspectName + " eq 'Value2'", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    error = GetError(output);
    //                }
    //                Assert.IsTrue(error.Message.Contains("Field not found"));

    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Filter_AspectField_AspectNotFound()
    //        {
    //            Test(() =>
    //            {
    //                var aspectName = "Aspect" + Guid.NewGuid().ToString("N");
    //                var site = CreateTestSite();

    //                ODataError error;
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc" + site.Path,
    //                        "$orderby=Index&$filter=" + aspectName + "/Field1 eq 'Value1'", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    error = GetError(output);
    //                }
    //                Assert.IsTrue(error.Message.Contains("Field not found"));

    //            });
    //        }


    //        [TestMethod]
    //        public void OData_InvokeAction_Post_GetPutMergePatchDelete()
    //        {
    //            Test(() =>
    //            {
    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    string result = null;
    //                    ODataError error;

    //                    //------------------------------------------------------------ POST: ok
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/ODataAction", "", output);
    //                        var handler = new ODataHandler();
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
    //                        result = GetStringResult(output);
    //                    }
    //                    Assert.AreEqual("ODataAction executed.", result);

    //                    //------------------------------------------------------------ GET PUT MERGE PATCH DELETE: error
    //                    var verbs = new[] {"GET", "PUT", "MERGE", "PATCH", "DELETE"};
    //                    foreach (var verb in verbs)
    //                    {
    //                        using (var output = new StringWriter())
    //                        {
    //                            var pc = CreatePortalContext("/OData.svc/Root('IMS')/ODataAction", "", output);
    //                            var handler = new ODataHandler();
    //                            handler.ProcessRequest(pc.OwnerHttpContext, verb, MemoryStream.Null);
    //                            error = GetError(output);
    //                            if (error == null)
    //                                Assert.Fail("Exception was not thrown: " + verb);
    //                        }
    //                        Assert.AreEqual(ODataExceptionCode.IllegalInvoke, error.Code, String.Format(
    //                            "Error code is {0}, expected: {1}, verb: {2}"
    //                            , error.Code, ODataExceptionCode.IllegalInvoke, verb));
    //                    }
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_InvokeFunction_PostGet_PutMergePatchDelete()
    //        {
    //            Test(() =>
    //            {
    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    string result = null;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/ODataFunction", "", output);
    //                        var handler = new ODataHandler();
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
    //                        result = GetStringResult(output);
    //                    }
    //                    Assert.AreEqual("ODataFunction executed.", result);

    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/ODataFunction", "", output);
    //                        var handler = new ODataHandler();
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "GET", MemoryStream.Null);
    //                        result = GetStringResult(output);
    //                    }
    //                    Assert.AreEqual("ODataFunction executed.", result);

    //                    //------------------------------------------------------------ GET PUT MERGE PATCH DELETE: error
    //                    var verbs = new[] {"PUT", "MERGE", "PATCH", "DELETE"};
    //                    foreach (var verb in verbs)
    //                    {
    //                        ODataError error = null;
    //                        using (var output = new StringWriter())
    //                        {
    //                            var pc = CreatePortalContext("/OData.svc/Root('IMS')/ODataAction", "", output);
    //                            var handler = new ODataHandler();
    //                            handler.ProcessRequest(pc.OwnerHttpContext, verb, MemoryStream.Null);
    //                            error = GetError(output);
    //                            if (error == null)
    //                                Assert.Fail("Exception was not thrown: " + verb);
    //                        }
    //                        Assert.AreEqual(ODataExceptionCode.IllegalInvoke, error.Code, String.Format(
    //                            "Error code is {0}, expected: {1}, verb: {2}"
    //                            , error.Code, ODataExceptionCode.IllegalInvoke, verb));
    //                    }
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_InvokeDictionaryHandlerFunction()
    //        {
    //            Test(() =>
    //            {
    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    ODataEntities result = null;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(
    //                            "/OData.svc/Root/System/Schema/ContentTypes/GenericContent('FieldSettingContent')/ODataGetParentChainAction"
    //                            , "metadata=no&$select=Id,Name&$top=2&$inlinecount=allpages", output);
    //                        var handler = new ODataHandler();
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
    //                        result = GetEntities(output);
    //                    }
    //                    Assert.AreEqual(6, result.TotalCount);
    //                    Assert.AreEqual(2, result.Length);
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }

    //        /* =========================================================================================================== */

    //        //[TestMethod]
    //        //public void OData_GetEntityById()
    //        //{
    //        //    try
    //        //    {
    //        //        //var name = Guid.NewGuid().ToString();
    //        //        //var content = Content.CreateNew("Car", testRoot, name);
    //        //        //content.Save();
    //        //        //var contentId = content.Id;
    //        //        var content = Content.Load(1);
    //        //        var id = content.Id;

    //        //        ODataEntity entity;
    //        //        CreateTestSite();
    //        //        using (var output = new StringWriter())
    //        //        {
    //        //            var pc = CreatePortalContext("/OData.svc/Content(" + id + ")", "", output);
    //        //            var handler = new ODataHandler();
    //        //            handler.ProcessRequest(pc.OwnerHttpContext);
    //        //            //handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //        //            output.Flush();
    //        //            entity = GetEntity(output);
    //        //        }
    //        //        Assert.AreEqual(id, entity.Id);
    //        //        Assert.AreEqual(content.Path, entity.Path);
    //        //        Assert.AreEqual(content.Name, entity.Name);
    //        //        Assert.AreEqual(content.ContentType, entity.ContentType);
    //        //    }
    //        //    finally
    //        //    {
    //        //        CleanupTestSite();
    //        //    }
    //        //}
    //        //[TestMethod]
    //        //public void OData_GetEntityById_InvalidId()
    //        //{
    //        //    try
    //        //    {
    //        //        ODataError err;
    //        //        CreateTestSite();
    //        //        using (var output = new StringWriter())
    //        //        {
    //        //            var pc = CreatePortalContext("/OData.svc/Content(qwer)", "", output);
    //        //            var handler = new ODataHandler();
    //        //            handler.ProcessRequest(pc.OwnerHttpContext);
    //        //            output.Flush();
    //        //            err = GetError(output);
    //        //        }
    //        //        Assert.AreEqual(ODataExceptionCode.InvalidId, err.Code);
    //        //    }
    //        //    finally
    //        //    {
    //        //        CleanupTestSite();
    //        //    }
    //        //}
    //        //[TestMethod]
    //        //public void OData_GetPropertyOfEntityById()
    //        //{
    //        //    try
    //        //    {
    //        //        //var name = Guid.NewGuid().ToString();
    //        //        //var content = Content.CreateNew("Car", testRoot, name);
    //        //        //content.Save();
    //        //        //var contentId = content.Id;
    //        //        var content = Content.Load(1);
    //        //        var id = content.Id;

    //        //        string result;
    //        //        CreateTestSite();
    //        //        using (var output = new StringWriter())
    //        //        {
    //        //            var pc = CreatePortalContext("/OData.svc/Content(" + id + ")/Name", "", output);
    //        //            var handler = new ODataHandler();
    //        //            handler.ProcessRequest(pc.OwnerHttpContext);
    //        //            //handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //        //            output.Flush();
    //        //            result = GetStringResult(output);
    //        //        }
    //        //    }
    //        //    finally
    //        //    {
    //        //        CleanupTestSite();
    //        //    }
    //        //}

    //        [TestMethod]
    //        public void OData_Put_ModifyingById()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();

    //                var name = Guid.NewGuid().ToString();
    //                var content = Content.CreateNew("Car", testRoot, name);
    //                content.DisplayName = "vadalma";
    //                var defaultMake = (string) content["Make"];
    //                content["Make"] = "Not default";
    //                content.Save();
    //                var id = content.Id;
    //                var path = content.Path;
    //                var url = GetUrl(content.Path);

    //                var newDisplayName = "szelídgesztenye";

    //                var json = String.Concat(@"models=[{
    //  ""DisplayName"": """, newDisplayName, @""",
    //  ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
    //  ""Index"": 42
    //}]");

    //                var output = new StringWriter();
    //                var pc = CreatePortalContext("/OData.svc/content(" + id + ")", "", output);
    //                var handler = new ODataHandler();
    //                var stream = CreateRequestStream(json);

    //                handler.ProcessRequest(pc.OwnerHttpContext, "PUT", stream);

    //                var c = Content.Load(id);
    //                var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
    //                var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

    //                Assert.IsTrue(c.DisplayName == newDisplayName);
    //                Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370");
    //                Assert.IsTrue(c.ContentHandler.Index == 42);
    //                Assert.IsTrue((string)c["Make"] == null);

    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Patch_ModifyingById()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();

    //                var name = Guid.NewGuid().ToString();
    //                var content = Content.CreateNew("Car", testRoot, name);
    //                content.DisplayName = "vadalma";
    //                var defaultMake = (string) content["Make"];
    //                content["Make"] = "Not default";
    //                content.Save();
    //                var id = content.Id;
    //                var path = content.Path;
    //                var url = GetUrl(content.Path);

    //                var newDisplayName = "szelídgesztenye";

    //                var json = String.Concat(@"models=[{
    //  ""DisplayName"": """, newDisplayName, @""",
    //  ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
    //  ""Index"": 42
    //}]");

    //                var output = new StringWriter();
    //                var pc = CreatePortalContext("/OData.svc/content(" + id + ")", "", output);
    //                var handler = new ODataHandler();
    //                var stream = CreateRequestStream(json);

    //                handler.ProcessRequest(pc.OwnerHttpContext, "PATCH", stream);

    //                var c = Content.Load(id);
    //                var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
    //                var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

    //                Assert.IsTrue(c.DisplayName == newDisplayName);
    //                Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370");
    //                Assert.IsTrue(c.ContentHandler.Index == 42);
    //                Assert.IsTrue((string)c["Make"] == "Not default");

    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Merge_ModifyingById()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();

    //                var name = Guid.NewGuid().ToString();
    //                var content = Content.CreateNew("Car", testRoot, name);
    //                content.DisplayName = "vadalma";
    //                var defaultMake = (string) content["Make"];
    //                content["Make"] = "Not default";
    //                content.Save();
    //                var id = content.Id;
    //                var path = content.Path;
    //                var url = GetUrl(content.Path);

    //                var newDisplayName = "szelídgesztenye";

    //                var json = String.Concat(@"models=[{
    //  ""DisplayName"": """, newDisplayName, @""",
    //  ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
    //  ""Index"": 42
    //}]");

    //                var output = new StringWriter();
    //                var pc = CreatePortalContext("/OData.svc/content(" + id + ")", "", output);
    //                var handler = new ODataHandler();
    //                var stream = CreateRequestStream(json);

    //                handler.ProcessRequest(pc.OwnerHttpContext, "MERGE", stream);

    //                var c = Content.Load(id);
    //                var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
    //                var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

    //                Assert.IsTrue(c.DisplayName == newDisplayName);
    //                Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370");
    //                Assert.IsTrue(c.ContentHandler.Index == 42);
    //                Assert.IsTrue((string)c["Make"] == "Not default");

    //            });
    //        }

    //        [TestMethod]
    //        public void OData_Posting_Creating_UnderById()
    //        {
    //            Test(() =>
    //            {
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();

    //                var name = Guid.NewGuid().ToString();
    //                var displayName = Guid.NewGuid().ToString();
    //                var path = RepositoryPath.Combine(testRoot.Path, name);
    //                //var json = string.Concat(@"models=[{""Id"":"""",""IsFile"":false,""Name"":"""",""DisplayName"":""", displayName, @""",""ModifiedBy"":null,""ModificationDate"":null,""CreationDate"":null,""Actions"":null}]");
    //                var json = string.Concat(@"models=[{""Name"":""", name, @""",""DisplayName"":""", displayName,
    //                    @""",""Index"":41}]");

    //                var output = new StringWriter();
    //                var pc = CreatePortalContext("/OData.svc/content(" + testRoot.Id + ")", "", output);
    //                var handler = new ODataHandler();
    //                var stream = CreateRequestStream(json);

    //                handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

    //                var content = Content.Load(path);
    //                Assert.IsTrue(content.DisplayName == displayName);

    //            });
    //        }
    //        [TestMethod]
    //        public void OData_Posting_Creating_ExplicitType_UnderById()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();

    //                var name = Guid.NewGuid().ToString();
    //                var path = RepositoryPath.Combine(testRoot.Path, name);
    //                //var json = string.Concat(@"models=[{""Id"":"""",""IsFile"":false,""Name"":"""",""DisplayName"":""", displayName, @""",""ModifiedBy"":null,""ModificationDate"":null,""CreationDate"":null,""Actions"":null}]");
    //                var json = string.Concat(@"models=[{""__ContentType"":""Car"",""Name"":""", name, @"""}]");

    //                var output = new StringWriter();
    //                var pc = CreatePortalContext("/OData.svc/content(" + testRoot.Id + ")", "", output);
    //                var handler = new ODataHandler();
    //                var stream = CreateRequestStream(json);

    //                handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

    //                var content = Content.Load(path);
    //                Assert.IsTrue(content.ContentType.Name == "Car");
    //                Assert.IsTrue(content.Name == name);

    //            });
    //        }

    //        [TestMethod]
    //        public void OData_DeletingBy()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateTestSite();

    //                var name = Guid.NewGuid().ToString();
    //                var content = Content.CreateNew("Car", testRoot, name);
    //                content.Save();
    //                var path = string.Concat("/OData.svc/content(" + content.Id + ")");

    //                var output = new StringWriter();
    //                var pc = CreatePortalContext(path, "", output);
    //                var handler = new ODataHandler();
    //                handler.ProcessRequest(pc.OwnerHttpContext, "DELETE", null);

    //                var repoPath = string.Concat(testRoot.Path, "/", name);
    //                Assert.IsTrue(Node.Exists(repoPath) == false);

    //            });
    //        }

    //        /* =========================================================================================================== Bug reproductions */

    //        [TestMethod]
    //        public void OData_InconsistentNameAfterCreating()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");

    //                var name = Guid.NewGuid().ToString();
    //                var path = RepositoryPath.Combine(testRoot.Path, name);
    //                var json = string.Concat(@"models=[{""__ContentType"":""Car"",""Name"":""", name, @"""}]");

    //                ODataEntity entity;
    //                //string result;
    //                CreateTestSite();

    //                var names = new string[3];
    //                for (int i = 0; i < 3; i++)
    //                {
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(String.Concat("/OData.svc", ODataHandler.GetEntityUrl(testRoot.Path)),
    //                            "", output);
    //                        var handler = new ODataHandler();
    //                        var stream = CreateRequestStream(json);
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                        output.Flush();
    //                        //result = GetStringResult(output);
    //                        entity = GetEntity(output);
    //                    }
    //                    names[i] = entity.Name;
    //                }

    //                Assert.AreNotEqual(names[0], names[1]);
    //                Assert.AreNotEqual(names[0], names[2]);
    //                Assert.AreNotEqual(names[1], names[2]);
    //            });
    //        }

    //        [TestMethod]
    //        public void OData_ModifyWithInvisibleParent()
    //        {
    //            Test(() =>
    //            {
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                var root = new Folder(testRoot) {Name = Guid.NewGuid().ToString()};
    //                root.Save();
    //                var node = new Folder(root) {Name = Guid.NewGuid().ToString()};
    //                node.Save();

    //                SecurityHandler.CreateAclEditor()
    //                    .BreakInheritance(root.Id, new[] { EntryType.Normal })
    //                    .ClearPermission(root.Id, User.Visitor.Id, false, PermissionType.See)
    //                    .Allow(node.Id, User.Visitor.Id, false, PermissionType.Save)
    //                    .Apply();

    //                var savedUser = User.Current;

    //                CreateTestSite();
    //                try
    //                {
    //                    User.Current = User.Visitor;

    //                    ODataEntity entity;
    //                    using (var output = new StringWriter())
    //                    {
    //                        var json = String.Concat(@"models=[{""Index"": 42}]");
    //                        var pc = CreatePortalContext("/OData.svc" + node.Path, "", output);
    //                        var handler = new ODataHandler();
    //                        var stream = CreateRequestStream(json);
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "PATCH", stream);
    //                        CheckError(output);
    //                        entity = GetEntity(output);
    //                    }
    //                    node = Node.Load<Folder>(node.Id);
    //                    Assert.AreEqual(42, entity.Index);
    //                    Assert.AreEqual(42, node.Index);
    //                }
    //                finally
    //                {
    //                    User.Current = savedUser;
    //                }
    //            });
    //        }

    //        [TestMethod]
    //        public void OData_SortingByMappedDateTimeAspectField()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                CreateTestSite();
    //                var testRoot = CreateTestRoot("ODataTestRoot");

    //                // Create an aspect with date field that is mapped to CreationDate
    //                var aspect1Name = "OData_SortingByMappedDateTimeAspectField";
    //                var aspect1Definition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
    //<Fields>
    //    <AspectField name='Field1' type='DateTime'>
    //        <!-- not bound -->
    //    </AspectField>
    //    <AspectField name='Field2' type='DateTime'>
    //        <Bind property=""CreationDate""></Bind>
    //    </AspectField>
    //    <AspectField name='Field3' type='DateTime'>
    //        <Bind property=""ModificationDate""></Bind>
    //    </AspectField>
    //    </Fields>
    //</AspectDefinition>";

    //                var aspect1 = new Aspect(Repository.AspectsFolder)
    //                {
    //                    Name = aspect1Name,
    //                    AspectDefinition = aspect1Definition
    //                };
    //                aspect1.Save();

    //                var field1Name = String.Concat(aspect1Name, Aspect.ASPECTFIELDSEPARATOR, "Field1");
    //                var field2Name = String.Concat(aspect1Name, Aspect.ASPECTFIELDSEPARATOR, "Field2");
    //                var field3Name = String.Concat(aspect1Name, Aspect.ASPECTFIELDSEPARATOR, "Field3");

    //                var container = new SystemFolder(testRoot) {Name = Guid.NewGuid().ToString()};
    //                container.Save();

    //                var today = DateTime.Now;
    //                (new[] {3, 1, 5, 2, 4}).Select(i =>
    //                {
    //                    var content = Content.CreateNew("Car", container, "Car-" + i + "-" + Guid.NewGuid());
    //                    content.AddAspects(aspect1);

    //                    content[field1Name] = today.AddDays(-5 + i);
    //                    //content[field2Name] = today.AddDays(-i);
    //                    //content[field3Name] = today.AddDays(-i);
    //                    content.CreationDate = today.AddDays(-i);
    //                    content.ModificationDate = today.AddDays(-i);

    //                    content.Save();
    //                    return i;
    //                }).ToArray();

    //                // check prerequisits

    //                var r1 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
    //                    .OrderBy(c => c[field1Name]).ToArray();
    //                var result1 = String.Join(",", r1.Select(x => x.Name[4]));
    //                Assert.AreEqual("1,2,3,4,5", result1);
    //                var r2 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
    //                    .OrderByDescending(c => c[field1Name]).ToArray();
    //                var result2 = String.Join(",", r2.Select(x => x.Name[4]));
    //                Assert.AreEqual("5,4,3,2,1", result2);
    //                var r3 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
    //                    .OrderBy(c => c[field2Name]).ToArray();
    //                var result3 = String.Join(",", r3.Select(x => x.Name[4]));
    //                Assert.AreEqual("5,4,3,2,1", result3);
    //                var r4 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
    //                    .OrderByDescending(c => c[field2Name]).ToArray();
    //                var result4 = String.Join(",", r4.Select(x => x.Name[4]));
    //                Assert.AreEqual("1,2,3,4,5", result4);
    //                var r5 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
    //                    .OrderBy(c => c[field3Name]).ToArray();
    //                var result5 = String.Join(",", r5.Select(x => x.Name[4]));
    //                Assert.AreEqual("5,4,3,2,1", result5);
    //                var r6 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
    //                    .OrderByDescending(c => c[field3Name]).ToArray();
    //                var result6 = String.Join(",", r6.Select(x => x.Name[4]));
    //                Assert.AreEqual("1,2,3,4,5", result6);

    //                //------------------------------------------

    //                ODataEntities entities;

    //                // Field1 ASC
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/" + container.Path, "$orderby=" + field1Name + " asc",
    //                        output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    entities = GetEntities(output);
    //                }
    //                Assert.AreEqual(5, entities.Length);
    //                Assert.AreEqual("1,2,3,4,5", string.Join(",", entities.Select(e => e.Name[4])));

    //                // Field1 DESC
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/" + container.Path, "$orderby=" + field1Name + " desc",
    //                        output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    entities = GetEntities(output);
    //                }
    //                Assert.AreEqual(5, entities.Length);
    //                Assert.AreEqual("5,4,3,2,1", string.Join(",", entities.Select(e => e.Name[4])));



    //                // Field2 ASC
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/" + container.Path, "$orderby=" + field2Name + " asc",
    //                        output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    entities = GetEntities(output);
    //                }
    //                Assert.AreEqual(5, entities.Length);
    //                Assert.AreEqual("5,4,3,2,1", string.Join(",", entities.Select(e => e.Name[4])));

    //                // Field2 DESC
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/" + container.Path, "$orderby=" + field2Name + " desc",
    //                        output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    entities = GetEntities(output);
    //                }
    //                Assert.AreEqual(5, entities.Length);
    //                Assert.AreEqual("1,2,3,4,5", string.Join(",", entities.Select(e => e.Name[4])));



    //                // Field3 ASC
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/" + container.Path, "$orderby=" + field2Name + " asc",
    //                        output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    entities = GetEntities(output);
    //                }
    //                Assert.AreEqual(5, entities.Length);
    //                Assert.AreEqual("5,4,3,2,1", string.Join(",", entities.Select(e => e.Name[4])));

    //                // Field3 DESC
    //                using (var output = new StringWriter())
    //                {
    //                    var pc = CreatePortalContext("/OData.svc/" + container.Path, "$orderby=" + field2Name + " desc",
    //                        output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext);
    //                    entities = GetEntities(output);
    //                }
    //                Assert.AreEqual(5, entities.Length);
    //                Assert.AreEqual("1,2,3,4,5", string.Join(",", entities.Select(e => e.Name[4])));
    //            });
    //        }

    //        /* =========================================================================================================== Bug reproductions 2 */

    //        [TestMethod]
    //        public void OData_FIX_DoNotUrlDecodeTheRequestStream()
    //        {
    //            Test(() =>
    //            {
    //                //var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                //var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                //odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());
    //                CreateGlobalActions();
    //                CreateTestSite();

    //                var testString = "a&b c+d%20e";
    //                string result = null;
    //                //------------------------------------------------------------ POST: ok
    //                using (var output = new StringWriter())
    //                {
    //                    //"{iii: 42, sss: 'asdf' }"
    //                    var json = $"{{testString: \'{testString}\' }}";
    //                    var stream = CreateRequestStream(json);
    //                    var pc = CreatePortalContext("/OData.svc/Root('IMS')/ParameterEcho", "", output);
    //                    var handler = new ODataHandler();
    //                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
    //                    result = GetStringResult(output);
    //                }
    //                Assert.AreEqual(testString, result);
    //            });
    //        }

    //        [TestMethod]
    //        public void OData_FIX_AutoFiltersInQueryAndParams()
    //        {
    //            Test(() =>
    //            {
    //                CreateTestSite();
    //                var urls = new[]
    //                {
    //                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder&$select=Path,Type",
    //                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder .AUTOFILTERS:OFF&$select=Path,Type",
    //                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder .AUTOFILTERS:ON&$select=Path,Type",
    //                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder&$select=Path,Type&enableautofilters=false",
    //                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder .AUTOFILTERS:OFF&$select=Path,Type&enableautofilters=false",
    //                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder .AUTOFILTERS:ON&$select=Path,Type&enableautofilters=false",
    //                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder&$select=Path,Type&enableautofilters=true",
    //                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder .AUTOFILTERS:OFF&$select=Path,Type&enableautofilters=true",
    //                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder .AUTOFILTERS:ON&$select=Path,Type&enableautofilters=true"
    //                };

    //                var actual = String.Join(" ",
    //                    urls.Select(u => GetResultFor_OData_FIX_AutoFiltersInQueryAndParams(u) == "0" ? "0" : "1"));
    //                Assert.AreEqual("0 1 0 1 1 1 0 0 0", actual);
    //            });
    //        }
    //        private string GetResultFor_OData_FIX_AutoFiltersInQueryAndParams(string url)
    //        {
    //            string result = null;
    //            var sides = url.Split('?');
    //            using (var output = new StringWriter())
    //            {
    //                var pc = CreatePortalContext(sides[0], sides[1], output);
    //                var handler = new ODataHandler();
    //                handler.ProcessRequest(pc.OwnerHttpContext);
    //                result = GetStringResult(output);
    //            }
    //            return result;
    //        }

    //        //TODO: Remove inconclusive test result and implement this test.
    //        //[TestMethod]
    //        public void OData_FIX_Move_RightExceptionIfTargetExists()
    //        {
    //            Assert.Inconclusive("InMemoryDataProvider.LoadChildTypesToAllow method is not implemented.");

    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateStructureFor_RightExceptionIfTargetExistsTests(testRoot, out var sourcePath, out var targetContainerPath);

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    //------------------------------------------------------------------------------------------------------------------------ test 1
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(
    //                            string.Format("/OData.svc/{0}('{1}')/MoveTo", RepositoryPath.GetParentPath(sourcePath),
    //                                RepositoryPath.GetFileName(sourcePath)), "", output);
    //                        var handler = new ODataHandler();
    //                        var stream = CreateRequestStream("{\"targetPath\":\"" + targetContainerPath + "\"}");
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

    //                        var error = GetError(output);
    //                        Assert.AreEqual(ODataExceptionCode.ContentAlreadyExists, error.Code);
    //                        Assert.IsTrue(error.Message.ToLowerInvariant().Contains("cannot move the content"));
    //                    }
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }
    //        [TestMethod]
    //        public void OData_FIX_Copy_RightExceptionIfTargetExists()
    //        {
    //            Test(() =>
    //            {
    //                InstallCarContentType();
    //                var testRoot = CreateTestRoot("ODataTestRoot");
    //                CreateStructureFor_RightExceptionIfTargetExistsTests(testRoot, out var sourcePath, out var targetContainerPath);

    //                var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
    //                var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
    //                odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

    //                CreateTestSite();
    //                try
    //                {
    //                    //------------------------------------------------------------------------------------------------------------------------ test 1
    //                    using (var output = new StringWriter())
    //                    {
    //                        var pc = CreatePortalContext(
    //                            string.Format("/OData.svc/{0}('{1}')/CopyTo", RepositoryPath.GetParentPath(sourcePath),
    //                                RepositoryPath.GetFileName(sourcePath)), "", output);
    //                        var handler = new ODataHandler();
    //                        var stream = CreateRequestStream("{\"targetPath\":\"" + targetContainerPath + "\"}");
    //                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

    //                        var error = GetError(output);
    //                        Assert.AreEqual(ODataExceptionCode.ContentAlreadyExists, error.Code);
    //                        Assert.IsTrue(error.Message.ToLowerInvariant().Contains("cannot copy the content"));
    //                    }
    //                }
    //                finally
    //                {
    //                    odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
    //                }
    //            });
    //        }
    //        public void CreateStructureFor_RightExceptionIfTargetExistsTests(Node testRoot, out string sourcePath, out string targetContainerPath)
    //        {
    //            var sourceFolder = new SystemFolder(testRoot) { Name = Guid.NewGuid().ToString() };
    //            sourceFolder.Save();
    //            var targetFolder = new SystemFolder(testRoot) { Name = Guid.NewGuid().ToString() };
    //            targetFolder.Save();

    //            var sourceContent = new GenericContent(sourceFolder, "Car") { Name = "DemoContent" };
    //            sourceContent.Save();

    //            var targetContent = new GenericContent(targetFolder, "Car") { Name = sourceContent.Name };
    //            targetContent.Save();

    //            sourcePath = sourceContent.Path;
    //            targetContainerPath = targetFolder.Path;
    //        }

    //        /* =========================================================================================================== */

    //        internal static JValue GetSimpleResult(StringWriter output)
    //        {
    //            var result = new Dictionary<string, object>();
    //            var jo = (JObject)Deserialize(output);
    //            var value = jo["d"]["result"];
    //            return (JValue)value;
    //        }

    //        private XmlDocument GetMetadataXml(string src, out XmlNamespaceManager nsmgr)
    //        {
    //            var xml = new XmlDocument();
    //            nsmgr = new XmlNamespaceManager(xml.NameTable);
    //            nsmgr.AddNamespace("edmx", "http://schemas.microsoft.com/ado/2007/06/edmx");
    //            nsmgr.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
    //            nsmgr.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");
    //            nsmgr.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
    //            nsmgr.AddNamespace("x", "http://schemas.microsoft.com/ado/2007/05/edm");
    //            xml.LoadXml(src);
    //            return xml;
    //        }

    //    }
}
