using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;

namespace SenseNet.Storage.Data.MsSqlClient
{
    internal class MsSqlDataInstaller
    {
        private static readonly byte Yes = 1;
        private static readonly byte No = 0;

        private static class TableName
        {
            public static readonly string Nodes = "Nodes";
            public static readonly string Versions = "Versions";
            public static readonly string LongTextProperties = "LongTextProperties";
            public static readonly string BinaryProperties = "BinaryProperties";
            public static readonly string Files = "Files";
        }

        private static Dictionary<string, string[]> ColumnNames;
        //private static Dictionary<string, string[]> ColumnNames = new Dictionary<string, string[]>
        //{
        //    {
        //        TableName.Nodes, new[]
        //        {
        //            "NodeId", "NodeTypeId", "CreatingInProgress", "IsDeleted", "IsInherited", "ParentNodeId",
        //            "Name", "Path", "Index", "Locked", "ETag", "LockType", "LockTimeout", "LockDate", "LockToken", "LastLockUpdate",
        //            "LastMinorVersionId", "LastMajorVersionId", "CreationDate", "CreatedById", "ModificationDate", "ModifiedById",
        //            "IsSystem", "OwnerId", "SavingState"
        //        }
        //    },
        //    {
        //        TableName.Versions, new[]
        //        {
        //            "VersionId", "NodeId", "MajorNumber", "MinorNumber",
        //            "CreationDate", "CreatedById", "ModificationDate", "ModifiedById", "Status"
        //        }
        //    },
        //    {
        //        TableName.FlatProperties, new[]
        //        {
        //            "Id", "VersionId", "Page", "int_1", "int_2", "int_3", "int_4", "int_5", "int_6", "int_7", "int_8",
        //            "int_9", "int_10", "int_19", "money_1"
        //        }
        //    },
        //    {
        //        TableName.BinaryProperties, new[]
        //        {
        //            "BinaryPropertyId", "VersionId", "PropertyTypeId", "FileId"
        //        }
        //    },
        //    {
        //        TableName.Files, new[]
        //        {
        //            "FileId", "ContentType", "FileNameWithoutExtension", "Extension", "Size", "Stream", "CreationDate"
        //        }
        //    },
        //    {
        //        TableName.Entities, new[]
        //        {
        //            "Id", "OwnerId", "ParentId", "IsInherited"
        //        }
        //    },
        //};

        public static async Task InstallInitialDataAsync(InitialData data, DataProvider2 dataProvider, string connectionString)
        {
            var dataSet = new DataSet();

            CreateTableStructure(dataSet);

            ColumnNames = dataSet.Tables.Cast<DataTable>().ToDictionary(
                table => table.TableName,
                table => table.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray());

            CreateData(dataSet, data, dataProvider);

            await WriteToDatabaseAsync(dataSet, connectionString);
        }

        /* ==================================================================================================== Tables */

        private static void CreateTableStructure(DataSet dataSet)
        {
            AddNodesTable(dataSet);
            AddVersionsTable(dataSet);
            AddLongTextPropertiesTable(dataSet);
            AddBinaryPropertiesTable(dataSet);
            AddFilesTable(dataSet);
        }

        private static void AddNodesTable(DataSet dataSet)
        {
            var nodes = new DataTable(TableName.Nodes);
            nodes.Columns.AddRange(new[]
            {
                new DataColumn {ColumnName = "NodeId", DataType = typeof(int)},
                new DataColumn {ColumnName = "NodeTypeId", DataType = typeof(int)},
                new DataColumn {ColumnName = "CreatingInProgress", DataType = typeof(byte), AllowDBNull = false },
                new DataColumn {ColumnName = "IsDeleted", DataType = typeof(byte), AllowDBNull = false},
                new DataColumn {ColumnName = "IsInherited", DataType = typeof(byte), AllowDBNull = false},
                new DataColumn {ColumnName = "ParentNodeId", DataType = typeof(int)},
                new DataColumn {ColumnName = "Name", DataType = typeof(string)},
                new DataColumn {ColumnName = "Path", DataType = typeof(string)},
                new DataColumn {ColumnName = "Index", DataType = typeof(int)},
                new DataColumn {ColumnName = "Locked", DataType = typeof(byte), AllowDBNull = false},
                new DataColumn {ColumnName = "ETag", DataType = typeof(string)},
                new DataColumn {ColumnName = "LockType", DataType = typeof(int)},
                new DataColumn {ColumnName = "LockTimeout", DataType = typeof(int)},
                new DataColumn {ColumnName = "LockDate", DataType = typeof(DateTime)},
                new DataColumn {ColumnName = "LockToken", DataType = typeof(string)},
                new DataColumn {ColumnName = "LastLockUpdate", DataType = typeof(DateTime)},
                new DataColumn {ColumnName = "LastMinorVersionId", DataType = typeof(int)},
                new DataColumn {ColumnName = "LastMajorVersionId", DataType = typeof(int)},
                new DataColumn {ColumnName = "CreationDate", DataType = typeof(DateTime)},
                new DataColumn {ColumnName = "CreatedById", DataType = typeof(int)},
                new DataColumn {ColumnName = "ModificationDate", DataType = typeof(DateTime)},
                new DataColumn {ColumnName = "ModifiedById", DataType = typeof(int)},
                new DataColumn {ColumnName = "IsSystem", DataType = typeof(byte), AllowDBNull = true},
                new DataColumn {ColumnName = "OwnerId", DataType = typeof(int)},
                new DataColumn {ColumnName = "SavingState", DataType = typeof(int)},
            });
            dataSet.Tables.Add(nodes);
        }
        private static void AddVersionsTable(DataSet dataSet)
        {
            var versions = new DataTable(TableName.Versions);
            versions.Columns.AddRange(new[]
            {
                new DataColumn {ColumnName = "VersionId", DataType = typeof(int), AllowDBNull = false },
                new DataColumn {ColumnName = "NodeId", DataType = typeof(int), AllowDBNull = false },
                new DataColumn {ColumnName = "MajorNumber", DataType = typeof(byte), AllowDBNull = false },
                new DataColumn {ColumnName = "MinorNumber", DataType = typeof(byte), AllowDBNull = false },
                new DataColumn {ColumnName = "Status", DataType = typeof(byte), AllowDBNull = false},
                new DataColumn {ColumnName = "CreationDate", DataType = typeof(DateTime), AllowDBNull = false },
                new DataColumn {ColumnName = "CreatedById", DataType = typeof(int), AllowDBNull = false},
                new DataColumn {ColumnName = "ModificationDate", DataType = typeof(DateTime), AllowDBNull = false },
                new DataColumn {ColumnName = "ModifiedById", DataType = typeof(int), AllowDBNull = false},
                new DataColumn {ColumnName = "IndexDocument", DataType = typeof(string), AllowDBNull = true},
                new DataColumn {ColumnName = "ChangedData", DataType = typeof(string), AllowDBNull = true},
                new DataColumn {ColumnName = "DynamicProperties", DataType = typeof(string), AllowDBNull = true},
            });
            dataSet.Tables.Add(versions);
        }
        private static void AddLongTextPropertiesTable(DataSet dataSet)
        {
            var longTexts = new DataTable(TableName.LongTextProperties);
            longTexts.Columns.AddRange(new[]
            {
                new DataColumn {ColumnName = "LongTextPropertyId", DataType = typeof(int), AllowDBNull = false },
                new DataColumn {ColumnName = "VersionId", DataType = typeof(int), AllowDBNull = false },
                new DataColumn {ColumnName = "PropertyTypeId", DataType = typeof(int), AllowDBNull = false },
                new DataColumn {ColumnName = "Length", DataType = typeof(int), AllowDBNull = true },
                new DataColumn {ColumnName = "Value", DataType = typeof(string), AllowDBNull = true},
            });
            dataSet.Tables.Add(longTexts);
        }
        private static void AddBinaryPropertiesTable(DataSet dataSet)
        {
            var binaryProperties = new DataTable(TableName.BinaryProperties);
            binaryProperties.Columns.AddRange(new[]
            {
                new DataColumn {ColumnName = "BinaryPropertyId", DataType = typeof(int), AllowDBNull = false },
                new DataColumn {ColumnName = "VersionId", DataType = typeof(int), AllowDBNull = false },
                new DataColumn {ColumnName = "PropertyTypeId", DataType = typeof(int), AllowDBNull = false },
                new DataColumn {ColumnName = "FileId", DataType = typeof(int), AllowDBNull = false },
            });
            dataSet.Tables.Add(binaryProperties);
        }
        private static void AddFilesTable(DataSet dataSet)
        {
            var files = new DataTable(TableName.Files);
            files.Columns.AddRange(new[]
            {
                new DataColumn {ColumnName = "FileId", DataType = typeof(int), AllowDBNull = false },
                new DataColumn {ColumnName = "ContentType", DataType = typeof(string), AllowDBNull = false },
                new DataColumn {ColumnName = "FileNameWithoutExtension", DataType = typeof(string), AllowDBNull = true },
                new DataColumn {ColumnName = "Extension", DataType = typeof(string), AllowDBNull = false },
                new DataColumn {ColumnName = "Size", DataType = typeof(long), AllowDBNull = false },
                new DataColumn {ColumnName = "Stream", DataType = typeof(byte[]), AllowDBNull = true },
                new DataColumn {ColumnName = "CreationDate", DataType = typeof(DateTime), AllowDBNull = false },
                new DataColumn {ColumnName = "BlobProvider", DataType = typeof(string), AllowDBNull = true },
                new DataColumn {ColumnName = "BlobProviderData", DataType = typeof(string), AllowDBNull = true },
            });
            dataSet.Tables.Add(files);
        }

        /* ==================================================================================================== Fill Data */

        private static void CreateData(DataSet dataSet, InitialData data, DataProvider2 dataProvider)
        {
            var now = DateTime.UtcNow;

            var nodes = dataSet.Tables[TableName.Nodes];
            foreach (var node in data.Nodes)
            {
                var row = nodes.NewRow();
                node.CreationDate = now;
                node.ModificationDate = now;
                SetNodeRow(row, node, dataProvider);
                nodes.Rows.Add(row);
            }

            var versions = dataSet.Tables[TableName.Versions];
            var longTexts = dataSet.Tables[TableName.LongTextProperties];
            var binaryProperties = dataSet.Tables[TableName.BinaryProperties];
            var files = dataSet.Tables[TableName.Files];
            var longTextId = 0;
            foreach (var version in data.Versions)
            {
                var props = data.DynamicProperties.FirstOrDefault(x => x.VersionId == version.VersionId);
                if (props?.LongTextProperties != null)
                {
                    foreach (var longTextData in props.LongTextProperties)
                    {
                        var longTextRow = longTexts.NewRow();
                        SetLongTextPropertyRow(longTextRow, ++longTextId, version.VersionId, longTextData.Key, longTextData.Value);
                        longTexts.Rows.Add(longTextRow);
                    }
                }
                if (props?.BinaryProperties != null)
                {
                    foreach (var binaryPropertyData in props.BinaryProperties)
                    {
                        var binaryPropertyRow = binaryProperties.NewRow();
                        SetBinaryPropertyRow(binaryPropertyRow, version.VersionId, binaryPropertyData.Key, binaryPropertyData.Value);
                        binaryProperties.Rows.Add(binaryPropertyRow);

                        var fileRow = files.NewRow();
                        SetFileRow(fileRow, binaryPropertyData.Value, data, binaryPropertyData.Key.Name);
                        files.Rows.Add(fileRow);
                    }
                }

                var versionRow = versions.NewRow();
                version.CreationDate = now;
                version.ModificationDate = now;
                SetVersionRow(versionRow, version, props?.DynamicProperties, dataProvider);
                versions.Rows.Add(versionRow);
            }


        }
        private static void SetNodeRow(DataRow row, NodeHeadData node, DataProvider2 dataProvider)
        {
            row["NodeId"] = node.NodeId;
            row["NodeTypeId"] = node.NodeTypeId;
            row["CreatingInProgress"] = node.CreatingInProgress ? Yes : No;
            row["IsDeleted"] = node.IsDeleted ? Yes : No;
            row["IsInherited"] = (byte)0;
            row["ParentNodeId"] = node.ParentNodeId;

            row["Name"] = node.Name;
            row["Path"] = node.Path;
            row["Index"] = node.Index;
            row["Locked"] = node.Locked ? Yes : No;

            row["ETag"] = node.ETag ?? string.Empty;
            row["LockType"] = node.LockType;
            row["LockTimeout"] = node.LockTimeout;
            row["LockDate"] = AlignDateTime(node.LockDate, dataProvider);
            row["LockToken"] = node.LockToken ?? string.Empty;
            row["LastLockUpdate"] = AlignDateTime(node.LastLockUpdate, dataProvider);

            row["LastMinorVersionId"] = node.LastMinorVersionId;
            row["LastMajorVersionId"] = node.LastMajorVersionId;

            row["CreationDate"] = AlignDateTime(node.CreationDate, dataProvider);
            row["CreatedById"] = node.CreatedById;
            row["ModificationDate"] = AlignDateTime(node.ModificationDate, dataProvider);
            row["ModifiedById"] = node.ModifiedById;

            row["IsSystem"] = node.IsSystem ? Yes : No;
            row["OwnerId"] = node.OwnerId;
            row["SavingState"] = node.SavingState;
        }
        private static void SetVersionRow(DataRow row, VersionData version, IDictionary<PropertyType, object> dynamicProperties, DataProvider2 dataProvider)
        {
            row["VersionId"] = version.VersionId;
            row["NodeId"] = version.NodeId;
            row["MajorNumber"] = version.Version.Major;
            row["MinorNumber"] = version.Version.Minor;
            row["Status"] = (byte)version.Version.Status;
            row["CreationDate"] = AlignDateTime(version.CreationDate, dataProvider);
            row["CreatedById"] = version.CreatedById;
            row["ModificationDate"] = AlignDateTime(version.CreationDate, dataProvider);
            row["ModifiedById"] = version.ModifiedById;
            row["IndexDocument"] = null;
            row["ChangedData"] = null;
            row["DynamicProperties"] = GetSerializedDynamicData(dynamicProperties);
        }
        private static void SetLongTextPropertyRow(DataRow row, int id, int versionId, PropertyType propertyType, string value)
        {
            row["LongTextPropertyId"] = id;
            row["VersionId"] = versionId;
            row["PropertyTypeId"] = propertyType.Id;
            row["Length"] = value?.Length;
            row["Value"] = value;
        }
        private static void SetBinaryPropertyRow(DataRow row, int versionId, PropertyType propertyType, BinaryDataValue data)
        {
            row["BinaryPropertyId"] = data.Id;
            row["VersionId"] = versionId;
            row["PropertyTypeId"] = propertyType.Id;
            row["FileId"] = data.FileId;
        }
        private static void SetFileRow(DataRow row, BinaryDataValue data, InitialData initialData, string propertyTypeName)
        {
            byte[] buffer = null;
            var providerName = data.BlobProviderName;
            var providerData = data.BlobProviderData;
            if (providerName == null && providerData != null &&
                providerData.StartsWith("/Root", StringComparison.OrdinalIgnoreCase))
            {
                buffer = GetBuffer(initialData, providerData, propertyTypeName);
                providerData = null;
            }
            else if(data.Stream != null)
            {
                //UNDONE: Predefined stream? (delete this condition?)
            }

            row["FileId"] = data.FileId;
            row["ContentType"] = data.ContentType;
            row["FileNameWithoutExtension"] = data.FileName.FileNameWithoutExtension;
            row["Extension"] = "." + data.FileName.Extension;
            row["Size"] = buffer?.Length ?? data.Size;
            row["Stream"] = buffer;
            row["CreationDate"] = DateTime.UtcNow;
            row["BlobProvider"] = providerName;
            row["BlobProviderData"] = providerData;
            //[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL unique DEFAULT NEWID(),
            //[Timestamp] [timestamp] NOT NULL,
            //[Staging] bit NULL,
            //[StagingVersionId] int NULL,
            //[StagingPropertyTypeId] int NULL,
            //[IsDeleted] bit NULL,
        }

        private static byte[] GetBuffer(InitialData data, string providerData, string propertyTypeName)
        {
            return data.GetBlobBytes(providerData, propertyTypeName);
        }

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Formatting = Formatting.Indented
        };
        private static string GetSerializedDynamicData(IDictionary<PropertyType, object> data)
        {
            if (data == null)
                return null;
            using (var writer = new StringWriter())
            {
                JsonSerializer.Create(SerializerSettings).Serialize(writer, data);
                var serializedDoc = writer.GetStringBuilder().ToString();
                return serializedDoc;
            }
        }

        private static DateTime AlignDateTime(DateTime dateTime, DataProvider2 dataProvider)
        {
            if (dateTime > dataProvider.DateTimeMaxValue)
                dateTime = dataProvider.DateTimeMaxValue;
            if (dateTime < dataProvider.DateTimeMinValue)
                dateTime = dataProvider.DateTimeMinValue;
            return dateTime;
        }

        /* ==================================================================================================== Writing */

        private static async Task WriteToDatabaseAsync(DataSet dataSet, string connectionString)
        {

            await BulkInsertAsync(dataSet, TableName.Nodes, connectionString);
            await BulkInsertAsync(dataSet, TableName.Versions, connectionString);
            await BulkInsertAsync(dataSet, TableName.LongTextProperties, connectionString);
            await BulkInsertAsync(dataSet, TableName.BinaryProperties, connectionString);
            await BulkInsertAsync(dataSet, TableName.Files, connectionString);
            //await BulkInsertAsync(dataSet, TableName.Entities, connectionString);
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

                    foreach (var name in ColumnNames[tableName])
                        bulkCopy.ColumnMappings.Add(name, name);

                    await bulkCopy.WriteToServerAsync(table);
                }
                connection.Close();
            }
        }
    }
}
