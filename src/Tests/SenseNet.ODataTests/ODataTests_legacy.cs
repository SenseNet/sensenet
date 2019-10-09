using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using SenseNet.Portal.ApplicationModel;
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

        //internal static JValue GetSimpleResult(StringWriter output)
        //{
        //    var result = new Dictionary<string, object>();
        //    var jo = (JObject)Deserialize(output);
        //    var value = jo["d"]["result"];
        //    return (JValue)value;
        //}

    }
}
