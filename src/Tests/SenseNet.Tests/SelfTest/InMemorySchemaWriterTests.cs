using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.InMemory;
using SenseNet.Tests.Implementations;
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local

namespace SenseNet.Tests.SelfTest
{
    [TestClass]
    public class InMemorySchemaWriterTests : TestBase
    {
        /* ============================================================================== PropertyType */

        [TestMethod]
        public void InMemSchemaWriter_CreatePropertyType()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();

            // ACTION
            writer.Open();
            writer.CreatePropertyType("PT1", DataType.String, 1, false);

            // ASSERT
            Assert.AreEqual(1, schema.PropertyTypes.Count);
            var propType = schema.PropertyTypes[0];
            Assert.AreEqual(1, propType.Id);
            Assert.AreEqual("PT1", propType.Name);
            Assert.AreEqual(DataType.String, propType.DataType);
            Assert.AreEqual(1, propType.Mapping);
            Assert.AreEqual(false, propType.IsContentListProperty);
        }
        [TestMethod]
        public void InMemSchemaWriter_CreateContentListPropertyType()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();

            // ACTION
            writer.Open();
            writer.CreatePropertyType("PT1", DataType.String, 1, true);

            // ASSERT
            Assert.AreEqual(1, schema.PropertyTypes.Count);
            var propType = schema.PropertyTypes[0];
            Assert.AreEqual(1, propType.Id);
            Assert.AreEqual("PT1", propType.Name);
            Assert.AreEqual(DataType.String, propType.DataType);
            Assert.AreEqual(1, propType.Mapping);
            Assert.AreEqual(true, propType.IsContentListProperty);
        }
        [TestMethod]
        public void InMemSchemaWriter_DeletePropertyType()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();
            schema.PropertyTypes.Add(new PropertyTypeData
                { Id = 1, Name = "PT0", DataType = DataType.String, Mapping = 1, IsContentListProperty = false });
            schema.PropertyTypes.Add(new PropertyTypeData
                { Id = 2, Name = "PT1", DataType = DataType.String, Mapping = 2, IsContentListProperty = false });
            var ed = new SchemaEditor();
            var pt = CreatePropertyType(ed, "PT0", DataType.String, 1);

            // ACTION
            writer.Open();
            writer.DeletePropertyType(pt);

            // ASSERT
            Assert.AreEqual(1, schema.PropertyTypes.Count);
            var propType = schema.PropertyTypes[0];
            Assert.AreEqual(2, propType.Id);
            Assert.AreEqual("PT1", propType.Name);
            Assert.AreEqual(DataType.String, propType.DataType);
            Assert.AreEqual(2, propType.Mapping);
            Assert.AreEqual(false, propType.IsContentListProperty);
        }

        /* ============================================================================== NodeType */

        [TestMethod]
        public void InMemSchemaWriter_CreateRootNodeType_WithoutClassName()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();

            // ACTION
            writer.Open();
            writer.CreateNodeType(null, "NT1", null);

            // ASSERT
            Assert.AreEqual(1, schema.NodeTypes.Count);
            var nodeType = schema.NodeTypes[0];
            Assert.AreEqual(1, nodeType.Id);
            Assert.AreEqual("NT1", nodeType.Name);
            Assert.AreEqual(null, nodeType.ParentName);
            Assert.AreEqual(null, nodeType.ClassName);
        }
        [TestMethod]
        public void InMemSchemaWriter_CreateRootNodeType_WithClassName()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();

            // ACTION
            writer.Open();
            writer.CreateNodeType(null, "NT1", "NT1Class");

            // ASSERT
            Assert.AreEqual(1, schema.NodeTypes.Count);
            var nodeType = schema.NodeTypes[0];
            Assert.AreEqual(1, nodeType.Id);
            Assert.AreEqual("NT1", nodeType.Name);
            Assert.AreEqual(null, nodeType.ParentName);
            Assert.AreEqual("NT1Class", nodeType.ClassName);
        }
        [TestMethod]
        public void InMemSchemaWriter_CreateNodeType_WithParent()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();

            var ed = new SchemaEditor();
            var nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);

            // ACTION
            writer.Open();
            writer.CreateNodeType(null, "NT0", "NT0Class");
            writer.CreateNodeType(nt, "NT1", "NT1Class");

            // ASSERT
            Assert.AreEqual(2, schema.NodeTypes.Count);
            var nodeType = schema.NodeTypes[1];
            Assert.AreEqual(2, nodeType.Id);
            Assert.AreEqual("NT1", nodeType.Name);
            Assert.AreEqual("NT0", nodeType.ParentName);
            Assert.AreEqual("NT1Class", nodeType.ClassName);
        }
        [TestMethod]
        public void InMemSchemaWriter_ModifyNodeType()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();
            schema.NodeTypes.Add(new NodeTypeData
                { Id = 1, Name = "NT0", ParentName = null, ClassName = "NT0Class" });
            var ed = new SchemaEditor();
            var nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);

            // ACTION
            writer.Open();
            writer.ModifyNodeType(nt, null, "NT0Class_modified");

            // ASSERT
            Assert.AreEqual(1, schema.NodeTypes.Count);
            var nodeType = schema.NodeTypes[0];
            Assert.AreEqual(1, nodeType.Id);
            Assert.AreEqual("NT0", nodeType.Name);
            Assert.AreEqual(null, nodeType.ParentName);
            Assert.AreEqual("NT0Class_modified", nodeType.ClassName);
        }
        [TestMethod]
        public void InMemSchemaWriter_DeleteNodeType()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();
            schema.NodeTypes.Add(new NodeTypeData
                { Id = 1, Name = "NT0", ParentName = null, ClassName = "NT0Class" });
            schema.NodeTypes.Add(new NodeTypeData
                { Id = 2, Name = "NT1", ParentName = "NT0", ClassName = "NT1Class" });
            var ed = new SchemaEditor();
            var nt = CreateNodeType(ed, null, "NT1", "NT1Class", 2);

            // ACTION
            writer.Open();
            writer.DeleteNodeType(nt);

            // ASSERT
            Assert.AreEqual(1, schema.NodeTypes.Count);
            Assert.AreEqual("NT0", schema.NodeTypes[0].Name);
        }

        /* ============================================================================== ContentListType */

        [TestMethod]
        public void InMemSchemaWriter_CreateContentListType()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();

            // ACTION
            writer.Open();
            writer.CreateContentListType("LT1");

            // ASSERT
            Assert.AreEqual(1, schema.ContentListTypes.Count);
            var listType = schema.ContentListTypes[0];
            Assert.AreEqual(1, listType.Id);
            Assert.AreEqual("LT1", listType.Name);
        }
        [TestMethod]
        public void InMemSchemaWriter_DeleteContentListType()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();
            schema.ContentListTypes.Add(new ContentListTypeData { Id = 1, Name = "LT0" });
            schema.ContentListTypes.Add(new ContentListTypeData { Id = 2, Name = "LT1" });
            schema.ContentListTypes.Add(new ContentListTypeData { Id = 3, Name = "LT2" });
            schema.ContentListTypes.Add(new ContentListTypeData { Id = 4, Name = "LT3" });
            var ed = new SchemaEditor();
            var lt0 = CreateContentListType(ed, "LT0", 1);
            var lt2 = CreateContentListType(ed, "LT2", 1);
            var lt3 = CreateContentListType(ed, "LT3", 1);

            // ACTION
            writer.Open();
            writer.DeleteContentListType(lt2); // last
            writer.DeleteContentListType(lt3); // middle
            writer.DeleteContentListType(lt0); // first

            // ASSERT
            Assert.AreEqual(1, schema.ContentListTypes.Count);
            var listType = schema.ContentListTypes[0];
            Assert.AreEqual(2, listType.Id);
            Assert.AreEqual("LT1", listType.Name);
        }

        /* ============================================================================== PropertyType assignment */

        [TestMethod]
        public void InMemSchemaWriter_AddPropertyTypeToNodeType_Declared()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();
            schema.NodeTypes.Add(new NodeTypeData { Id = 1, Name = "NT0", ParentName = null, ClassName = "NT0Class" });

            var ed = new SchemaEditor();
            var nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            var pt0 = CreatePropertyType(ed, "PT0", DataType.String, 2);
            var pt1 = CreatePropertyType(ed, "PT1", DataType.String, 3);

            // ACTION
            writer.Open();
            writer.AddPropertyTypeToPropertySet(pt0, nt, true);
            writer.AddPropertyTypeToPropertySet(pt1, nt, true);

            // ASSERT
            Assert.AreEqual("PT0,PT1", ArrayToString(schema.NodeTypes[0].Properties));
        }
        [TestMethod]
        public void InMemSchemaWriter_AddPropertyTypeToNodeType_Inherited()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();
            schema.NodeTypes.Add(new NodeTypeData { Id = 1, Name = "NT0", ParentName = null, ClassName = "NT0Class" });

            var ed = new SchemaEditor();
            var nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            var pt0 = CreatePropertyType(ed, "PT0", DataType.String, 2);
            var pt1 = CreatePropertyType(ed, "PT1", DataType.String, 3);

            // ACTION
            writer.Open();
            writer.AddPropertyTypeToPropertySet(pt0, nt, false);
            writer.AddPropertyTypeToPropertySet(pt1, nt, true);

            // ASSERT
            Assert.AreEqual("PT1", ArrayToString(schema.NodeTypes[0].Properties));
        }
        [TestMethod]
        public void InMemSchemaWriter_AddPropertyTypeToContentListType()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();
            schema.ContentListTypes.Add(new ContentListTypeData() { Id = 1, Name = "LT0" });
            SchemaEditor ed = new SchemaEditor();
            var lt = CreateContentListType(ed, "LT0", 1);
            var pt0 = CreateContentListPropertyType(ed, DataType.String, 0, 1);
            var pt1 = CreateContentListPropertyType(ed, DataType.Int, 0, 2);
            var pt2 = CreateContentListPropertyType(ed, DataType.String, 1, 3);
            var pt3 = CreateContentListPropertyType(ed, DataType.Int, 1, 4);

            // ACTION
            writer.Open();
            writer.AddPropertyTypeToPropertySet(pt0, lt, false);
            writer.AddPropertyTypeToPropertySet(pt1, lt, false);
            writer.AddPropertyTypeToPropertySet(pt2, lt, false);
            writer.AddPropertyTypeToPropertySet(pt3, lt, false);

            // ASSERT
            Assert.AreEqual("#String_0,#Int_0,#String_1,#Int_1", ArrayToString(schema.ContentListTypes[0].Properties));
        }

        [TestMethod]
        public void InMemSchemaWriter_RemovePropertyTypeFromNodeType()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();
            schema.NodeTypes.Add(new NodeTypeData
            {
                Id = 1,
                Name = "NT0",
                ParentName = null,
                ClassName = "NT0Class",
                Properties = new List<string> {"PT0", "PT1", "PT2", "PT3", "PT4" }
            });

            var ed = new SchemaEditor();
            var nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            var pt0 = CreatePropertyType(ed, "PT0", DataType.String, 1);
            var pt1 = CreatePropertyType(ed, "PT1", DataType.String, 2);
            var pt2 = CreatePropertyType(ed, "PT2", DataType.String, 3);
            var pt3 = CreatePropertyType(ed, "PT3", DataType.String, 4);
            var pt4 = CreatePropertyType(ed, "PT4", DataType.String, 5);

            // ACTION
            writer.Open();
            writer.RemovePropertyTypeFromPropertySet(pt4, nt); // last
            writer.RemovePropertyTypeFromPropertySet(pt2, nt); // middle
            writer.RemovePropertyTypeFromPropertySet(pt0, nt); // first

            // ASSERT
            Assert.AreEqual("PT1,PT3", ArrayToString(schema.NodeTypes[0].Properties));
        }
        [TestMethod]
        public void InMemSchemaWriter_RemovePropertyTypeFromContentListType()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();
            schema.ContentListTypes.Add(new ContentListTypeData()
            {
                Id = 1,
                Name = "LT0",
                Properties = new List<string> { "#Int_0", "#Int_1", "#Int_2", "#Int_3", "#Int_4" }
            });
            SchemaEditor ed = new SchemaEditor();
            var lt = CreateContentListType(ed, "LT0", 1);
            var pt0 = CreateContentListPropertyType(ed, DataType.Int, 0, 1);
            var pt1 = CreateContentListPropertyType(ed, DataType.Int, 1, 2);
            var pt2 = CreateContentListPropertyType(ed, DataType.Int, 2, 3);
            var pt3 = CreateContentListPropertyType(ed, DataType.Int, 3, 4);
            var pt4 = CreateContentListPropertyType(ed, DataType.Int, 4, 5);

            // ACTION
            writer.Open();
            writer.RemovePropertyTypeFromPropertySet(pt4, lt); // last
            writer.RemovePropertyTypeFromPropertySet(pt2, lt); // middle
            writer.RemovePropertyTypeFromPropertySet(pt0, lt); // first

            // ASSERT
            Assert.AreEqual("#Int_1,#Int_3", ArrayToString(schema.ContentListTypes[0].Properties));
        }

        [TestMethod]
        public void InMemSchemaWriter_UpdatePropertyTypeDeclarationState()
        {
            (RepositorySchemaData schema, SchemaWriter writer) = CreateEmptySchemaAndWriter();
            schema.NodeTypes.Add(new NodeTypeData
            {
                Id = 1,
                Name = "NT0",
                ParentName = null,
                ClassName = "NT0Class",
                Properties = new List<string> { "PT0", "PT1", "PT2" }
            });

            var ed = new SchemaEditor();
            var nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            var pt0 = CreatePropertyType(ed, "PT0", DataType.String, 1);
            var pt1 = CreatePropertyType(ed, "PT1", DataType.String, 2);
            var pt2 = CreatePropertyType(ed, "PT2", DataType.String, 3);
            var pt3 = CreatePropertyType(ed, "PT3", DataType.String, 4);

            // ACTION
            writer.Open();
            writer.UpdatePropertyTypeDeclarationState(pt1, nt, false);
            writer.UpdatePropertyTypeDeclarationState(pt3, nt, true);

            // ASSERT
            Assert.AreEqual("PT0,PT2,PT3", ArrayToString(schema.NodeTypes[0].Properties));
        }

        /* ================================================================================================== Tools */

        private class SchemaItemAccessor : Accessor
        {
            public SchemaItemAccessor(SchemaItem target) : base(target) { }
            public int Id
            {
                get => ((SchemaItem)_target).Id;
                set => SetPrivateField("_id", value);
            }
        }

        private (RepositorySchemaData, SchemaWriter) CreateEmptySchemaAndWriter()
        {
            var dp = new InMemoryDataProvider();
            var writer = dp.CreateSchemaWriter();

            var writerAcc = new PrivateObject(writer);
            var schema = (RepositorySchemaData)writerAcc.GetField("_schema");

            Assert.AreEqual(0, schema.PropertyTypes.Count);
            Assert.AreEqual(0, schema.NodeTypes.Count);
            Assert.AreEqual(0, schema.ContentListTypes.Count);

            return (schema, writer);
        }

        private void SetSchemaItemId(SchemaItem item, int id)
        {
            SchemaItemAccessor slotAcc = new SchemaItemAccessor(item);
            slotAcc.Id = id;
        }
        private NodeType CreateNodeType(SchemaEditor editor, NodeType parent, string name, string className, int id)
        {
            NodeType nt = editor.CreateNodeType(parent, name, className);
            SetSchemaItemId(nt, id);
            return nt;
        }
        private ContentListType CreateContentListType(SchemaEditor editor, string name, int id)
        {
            var lt = editor.CreateContentListType(name);
            SetSchemaItemId(lt, id);
            return lt;
        }
        private PropertyType CreatePropertyType(SchemaEditor editor, string name, DataType dataType, int id)
        {
            PropertyType pt = editor.CreatePropertyType(name, dataType);
            SetSchemaItemId(pt, id);
            return pt;
        }
        private PropertyType CreateContentListPropertyType(SchemaEditor editor, DataType dataType, int mapping, int id)
        {
            PropertyType pt = editor.CreateContentListPropertyType(dataType, mapping);
            SetSchemaItemId(pt, id);
            return pt;
        }
    }
}
