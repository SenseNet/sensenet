using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class SchemaEditorTests : TestBase
    {
        [TestMethod]
        public void SchemaEditor_LoadSchema()
        {
            Test(() =>
            {
                SchemaEditor editor1 = new SchemaEditor();
                editor1.Load();

                editor1.CreatePropertyType("NewProperty", DataType.String); //.CreatePermissionType("PermA");

                string s = editor1.ToXml();
                XmlDocument xd = new XmlDocument();
                xd.LoadXml(s);

                SchemaEditor editor2 = new SchemaEditor();
                editor2.Load(xd);
                string ss = editor2.ToXml();
            });
        }

        #region PropertyType
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SchemaEditor_CreatePropertySlot_WithTheSameName()
        {
            //-- hiba: nevutkozes
            SchemaEditor editor = new SchemaEditor();
            PropertyType slot1 = editor.CreatePropertyType("NewSlot", DataType.String);
            PropertyType slot2 = editor.CreatePropertyType("NewSlot", DataType.String);
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidSchemaException))]
        public void SchemaEditor_CreatePropertySlot_WithTheSameMapping()
        {
            //-- hiba: mapping utkozes
            SchemaEditor editor = new SchemaEditor();
            PropertyType slot1 = editor.CreatePropertyType("NewSlot1", DataType.String, 0);
            PropertyType slot2 = editor.CreatePropertyType("NewSlot2", DataType.String, 0);
        }
        [TestMethod]
        public void SchemaEditor_CreatePropertySlot()
        {
            //-- 
            SchemaEditor editor = new SchemaEditor();
            PropertyType slot = editor.CreatePropertyType("NewSlot1", DataType.String, 1);
            Assert.IsTrue(slot.Id == 0, "Id was not 0");
        }

        [TestMethod]
        [ExpectedException(typeof(SchemaEditorCommandException))]
        public void SchemaEditor_RemovePropertySlot_WrongContext()
        {
            //-- hiba: rossz context (ket SchemaEditor)
            SchemaEditor editor1 = new SchemaEditor();
            SchemaEditor editor2 = new SchemaEditor();
            editor1.CreatePropertyType("slot", DataType.String, 0);
            editor2.CreatePropertyType("slot", DataType.String, 1);
            PropertyType slot = editor1.PropertyTypes["slot"];
            editor2.DeletePropertyType(slot);
        }
        [TestMethod]
        [ExpectedException(typeof(SchemaEditorCommandException))]
        public void SchemaEditor_ModifyPropertySlot_RemoveProtected()
        {
            //-- vedett elem torlese
            SchemaEditor editor = new SchemaEditor();
            PropertyType slot = editor.CreatePropertyType("slot1", DataType.String, 0);
            NodeType nt = editor.CreateNodeType(null, "NodeType1", "class");
            editor.AddPropertyTypeToPropertySet(slot, nt);

            editor.DeletePropertyType(slot);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SchemaEditor_RemovePropertySlot_Null()
        {
            //---- hiba: Target item cannot be null
            new SchemaEditor().DeletePropertyType(null);
        }
        [TestMethod]
        [ExpectedException(typeof(SchemaEditorCommandException))]
        public void SchemaEditor_RemovePropertySlot()
        {
            SchemaEditor editor = new SchemaEditor();
            PropertyType slot = editor.CreatePropertyType("slot1", DataType.String, 0);
            NodeType nt = editor.CreateNodeType(null, "NodeType1", "class");
            editor.AddPropertyTypeToPropertySet(slot, nt);

            slot = editor.PropertyTypes["slot1"];
            editor.DeletePropertyType(slot);
        }
        #endregion

        #region NodeType
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SchemaEditor_CreateNodeType_Null()
        {
            //-- nev nem lehet null
            new SchemaEditor().CreateNodeType(null, null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SchemaEditor_CreateNodeType_TwoNodeWithSameName()
        {
            //-- nevutkozes
            SchemaEditor editor = new SchemaEditor();
            editor.CreateNodeType(null, "NodeType");
            editor.CreateNodeType(null, "NodeType");
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidSchemaException))]
        public void SchemaEditor_CreateNodeType_WrongContext()
        {
            //-- hiba: rossz context (PropertySlot)
            SchemaEditor editor1 = new SchemaEditor();
            SchemaEditor editor2 = new SchemaEditor();
            NodeType nt1 = editor1.CreateNodeType(null, "NT1");
            editor2.CreateNodeType(nt1, "NT2");
        }
        [TestMethod]
        public void SchemaEditor_CreateNodeType_Child()
        {
            SchemaEditor editor = new SchemaEditor();
            NodeType nt1 = editor.CreateNodeType(null, "NT1");
            NodeType nt2 = editor.CreateNodeType(null, "NT2");
            NodeType nt3 = editor.CreateNodeType(nt1, "NT3", "NT3class");
            NodeType nt4 = editor.CreateNodeType(null, "NT4");

            Assert.IsTrue(Object.ReferenceEquals(nt1, editor.NodeTypes["NT1"]), "#1");
            Assert.IsTrue(Object.ReferenceEquals(nt2, editor.NodeTypes["NT2"]), "#2");
            Assert.IsTrue(Object.ReferenceEquals(nt3, editor.NodeTypes["NT3"]), "#3");
            Assert.IsTrue(nt1.Children.Count == 1, "#4");
            Assert.IsTrue(nt2.Children.Count == 0, "#5");
            Assert.IsTrue(nt3.Children.Count == 0, "#6");
            Assert.IsTrue(Object.ReferenceEquals(nt3, nt1.Children[0]), "#7");
            Assert.IsTrue(Object.ReferenceEquals(nt1, nt3.Parent), "#8");
            Assert.IsNull(nt1.Parent, "#9");
            Assert.IsNull(nt2.Parent, "#10");

            Assert.IsTrue(nt1.ClassName == null, "#11");
            Assert.IsTrue(nt2.ClassName == null, "#12");
            Assert.IsTrue(nt3.ClassName == "NT3class", "#13");
        }
        [TestMethod]
        public void SchemaEditor_CreateNodeType()
        {
            SchemaEditor editor = new SchemaEditor();
            PropertyType slot = editor.CreatePropertyType("slot", DataType.String);
            NodeType nt1 = editor.CreateNodeType(null, "NT1");
            editor.AddPropertyTypeToPropertySet(slot, nt1);
            NodeType nt2 = editor.CreateNodeType(nt1, "NT2");
            Assert.IsNotNull(nt2.PropertyTypes["slot"]);
            Assert.IsTrue(nt1.Id == 0, "Id was not 0");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SchemaEditor_ModifyNodeType_Null()
        {
            //-- hiba: Target item cannot be null
            new SchemaEditor().ModifyNodeType(null, (string)null);
        }
        [TestMethod]
        [ExpectedException(typeof(SchemaEditorCommandException))]
        public void SchemaEditor_ModifyNodeType_WrongContxt()
        {
            //-- hiba: rossz context
            SchemaEditor editor1 = new SchemaEditor();
            SchemaEditor editor2 = new SchemaEditor();
            NodeType nt = editor1.CreateNodeType(null, "NT");
            editor2.ModifyNodeType(nt, (string)null);
        }
        [TestMethod]
        public void SchemaEditor_ModifyNodeType()
        {
            SchemaEditor editor = new SchemaEditor();
            NodeType nt = editor.CreateNodeType(null, "NT1");

            Assert.IsTrue(nt.Name == "NT1" && nt.ClassName == null, "#1");
            editor.ModifyNodeType(nt, "class1");
            Assert.IsTrue(nt.ClassName == "class1", "#2");
            Assert.IsTrue(nt.Id == 0, "Id was not 0");
        }
        [TestMethod]
        [ExpectedException(typeof(SchemaEditorCommandException))]
        public void SchemaEditor_ModifyNodeType_Circular()
        {
            SchemaEditor editor = new SchemaEditor();
            NodeType nt = editor.CreateNodeType(null, "NT1");
            editor.ModifyNodeType(nt, nt);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SchemaEditor_RemoveNodeType_Null()
        {
            //-- hiba: Target item cannot be null
            new SchemaEditor().DeleteNodeType(null);
        }
        [TestMethod]
        [ExpectedException(typeof(SchemaEditorCommandException))]
        public void SchemaEditor_RemoveNodeType_WrongContext()
        {
            //-- hiba: rossz context
            SchemaEditor editor1 = new SchemaEditor();
            SchemaEditor editor2 = new SchemaEditor();
            NodeType nt = editor1.CreateNodeType(null, "NT");
            editor2.DeleteNodeType(nt);
        }
        [TestMethod]
        public void SchemaEditor_RemoveNodeType()
        {
            SchemaEditor editor = new SchemaEditor();

            NodeType nt1 = editor.CreateNodeType(null, "NT1");
            NodeType nt2 = editor.CreateNodeType(nt1, "NT2");
            NodeType nt3 = editor.CreateNodeType(nt2, "NT3");
            PropertyType slot1 = editor.CreatePropertyType("Slot1", DataType.String);
            PropertyType slot2 = editor.CreatePropertyType("Slot2", DataType.String);
            PropertyType slot3 = editor.CreatePropertyType("Slot3", DataType.String);
            editor.AddPropertyTypeToPropertySet(slot1, nt1);
            editor.AddPropertyTypeToPropertySet(slot2, nt2);
            editor.AddPropertyTypeToPropertySet(slot3, nt3);

            editor.DeleteNodeType(editor.NodeTypes["NT2"]);

            Assert.IsTrue(editor.NodeTypes.Count == 1, "#1");

        }
        #endregion

        #region ContentListType
        [TestMethod]
        public void SchemaEditor_CreateContentListType()
        {
            SchemaEditor editor = new SchemaEditor();
            var lt1 = editor.CreateContentListType("LT1");
            Assert.IsTrue(editor.NodeTypes.Count == 0, "#1");
            Assert.IsNotNull(editor.ContentListTypes["LT1"], "#2");
            Assert.IsTrue(lt1.Id == 0, "#3");
        }
        [TestMethod]
        public void SchemaEditor_RemoveContentListType()
        {
            SchemaEditor editor = new SchemaEditor();

            var lt1 = editor.CreateContentListType("LT1");
            PropertyType slot1 = editor.CreateContentListPropertyType(DataType.String, 0);
            editor.AddPropertyTypeToPropertySet(slot1, lt1);

            editor.DeleteContentListType(editor.ContentListTypes["LT1"]);

            Assert.IsTrue(editor.ContentListTypes.Count == 0);
        }
        #endregion

        #region PropertyTypeToNodeType

        [TestMethod]
        [ExpectedException(typeof(InvalidSchemaException))]
        public void SchemaEditor_CreatePropertyType_WrongNodeTypeContext()
        {
            //-- hiba: rossz context (NodeType)
            SchemaEditor editor1 = new SchemaEditor();
            SchemaEditor editor2 = new SchemaEditor();
            editor1.CreatePropertyType("slot", DataType.String);
            editor1.CreateNodeType(null, "nt");
            editor2.CreatePropertyType("slot", DataType.String);
            editor2.CreateNodeType(null, "nt");

            NodeType owner = editor2.NodeTypes["nt"];
            PropertyType slot = editor1.PropertyTypes["slot"];
            editor1.AddPropertyTypeToPropertySet(slot, owner);
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidSchemaException))]
        public void SchemaEditor_CreatePropertyType_WrongPropertySlotContext()
        {
            //-- hiba: rossz context (PropertySlot)
            SchemaEditor editor1 = new SchemaEditor();
            SchemaEditor editor2 = new SchemaEditor();
            editor1.CreatePropertyType("slot", DataType.String);
            editor1.CreateNodeType(null, "nt");
            editor2.CreatePropertyType("slot", DataType.String);
            editor2.CreateNodeType(null, "nt");

            NodeType owner = editor1.NodeTypes["nt"];
            PropertyType slot = editor2.PropertyTypes["slot"];
            editor1.AddPropertyTypeToPropertySet(slot, owner);
        }

        [TestMethod]
        [ExpectedException(typeof(SchemaEditorCommandException))]
        public void SchemaEditor_AddWrongPropertyToNodeType()
        {
            SchemaEditor editor = new SchemaEditor();
            PropertyType pt1 = editor.CreateContentListPropertyType(DataType.String, 0);
            NodeType nt1 = editor.CreateNodeType(null, "NT1");
            editor.AddPropertyTypeToPropertySet(pt1, nt1);
        }
        [TestMethod]
        public void SchemaEditor_AddPropertyToNodeType()
        {
            SchemaEditor editor = new SchemaEditor();
            PropertyType pt1 = editor.CreatePropertyType("PT1", DataType.String);
            NodeType nt1 = editor.CreateNodeType(null, "NT1");
            NodeType nt2 = editor.CreateNodeType(nt1, "NT2");
            NodeType nt3 = editor.CreateNodeType(nt2, "NT3");

            editor.AddPropertyTypeToPropertySet(pt1, nt2);

            Assert.IsNull(nt1.PropertyTypes["PT1"]);
            Assert.IsNull(nt1.DeclaredPropertyTypes["PT1"]);
            Assert.IsTrue(Object.ReferenceEquals(nt2.PropertyTypes["PT1"], pt1));
            Assert.IsTrue(Object.ReferenceEquals(nt2.DeclaredPropertyTypes["PT1"], pt1));
            Assert.IsTrue(Object.ReferenceEquals(nt3.PropertyTypes["PT1"], pt1));
            Assert.IsNull(nt3.DeclaredPropertyTypes["PT1"]);
        }
        [TestMethod]
        public void SchemaEditor_OverridePropertyOnNodeType()
        {
            SchemaEditor editor = new SchemaEditor();
            PropertyType pt1 = editor.CreatePropertyType("PT1", DataType.String);
            NodeType nt1 = editor.CreateNodeType(null, "NT1");
            NodeType nt2 = editor.CreateNodeType(nt1, "NT2");
            NodeType nt3 = editor.CreateNodeType(nt2, "NT3");
            NodeType nt4 = editor.CreateNodeType(nt3, "NT4");
            NodeType nt5 = editor.CreateNodeType(nt4, "NT5");

            editor.AddPropertyTypeToPropertySet(pt1, nt2);
            editor.AddPropertyTypeToPropertySet(pt1, nt4);

            Assert.IsNull(nt1.PropertyTypes["PT1"], "#1");
            Assert.IsNotNull(nt2.PropertyTypes["PT1"], "#2");
            Assert.IsNotNull(nt3.PropertyTypes["PT1"], "#3");
            Assert.IsNotNull(nt4.PropertyTypes["PT1"], "#4");
            Assert.IsNotNull(nt5.PropertyTypes["PT1"], "#5");

            Assert.IsNull(nt1.DeclaredPropertyTypes["PT1"], "#6");
            Assert.IsNotNull(nt2.DeclaredPropertyTypes["PT1"], "#7");
            Assert.IsNull(nt3.DeclaredPropertyTypes["PT1"], "#8");
            Assert.IsNotNull(nt4.DeclaredPropertyTypes["PT1"], "#9");
            Assert.IsNull(nt5.DeclaredPropertyTypes["PT1"], "#10");
        }
        [TestMethod]
        public void SchemaEditor_AddPropertyToAncestorNodeType()
        {
            SchemaEditor editor = new SchemaEditor();
            PropertyType pt1 = editor.CreatePropertyType("PT1", DataType.String);
            NodeType nt1 = editor.CreateNodeType(null, "NT1");
            NodeType nt2 = editor.CreateNodeType(nt1, "NT2");
            NodeType nt3 = editor.CreateNodeType(nt2, "NT3");
            NodeType nt4 = editor.CreateNodeType(nt3, "NT4");
            NodeType nt5 = editor.CreateNodeType(nt4, "NT5");

            editor.AddPropertyTypeToPropertySet(pt1, nt4);
            editor.AddPropertyTypeToPropertySet(pt1, nt2);

            Assert.IsNull(nt1.PropertyTypes["PT1"], "#1");
            Assert.IsNotNull(nt2.PropertyTypes["PT1"], "#2");
            Assert.IsNotNull(nt3.PropertyTypes["PT1"], "#3");
            Assert.IsNotNull(nt4.PropertyTypes["PT1"], "#4");
            Assert.IsNotNull(nt5.PropertyTypes["PT1"], "#5");

            Assert.IsNull(nt1.DeclaredPropertyTypes["PT1"], "#6");
            Assert.IsNotNull(nt2.DeclaredPropertyTypes["PT1"], "#7");
            Assert.IsNull(nt3.DeclaredPropertyTypes["PT1"], "#8");
            Assert.IsNotNull(nt4.DeclaredPropertyTypes["PT1"], "#9");
            Assert.IsNull(nt5.DeclaredPropertyTypes["PT1"], "#10");
        }
        [TestMethod]
        public void SchemaEditor_RemoveOverriddenPropertyFromNodeType()
        {
            SchemaEditor editor = new SchemaEditor();
            PropertyType pt1 = editor.CreatePropertyType("PT1", DataType.String);
            NodeType nt1 = editor.CreateNodeType(null, "NT1");
            NodeType nt2 = editor.CreateNodeType(nt1, "NT2");
            NodeType nt3 = editor.CreateNodeType(nt2, "NT3");
            NodeType nt4 = editor.CreateNodeType(nt3, "NT4");
            NodeType nt5 = editor.CreateNodeType(nt4, "NT5");

            editor.AddPropertyTypeToPropertySet(pt1, nt4);
            editor.AddPropertyTypeToPropertySet(pt1, nt2);
            editor.RemovePropertyTypeFromPropertySet(pt1, nt4);

            Assert.IsNull(nt1.PropertyTypes["PT1"], "#1");
            Assert.IsNotNull(nt2.PropertyTypes["PT1"], "#2");
            Assert.IsNotNull(nt3.PropertyTypes["PT1"], "#3");
            Assert.IsNotNull(nt4.PropertyTypes["PT1"], "#4");
            Assert.IsNotNull(nt5.PropertyTypes["PT1"], "#5");

            Assert.IsNull(nt1.DeclaredPropertyTypes["PT1"], "#6");
            Assert.IsNotNull(nt2.DeclaredPropertyTypes["PT1"], "#7");
            Assert.IsNull(nt3.DeclaredPropertyTypes["PT1"], "#8");
            Assert.IsNull(nt4.DeclaredPropertyTypes["PT1"], "#9");
            Assert.IsNull(nt5.DeclaredPropertyTypes["PT1"], "#10");
        }
        [TestMethod]
        public void SchemaEditor_RemoveAncestorOfOverriddenPropertyFromNodeType()
        {
            SchemaEditor editor = new SchemaEditor();
            PropertyType pt1 = editor.CreatePropertyType("PT1", DataType.String);
            NodeType nt1 = editor.CreateNodeType(null, "NT1");
            NodeType nt2 = editor.CreateNodeType(nt1, "NT2");
            NodeType nt3 = editor.CreateNodeType(nt2, "NT3");
            NodeType nt4 = editor.CreateNodeType(nt3, "NT4");
            NodeType nt5 = editor.CreateNodeType(nt4, "NT5");

            editor.AddPropertyTypeToPropertySet(pt1, nt4);
            editor.AddPropertyTypeToPropertySet(pt1, nt2);
            editor.RemovePropertyTypeFromPropertySet(pt1, nt2);

            Assert.IsNull(nt1.PropertyTypes["PT1"], "#1");
            Assert.IsNull(nt2.PropertyTypes["PT1"], "#2");
            Assert.IsNull(nt3.PropertyTypes["PT1"], "#3");
            Assert.IsNotNull(nt4.PropertyTypes["PT1"], "#4");
            Assert.IsNotNull(nt5.PropertyTypes["PT1"], "#5");

            Assert.IsNull(nt1.DeclaredPropertyTypes["PT1"], "#6");
            Assert.IsNull(nt2.DeclaredPropertyTypes["PT1"], "#7");
            Assert.IsNull(nt3.DeclaredPropertyTypes["PT1"], "#8");
            Assert.IsNotNull(nt4.DeclaredPropertyTypes["PT1"], "#9");
            Assert.IsNull(nt5.DeclaredPropertyTypes["PT1"], "#10");
        }

        [TestMethod]
        public void SchemaEditor_AddPropertyToContentListType()
        {
            SchemaEditor editor = new SchemaEditor();
            PropertyType pt1 = editor.CreateContentListPropertyType(DataType.String, 0);
            var lt1 = editor.CreateContentListType("LT1");
            editor.AddPropertyTypeToPropertySet(pt1, lt1);
            Assert.IsNotNull(lt1.PropertyTypes["#String_0"], "#1");
            Assert.IsTrue(lt1.Id == 0, "#2");
        }
        [TestMethod]
        [ExpectedException(typeof(SchemaEditorCommandException))]
        public void SchemaEditor_AddWrongPropertyToContentListType()
        {
            SchemaEditor editor = new SchemaEditor();
            PropertyType pt1 = editor.CreatePropertyType("PT1", DataType.String);
            var lt1 = editor.CreateContentListType("LT1");
            editor.AddPropertyTypeToPropertySet(pt1, lt1);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidSchemaException))]
        public void SchemaEditor_RemovePropertyType_WrongContext()
        {
            //-- hiba: hibas context
            SchemaEditor editor = new SchemaEditor();
            NodeType nt = editor.CreateNodeType(null, "nt");
            PropertyType slot = editor.CreatePropertyType("slot", DataType.String, 0);
            editor.AddPropertyTypeToPropertySet(slot, nt);
            SchemaEditor editor1 = new SchemaEditor();
            nt = editor1.CreateNodeType(null, "nt");
            slot = editor1.CreatePropertyType("slot", DataType.String, 0);
            editor1.AddPropertyTypeToPropertySet(slot, nt);

            editor.RemovePropertyTypeFromPropertySet(editor1.PropertyTypes["slot"], editor1.NodeTypes["nt"]);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SchemaEditor_RemovePropertyType_NodeTypeNull()
        {
            //---- hiba: Target item cannot be null
            SchemaEditor editor = new SchemaEditor();
            NodeType nt = editor.CreateNodeType(null, "nt");
            new SchemaEditor().RemovePropertyTypeFromPropertySet(null, nt);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SchemaEditor_RemovePropertyType_NullPropertyType()
        {
            //---- hiba: Target item cannot be null
            SchemaEditor editor = new SchemaEditor();
            NodeType nt = editor.CreateNodeType(null, "nt");
            PropertyType slot = editor.CreatePropertyType("slot", DataType.String, 0);
            editor.AddPropertyTypeToPropertySet(slot, nt);
            new SchemaEditor().RemovePropertyTypeFromPropertySet(slot, null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SchemaEditor_RemovePropertyType_NullNull()
        {
            //---- hiba: Target item cannot be null
            new SchemaEditor().RemovePropertyTypeFromPropertySet(null, null);
        }
        [TestMethod]
        public void SchemaEditor_RemovePropertyType_Inherited()
        {
            SchemaEditor editor = new SchemaEditor();
            NodeType nt1 = editor.CreateNodeType(null, "nt1");
            NodeType nt2 = editor.CreateNodeType(nt1, "nt2");
            PropertyType slot = editor.CreatePropertyType("slot", DataType.String, 0);
            editor.AddPropertyTypeToPropertySet(slot, nt1);

            PropertyType pt2 = nt2.PropertyTypes["slot"];
            editor.RemovePropertyTypeFromPropertySet(pt2, nt2);

            Assert.IsNotNull(nt1.PropertyTypes["slot"], "#1");
            Assert.IsNotNull(nt2.PropertyTypes["slot"], "#2");
            Assert.IsNotNull(nt1.DeclaredPropertyTypes["slot"], "#3");
            Assert.IsNull(nt2.DeclaredPropertyTypes["slot"], "#4");
        }
        [TestMethod]
        public void SchemaEditor_RemovePropertyType_FromDeclarerType()
        {
            //-- krealunk egy torolhetot es felulirjuk
            SchemaEditor editor = new SchemaEditor();
            NodeType nt1 = editor.CreateNodeType(null, "nt1");
            NodeType nt2 = editor.CreateNodeType(nt1, "nt2");
            PropertyType slot = editor.CreatePropertyType("slot", DataType.String, 0);
            editor.AddPropertyTypeToPropertySet(slot, nt1);

            //-- meg kell jelenjen mindketton
            PropertyType pt1 = nt1.PropertyTypes["slot"];
            PropertyType pt2 = nt2.PropertyTypes["slot"];

            //-- toroljuk a deklaralas eredeti helyerol
            PropertyType pt = editor.PropertyTypes["slot"];
            editor.RemovePropertyTypeFromPropertySet(pt, nt1);

            //-- el kell tunjon mindkettorol
            pt1 = nt1.PropertyTypes["slot"];
            pt2 = nt2.PropertyTypes["slot"];
            Assert.IsNull(nt1.PropertyTypes["slot"], "Ancestor PropertyType was not deleted");
            Assert.IsNull(nt2.PropertyTypes["slot"], "Inherited PropertyType was not deleted");
        }
        [TestMethod]
        public void SchemaEditor_RemovePropertyType_FromTopReDeclarerType()
        {
            SchemaEditor editor = new SchemaEditor();
            NodeType nt1 = editor.CreateNodeType(null, "nt1");
            NodeType nt2 = editor.CreateNodeType(nt1, "nt2");
            NodeType nt3 = editor.CreateNodeType(nt2, "nt3");
            PropertyType slot = editor.CreatePropertyType("slot", DataType.String, 0);
            editor.AddPropertyTypeToPropertySet(slot, nt2);
            editor.AddPropertyTypeToPropertySet(slot, nt1);

            //-- meg kell jelenjen mindharmon
            PropertyType pt1 = nt1.PropertyTypes["slot"];
            PropertyType pt2 = nt2.PropertyTypes["slot"];
            PropertyType pt3 = nt3.PropertyTypes["slot"];

            //-- toroljuk a deklaralas eredeti helyerol
            PropertyType pt = editor.PropertyTypes["slot"];
            editor.RemovePropertyTypeFromPropertySet(pt, nt1);

            //-- el kell tunjon mindkettorol
            pt1 = nt1.PropertyTypes["slot"];
            pt2 = nt2.PropertyTypes["slot"];
            pt3 = nt3.PropertyTypes["slot"];
            Assert.IsNull(nt1.PropertyTypes["slot"], "#1");
            Assert.IsNotNull(nt2.PropertyTypes["slot"], "#2");
            Assert.IsNotNull(nt3.PropertyTypes["slot"], "#3");
        }
        #endregion

        #region PermissionType
        #endregion

    }
}
