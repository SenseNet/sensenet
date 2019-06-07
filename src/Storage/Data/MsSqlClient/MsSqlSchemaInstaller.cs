using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    internal class MsSqlSchemaInstaller
    {
        private static readonly byte Yes = 1;
        private static readonly byte No = 0;

        private static class TableName
        {
            public static readonly string PropertyTypes = "PropertyTypes";
            public static readonly string NodeTypes = "NodeTypes";
            public static readonly string ContentListTypes = "ContentListTypes";
        }

        private static Dictionary<string, string[]> _columnNames;

        public static async Task InstallSchemaAsync(RepositorySchemaData schema, string connectionString)
        {
            var dataSet = new DataSet();

            CreateTableStructure(dataSet);

            _columnNames = dataSet.Tables.Cast<DataTable>().ToDictionary(
                table => table.TableName,
                table => table.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray());

            CreateData(dataSet, schema);

            await WriteToDatabaseAsync(dataSet, connectionString);
        }

        /* ==================================================================================================== Tables */

        private static void CreateTableStructure(DataSet dataSet)
        {
            AddNodeTypesTable(dataSet);
            AddPropertyTypesTable(dataSet);
            AddContentListTypesTable(dataSet);
        }

        private static void AddPropertyTypesTable(DataSet dataSet)
        {
            var table = new DataTable(TableName.PropertyTypes);
            table.Columns.AddRange(new[]
            {
                new DataColumn {ColumnName = "PropertyTypeId", DataType = typeof(int), AllowDBNull = false},
                new DataColumn {ColumnName = "Name", DataType = typeof(string), AllowDBNull = false},
                new DataColumn {ColumnName = "DataType", DataType = typeof(string), AllowDBNull = false},
                new DataColumn {ColumnName = "Mapping", DataType = typeof(int), AllowDBNull = false},
                new DataColumn {ColumnName = "IsContentListProperty", DataType = typeof(byte), AllowDBNull = false},
            });
            dataSet.Tables.Add(table);
        }

        private static void AddNodeTypesTable(DataSet dataSet)
        {
            var table = new DataTable(TableName.NodeTypes);
            table.Columns.AddRange(new[]
            {
                new DataColumn {ColumnName = "NodeTypeId", DataType = typeof(int), AllowDBNull = false},
                new DataColumn {ColumnName = "ParentId", DataType = typeof(int), AllowDBNull = true},
                new DataColumn {ColumnName = "Name", DataType = typeof(string), AllowDBNull = false},
                new DataColumn {ColumnName = "ClassName", DataType = typeof(string), AllowDBNull = false},
                new DataColumn {ColumnName = "Properties", DataType = typeof(string), AllowDBNull = false},
            });
            dataSet.Tables.Add(table);
        }

        private static void AddContentListTypesTable(DataSet dataSet)
        {
            var table = new DataTable(TableName.ContentListTypes);
            table.Columns.AddRange(new[]
            {
                new DataColumn {ColumnName = "ContentListTypeId", DataType = typeof(int), AllowDBNull = false},
                new DataColumn {ColumnName = "Name", DataType = typeof(string), AllowDBNull = false},
                new DataColumn {ColumnName = "Properties", DataType = typeof(string), AllowDBNull = false},
            });
            dataSet.Tables.Add(table);
        }

        /* ==================================================================================================== Fill Data */

        private static void CreateData(DataSet dataSet, RepositorySchemaData schema)
        {
            var propertyTypes = dataSet.Tables[TableName.PropertyTypes];
            foreach (var propertyType in schema.PropertyTypes)
            {
                var row = propertyTypes.NewRow();
                SetPropertyTypeRow(row, propertyType);
                propertyTypes.Rows.Add(row);
            }

            var nodeTypes = dataSet.Tables[TableName.NodeTypes];
            foreach (var nodeType in schema.NodeTypes)
            {
                var row = nodeTypes.NewRow();
                SetNodeTypeRow(row, nodeType, schema.NodeTypes);
                nodeTypes.Rows.Add(row);
            }

            var contentListTypes = dataSet.Tables[TableName.ContentListTypes];
            foreach (var contentListType in schema.ContentListTypes)
            {
                var row = contentListTypes.NewRow();
                SetContentListTypeRow(row, contentListType);
                contentListTypes.Rows.Add(row);
            }
        }

        private static void SetPropertyTypeRow(DataRow row, PropertyTypeData propertyType)
        {
            row["PropertyTypeId"] = propertyType.Id;
            row["Name"] = propertyType.Name;
            row["DataType"] = propertyType.DataType.ToString();
            row["Mapping"] = propertyType.Mapping;
            row["IsContentListProperty"] = propertyType.IsContentListProperty ? Yes : No;
        }

        private static void SetNodeTypeRow(DataRow row, NodeTypeData nodeType, List<NodeTypeData> allNodeTypes)
        {
            row["NodeTypeId"] = nodeType.Id;
            row["Name"] = nodeType.Name;
            row["ParentId"] = (object) allNodeTypes.FirstOrDefault(x => x.Name == nodeType.ParentName)?.Id ??
                              DBNull.Value;
            row["ClassName"] = nodeType.ClassName;
            row["Properties"] = string.Join(" ", nodeType.Properties);
        }

        private static void SetContentListTypeRow(DataRow row, ContentListTypeData contentListType)
        {
            row["ContentListTypeId"] = contentListType.Id;
            row["Name"] = contentListType.Name;
            row["Properties"] = string.Join(" ", contentListType.Properties);
        }

        /* ==================================================================================================== Writing */

        private static async Task WriteToDatabaseAsync(DataSet dataSet, string connectionString)
        {
            await BulkInsertAsync(dataSet, TableName.PropertyTypes, connectionString);
            await BulkInsertAsync(dataSet, TableName.NodeTypes, connectionString);
            await BulkInsertAsync(dataSet, TableName.ContentListTypes, connectionString);
        }

        private static async Task BulkInsertAsync(DataSet dataSet, string tableName, string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "DELETE " + tableName;
                connection.Open();
                command.ExecuteNonQuery();
            }
            using (var connection = new SqlConnection(connectionString))
            {
                var options = SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.KeepIdentity |
                              SqlBulkCopyOptions.UseInternalTransaction;

                connection.Open();
                using (var bulkCopy = new SqlBulkCopy(connection, options, null))
                {
                    bulkCopy.DestinationTableName = tableName;
                    bulkCopy.BulkCopyTimeout = 60 * 30;

                    var table = dataSet.Tables[tableName];

                    foreach (var name in _columnNames[tableName])
                        bulkCopy.ColumnMappings.Add(name, name);

                    await bulkCopy.WriteToServerAsync(table);
                }
                connection.Close();
            }
        }
    }
}
