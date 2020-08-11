using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Tests.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SenseNet.ContentRepository.Storage.DataModel;

namespace SenseNet.ContentRepository.Tests
{
    #region Implementation classes

    internal class TestSchemaWriter : SchemaWriter
    {
        private StringBuilder _log;

        public string Log
        {
            get { return _log.ToString(); }
        }

        public TestSchemaWriter()
        {
            _log = new StringBuilder();
        }

        public override System.Threading.Tasks.Task WriteSchemaAsync(RepositorySchemaData schema)
        {
            throw new NotSupportedException();
        }

        public override void Open()
        {
            WriteLog(MethodInfo.GetCurrentMethod());
        }
        public override void Close()
        {
            WriteLog(MethodInfo.GetCurrentMethod());
        }

        public override void CreatePropertyType(string name, DataType dataType, int mapping, bool isContentListProperty)
        {
            WriteLog(MethodInfo.GetCurrentMethod(), name, dataType, mapping, isContentListProperty);
        }
        public override void DeletePropertyType(PropertyType propertyType)
        {
            WriteLog(MethodInfo.GetCurrentMethod(), propertyType.Name);
        }

        public override void CreateNodeType(NodeType parent, string name, string className)
        {
            WriteLog(MethodInfo.GetCurrentMethod(), parent == null ? "[null]" : parent.Name, name, className);
        }
        public override void ModifyNodeType(NodeType nodeType, NodeType parent, string className)
        {
            WriteLog(MethodInfo.GetCurrentMethod(), nodeType.Name, parent == null ? "[null]" : parent.Name, className);
        }
        public override void DeleteNodeType(NodeType nodeType)
        {
            WriteLog(MethodInfo.GetCurrentMethod(), nodeType.Name);
        }
        public override void CreateContentListType(string name)
        {
            WriteLog(MethodInfo.GetCurrentMethod(), name);
        }
        public override void DeleteContentListType(ContentListType contentListType)
        {
            WriteLog(MethodInfo.GetCurrentMethod(), contentListType.Name);
        }
        public override void AddPropertyTypeToPropertySet(PropertyType propertyType, PropertySet owner, bool isDeclared)
        {
            WriteLog(MethodInfo.GetCurrentMethod(), propertyType.Name, owner.Name, isDeclared);
        }
        public override void RemovePropertyTypeFromPropertySet(PropertyType propertyType, PropertySet owner)
        {
            WriteLog(MethodInfo.GetCurrentMethod(), propertyType.Name, owner.Name);
        }
        public override void UpdatePropertyTypeDeclarationState(PropertyType propType, NodeType newSet, bool isDeclared)
        {
            WriteLog(MethodInfo.GetCurrentMethod(), propType.Name, newSet.Name, isDeclared);
        }

        private void WriteLog(MethodBase methodBase, params object[] prms)
        {
            _log.Append(methodBase.Name).Append("(");
            ParameterInfo[] prmInfos = methodBase.GetParameters();
            for (int i = 0; i < prmInfos.Length; i++)
            {
                if (prmInfos[i].Name == "mapping")
                    continue;

                if (i > 0)
                    _log.Append(", ");

                _log.Append(prmInfos[i].Name).Append("=<");
                _log.Append(prms[i]).Append(">");
            }
            _log.Append(");").Append("\r\n");
        }

    }
    #endregion

    [TestClass]
    public class SchemaEditorSchemaWriterTests : TestBase
    {
        //============================================================================== Simple tests

        [TestMethod]
        public void SchEd_WriterCalling_CreatePropertyType()
        {
            Test(() =>
            {
                SchemaEditor ed1 = new SchemaEditor();
                SchemaEditor ed2 = new SchemaEditor();
                SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
                TestSchemaWriter wr = new TestSchemaWriter();

                //-- edit
                ed2.CreatePropertyType("PT1", DataType.String);

                //-- register
                ed2Acc.RegisterSchema(ed1, wr);

                //-- test
                string log = wr.Log.Replace("\r\n", "");
                Assert.IsTrue(log == "Open();CreatePropertyType(name=<PT1>, dataType=<String>, isContentListProperty=<False>);Close();");
            });
        }
        [TestMethod]
        public void SchEd_WriterCalling_DeletePropertyType()
        {
            Test(() =>
            {
                SchemaEditor ed1 = new SchemaEditor();
                SchemaEditor ed2 = new SchemaEditor();
                SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
                TestSchemaWriter wr = new TestSchemaWriter();

                //-- create original
                ed1.CreatePropertyType("PT1", DataType.String);
                SetSchemaItemId(ed1.PropertyTypes["PT1"], 1);
                //-- create current
                ed2.CreatePropertyType("PT1", DataType.String);
                SetSchemaItemId(ed2.PropertyTypes["PT1"], 1);

                //-- edit
                ed2.DeletePropertyType(ed2.PropertyTypes["PT1"]);
                //-- register
                ed2Acc.RegisterSchema(ed1, wr);

                //-- test
                string log = wr.Log.Replace("\r\n", "");
                Assert.IsTrue(log == "Open();DeletePropertyType(propertyType=<PT1>);Close();");
            });
        }
        [TestMethod]
        public void SchEd_WriterCalling_ReCreatePropertyType()
        {
            Test(() =>
            {
                SchemaEditor ed1 = new SchemaEditor();
                SchemaEditor ed2 = new SchemaEditor();
                SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
                TestSchemaWriter wr = new TestSchemaWriter();

                //-- create original
                ed1.CreatePropertyType("PT1", DataType.String);
                SetSchemaItemId(ed1.PropertyTypes["PT1"], 1);
                //-- create current
                ed2.CreatePropertyType("PT1", DataType.String);
                SetSchemaItemId(ed2.PropertyTypes["PT1"], 1);

                //-- edit
                ed2.DeletePropertyType(ed2.PropertyTypes["PT1"]);
                ed2.CreatePropertyType("PT1", DataType.String);

                //-- register
                ed2Acc.RegisterSchema(ed1, wr);

                //-- test
                string log = wr.Log.Replace("\r\n", "");
                Assert.IsTrue(log == "Open();CreatePropertyType(name=<PT1>, dataType=<String>, isContentListProperty=<False>);DeletePropertyType(propertyType=<PT1>);Close();");
            });
        }


        [TestMethod]
        public void SchEd_WriterCalling_CreateNodeType()
        {
            Test(() =>
            {
                SchemaEditor ed1 = new SchemaEditor();
                SchemaEditor ed2 = new SchemaEditor();
                SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
                TestSchemaWriter wr = new TestSchemaWriter();

                //-- create original
                ed1.CreatePropertyType("PT1", DataType.String);
                SetSchemaItemId(ed1.PropertyTypes["PT1"], 1);
                //-- create current
                ed2.CreatePropertyType("PT1", DataType.String);
                SetSchemaItemId(ed2.PropertyTypes["PT1"], 1);

                //-- edit
                ed2.CreateNodeType(null, "NT1");
                ed2.AddPropertyTypeToPropertySet(ed2.PropertyTypes["PT1"], ed2.NodeTypes["NT1"]);

                //-- register
                ed2Acc.RegisterSchema(ed1, wr);

                //-- test
                string expectedLog = @"
				Open();
				CreateNodeType(parent=<[null]>, name=<NT1>, className=<>);
				AddPropertyTypeToPropertySet(propertyType=<PT1>, owner=<NT1>, isDeclared=<True>);
				Close();
				".Replace("\r\n", "").Replace("\t", "");
                string log = wr.Log.Replace("\r\n", "");
                Assert.IsTrue(log == expectedLog);
            });
        }
        [TestMethod]
        public void SchEd_WriterCalling_ModifyNodeType()
        {
            Test(() =>
            {
                SchemaEditor ed1 = new SchemaEditor();
                SchemaEditor ed2 = new SchemaEditor();
                SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
                TestSchemaWriter wr = new TestSchemaWriter();

                //-- create original
                ed1.CreatePropertyType("PT1", DataType.String);
                SetSchemaItemId(ed1.PropertyTypes["PT1"], 1);
                ed1.CreateNodeType(null, "NT1");
                SetSchemaItemId(ed1.NodeTypes["NT1"], 1);
                ed1.AddPropertyTypeToPropertySet(ed1.PropertyTypes["PT1"], ed1.NodeTypes["NT1"]);
                //-- create current
                ed2.CreatePropertyType("PT1", DataType.String);
                SetSchemaItemId(ed2.PropertyTypes["PT1"], 1);
                ed2.CreateNodeType(null, "NT1");
                SetSchemaItemId(ed2.NodeTypes["NT1"], 1);
                ed2.AddPropertyTypeToPropertySet(ed2.PropertyTypes["PT1"], ed2.NodeTypes["NT1"]);

                //-- edit
                ed2.ModifyNodeType(ed2.NodeTypes["NT1"], "ClassName2");

                //-- register
                ed2Acc.RegisterSchema(ed1, wr);

                //-- test
                string log = wr.Log.Replace("\r\n", "");
                Assert.IsTrue(log == "Open();ModifyNodeType(nodeType=<NT1>, parent=<[null]>, className=<ClassName2>);Close();");
            });
        }
        [TestMethod]
        public void SchEd_WriterCalling_ModifyNodeType_ChangeParent()
        {
            Test(() =>
            {
                SchemaEditor ed1 = new SchemaEditor();
                SchemaEditor ed2 = new SchemaEditor();
                SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
                TestSchemaWriter wr = new TestSchemaWriter();

                //-- create original
                ed1.CreatePropertyType("PT1", DataType.String); SetSchemaItemId(ed1.PropertyTypes["PT1"], 1);
                ed1.CreatePropertyType("PT2", DataType.String); SetSchemaItemId(ed1.PropertyTypes["PT1"], 2);
                ed1.CreatePropertyType("PT3", DataType.String); SetSchemaItemId(ed1.PropertyTypes["PT1"], 3);
                NodeType nt1 = ed1.CreateNodeType(null, "NT1"); SetSchemaItemId(ed1.NodeTypes["NT1"], 1);
                NodeType nt2 = ed1.CreateNodeType(null, "NT2"); SetSchemaItemId(ed1.NodeTypes["NT2"], 2);
                NodeType nt3 = ed1.CreateNodeType(nt1, "NT3"); SetSchemaItemId(ed1.NodeTypes["NT3"], 3);
                ed1.AddPropertyTypeToPropertySet(ed1.PropertyTypes["PT1"], ed1.NodeTypes["NT1"]);
                ed1.AddPropertyTypeToPropertySet(ed1.PropertyTypes["PT2"], ed1.NodeTypes["NT2"]);
                ed1.AddPropertyTypeToPropertySet(ed1.PropertyTypes["PT3"], ed1.NodeTypes["NT3"]);

                //-- create current
                ed2.CreatePropertyType("PT1", DataType.String); SetSchemaItemId(ed2.PropertyTypes["PT1"], 1);
                ed2.CreatePropertyType("PT2", DataType.String); SetSchemaItemId(ed2.PropertyTypes["PT1"], 2);
                ed2.CreatePropertyType("PT3", DataType.String); SetSchemaItemId(ed2.PropertyTypes["PT1"], 3);
                nt1 = ed2.CreateNodeType(null, "NT1"); SetSchemaItemId(ed2.NodeTypes["NT1"], 1);
                nt2 = ed2.CreateNodeType(null, "NT2"); SetSchemaItemId(ed2.NodeTypes["NT2"], 2);
                nt3 = ed2.CreateNodeType(nt1, "NT3"); SetSchemaItemId(ed2.NodeTypes["NT3"], 3);
                ed2.AddPropertyTypeToPropertySet(ed2.PropertyTypes["PT1"], ed2.NodeTypes["NT1"]);
                ed2.AddPropertyTypeToPropertySet(ed2.PropertyTypes["PT2"], ed2.NodeTypes["NT2"]);
                ed2.AddPropertyTypeToPropertySet(ed2.PropertyTypes["PT3"], ed2.NodeTypes["NT3"]);

                //-- edit
                ed2.ModifyNodeType(ed2.NodeTypes["NT3"], ed2.NodeTypes["NT2"]);

                //-- register
                ed2Acc.RegisterSchema(ed1, wr);

                //-- test
                string expectedLog = @"
				Open();
				ModifyNodeType(nodeType=<NT3>, parent=<NT2>, className=<>);
				RemovePropertyTypeFromPropertySet(propertyType=<PT1>, owner=<NT3>);
				AddPropertyTypeToPropertySet(propertyType=<PT2>, owner=<NT3>, isDeclared=<True>);
				Close();
				".Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
                string log = wr.Log.Replace("\r\n", "").Replace(" ", "");
                Assert.IsTrue(log == expectedLog, "#1");
                Assert.IsNull(nt1.Parent, "#2");
                Assert.IsNull(nt2.Parent, "#3");
                Assert.IsTrue(nt3.Parent == nt2, "#4");
                Assert.IsTrue(nt1.PropertyTypes.Count == 1, "#5");
                Assert.IsTrue(nt2.PropertyTypes.Count == 1, "#6");
                Assert.IsTrue(nt3.PropertyTypes.Count == 2, "#7");
                Assert.IsNotNull(nt1.PropertyTypes["PT1"], "#8");
                Assert.IsNotNull(nt2.PropertyTypes["PT2"], "#9");
                Assert.IsNotNull(nt3.PropertyTypes["PT3"], "#10");
                Assert.IsNull(nt3.PropertyTypes["PT1"], "#11");
                Assert.IsNotNull(nt3.PropertyTypes["PT2"], "#12");
            });
        }

        [TestMethod]
        public void SchEd_WriterCalling_DeleteNodeType()
        {
            Test(() =>
            {
                SchemaEditor ed1 = new SchemaEditor();
                SchemaEditor ed2 = new SchemaEditor();
                SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
                TestSchemaWriter wr = new TestSchemaWriter();

                //-- create original
                ed1.CreatePropertyType("PT1", DataType.String);
                SetSchemaItemId(ed1.PropertyTypes["PT1"], 1);
                ed1.CreateNodeType(null, "NT1");
                SetSchemaItemId(ed1.NodeTypes["NT1"], 1);
                ed1.AddPropertyTypeToPropertySet(ed1.PropertyTypes["PT1"], ed1.NodeTypes["NT1"]);
                //-- create current
                ed2.CreatePropertyType("PT1", DataType.String);
                SetSchemaItemId(ed2.PropertyTypes["PT1"], 1);
                ed2.CreateNodeType(null, "NT1");
                SetSchemaItemId(ed2.NodeTypes["NT1"], 1);
                ed2.AddPropertyTypeToPropertySet(ed2.PropertyTypes["PT1"], ed2.NodeTypes["NT1"]);

                //-- edit
                ed2.DeleteNodeType(ed2.NodeTypes["NT1"]);

                //-- register
                ed2Acc.RegisterSchema(ed1, wr);

                //-- test
                string expectedLog = @"
				Open();
				DeleteNodeType(nodeType=<NT1>);
				Close();
				".Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
                string log = wr.Log.Replace("\r\n", "").Replace(" ", "");
                Assert.IsTrue(log == expectedLog);
            });
        }
        [TestMethod]
        public void SchEd_WriterCalling_ReCreateNodeType()
        {
            Test(() =>
            {
                SchemaEditor ed1 = new SchemaEditor();
                SchemaEditor ed2 = new SchemaEditor();
                SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
                TestSchemaWriter wr = new TestSchemaWriter();

                //-- create original
                ed1.CreatePropertyType("PT1", DataType.String);
                SetSchemaItemId(ed1.PropertyTypes["PT1"], 1);
                ed1.CreateNodeType(null, "NT1");
                SetSchemaItemId(ed1.NodeTypes["NT1"], 1);
                ed1.AddPropertyTypeToPropertySet(ed1.PropertyTypes["PT1"], ed1.NodeTypes["NT1"]);
                //-- create current
                ed2.CreatePropertyType("PT1", DataType.String);
                SetSchemaItemId(ed2.PropertyTypes["PT1"], 1);
                ed2.CreateNodeType(null, "NT1");
                SetSchemaItemId(ed2.NodeTypes["NT1"], 1);
                ed2.AddPropertyTypeToPropertySet(ed2.PropertyTypes["PT1"], ed2.NodeTypes["NT1"]);

                //-- edit
                ed2.DeleteNodeType(ed2.NodeTypes["NT1"]);
                ed2.CreateNodeType(null, "NT1");
                ed2.AddPropertyTypeToPropertySet(ed2.PropertyTypes["PT1"], ed2.NodeTypes["NT1"]);

                //-- register
                ed2Acc.RegisterSchema(ed1, wr);

                //-- test
                string expectedLog = @"
				Open();
				DeleteNodeType(nodeType=<NT1>);
				CreateNodeType(parent=<[null]>, name=<NT1>, className=<>);
				AddPropertyTypeToPropertySet(propertyType=<PT1>, owner=<NT1>, isDeclared=<True>);
				Close();
				".Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
                string log = wr.Log.Replace("\r\n", "").Replace(" ", "");
                Assert.IsTrue(log == expectedLog);
            });
        }

        [TestMethod]
        public void SchEd_WriterCalling_CreateContentListType()
        {
            Test(() =>
            {
                SchemaEditor ed1 = new SchemaEditor();
                SchemaEditor ed2 = new SchemaEditor();
                SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
                TestSchemaWriter wr = new TestSchemaWriter();

                //-- create original
                ed1.CreateContentListPropertyType(DataType.String, 0);
                SetSchemaItemId(ed1.PropertyTypes["#String_0"], 1);
                //-- create current
                ed2.CreateContentListPropertyType(DataType.String, 0);
                SetSchemaItemId(ed2.PropertyTypes["#String_0"], 1);

                //-- edit
                ed2.CreateContentListType("LT1");
                ed2.AddPropertyTypeToPropertySet(ed2.PropertyTypes["#String_0"], ed2.ContentListTypes["LT1"]);

                //-- register
                ed2Acc.RegisterSchema(ed1, wr);

                //-- test
                string expectedLog = @"
				Open();
				CreateContentListType(name=<LT1>);
				AddPropertyTypeToPropertySet(propertyType=<#String_0>, owner=<LT1>, isDeclared=<True>);
				Close();
				".Replace("\r\n", "").Replace("\t", "");
                string log = wr.Log.Replace("\r\n", "");
                Assert.IsTrue(log == expectedLog);
            });
        }
        [TestMethod]
        public void SchEd_WriterCalling_DeleteContentListType()
        {
            Test(() =>
            {
                SchemaEditor ed1 = new SchemaEditor();
                SchemaEditor ed2 = new SchemaEditor();
                SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
                TestSchemaWriter wr = new TestSchemaWriter();

                //-- create original
                ed1.CreateContentListPropertyType(DataType.String, 0);
                SetSchemaItemId(ed1.PropertyTypes["#String_0"], 1);
                ed1.CreateContentListType("LT1");
                SetSchemaItemId(ed1.ContentListTypes["LT1"], 1);
                ed1.AddPropertyTypeToPropertySet(ed1.PropertyTypes["#String_0"], ed1.ContentListTypes["LT1"]);
                //-- create current
                ed2.CreateContentListPropertyType(DataType.String, 0);
                SetSchemaItemId(ed2.PropertyTypes["#String_0"], 1);
                ed2.CreateContentListType("LT1");
                SetSchemaItemId(ed2.ContentListTypes["LT1"], 1);
                ed2.AddPropertyTypeToPropertySet(ed2.PropertyTypes["#String_0"], ed2.ContentListTypes["LT1"]);

                //-- edit
                ed2.DeleteContentListType(ed2.ContentListTypes["LT1"]);

                //-- register
                ed2Acc.RegisterSchema(ed1, wr);

                //-- test
                string expectedLog = @"
				Open();
				DeleteContentListType(contentListType=<LT1>);
				Close();
				".Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
                string log = wr.Log.Replace("\r\n", "").Replace(" ", "");
                Assert.IsTrue(log == expectedLog);
            });
        }

        //============================================================================== Complex tests

        [TestMethod]
        public void SchEd_WriterCalling_Complex_01()
        {
            Test(() =>
            {
                SchemaEditor ed1 = new SchemaEditor();
                SchemaEditor ed2 = new SchemaEditor();
                SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
                TestSchemaWriter wr = new TestSchemaWriter();

                //-- create original
                PropertyType ptX = CreatePropertyType(ed1, "X", DataType.String, 1);
                PropertyType ptY = CreatePropertyType(ed1, "Y", DataType.String, 2);
                PropertyType ptZ = CreatePropertyType(ed1, "Z", DataType.String, 3);
                NodeType ntA = CreateNodeType(ed1, null, "A", null, 1);
                NodeType ntB = CreateNodeType(ed1, ntA, "B", null, 2);
                NodeType ntC = CreateNodeType(ed1, ntB, "C", null, 3);
                ed1.AddPropertyTypeToPropertySet(ptX, ntB);
                ed1.AddPropertyTypeToPropertySet(ptY, ntC);
                ed1.AddPropertyTypeToPropertySet(ptX, ntA);

                //-- create current
                XmlDocument xd = new XmlDocument();
                xd.LoadXml(ed1.ToXml());
                ed2.Load(xd);
                ptX = ed2.PropertyTypes["X"];
                ptY = ed2.PropertyTypes["Y"];
                ptZ = ed2.PropertyTypes["Z"];
                ntA = ed2.NodeTypes["A"];
                ntB = ed2.NodeTypes["B"];
                ntC = ed2.NodeTypes["C"];

                //-- edit
                ed2.RemovePropertyTypeFromPropertySet(ptX, ntA);

                //-- register
                ed2Acc.RegisterSchema(ed1, wr);

                //-- test
                string log = wr.Log.Replace("\r\n", "");
                Assert.IsTrue(log == "Open();RemovePropertyTypeFromPropertySet(propertyType=<X>, owner=<A>);Close();");
            });
        }
        [TestMethod]
        public void SchEd_WriterCalling_OverridePropertyOnNodeType()
        {
            Test(() =>
            {
                SchemaEditor ed1 = new SchemaEditor();
                SchemaEditor ed2 = new SchemaEditor();
                SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
                TestSchemaWriter wr = new TestSchemaWriter();

                //-- create original
                PropertyType pt1 = CreatePropertyType(ed1, "PT1", DataType.String, 1);
                NodeType nt1 = CreateNodeType(ed1, null, "NT1", "NT1", 1);
                NodeType nt2 = CreateNodeType(ed1, nt1, "NT2", "NT2", 2);
                NodeType nt3 = CreateNodeType(ed1, nt2, "NT3", "NT3", 3);
                NodeType nt4 = CreateNodeType(ed1, nt3, "NT4", "NT4", 4);
                NodeType nt5 = CreateNodeType(ed1, nt4, "NT5", "NT5", 5);
                ed1.AddPropertyTypeToPropertySet(pt1, nt2);

                //-- create current
                XmlDocument xd = new XmlDocument();
                xd.LoadXml(ed1.ToXml());
                ed2.Load(xd);

                //-- edit
                ed2.AddPropertyTypeToPropertySet(ed2.PropertyTypes["PT1"], ed2.NodeTypes["NT4"]);

                //-- register
                ed2Acc.RegisterSchema(ed1, wr);

                //-- test
                string log = wr.Log.Replace("\r\n", "");
                Assert.IsTrue(log == "Open();UpdatePropertyTypeDeclarationState(propType=<PT1>, newSet=<NT4>, isDeclared=<True>);Close();");
            });
        }
        [TestMethod]
        public void SchEd_WriterCalling_AddPropertyToAncestorNodeType()
        {
            Test(() =>
            {
                SchemaEditor ed1 = new SchemaEditor();
                SchemaEditor ed2 = new SchemaEditor();
                SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
                TestSchemaWriter wr = new TestSchemaWriter();

                //-- create original
                PropertyType pt1 = CreatePropertyType(ed1, "PT1", DataType.String, 1);
                NodeType nt1 = CreateNodeType(ed1, null, "NT1", "NT1", 1);
                NodeType nt2 = CreateNodeType(ed1, nt1, "NT2", "NT2", 2);
                NodeType nt3 = CreateNodeType(ed1, nt2, "NT3", "NT3", 3);
                NodeType nt4 = CreateNodeType(ed1, nt3, "NT4", "NT4", 4);
                NodeType nt5 = CreateNodeType(ed1, nt4, "NT5", "NT5", 5);
                ed1.AddPropertyTypeToPropertySet(pt1, nt4);

                //-- create current
                XmlDocument xd = new XmlDocument();
                xd.LoadXml(ed1.ToXml());
                ed2.Load(xd);

                //-- edit
                ed2.AddPropertyTypeToPropertySet(ed2.PropertyTypes["PT1"], ed2.NodeTypes["NT2"]);

                //-- register
                ed2Acc.RegisterSchema(ed1, wr);

                //-- test
                string expectedLog = @"
				Open();
				AddPropertyTypeToPropertySet(propertyType=<PT1>, owner=<NT2>, isDeclared=<True>);
				AddPropertyTypeToPropertySet(propertyType=<PT1>, owner=<NT3>, isDeclared=<False>);
				Close();
				".Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
                string log = wr.Log.Replace("\r\n", "").Replace(" ", "");
                Assert.IsTrue(log == expectedLog);
            });
        }
        [TestMethod]
        public void SchEd_WriterCalling_RemoveOverriddenPropertyFromNodeType()
        {
            Test(() =>
            {
                SchemaEditor ed1 = new SchemaEditor();
                SchemaEditor ed2 = new SchemaEditor();
                SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
                TestSchemaWriter wr = new TestSchemaWriter();

                //-- create original
                PropertyType pt1 = CreatePropertyType(ed1, "PT1", DataType.String, 1);
                NodeType nt1 = CreateNodeType(ed1, null, "NT1", "NT1", 1);
                NodeType nt2 = CreateNodeType(ed1, nt1, "NT2", "NT2", 2);
                NodeType nt3 = CreateNodeType(ed1, nt2, "NT3", "NT3", 3);
                NodeType nt4 = CreateNodeType(ed1, nt3, "NT4", "NT4", 4);
                NodeType nt5 = CreateNodeType(ed1, nt4, "NT5", "NT5", 5);
                ed1.AddPropertyTypeToPropertySet(pt1, nt4);
                ed1.AddPropertyTypeToPropertySet(pt1, nt2);

                //-- create current
                XmlDocument xd = new XmlDocument();
                xd.LoadXml(ed1.ToXml());
                ed2.Load(xd);

                //-- edit
                ed2.RemovePropertyTypeFromPropertySet(ed2.PropertyTypes["PT1"], ed2.NodeTypes["NT4"]);

                //-- register
                ed2Acc.RegisterSchema(ed1, wr);

                //-- test
                string log = wr.Log.Replace("\r\n", "");
                Assert.IsTrue(log == "Open();UpdatePropertyTypeDeclarationState(propType=<PT1>, newSet=<NT4>, isDeclared=<False>);Close();");
            });
        }
        [TestMethod]
        public void SchEd_WriterCalling_RemoveAncestorOfOverriddenPropertyFromNodeType()
        {
            Test(() =>
            {
                SchemaEditor ed1 = new SchemaEditor();
                SchemaEditor ed2 = new SchemaEditor();
                SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
                TestSchemaWriter wr = new TestSchemaWriter();

                //-- create original
                PropertyType pt1 = CreatePropertyType(ed1, "PT1", DataType.String, 1);
                NodeType nt1 = CreateNodeType(ed1, null, "NT1", "NT1", 1);
                NodeType nt2 = CreateNodeType(ed1, nt1, "NT2", "NT2", 2);
                NodeType nt3 = CreateNodeType(ed1, nt2, "NT3", "NT3", 3);
                NodeType nt4 = CreateNodeType(ed1, nt3, "NT4", "NT4", 4);
                NodeType nt5 = CreateNodeType(ed1, nt4, "NT5", "NT5", 5);
                ed1.AddPropertyTypeToPropertySet(pt1, nt4);
                ed1.AddPropertyTypeToPropertySet(pt1, nt2);

                //-- create current
                XmlDocument xd = new XmlDocument();
                xd.LoadXml(ed1.ToXml());
                ed2.Load(xd);

                //-- edit
                ed2.RemovePropertyTypeFromPropertySet(ed2.PropertyTypes["PT1"], ed2.NodeTypes["NT2"]);

                //-- register
                ed2Acc.RegisterSchema(ed1, wr);

                //-- test
                string expectedLog = @"
				Open();
				RemovePropertyTypeFromPropertySet(propertyType=<PT1>, owner=<NT2>);
				RemovePropertyTypeFromPropertySet(propertyType=<PT1>, owner=<NT3>);
				Close();".Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
                string log = wr.Log.Replace("\r\n", "").Replace(" ", "");
                Assert.IsTrue(log == expectedLog);
            });
        }

        //================================================= Tools =================================================

        private NodeType CreateNodeType(SchemaEditor editor, NodeType parent, string name, string className, int id)
        {
            NodeType nt = editor.CreateNodeType(parent, name, className);
            SetSchemaItemId(nt, id);
            return nt;
        }
        private PropertyType CreatePropertyType(SchemaEditor editor, string name, DataType dataType, int id)
        {
            PropertyType pt = editor.CreatePropertyType(name, dataType);
            SetSchemaItemId(pt, id);
            return pt;
        }
        private void SetSchemaItemId(SchemaItem item, int id)
        {
            SchemaItemAccessor slotAcc = new SchemaItemAccessor(item);
            slotAcc.Id = id;
        }
    }
}
