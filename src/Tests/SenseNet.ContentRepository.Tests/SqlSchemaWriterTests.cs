using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using System.Reflection;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests.Schema
{
    [TestClass]
    public class SqlSchemaWriterTests : TestBase
    {
        #region Accessors
        private class SchemaItemAccessor : Accessor
        {
            public SchemaItemAccessor(SchemaItem target) : base(target) { }
            public int Id
            {
                get { return ((SchemaItem)_target).Id; }
                set { SetPrivateField("_id", value); }
            }
        }
        private class SqlSchemaWriterAccessor : Accessor
        {
            private SqlSchemaWriterAccessor(object target) : base(target) { }

            public static SqlSchemaWriterAccessor Create()
            {
                PrivateType pt = new PrivateType("SenseNet.Storage", "SenseNet.ContentRepository.Storage.Data.SqlClient.SqlSchemaWriter");
                object target = Activator.CreateInstance(pt.ReferencedType);
                return new SqlSchemaWriterAccessor(target);
            }
            public string GetSqlScript()
            {
                return (string)CallPrivateMethod("GetSqlScript");
            }


            public void Open()
            {
                _target.GetType().GetMethod("Open").Invoke(_target, new object[] { });
            }
            public void Close()
            {
                _target.GetType().GetMethod("Close").Invoke(_target, new object[] { });
            }

            public void CreatePropertyType(string name, DataType dataType, int mapping, bool isContentListProperty)
            {
                _target.GetType().GetMethod("CreatePropertyType").Invoke(_target, new object[] { name, dataType, mapping, isContentListProperty });
            }
            public void DeletePropertyType(PropertyType propertyType)
            {
                _target.GetType().GetMethod("DeletePropertyType").Invoke(_target, new object[] { propertyType });
            }

            public void CreateNodeType(NodeType parent, string name, string className)
            {
                _target.GetType().GetMethod("CreateNodeType").Invoke(_target, new object[] { parent, name, className });
            }
            public void ModifyNodeType(NodeType nodeType, NodeType parent, string className)
            {
                _target.GetType().GetMethod("ModifyNodeType").Invoke(_target, new object[] { nodeType, parent, className });
            }
            public void DeleteNodeType(NodeType nodeType)
            {
                _target.GetType().GetMethod("DeleteNodeType").Invoke(_target, new object[] { nodeType });
            }

            public void CreateContentListType(string name)
            {
                _target.GetType().GetMethod("CreateContentListType").Invoke(_target, new object[] { name });
            }
            public void DeleteContentListType(ContentListType contentListType)
            {
                _target.GetType().GetMethod("DeleteContentListType").Invoke(_target, new object[] { contentListType });
            }

            public void AddPropertyTypeToPropertySet(PropertyType propertyType, PropertySet owner, bool isDeclared)
            {
                _target.GetType().GetMethod("AddPropertyTypeToPropertySet").Invoke(_target, new object[] { propertyType, owner, isDeclared });
            }
            public void RemovePropertyTypeFromPropertySet(PropertyType propertyType, PropertySet owner)
            {
                _target.GetType().GetMethod("RemovePropertyTypeFromPropertySet").Invoke(_target, new object[] { propertyType, owner });
            }
            public void UpdatePropertyTypeDeclarationState(PropertyType propertyType, NodeType owner, bool isDeclared)
            {
                _target.GetType().GetMethod("UpdatePropertyTypeDeclarationState").Invoke(_target, new object[] { propertyType, owner, isDeclared });
            }

            public void CreatePermissionType(string name)
            {
                _target.GetType().GetMethod("CreatePermissionType").Invoke(_target, new object[] { name });
            }
            public void DeletePermissionType(PermissionType permissionType)
            {
                _target.GetType().GetMethod("DeletePermissionType").Invoke(_target, new object[] { permissionType });
            }

        }
        #endregion

        //============================ PropertyType

        [TestMethod]
        public void SqlSchemaWriter_CreatePropertyType()
        {
            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.CreatePropertyType("PT1", DataType.String, 1, false);

            string expectedSql = @"
				-- Create PropertyType 'PT1', String
				INSERT INTO [dbo].[SchemaPropertyTypes] ([Name], [DataTypeId], [Mapping], [IsContentListProperty]) VALUES ('PT1', 1, 1, 0)
				GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_CreateContentListPropertyType()
        {
            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.CreatePropertyType("PT1", DataType.String, 1, true);

            string expectedSql = @"
						-- Create PropertyType 'PT1', String
						INSERT INTO [dbo].[SchemaPropertyTypes] ([Name], [DataTypeId], [Mapping], [IsContentListProperty]) VALUES ('PT1', 1, 1, 1)
						GO";


            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_DeletePropertyType()
        {
            SchemaEditor ed = new SchemaEditor();
            PropertyType pt = CreatePropertyType(ed, "PT0", DataType.String, 1);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.DeletePropertyType(pt);

            string expectedSql = @"
						-- Delete PropertyType 'PT0'
						DELETE FROM [dbo].[SchemaPropertyTypes] WHERE PropertyTypeId = 1
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }

        //============================ NodeType

        [TestMethod]
        public void SqlSchemaWriter_CreateRootNodeType_WithoutClassName()
        {
            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.CreateNodeType(null, "NT1", null);

            string expectedSql = @"
						-- Create NodeType without parent and handler: NT1
						INSERT INTO [dbo].[SchemaPropertySets] ([Name], [PropertySetTypeId]) VALUES ('NT1', 1)
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_CreateRootNodeType_WithClassName()
        {
            SchemaEditor ed = new SchemaEditor();
            CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            NodeType nt = ed.NodeTypes[0];

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.CreateNodeType(null, "NT1", "NT1Class");

            string expectedSql = @"
						-- Create NodeType without parent: NT1
						INSERT INTO [dbo].[SchemaPropertySets] ([Name], [PropertySetTypeId], [ClassName]) VALUES ('NT1', 1, 'NT1Class')
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_CreateNodeType_WithoutClassName()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.CreateNodeType(nt, "NT1", null);

            string expectedSql = @"
						-- Create NodeType without handler: NT0/NT1
						DECLARE @parentId int
						SELECT @parentId = [PropertySetId] FROM [dbo].[SchemaPropertySets] WHERE [Name] = 'NT0'
						INSERT INTO [dbo].[SchemaPropertySets] ([ParentId], [Name], [PropertySetTypeId]) VALUES (@parentId, 'NT1', 1)
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_CreateNodeType_WithClassName()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.CreateNodeType(nt, "NT1", "NT1Class");

            string expectedSql = @"
						-- Create NodeType NT0/NT1
						DECLARE @parentId int
						SELECT @parentId = [PropertySetId] FROM [dbo].[SchemaPropertySets] WHERE [Name] = 'NT0'
						INSERT INTO [dbo].[SchemaPropertySets] ([ParentId], [Name], [PropertySetTypeId], [ClassName]) VALUES (@parentId, 'NT1', 1, 'NT1Class')
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_ModifyNodeType_NullClass()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0C", 1);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.ModifyNodeType(nt, nt.Parent, null);

            string expectedSql = @"
						-- Modify NodeType: NT0 (original ClassName = 'NT0C')
						UPDATE [dbo].[SchemaPropertySets] SET
								[ParentId] = null
								,[ClassName] = null
						WHERE PropertySetId = 1
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_ModifyNodeType()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt0 = CreateNodeType(ed, null, "NT0", "NT0C", 1);
            NodeType nt1 = CreateNodeType(ed, nt0, "NT1", "NT1C", 2);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.ModifyNodeType(nt0, nt0.Parent, "NT0Cmod");
            writer.ModifyNodeType(nt1, nt1.Parent, "NT1Cmod");

            string expectedSql = @"
						-- Modify NodeType: NT0 (original ClassName = 'NT0C')
						UPDATE [dbo].[SchemaPropertySets] SET
								[ParentId] = null
								,[ClassName] = 'NT0Cmod'
						WHERE PropertySetId = 1
						GO
						-- Modify NodeType: NT1 (original ClassName = 'NT1C')
						UPDATE [dbo].[SchemaPropertySets] SET
								[ParentId] = 1
								,[ClassName] = 'NT1Cmod'
						WHERE PropertySetId = 2
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_DeleteNodeType()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0C", 1);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.DeleteNodeType(nt);

            string expectedSql = @"
						-- Delete NodeType 'NT0'
						DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertySetId = 1
						DELETE FROM [dbo].[SchemaPropertySets] WHERE PropertySetId = 1
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_DeleteNodeTypeWithProperties()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0C", 1);
            PropertyType pt1 = CreatePropertyType(ed, "PT0", DataType.String, 2);
            PropertyType pt2 = CreatePropertyType(ed, "PT1", DataType.String, 3);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.DeleteNodeType(nt);

            string expectedSql = @"
						-- Delete NodeType 'NT0'
						DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertySetId = 1
						DELETE FROM [dbo].[SchemaPropertySets] WHERE PropertySetId = 1
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }

        //============================ ContentListType

        [TestMethod]
        public void SqlSchemaWriter_CreateContentListType()
        {
            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.CreateContentListType("LT1");

            string expectedSql = @"
						-- CreateContentListType: LT1
						INSERT INTO [dbo].[SchemaPropertySets] ([Name], [PropertySetTypeId]) VALUES ('LT1', 2)
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_DeleteContentListType()
        {
            SchemaEditor ed = new SchemaEditor();
            var lt = CreateContentListType(ed, "LT0", 1);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.DeleteContentListType(lt);

            string expectedSql = @"
						-- Delete ContentListType 'LT0'
						DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertySetId = 1
						DELETE FROM [dbo].[SchemaPropertySets] WHERE PropertySetId = 1
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }

        //============================ PropertyType

        [TestMethod]
        public void SqlSchemaWriter_AddPropertyTypeToNodeType_Declared()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            PropertyType pt = CreatePropertyType(ed, "PT0", DataType.String, 2);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.AddPropertyTypeToPropertySet(pt, nt, true);

            string expectedSql = @"
						-- Add PropertyType to PropertySet (NT0.PT0 : String)
						DECLARE @propertyTypeId int
						SELECT @propertyTypeId = [PropertyTypeId] FROM [dbo].[SchemaPropertyTypes] WHERE [Name] = 'PT0'
						DECLARE @ownerId int
						SELECT @ownerId = [PropertySetId] FROM [dbo].[SchemaPropertySets] WHERE [Name] = 'NT0'
						INSERT INTO [dbo].[SchemaPropertySetsPropertyTypes] ([PropertyTypeId], [PropertySetId], [IsDeclared]) VALUES (@propertyTypeId, @ownerId, 1)
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_AddPropertyTypeToNodeType_Inherited()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            PropertyType pt = CreatePropertyType(ed, "PT0", DataType.String, 2);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.AddPropertyTypeToPropertySet(pt, nt, false);

            string expectedSql = @"
						-- Add PropertyType to PropertySet (NT0.PT0 : String)
						DECLARE @propertyTypeId int
						SELECT @propertyTypeId = [PropertyTypeId] FROM [dbo].[SchemaPropertyTypes] WHERE [Name] = 'PT0'
						DECLARE @ownerId int
						SELECT @ownerId = [PropertySetId] FROM [dbo].[SchemaPropertySets] WHERE [Name] = 'NT0'
						INSERT INTO [dbo].[SchemaPropertySetsPropertyTypes] ([PropertyTypeId], [PropertySetId], [IsDeclared]) VALUES (@propertyTypeId, @ownerId, 0)
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_AddPropertyTypeToContentListType()
        {
            SchemaEditor ed = new SchemaEditor();
            var lt = CreateContentListType(ed, "LT0", 1);
            PropertyType pt = CreateContentListPropertyType(ed, DataType.String, 0, 2);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.AddPropertyTypeToPropertySet(pt, lt, false);

            string expectedSql = @"
						-- Add PropertyType to PropertySet (LT0.#String_0 : String)
						DECLARE @propertyTypeId int
						SELECT @propertyTypeId = [PropertyTypeId] FROM [dbo].[SchemaPropertyTypes] WHERE [Name] = '#String_0'
						DECLARE @ownerId int
						SELECT @ownerId = [PropertySetId] FROM [dbo].[SchemaPropertySets] WHERE [Name] = 'LT0'
						INSERT INTO [dbo].[SchemaPropertySetsPropertyTypes] ([PropertyTypeId], [PropertySetId], [IsDeclared]) VALUES (@propertyTypeId, @ownerId, 0)
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }

        [TestMethod]
        public void SqlSchemaWriter_RemovePropertyTypeFromNodeType_Binary()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            PropertyType pt = CreatePropertyType(ed, "PT0", DataType.Binary, 2);
            ed.AddPropertyTypeToPropertySet(pt, nt);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.RemovePropertyTypeFromPropertySet(pt, nt);

            string expectedSql = @"-- Reset property value: NT0.PT0:Binary
DELETE FROM dbo.BinaryProperties WHERE BinaryPropertyId IN (SELECT dbo.BinaryProperties.BinaryPropertyId FROM dbo.Nodes
	INNER JOIN dbo.Versions ON dbo.Versions.NodeId = dbo.Nodes.NodeId
	INNER JOIN dbo.BinaryProperties ON dbo.Versions.VersionId = dbo.BinaryProperties.VersionId
WHERE (dbo.Nodes.NodeTypeId = 1) AND (dbo.BinaryProperties.PropertyTypeId = 2))
-- Remove PropertyType 'PT0' from PropertySet 'NT0'
DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertyTypeId = 2 AND PropertySetId = 1
GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);
        }
        [TestMethod]
        public void SqlSchemaWriter_RemovePropertyTypeFromNodeType_Currency()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            PropertyType pt = CreatePropertyType(ed, "PT0", DataType.Currency, 2);
            ed.AddPropertyTypeToPropertySet(pt, nt);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.RemovePropertyTypeFromPropertySet(pt, nt);

            string expectedSql = @"
						-- Remove PropertyType 'PT0' from PropertySet 'NT0'
						DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertyTypeId = 2 AND PropertySetId = 1
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_RemovePropertyTypeFromNodeType_DateTime()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            PropertyType pt = CreatePropertyType(ed, "PT0", DataType.DateTime, 2);
            ed.AddPropertyTypeToPropertySet(pt, nt);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.RemovePropertyTypeFromPropertySet(pt, nt);

            string expectedSql = @"
						-- Remove PropertyType 'PT0' from PropertySet 'NT0'
						DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertyTypeId = 2 AND PropertySetId = 1
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_RemovePropertyTypeFromNodeType_Int()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            PropertyType pt = CreatePropertyType(ed, "PT0", DataType.Int, 2);
            ed.AddPropertyTypeToPropertySet(pt, nt);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.RemovePropertyTypeFromPropertySet(pt, nt);

            string expectedSql = @"
						-- Remove PropertyType 'PT0' from PropertySet 'NT0'
						DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertyTypeId = 2 AND PropertySetId = 1
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_RemovePropertyTypeFromNodeType_Reference()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            PropertyType pt = CreatePropertyType(ed, "PT0", DataType.Reference, 2);
            ed.AddPropertyTypeToPropertySet(pt, nt);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.RemovePropertyTypeFromPropertySet(pt, nt);

            string expectedSql = @"
						-- Reset property value: NT0.PT0:Reference
						DELETE FROM dbo.ReferenceProperties WHERE ReferencePropertyId IN (SELECT dbo.ReferenceProperties.ReferencePropertyId FROM dbo.Nodes
							INNER JOIN dbo.Versions ON dbo.Versions.NodeId = dbo.Nodes.NodeId
							INNER JOIN dbo.ReferenceProperties ON dbo.Versions.VersionId = dbo.ReferenceProperties.VersionId
						WHERE (dbo.Nodes.NodeTypeId = 1) AND (dbo.ReferenceProperties.PropertyTypeId = 2))
						-- Remove PropertyType 'PT0' from PropertySet 'NT0'
						DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertyTypeId = 2 AND PropertySetId = 1
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_RemovePropertyTypeFromNodeType_String()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            PropertyType pt = CreatePropertyType(ed, "PT0", DataType.String, 2);
            ed.AddPropertyTypeToPropertySet(pt, nt);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.RemovePropertyTypeFromPropertySet(pt, nt);

            string expectedSql = @"
						-- Remove PropertyType 'PT0' from PropertySet 'NT0'
						DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertyTypeId = 2 AND PropertySetId = 1
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_RemovePropertyTypeFromNodeType_Text()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            PropertyType pt = CreatePropertyType(ed, "PT0", DataType.Text, 2);
            ed.AddPropertyTypeToPropertySet(pt, nt);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.RemovePropertyTypeFromPropertySet(pt, nt);

            string expectedSql = @"-- Reset property value: NT0.PT0:Text
DELETE FROM dbo.TextPropertiesNVarchar WHERE TextPropertyNVarcharId IN (SELECT dbo.TextPropertiesNVarchar.TextPropertyNVarcharId FROM dbo.Nodes
	INNER JOIN dbo.Versions ON dbo.Versions.NodeId = dbo.Nodes.NodeId
	INNER JOIN dbo.TextPropertiesNVarchar ON dbo.Versions.VersionId = dbo.TextPropertiesNVarchar.VersionId
WHERE (dbo.Nodes.NodeTypeId = 1) AND (dbo.TextPropertiesNVarchar.PropertyTypeId = 2))
-- Reset property value: NT0.PT0:Text
DELETE FROM dbo.TextPropertiesNText WHERE TextPropertyNTextId IN (SELECT dbo.TextPropertiesNText.TextPropertyNTextId FROM dbo.Nodes
	INNER JOIN dbo.Versions ON dbo.Versions.NodeId = dbo.Nodes.NodeId
	INNER JOIN dbo.TextPropertiesNText ON dbo.Versions.VersionId = dbo.TextPropertiesNText.VersionId
WHERE (dbo.Nodes.NodeTypeId = 1) AND (dbo.TextPropertiesNText.PropertyTypeId = 2))
-- Remove PropertyType 'PT0' from PropertySet 'NT0'
DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertyTypeId = 2 AND PropertySetId = 1
GO
";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }

        [TestMethod]
        public void SqlSchemaWriter_UpdatePropertyTypeDeclarationState()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            PropertyType pt = CreatePropertyType(ed, "PT0", DataType.String, 2);
            ed.AddPropertyTypeToPropertySet(pt, nt);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.UpdatePropertyTypeDeclarationState(pt, nt, true);

            string expectedSql = @"
						-- Update PropertyType declaration: NT0.PT0. Set IsDeclared = true
						UPDATE [dbo].[SchemaPropertySetsPropertyTypes] SET
								[IsDeclared] = 1
						WHERE PropertySetId = 1 AND PropertyTypeId = 2
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }

        //============================ More actions

        [TestMethod]
        public void SqlSchemaWriter_RemovePropertyTypeFromNodeType_More()
        {
            //CreateTestNodeForRemovePropertyType();

            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);
            PropertyType pt0 = CreatePropertyType(ed, "PT0", DataType.String, 2);
            PropertyType pt1 = CreatePropertyType(ed, "PT1", DataType.String, 3);
            PropertyType pt2 = CreatePropertyType(ed, "PT2", DataType.String, 4);
            ed.AddPropertyTypeToPropertySet(pt0, nt);
            ed.AddPropertyTypeToPropertySet(pt1, nt);
            ed.AddPropertyTypeToPropertySet(pt2, nt);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.RemovePropertyTypeFromPropertySet(pt0, nt);
            writer.RemovePropertyTypeFromPropertySet(pt1, nt);
            writer.RemovePropertyTypeFromPropertySet(pt2, nt);

            #region Bad script
            //-- Reset property value: NT0.PT0:String
            //UPDATE dbo.FlatProperties SET nvarchar_1 = NULL 
            //WHERE Id IN (SELECT dbo.FlatProperties.Id FROM dbo.Nodes 
            //    INNER JOIN dbo.Versions ON dbo.Versions.NodeId = dbo.Nodes.NodeId 
            //    INNER JOIN dbo.FlatProperties ON dbo.Versions.VersionId = dbo.FlatProperties.VersionId 
            //    WHERE (dbo.Nodes.NodeTypeId = 1) AND (dbo.FlatProperties.Page = 0))
            //-- Remove PropertyType 'PT0' from PropertySet 'NT0'
            //DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertyTypeId = 2 AND PropertySetId = 1
            //GO
            //-- Reset property value: NT0.PT1:String
            //UPDATE dbo.FlatProperties SET nvarchar_2 = NULL 
            //WHERE Id IN (SELECT dbo.FlatProperties.Id FROM dbo.Nodes 
            //    INNER JOIN dbo.Versions ON dbo.Versions.NodeId = dbo.Nodes.NodeId 
            //    INNER JOIN dbo.FlatProperties ON dbo.Versions.VersionId = dbo.FlatProperties.VersionId 
            //    WHERE (dbo.Nodes.NodeTypeId = 1) AND (dbo.FlatProperties.Page = 0))
            //-- Remove PropertyType 'PT1' from PropertySet 'NT0'
            //DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertyTypeId = 3 AND PropertySetId = 1
            //GO
            //-- Reset property value: NT0.PT2:String
            //UPDATE dbo.FlatProperties SET nvarchar_3 = NULL 
            //WHERE Id IN (SELECT dbo.FlatProperties.Id FROM dbo.Nodes 
            //    INNER JOIN dbo.Versions ON dbo.Versions.NodeId = dbo.Nodes.NodeId 
            //    INNER JOIN dbo.FlatProperties ON dbo.Versions.VersionId = dbo.FlatProperties.VersionId 
            //    WHERE (dbo.Nodes.NodeTypeId = 1) AND (dbo.FlatProperties.Page = 0))
            //-- Remove PropertyType 'PT2' from PropertySet 'NT0'
            //DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertyTypeId = 4 AND PropertySetId = 1
            //GO
            #endregion

            string expectedSql = @"
						-- Remove PropertyType 'PT0' from PropertySet 'NT0'
						DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertyTypeId = 2 AND PropertySetId = 1
						GO
						-- Remove PropertyType 'PT1' from PropertySet 'NT0'
						DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertyTypeId = 3 AND PropertySetId = 1
						GO
						-- Remove PropertyType 'PT2' from PropertySet 'NT0'
						DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertyTypeId = 4 AND PropertySetId = 1
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }
        [TestMethod]
        public void SqlSchemaWriter_CreateNodeType_More()
        {
            SchemaEditor ed = new SchemaEditor();
            NodeType nt = CreateNodeType(ed, null, "NT0", "NT0Class", 1);

            var writer = SqlSchemaWriterAccessor.Create();
            writer.Open();
            writer.CreateNodeType(nt, "NT1", "NT1Class");
            writer.CreateNodeType(nt, "NT2", "NT2Class");

            string expectedSql = @"
						-- Create NodeType NT0/NT1
						DECLARE @parentId int
						SELECT @parentId = [PropertySetId] FROM [dbo].[SchemaPropertySets] WHERE [Name] = 'NT0'
						INSERT INTO [dbo].[SchemaPropertySets] ([ParentId], [Name], [PropertySetTypeId], [ClassName]) VALUES (@parentId, 'NT1', 1, 'NT1Class')
						GO
						-- Create NodeType NT0/NT2
						DECLARE @parentId int
						SELECT @parentId = [PropertySetId] FROM [dbo].[SchemaPropertySets] WHERE [Name] = 'NT0'
						INSERT INTO [dbo].[SchemaPropertySets] ([ParentId], [Name], [PropertySetTypeId], [ClassName]) VALUES (@parentId, 'NT2', 1, 'NT2Class')
						GO";

            string sql = writer.GetSqlScript();
            AssertScriptsAreEqual(expectedSql, sql);;
        }

        //================================================= Tools =================================================

        private void AssertScriptsAreEqual(string striptA, string scriptB)
        {
            Assert.AreEqual(
                striptA.Replace("\r\n", "").Replace("\t", "").Replace("    ", ""),
                scriptB.Replace("\r\n", "").Replace("\t", "").Replace("    ", ""));
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

        private Content CreateTestNodeForRemovePropertyType()
        {
            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='TestNodeForRemovePropertyType' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='Int1' type='Integer' />
						<Field name='Int2' type='Integer' />
						<Field name='String1' type='ShortText' />
						<Field name='String2' type='ShortText' />
						<Field name='String3' type='ShortText' />
						<Field name='String4' type='ShortText' />
						<Field name='Text1' type='LongText' />
						<Field name='Text2' type='LongText' />
						<Field name='Text3' type='LongText' />
						<Field name='Text4' type='LongText' />
					</Fields>
				</ContentType>");
            Content content = Content.CreateNew("TestNodeForRemovePropertyType", Repository.Root, "TestNodeForRemovePropertyType");

            StringBuilder sb = new StringBuilder();
            string shortString = "Short string";
            for (int i = 0; i < 50; i++)
                sb.Append("|ten char|");
            string midString = sb.ToString(); //length = 500
            sb.Length = 0;
            for (int i = 0; i < 500; i++)
                sb.Append("|ten char|");
            string longString = sb.ToString(); //length = 5000

            content["Int2"] = 1234;
            content["String2"] = shortString;
            content["String3"] = midString;
            content["String4"] = longString;
            content["Text2"] = shortString;
            content["Text3"] = midString;
            content["Text4"] = longString;

            content.Save();
            return content;
        }
    }

}