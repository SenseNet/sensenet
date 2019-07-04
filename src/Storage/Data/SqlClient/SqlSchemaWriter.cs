using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage.Schema;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    [Obsolete("##", true)]
    internal class SqlSchemaWriter : SchemaWriter
    {
        private List<StringBuilder> _scripts;
        private List<string> _variables;
        private Dictionary<string, List<string>> _slotsToReset; // key: "{NodeTypeId}.{PageIndex}", value: "{slotName list}", e.g.: "123.0", "nvarchar_12"

        public SqlSchemaWriter() { }

        public override void Open()
        {
            if (_scripts != null)
                throw new InvalidOperationException("Writer is already opened.");
            _scripts = new List<StringBuilder>();
            _variables = new List<string>();
            _slotsToReset = new Dictionary<string, List<string>>();
        }
        public override void Close()
        {
            if (_scripts == null)
                throw new InvalidOperationException("Writer is closed.");
            if (_scripts.Count == 0)
                return;
            ExecuteScripts(_scripts);
            _scripts = null;
        }
        private void ExecuteScripts(List<StringBuilder> scripts)
        {
            using (var op = SnTrace.Database.StartOperation("Execute storage schema scripts."))
            {
                // Ensure transaction encapsulation
                bool isLocalTransaction = !TransactionScope.IsActive;
                if (isLocalTransaction)
                    TransactionScope.Begin();
                try
                {
                    if (_slotsToReset.Count > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        scripts.Insert(0, sb);
                        WriteSlotsToResetScripts(sb, _slotsToReset);
                    }
                    foreach (StringBuilder script in scripts)
                    {
                        using (var proc = new SqlProcedure { CommandText = script.ToString(), CommandType = CommandType.Text })
                        {
                            proc.ExecuteNonQuery();
                        }
                    }
                    if (isLocalTransaction)
                        TransactionScope.Commit();
                    op.Successful = true;
                }
                finally
                {
                    if (isLocalTransaction && TransactionScope.IsActive)
                        TransactionScope.Rollback();
                }
            }
        }

        private string GetSqlScript()
        {
            StringBuilder sb = new StringBuilder();
            if (_slotsToReset.Count > 0)
                WriteSlotsToResetScripts(sb, _slotsToReset);
            foreach (StringBuilder script in _scripts)
                sb.Append(script).Append("GO").AppendLine();
            return sb.ToString();
        }

        public override void CreatePropertyType(string name, DataType dataType, int mapping, bool isContentListProperty)
        {
            StringBuilder sb = new StringBuilder();
            WriteInsertScript(sb, "SchemaPropertyTypes",
                CreateCommentLine("Create PropertyType '", name, "', ", dataType),
                "Name", name,
                "DataTypeId", (int)dataType,
                "Mapping", mapping,
                "IsContentListProperty", isContentListProperty ? 1 : 0);
            AddScript(sb);
        }
        public override void DeletePropertyType(PropertyType propertyType)
        {
            StringBuilder sb = new StringBuilder();
            if (propertyType == null)
                throw new ArgumentNullException("propertyType");
            sb.Append(CreateCommentLine("Delete PropertyType '", propertyType.Name, "'"));
            sb.Append("DELETE FROM [dbo].[SchemaPropertyTypes] WHERE PropertyTypeId = ");
            sb.Append(propertyType.Id).AppendLine();
            AddScript(sb);
        }

        public override void CreateNodeType(NodeType parent, string name, string className)
        {
            StringBuilder sb = new StringBuilder();
            if (parent == null)
            {
                if (className == null)
                {
                    WriteInsertScript(sb, "SchemaPropertySets",
                        CreateCommentLine("Create NodeType without parent and handler: ", name),
                        "Name", name,
                        "PropertySetTypeId", NodeTypeSchemaId
                        );
                }
                else
                {
                    WriteInsertScript(sb, "SchemaPropertySets",
                        CreateCommentLine("Create NodeType without parent: ", name),
                        "Name", name,
                        "PropertySetTypeId", NodeTypeSchemaId,
                        "ClassName", className
                        );
                }
            }
            else
            {
                sb.Append(CreateCommentLine("Create NodeType ", className == null ? "without handler: " : "", parent.Name, "/", name));
                DeclareIntVariable(sb, "parentId");
                sb.Append("SELECT @parentId = [PropertySetId] FROM [dbo].[SchemaPropertySets] WHERE [Name] = '").Append(parent.Name).Append("'").AppendLine();
                if (className == null)
                {
                    WriteInsertScript(sb, "SchemaPropertySets",
                        null,
                        "ParentId", "@parentId",
                        "Name", name,
                        "PropertySetTypeId", NodeTypeSchemaId
                        );
                }
                else
                {
                    WriteInsertScript(sb, "SchemaPropertySets",
                        null,
                        "ParentId", "@parentId",
                        "Name", name,
                        "PropertySetTypeId", NodeTypeSchemaId,
                        "ClassName", className
                        );
                }
            }
            AddScript(sb);
        }
        public override void ModifyNodeType(NodeType nodeType, NodeType parent, string className)
        {
            StringBuilder sb = new StringBuilder();
            if (nodeType == null)
                throw new ArgumentNullException("nodeType");
            WriteUpdateScript(sb, "SchemaPropertySets",
                String.Concat("PropertySetId = ", nodeType.Id),
                CreateCommentLine("Modify NodeType: ", nodeType.Name, " (original ClassName = '", nodeType.ClassName, "')"),
                "ParentId", parent == null ? (object)null : (object)parent.Id,
                "ClassName", className);
            AddScript(sb);
        }
        public override void DeleteNodeType(NodeType nodeType)
        {
            StringBuilder sb = new StringBuilder();
            if (nodeType == null)
                throw new ArgumentNullException("nodeType");
            sb.Append(CreateCommentLine("Delete NodeType '", nodeType.Name, "'"));
            sb.Append("DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertySetId = ").Append(nodeType.Id).AppendLine();
            sb.Append("DELETE FROM [dbo].[SchemaPropertySets] WHERE PropertySetId = ").Append(nodeType.Id).AppendLine();
            AddScript(sb);
        }

        public override void CreateContentListType(string name)
        {
            StringBuilder sb = new StringBuilder();
            WriteInsertScript(sb, "SchemaPropertySets",
                CreateCommentLine("CreateContentListType: ", name),
                "Name", name,
                "PropertySetTypeId", ContentListTypeSchemaId
                );
            AddScript(sb);
        }
        public override void DeleteContentListType(ContentListType contentListType)
        {
            StringBuilder sb = new StringBuilder();
            if (contentListType == null)
                throw new ArgumentNullException("contentListType");
            sb.Append(CreateCommentLine("Delete ContentListType '", contentListType.Name, "'"));
            sb.Append("DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertySetId = ").Append(contentListType.Id).AppendLine();
            sb.Append("DELETE FROM [dbo].[SchemaPropertySets] WHERE PropertySetId = ").Append(contentListType.Id).AppendLine();
            AddScript(sb);
        }

        public override void AddPropertyTypeToPropertySet(PropertyType propertyType, PropertySet owner, bool isDeclared)
        {
            StringBuilder sb = new StringBuilder();
            if (propertyType == null)
                throw new ArgumentNullException("propertyType");
            if (owner == null)
                throw new ArgumentNullException("owner");
            sb.Append(CreateCommentLine("Add PropertyType to PropertySet (", owner.Name, ".", propertyType.Name, " : ", propertyType.DataType, ")"));
            DeclareIntVariable(sb, "propertyTypeId");
            sb.Append("SELECT @propertyTypeId = [PropertyTypeId] FROM [dbo].[SchemaPropertyTypes] WHERE [Name] = '").Append(propertyType.Name).Append("'").AppendLine();
            DeclareIntVariable(sb, "ownerId");
            sb.Append("SELECT @ownerId = [PropertySetId] FROM [dbo].[SchemaPropertySets] WHERE [Name] = '").Append(owner.Name).Append("'").AppendLine();
            WriteInsertScript(sb, "SchemaPropertySetsPropertyTypes",
                null,
                "PropertyTypeId", "@propertyTypeId",
                "PropertySetId", "@ownerId",
                "IsDeclared", isDeclared ? 1 : 0
                );
            AddScript(sb);
        }
        public override void RemovePropertyTypeFromPropertySet(PropertyType propertyType, PropertySet owner)
        {
            StringBuilder sb = new StringBuilder();
            if (propertyType == null)
                throw new ArgumentNullException("propertyType");
            if (owner == null)
                throw new ArgumentNullException("owner");
            WriteResetPropertyScript(sb, owner, propertyType);
            sb.Append(CreateCommentLine("Remove PropertyType '", propertyType.Name, "' from PropertySet '", owner.Name, "'"));
            sb.Append("DELETE FROM [dbo].[SchemaPropertySetsPropertyTypes] WHERE PropertyTypeId = ");
            sb.Append(propertyType.Id);
            sb.Append(" AND PropertySetId = ");
            sb.Append(owner.Id).AppendLine();
            AddScript(sb);
        }
        public override void UpdatePropertyTypeDeclarationState(PropertyType propertyType, NodeType owner, bool isDeclared)
        {
            StringBuilder sb = new StringBuilder();
            if (propertyType == null)
                throw new ArgumentNullException("propertyType");
            if (owner == null)
                throw new ArgumentNullException("owner");
            WriteUpdateScript(sb, "SchemaPropertySetsPropertyTypes",
                String.Concat("PropertySetId = ", owner.Id, " AND PropertyTypeId = ", propertyType.Id),
                CreateCommentLine("Update PropertyType declaration: ", owner.Name, ".", propertyType.Name, ". Set IsDeclared = ", isDeclared ? "true" : "false"),
                "IsDeclared", isDeclared ? 1 : 0);
            AddScript(sb);
        }

        // ================================================================================= Tools

        private void AddScript(StringBuilder sb)
        {
            _scripts.Add(sb);
            _variables.Clear();
        }
        private static string CreateCommentLine(params object[] items)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("-- ");
            for (int i = 0; i < items.Length; i++)
                sb.Append(items[i]);
            sb.AppendLine();
            return sb.ToString();
        }
        private void DeclareIntVariable(StringBuilder scriptBuilder, string name)
        {
            if (_variables.Contains(name))
                return;
            _variables.Add(name);
            scriptBuilder.Append("DECLARE @").Append(name).Append(" int").AppendLine();
        }

        private static void WriteInsertScript(StringBuilder scriptBuilder, string tableName, string comment, params object[] paramArray)
        {
            if (!String.IsNullOrEmpty(comment))
                scriptBuilder.Append(comment);
            scriptBuilder.Append("INSERT INTO [dbo].[").Append(tableName).Append("] (");
            for (int i = 0; i < paramArray.Length; i += 2)
            {
                if (i > 1)
                    scriptBuilder.Append(", ");
                scriptBuilder.Append("[").Append(paramArray[i]).Append("]");
            }
            scriptBuilder.Append(") VALUES (");
            for (int i = 1; i < paramArray.Length; i += 2)
            {
                var stringValue = paramArray[i] as string;
                var isString = stringValue != null && !stringValue.StartsWith("@", StringComparison.Ordinal);

                if (i > 1)
                    scriptBuilder.Append(", ");
                if (isString)
                    scriptBuilder.Append("'");
                scriptBuilder.Append(paramArray[i]);
                if (isString)
                    scriptBuilder.Append("'");
            }
            scriptBuilder.Append(")").AppendLine();
        }
        private static void WriteUpdateScript(StringBuilder scriptBuilder, string tableName, string whereClause, string comment, params object[] paramArray)
        {
            if (!String.IsNullOrEmpty(comment))
                scriptBuilder.Append(comment);
            scriptBuilder.Append("UPDATE [dbo].[").Append(tableName).Append("] SET").AppendLine();
            for (int i = 0; i < paramArray.Length; i += 2)
            {
                // Line start
                scriptBuilder.Append("\t\t");
                if (i > 1)
                    scriptBuilder.Append(",");

                // Param name
                scriptBuilder.Append("[").Append(paramArray[i]).Append("] = ");

                // Param value
                object value = paramArray[i + 1];
                if (value == null)
                {
                    scriptBuilder.Append("null");
                }
                else
                {
                    var stringValue = value as string;
                    var isString = stringValue != null && !stringValue.StartsWith("@", StringComparison.Ordinal);
                    if (isString)
                        scriptBuilder.Append("'");
                    scriptBuilder.Append(value);
                    if (isString)
                        scriptBuilder.Append("'");
                }

                // Line end
                scriptBuilder.AppendLine();
            }
            scriptBuilder.Append("\tWHERE ").Append(whereClause).AppendLine();

        }
        private void WriteResetPropertyScript(StringBuilder scriptBuilder, PropertySet nodeType, PropertyType slot)
        {
            string comment = CreateCommentLine("Reset property value: ", nodeType.Name, ".", slot.Name, ":", slot.DataType);
            switch (slot.DataType)
            {
                case DataType.String:
                    WriteResetPropertySlotScript(scriptBuilder, nodeType.Id, slot.Mapping, SqlProvider.StringMappingPrefix, SqlProvider.StringPageSize, comment);
                    break;
                case DataType.Int:
                    WriteResetPropertySlotScript(scriptBuilder, nodeType.Id, slot.Mapping, SqlProvider.IntMappingPrefix, SqlProvider.IntPageSize, comment);
                    break;
                case DataType.Currency:
                    WriteResetPropertySlotScript(scriptBuilder, nodeType.Id, slot.Mapping, SqlProvider.CurrencyMappingPrefix, SqlProvider.CurrencyPageSize, comment);
                    break;
                case DataType.DateTime:
                    WriteResetPropertySlotScript(scriptBuilder, nodeType.Id, slot.Mapping, SqlProvider.DateTimeMappingPrefix, SqlProvider.DateTimePageSize, comment);
                    break;
                case DataType.Binary:
                    WriteDeletePropertyScript(scriptBuilder, nodeType.Id, slot.Id, "BinaryProperties", "BinaryPropertyId", comment);
                    break;
                case DataType.Text:
                    WriteDeletePropertyScript(scriptBuilder, nodeType.Id, slot.Id, "TextPropertiesNVarchar", "TextPropertyNVarcharId", comment);
                    WriteDeletePropertyScript(scriptBuilder, nodeType.Id, slot.Id, "TextPropertiesNText", "TextPropertyNTextId", comment);
                    break;
                case DataType.Reference:
                    WriteDeletePropertyScript(scriptBuilder, nodeType.Id, slot.Id, "ReferenceProperties", "ReferencePropertyId", comment);
                    break;
                default:
                    break;
            }
        }
        private void WriteResetPropertySlotScript(StringBuilder scriptBuilder, int nodeTypeId, int mapping, string columnPrefix, int mappingPageSize, string comment)
        {
            int pageIndex = mapping / mappingPageSize;
            int columnIndex = mapping % mappingPageSize;

            string key = String.Concat(nodeTypeId, ".", pageIndex);
            string val = String.Concat(columnPrefix, columnIndex + 1);
            if (_slotsToReset.ContainsKey(key))
                _slotsToReset[key].Add(val);
            else
                _slotsToReset.Add(key, new List<string>(new string[] { val }));
        }
        private static void WriteDeletePropertyScript(StringBuilder scriptBuilder, int nodeTypeId, int propertyTypeId, string tableName, string tableIdColumn, string comment)
        {
            // BinaryProperty, NText, NVarchar, ReferenceProperty delete
            if (!String.IsNullOrEmpty(comment))
                scriptBuilder.Append(comment);
            scriptBuilder.Append("DELETE FROM dbo.").Append(tableName).Append(" WHERE ").Append(tableIdColumn);
            scriptBuilder.Append(" IN (SELECT dbo.").Append(tableName).Append(".").Append(tableIdColumn).Append(" FROM dbo.Nodes").AppendLine();
            scriptBuilder.Append("\tINNER JOIN dbo.Versions ON dbo.Versions.NodeId = dbo.Nodes.NodeId").AppendLine();
            scriptBuilder.Append("\tINNER JOIN dbo.").Append(tableName).Append(" ON dbo.Versions.VersionId = dbo.").Append(tableName);
            scriptBuilder.Append(".VersionId").AppendLine().Append("WHERE (dbo.Nodes.NodeTypeId = ").Append(nodeTypeId);
            scriptBuilder.Append(") AND (dbo.").Append(tableName).Append(".PropertyTypeId = ").Append(propertyTypeId).Append("))").AppendLine();
        }

        private void WriteSlotsToResetScripts(StringBuilder scriptBuilder, Dictionary<string, List<string>> slotsToReset)
        {
            scriptBuilder.Append(CreateCommentLine("Reset property values"));
            foreach (string key in slotsToReset.Keys)
            {
                // key: "{NodeTypeId}.{PageIndex}", value: "{slotName list}", e.g.: "123.0", "nvarchar_12"
                string[] sa = key.Split('.');
                int propertySetId = Convert.ToInt32(sa[0], CultureInfo.InvariantCulture);
                int pageIndex = Convert.ToInt32(sa[1], CultureInfo.InvariantCulture);
                scriptBuilder.Append("UPDATE dbo.FlatProperties ").AppendLine();
                int count = slotsToReset[key].Count;
                for (int i = 0; i < count; i++)
                    scriptBuilder.Append(i == 0 ? "\tSET " : "\t\t").Append(slotsToReset[key][i]).Append(" = NULL").Append(i < count - 1 ? "," : "").AppendLine();
                scriptBuilder.Append("WHERE Id IN (SELECT dbo.FlatProperties.Id FROM dbo.Nodes ").AppendLine();
                scriptBuilder.Append("\tINNER JOIN dbo.Versions ON dbo.Versions.NodeId = dbo.Nodes.NodeId ").AppendLine();
                scriptBuilder.Append("\tINNER JOIN dbo.FlatProperties ON dbo.Versions.VersionId = dbo.FlatProperties.VersionId ").AppendLine();
                scriptBuilder.Append("\tWHERE (dbo.Nodes.NodeTypeId = ").Append(propertySetId);
                scriptBuilder.Append(") AND (dbo.FlatProperties.Page = ").Append(pageIndex).Append("))").AppendLine();
            }
        }
    }
}