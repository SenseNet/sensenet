using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using SenseNet.ContentRepository.Storage.Data;
using System.Globalization;
using System.Xml;
using SenseNet.Diagnostics;
using System.Diagnostics;
using System.Linq;

namespace SenseNet.ContentRepository.Storage.Schema
{
    public abstract class SchemaRoot : ISchemaRoot
    {
        public static readonly string RepositoryStorageSchemaXmlNamespace = "http://schemas.sensenet.com/SenseNet/ContentRepository/Storage/Schema";

        private class NodeTypeInfo
        {
            public int Id;
            public int ParentId;
            public string Name;
            public string ClassName;

            public NodeTypeInfo(int id, int parentId, string name, string className)
            {
                Id = id;
                ParentId = parentId;
                Name = name;
                ClassName = className;
            }
        }
        private enum PropertySetType
        {
            NodeType, ContentListType
        }

        // ================================================================================ Fields

        private TypeCollection<PropertyType> _propertyTypes;
        private TypeCollection<NodeType> _nodeTypes;
        private TypeCollection<ContentListType> _contentListTypes;

        // ================================================================================ Properties

        public TypeCollection<PropertyType> PropertyTypes
        {
            get { return _propertyTypes; }
        }
        public TypeCollection<NodeType> NodeTypes
        {
            get { return _nodeTypes; }
        }
        public TypeCollection<ContentListType> ContentListTypes
        {
            get { return _contentListTypes; }
        }

        public long SchemaTimestamp { get; private set; }

        // ================================================================================ Construction

        protected SchemaRoot()
        {
            _propertyTypes = new TypeCollection<PropertyType>(this);
            _nodeTypes = new TypeCollection<NodeType>(this);
            _contentListTypes = new TypeCollection<ContentListType>(this);
        }

        // ================================================================================ Methods

        public void Clear()
        {
            _propertyTypes.Clear();
            _nodeTypes.Clear();
            _contentListTypes.Clear();
        }

        // -------------------------------------------------------------------------------- Load

        public void Load()
        {
            using (var op = SnTrace.Database.StartOperation("Load storage schema."))
            {
                Clear();
                DataSet dataSet = DataProvider.Current.LoadSchema();
                Load(dataSet);
                op.Successful = true;
            }
        }
        public void Load(XmlDocument schemaXml)
        {
            Load(BuildDataSetFromXml(schemaXml));
        }

        // -------------------------------------------------------------------------------- Load from DataSet

        private void Load(DataSet dataSet)
        {

            Dictionary<int, DataType> dataTypeHelper = BuildDataTypeHelper(dataSet);
            Dictionary<int, PropertySetType> propertySetTypeHelper = BuildPropertySetTypeHelper(dataSet);

            BuildPropertyTypes(dataSet.Tables["PropertyTypes"], dataTypeHelper);
            BuildPropertySets(dataSet.Tables["PropertySets"], propertySetTypeHelper);
            BuildPropertyTypeAssignments(dataSet.Tables["PropertySetsPropertyTypes"]);

            SchemaTimestamp = GetSchemaTimestamp(dataSet);
        }

        private static long GetSchemaTimestamp(DataSet dataSet)
        {
            if (dataSet == null) // usecase: in tests
                return 0;
            var table = dataSet.Tables["SchemaModification"];
            if (table == null) // usecase: in tests
                return 0;
            var rows = table.Rows;
            if (rows.Count == 0)
                return 0;
            var row = rows[0];
            return SenseNet.ContentRepository.Storage.Data.SqlClient.SqlProvider.GetLongFromBytes((byte[])row["Timestamp"]);
        }
        private static Dictionary<int, DataType> BuildDataTypeHelper(DataSet dataSet)
        {
            Dictionary<int, DataType> dataTypeHelper = new Dictionary<int, DataType>();
            foreach (DataRow row in dataSet.Tables["DataTypes"].Rows)
                dataTypeHelper.Add(TypeConverter.ToInt32(row["DataTypeID"]), (DataType)Enum.Parse(typeof(DataType), TypeConverter.ToString(row["Name"])));
            return dataTypeHelper;
        }
        private static Dictionary<int, PropertySetType> BuildPropertySetTypeHelper(DataSet dataSet)
        {
            Dictionary<int, PropertySetType> dataTypeHelper = new Dictionary<int, PropertySetType>();
            foreach (DataRow row in dataSet.Tables["PropertySetTypes"].Rows)
            {
                switch (TypeConverter.ToString(row["Name"]))
                {
                    case "NodeType":
                        dataTypeHelper.Add(TypeConverter.ToInt32(row["PropertySetTypeID"]), PropertySetType.NodeType);
                        break;
                    case "ContentListType":
                        dataTypeHelper.Add(TypeConverter.ToInt32(row["PropertySetTypeID"]), PropertySetType.ContentListType);
                        break;
                }
            }
            return dataTypeHelper;
        }

        private void BuildPropertyTypes(DataTable table, Dictionary<int, DataType> dataTypes)
        {
            foreach (DataRow row in table.Rows)
            {
                int id = TypeConverter.ToInt32(row["PropertyTypeID"]);
                string name = TypeConverter.ToString(row["Name"]);
                DataType dataType = dataTypes[TypeConverter.ToInt32(row["DataTypeID"])];
                int mapping = TypeConverter.ToInt32(row["Mapping"]);
                bool isContentListProperty = TypeConverter.ToBoolean(row["IsContentListProperty"]);

                CreatePropertyType(id, name, dataType, mapping, isContentListProperty);
            }
        }
        private void BuildPropertySets(DataTable table, Dictionary<int, PropertySetType> propertySetTypes)
        {
            List<NodeTypeInfo> ntiList = new List<NodeTypeInfo>();
            foreach (DataRow row in table.Rows)
            {
                int id = TypeConverter.ToInt32(row["PropertySetID"]);
                int parentID = row["ParentID"] is DBNull ? 0 : TypeConverter.ToInt32(row["ParentID"]);
                string name = TypeConverter.ToString(row["Name"]);
                string className = row["ClassName"] is DBNull ? null : TypeConverter.ToString(row["ClassName"]);
                PropertySetType propertySetType = propertySetTypes[TypeConverter.ToInt32(row["PropertySetTypeID"])];
                switch (propertySetType)
                {
                    case PropertySetType.NodeType:
                        ntiList.Add(new NodeTypeInfo(id, parentID, name, className));
                        break;
                    case PropertySetType.ContentListType:
                        CreateContentListType(id, name);
                        break;
                    default:
                        throw new InvalidSchemaException(String.Concat(SR.Exceptions.Schema.Msg_UnknownPropertySetType, propertySetType));
                }
            }
            while (ntiList.Count > 0)
            {
                for (int i = ntiList.Count - 1; i >= 0; i--)
                {
                    NodeTypeInfo nti = ntiList[i];
                    NodeType parent = null;
                    if (nti.ParentId == 0 || ((parent = _nodeTypes.GetItemById(nti.ParentId)) != null))
                    {
                        CreateNodeType(nti.Id, _nodeTypes.GetItemById(nti.ParentId), nti.Name, nti.ClassName);
                        ntiList.Remove(nti);
                    }
                }
            }
        }
        private void BuildPropertyTypeAssignments(DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                int propertyTypeId = TypeConverter.ToInt32(row["PropertyTypeId"]);
                int propertySetId = TypeConverter.ToInt32(row["PropertySetID"]);
                bool isDeclared = TypeConverter.ToBoolean(row["IsDeclared"]);
                PropertyType propertyType = _propertyTypes.GetItemById(propertyTypeId);
                if (propertyType == null)
                    throw new InvalidSchemaException(String.Concat(SR.Exceptions.Schema.Msg_PropertyTypeDoesNotExist, ": id=", propertyTypeId));
                PropertySet propertySet = null;
                if (propertyType.IsContentListProperty)
                    propertySet = _contentListTypes.GetItemById(propertySetId);
                else
                    propertySet = _nodeTypes.GetItemById(propertySetId);

                try
                {
                    if (isDeclared)
                        AddPropertyTypeToPropertySet(propertyType, propertySet);
                }
                catch (ArgumentNullException ex)
                {
                    SnLog.WriteWarning("Unknown property set: " + propertySetId, properties: new Dictionary<string, object>
                    {
                        {"PropertyTypeId", propertyType.Id},
                        {"Content list types: ", string.Join(", ", _contentListTypes.Select(cl => cl.Id))},
                        {"Node types: ", string.Join(", ", _nodeTypes.Select(nt => string.Format("{0} ({1})", nt.Name, nt.Id)))},
                    });

                    throw new InvalidSchemaException("Unknown property set: " + propertySetId, ex);
                }
            }
        }

        // -------------------------------------------------------------------------------- ToXml

        public string ToXml()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            sb.Append("<StorageSchema xmlns=\"").Append(RepositoryStorageSchemaXmlNamespace).AppendLine("\">");

            if (_propertyTypes.Count > 0)
            {
                sb.AppendLine("	<UsedPropertyTypes>");
                foreach (PropertyType propertyType in _propertyTypes)
                    PropertyTypeToXml(propertyType, sb, "\t\t");
                sb.AppendLine("	</UsedPropertyTypes>");
            }

            if (_nodeTypes.Count > 0)
            {
                sb.AppendLine("	<NodeTypeHierarchy>");
                foreach (NodeType nt in _nodeTypes)
                    if (nt.Parent == null)
                        NodeTypeToXml(nt, sb, "\t\t");
                sb.AppendLine("	</NodeTypeHierarchy>");
            }

            if (_contentListTypes.Count > 0)
            {
                sb.AppendLine("	<ContentListTypes>");
                foreach (var lt in _contentListTypes)
                    ContentListTypeToXml(lt, sb, "\t\t");
                sb.AppendLine("	</ContentListTypes>");
            }

            sb.AppendLine("</StorageSchema>");
            return sb.ToString();
        }
        private static void NodeTypeToXml(NodeType nt, StringBuilder sb, string indent)
        {
            // <NodeType itemID="1" name="NodeType1">
            sb.Append(indent).Append("<NodeType");
            sb.Append(" itemID=\"").Append(nt.Id).Append("\"");
            sb.Append(" name=\"").Append(nt.Name).Append("\"");
            if (nt.ClassName != null)
                sb.Append(" className=\"").Append(nt.ClassName).Append("\"");

            if (nt.DeclaredPropertyTypes.Count == 0 && nt.Children.Count == 0)
            {
                sb.AppendLine(" />");
                return;
            }
            sb.AppendLine(">");
            // Inherited PropertyTypes are not be written
            foreach (PropertyType pt in nt.DeclaredPropertyTypes)
                PropertyTypeReferenceToXml(pt/*, nt*/, sb, indent + "\t");

            // Types that are inherited from "nt"
            foreach (NodeType cnt in nt.Children)
                NodeTypeToXml(cnt, sb, indent + "\t");
            sb.Append(indent).AppendLine("</NodeType>");
        }
        private static void ContentListTypeToXml(ContentListType lt, StringBuilder sb, string indent)
        {
            // <NodeType itemID="1" name="NodeType1">
            sb.Append(indent).Append("<ContentListType");
            sb.Append(" itemID=\"").Append(lt.Id).Append("\"");
            sb.Append(" name=\"").Append(lt.Name).Append("\"");

            if (lt.PropertyTypes.Count == 0)
            {
                sb.AppendLine(" />");
                return;
            }
            sb.AppendLine(">");

            foreach (PropertyType pt in lt.PropertyTypes)
                PropertyTypeReferenceToXml(pt, sb, indent + "\t");

            sb.Append(indent).AppendLine("</ContentListType>");
        }
        private static void PropertyTypeToXml(PropertyType propertyType, StringBuilder sb, string indent)
        {
            sb.Append(indent).Append("<PropertyType itemID=\"").Append(propertyType.Id);
            sb.Append("\" name=\"").Append(propertyType.Name).Append("\" dataType=\"").Append(propertyType.DataType);
            sb.Append("\" mapping=\"").Append(propertyType.Mapping).Append("\"");
            if (propertyType.IsContentListProperty)
                sb.Append(" isContentListProperty=\"yes\"");
            sb.AppendLine(" />");
        }
        private static void PropertyTypeReferenceToXml(PropertyType pt, StringBuilder sb, string indent)
        {
            sb.Append(indent).AppendLine(String.Format(CultureInfo.CurrentCulture, "<PropertyType name=\"{0}\" />", pt.Name));
        }

        // -------------------------------------------------------------------------------- FromXml

        public static DataSet BuildDataSetFromXml(XmlDocument schemaXml)
        {
            Dictionary<string, int> dataTypes = new Dictionary<string, int>();
            dataTypes.Add("String", 1);
            dataTypes.Add("Text", 2);
            dataTypes.Add("Int", 3);
            dataTypes.Add("Currency", 4);
            dataTypes.Add("DateTime", 5);
            dataTypes.Add("Binary", 6);
            dataTypes.Add("Reference", 7);

            Dictionary<PropertySetType, int> propertySetTypes = new Dictionary<PropertySetType, int>();
            propertySetTypes.Add(PropertySetType.NodeType, 1);
            propertySetTypes.Add(PropertySetType.ContentListType, 2);

            DataSet dataSet = CreateDataSet(dataTypes, propertySetTypes);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(schemaXml.NameTable);
            nsmgr.AddNamespace("x", RepositoryStorageSchemaXmlNamespace);

            foreach (XmlNode node in schemaXml.DocumentElement.SelectNodes("x:UsedPropertyTypes/x:PropertyType", nsmgr))
                BuildPropertyTypeRow(node, dataSet.Tables["PropertyTypes"], dataTypes);
            foreach (XmlNode node in schemaXml.DocumentElement.SelectNodes("x:NodeTypeHierarchy/x:NodeType", nsmgr))
                BuildNodeTypeRow(node, nsmgr, dataSet.Tables["PropertySets"], PropertySetType.NodeType, propertySetTypes);
            foreach (XmlNode node in schemaXml.DocumentElement.SelectNodes("x:ContentListTypes/x:ContentListType", nsmgr))
                BuildNodeTypeRow(node, nsmgr, dataSet.Tables["PropertySets"], PropertySetType.ContentListType, propertySetTypes);

            return dataSet;
        }
        private static DataSet CreateDataSet(Dictionary<string, int> dataTypes, Dictionary<PropertySetType, int> propertySetTypes)
        {
            DataTable table;

            DataSet dataSet = new DataSet();

            table = dataSet.Tables.Add("DataTypes");
            table.Columns.Add("DataTypeID", typeof(int));
            table.Columns.Add("Name", typeof(string));
            foreach (string key in dataTypes.Keys)
                table.Rows.Add(dataTypes[key].ToString(CultureInfo.InvariantCulture), key);

            table = dataSet.Tables.Add("PropertySetTypes");
            table.Columns.Add("PropertySetTypeID", typeof(int));
            table.Columns.Add("Name", typeof(string));
            foreach (PropertySetType key in propertySetTypes.Keys)
                table.Rows.Add(propertySetTypes[key], key);

            table = dataSet.Tables.Add("PropertySets");
            table.Columns.Add("PropertySetID", typeof(int));
            table.Columns.Add("ParentID", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("PropertySetTypeID", typeof(int));
            table.Columns.Add("ClassName", typeof(string));

            table = dataSet.Tables.Add("PropertyTypes");
            table.Columns.Add("PropertyTypeId", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("DataTypeID", typeof(int));
            table.Columns.Add("Mapping", typeof(int));
            table.Columns.Add("IsContentListProperty", typeof(byte));

            table = dataSet.Tables.Add("PropertySetsPropertyTypes");
            table.Columns.Add("PropertyTypeID", typeof(int));
            table.Columns.Add("PropertySetID", typeof(int));
            table.Columns.Add("IsDeclared", typeof(byte));

            return dataSet;
        }

        private static void BuildPropertyTypeRow(XmlNode node, DataTable table, Dictionary<string, int> dataTypes)
        {
            // <PropertyType itemID="1" name="PageNameInMenu" dataType="String" mapping="1" isContentListProperty="yes" />

            int id = GetIntFromXmlAttribute(node, "itemID");
            string name = GetStringFromXmlAttribute(node, "name");
            int dataTypeId = dataTypes[GetStringFromXmlAttribute(node, "dataType")];
            int mapping = GetIntFromXmlAttribute(node, "mapping");
            byte isContentListProperty = GetByteFromXmlAttribute(node, "isContentListProperty");

            table.Rows.Add(id, name, dataTypeId, mapping, isContentListProperty);
        }
        private static void BuildNodeTypeRow(XmlNode node, XmlNamespaceManager nsmgr, DataTable table, PropertySetType propertySetType, Dictionary<PropertySetType, int> propertySetTypes)
        {
            // <NodeType itemID="2" name="NodeType11" className="NodeTypeClass2">
            //    <PropertyType name="Int1" />
            //    <PropertyType name="DateTime1" />
            //    <NodeType itemID="8" name="NodeType111" className="NodeTypeClass111" />
            // </NodeType>

            int id = GetIntFromXmlAttribute(node, "itemID");
            int parentId = node.ParentNode == null ? 0 : GetIntFromXmlAttribute(node.ParentNode, "itemID");
            string name = GetStringFromXmlAttribute(node, "name");
            string className = GetStringFromXmlAttribute(node, "className");
            int propertySetTypeId = propertySetTypes[propertySetType];

            table.Rows.Add(id, parentId == 0 ? DBNull.Value : (object)parentId, name, propertySetTypeId, className == null ? (object)DBNull.Value : (object)className);

            foreach (XmlNode subNode in node.SelectNodes("x:PropertyType", nsmgr))
                BuildPropertySetPropertyTypeRow(subNode, table.DataSet.Tables["PropertySetsPropertyTypes"]);
            foreach (XmlNode subNode in node.SelectNodes("x:NodeType", nsmgr))
                BuildNodeTypeRow(subNode, nsmgr, table, propertySetType, propertySetTypes);
        }
        private static void BuildPropertySetPropertyTypeRow(XmlNode node, DataTable table)
        {
            // <PropertyType name="Int1" />

            string name = GetStringFromXmlAttribute(node, "name");
            int propTypeId = GetPropertyTypeIdByName(table.DataSet, name);
            int propertySetId = GetIntFromXmlAttribute(node.ParentNode, "itemID");

            table.Rows.Add(propTypeId, propertySetId, 1);
        }

        private static int GetIntFromXmlAttribute(XmlNode node, string attrName)
        {
            XmlAttribute attr = node.Attributes[attrName];
            return attr == null ? 0 : Convert.ToInt32(attr.Value, CultureInfo.CurrentCulture);
        }
        private static byte GetByteFromXmlAttribute(XmlNode node, string attrName)
        {
            XmlAttribute attr = node.Attributes[attrName];
            if (attr == null)
                return 0;
            return (byte)(attr.Value == "yes" ? 1 : 0);
        }
        private static string GetStringFromXmlAttribute(XmlNode node, string attrName)
        {
            XmlAttribute attr = node.Attributes[attrName];
            return attr == null ? null : node.Attributes[attrName].Value;
        }
        private static int GetPropertyTypeIdByName(DataSet dataSet, string name)
        {
            foreach (DataRow row in dataSet.Tables["PropertyTypes"].Rows)
                if (TypeConverter.ToString(row["Name"]) == name)
                    return TypeConverter.ToInt32(row[0]);
            return 0;
        }

        // -------------------------------------------------------------------------------- Editor

        #region PropertyType
        public PropertyType CreatePropertyType(string name, DataType dataType)
        {
            return CreatePropertyType(name, dataType, GetNextMapping(dataType));
        }
        public PropertyType CreatePropertyType(string name, DataType dataType, int mapping)
        {
            return CreatePropertyType(0, name, dataType, mapping, false);
        }
        public PropertyType CreateContentListPropertyType(DataType dataType, int ordinalNumber)
        {
            string name = String.Concat("#", dataType, "_", ordinalNumber);
            int mapping = ordinalNumber + DataProvider.Current.ContentListMappingOffsets[dataType];
            return CreateContentListPropertyType(name, dataType, mapping);
        }
        private PropertyType CreateContentListPropertyType(string name, DataType dataType, int mapping)
        {
            return CreatePropertyType(0, name, dataType, mapping, true);
        }
        private PropertyType CreatePropertyType(int id, string name, DataType dataType, int mapping, bool isContentListProperty)
        {
            PropertyType propType = new PropertyType(this, name, id, dataType, mapping, isContentListProperty);
            this.PropertyTypes.Add(propType);

            return propType;
        }
        public void DeletePropertyType(PropertyType propertyType)
        {
            if (propertyType == null)
                throw new ArgumentNullException("propertyType");
            if (propertyType.SchemaRoot != this)
                throw new SchemaEditorCommandException(SR.Exceptions.Schema.Msg_InconsistentHierarchy);

            propertyType.CheckPropertyTypeUsage(SR.Exceptions.Schema.Msg_ProtectedPropetyTypeDeleteViolation);

            // remove slot
            this.PropertyTypes.Remove(propertyType);
        }
        #endregion

        #region NodeType
        public NodeType CreateNodeType(NodeType parent, string name)
        {
            return CreateNodeType(parent, name, null);
        }
        public NodeType CreateNodeType(NodeType parent, string name, string className)
        {
            return CreateNodeType(0, parent, name, className);
        }
        private NodeType CreateNodeType(int id, NodeType parent, string name, string className)
        {
            NodeType nodeType = new NodeType(id, name, this, className, parent);
            this.NodeTypes.Add(nodeType);
            return nodeType;
        }
        public void ModifyNodeType(NodeType nodeType, string className)
        {
            if (nodeType == null)
                throw new ArgumentNullException("nodeType");
            if (nodeType.SchemaRoot != this)
                throw new SchemaEditorCommandException(SR.Exceptions.Schema.Msg_InconsistentHierarchy);
            nodeType.ClassName = className;
        }
        public void ModifyNodeType(NodeType nodeType, NodeType parent)
        {
            if (nodeType == null)
                throw new ArgumentNullException("nodeType");
            if (nodeType.SchemaRoot != this)
                throw new SchemaEditorCommandException(SR.Exceptions.Schema.Msg_InconsistentHierarchy);
            if (parent.SchemaRoot != this)
                throw new SchemaEditorCommandException(SR.Exceptions.Schema.Msg_InconsistentHierarchy);
            if (nodeType == parent)
                throw new SchemaEditorCommandException(SR.Exceptions.Schema.Msg_CircularReference);
            if (nodeType.Parent != parent)
                nodeType.MoveTo(parent);
        }
        public void DeleteNodeType(NodeType nodeType)
        {
            if (nodeType == null)
                throw new ArgumentNullException("nodeType");
            if (nodeType.SchemaRoot != this)
                throw new SchemaEditorCommandException(SR.Exceptions.Schema.Msg_InconsistentHierarchy);
            DeleteNodeTypeInternal(nodeType);
        }
        private void DeleteNodeTypeInternal(NodeType nodeType)
        {
            foreach (NodeType subType in nodeType.Children.ToArray())
                DeleteNodeTypeInternal(subType);

            nodeType.PropertyTypes.Clear();
            nodeType.DeclaredPropertyTypes.Clear();

            this.NodeTypes.Remove(nodeType);
        }
        #endregion

        #region ContentListType
        public ContentListType CreateContentListType(string name)
        {
            return CreateContentListType(0, name);
        }
        private ContentListType CreateContentListType(int id, string name)
        {
            var listType = new ContentListType(id, name, this);
            this.ContentListTypes.Add(listType);
            return listType;
        }
        public void DeleteContentListType(ContentListType listType)
        {
            if (listType == null)
                throw new ArgumentNullException("listType");
            if (listType.SchemaRoot != this)
                throw new SchemaEditorCommandException(SR.Exceptions.Schema.Msg_InconsistentHierarchy);
            listType.PropertyTypes.Clear();
            this.ContentListTypes.Remove(listType);
        }
        #endregion

        #region PropertyType to PropertySet
        public void AddPropertyTypeToPropertySet(PropertyType propertyType, PropertySet owner)
        {
            if (propertyType == null)
                throw new ArgumentNullException("propertyType");
            if (owner == null)
                throw new ArgumentNullException("owner");
            if (propertyType.SchemaRoot != this)
                throw new InvalidSchemaException(SR.Exceptions.Schema.Msg_InconsistentHierarchy);
            if (owner.SchemaRoot != this)
                throw new InvalidSchemaException(SR.Exceptions.Schema.Msg_InconsistentHierarchy);
            owner.AddPropertyType(propertyType);
        }
        public void RemovePropertyTypeFromPropertySet(PropertyType propertyType, PropertySet owner)
        {
            if (propertyType == null)
                throw new ArgumentNullException("propertyType");
            if (owner == null)
                throw new ArgumentNullException("owner");
            if (propertyType.SchemaRoot != this)
                throw new InvalidSchemaException(SR.Exceptions.Schema.Msg_InconsistentHierarchy);
            if (owner.SchemaRoot != this)
                throw new InvalidSchemaException(SR.Exceptions.Schema.Msg_InconsistentHierarchy);

            owner.RemovePropertyType(propertyType);
            if (!IsUsedPropertyType(propertyType))
                DeletePropertyType(propertyType);
        }
        #endregion

        private int GetNextMapping(DataType dataType)
        {
            List<bool> usedSlots = new List<bool>();

            foreach (PropertyType propType in this.PropertyTypes)
            {
                if (propType.DataType == dataType && !propType.IsContentListProperty)
                {
                    while (usedSlots.Count <= propType.Mapping)
                        usedSlots.Add(false);
                    usedSlots[propType.Mapping] = true;
                }
            }
            usedSlots.Add(false);
            for (int i = 0; i < usedSlots.Count; i++)
                if (!usedSlots[i])
                    return i;

            throw new InvalidSchemaException("GetNextMapping");
        }
        private bool IsUsedPropertyType(PropertyType propertyType)
        {
            foreach (var nodeType in this.NodeTypes)
                if (nodeType.PropertyTypes.Contains(propertyType))
                    return true;
            foreach (var listType in this.ContentListTypes)
                if (listType.PropertyTypes.Contains(propertyType))
                    return true;
            return false;
        }

    }
}