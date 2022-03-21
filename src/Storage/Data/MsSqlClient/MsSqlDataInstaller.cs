﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{    
    public class MsSqlDataInstaller : IDataInstaller
    {
        private static readonly byte Yes = 1;
        private static readonly byte No = 0;

        private static class TableName
        {
            public static readonly string PropertyTypes = "PropertyTypes";
            public static readonly string NodeTypes = "NodeTypes";
            public static readonly string Nodes = "Nodes";
            public static readonly string Versions = "Versions";
            public static readonly string LongTextProperties = "LongTextProperties";
            public static readonly string ReferenceProperties = "ReferenceProperties";
            public static readonly string BinaryProperties = "BinaryProperties";
            public static readonly string Files = "Files";
        }

        private Dictionary<string, string[]> _columnNames;
        private ILogger _logger;
        private ConnectionStringOptions ConnectionStrings { get; }

        public MsSqlDataInstaller(IOptions<ConnectionStringOptions> connectionOptions,
            ILogger<MsSqlDataInstaller> logger)
        {
            _columnNames = new Dictionary<string,string[]>();            

            ConnectionStrings = connectionOptions?.Value ?? new ConnectionStringOptions();
            _logger = logger;
        }

        public async Task InstallInitialDataAsync(InitialData data, DataProvider dataProvider, CancellationToken cancel)
        {
            if (dataProvider is not MsSqlDataProvider msdp)
                throw new InvalidOperationException("MsSqlDataInstaller error: data provider is expected to be MsSqlDataProvider.");

            var dataSet = new DataSet();

            CreateTableStructure(dataSet);

            _columnNames = dataSet.Tables.Cast<DataTable>().ToDictionary(
                table => table.TableName,
                table => table.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray());

            CreateData(dataSet, data, msdp);

            await WriteToDatabaseAsync(dataSet, ConnectionStrings.Repository, cancel).ConfigureAwait(false);
        }

        /* ==================================================================================================== Tables */

        private static void CreateTableStructure(DataSet dataSet)
        {
            AddNodeTypesTable(dataSet);
            AddPropertyTypesTable(dataSet);
            //AddContentListTypesTable(dataSet);
            AddNodesTable(dataSet);
            AddVersionsTable(dataSet);
            AddLongTextPropertiesTable(dataSet);
            AddReferencePropertiesTable(dataSet);
            AddBinaryPropertiesTable(dataSet);
            AddFilesTable(dataSet);
        }

        private static void AddPropertyTypesTable(DataSet dataSet)
        {
            var table = new DataTable(TableName.PropertyTypes);
            table.Columns.AddRange(new[]
            {
                new DataColumn {ColumnName = "PropertyTypeId", DataType = typeof(int), AllowDBNull = false },
                new DataColumn {ColumnName = "Name", DataType = typeof(string), AllowDBNull = false },
                new DataColumn {ColumnName = "DataType", DataType = typeof(string), AllowDBNull = false },
                new DataColumn {ColumnName = "Mapping", DataType = typeof(int), AllowDBNull = false },
                new DataColumn {ColumnName = "IsContentListProperty", DataType = typeof(byte), AllowDBNull = false},
            });
            dataSet.Tables.Add(table);
        }
        private static void AddNodeTypesTable(DataSet dataSet)
        {
            var table = new DataTable(TableName.NodeTypes);
            table.Columns.AddRange(new[]
            {
                new DataColumn {ColumnName = "NodeTypeId", DataType = typeof(int), AllowDBNull = false },
                new DataColumn {ColumnName = "ParentId", DataType = typeof(int), AllowDBNull = true },
                new DataColumn {ColumnName = "Name", DataType = typeof(string), AllowDBNull = false },
                new DataColumn {ColumnName = "ClassName", DataType = typeof(string), AllowDBNull = false },
                new DataColumn {ColumnName = "Properties", DataType = typeof(string), AllowDBNull = false},
            });
            dataSet.Tables.Add(table);
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
        private static void AddReferencePropertiesTable(DataSet dataSet)
        {
            var refs = new DataTable(TableName.ReferenceProperties);
            refs.Columns.AddRange(new[]
            {
                new DataColumn {ColumnName = "ReferencePropertyId", DataType = typeof(int), AllowDBNull = false },
                new DataColumn {ColumnName = "VersionId", DataType = typeof(int), AllowDBNull = false },
                new DataColumn {ColumnName = "PropertyTypeId", DataType = typeof(int), AllowDBNull = false },
                new DataColumn {ColumnName = "ReferredNodeId", DataType = typeof(int), AllowDBNull = false},
            });
            dataSet.Tables.Add(refs);
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

        private static void CreateData(DataSet dataSet, InitialData data, MsSqlDataProvider dataProvider)
        {
            var now = DateTime.UtcNow;

            var propertyTypes = dataSet.Tables[TableName.PropertyTypes];
            foreach (var propertyType in data.Schema.PropertyTypes)
            {
                var row = propertyTypes.NewRow();
                SetPropertyTypeRow(row, propertyType, dataProvider);
                propertyTypes.Rows.Add(row);
            }

            var nodeTypes = dataSet.Tables[TableName.NodeTypes];
            foreach (var nodeType in data.Schema.NodeTypes)
            {
                var row = nodeTypes.NewRow();
                SetNodeTypeRow(row, nodeType, data.Schema.NodeTypes, dataProvider);
                nodeTypes.Rows.Add(row);
            }

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
            var refProps = dataSet.Tables[TableName.ReferenceProperties];
            var binaryProperties = dataSet.Tables[TableName.BinaryProperties];
            var files = dataSet.Tables[TableName.Files];
            var longTextId = 0;
            var refPropId = 0;
            foreach (var version in data.Versions)
            {
                var props = data.DynamicProperties.FirstOrDefault(x => x.VersionId == version.VersionId);
                if (props?.LongTextProperties != null)
                {
                    foreach (var longTextData in props.LongTextProperties)
                    {
                        var longTextRow = longTexts.NewRow();
                        var propertyTypeId =
                            data.Schema.PropertyTypes.FirstOrDefault(x => x.Name == longTextData.Key.Name)?.Id ?? 0;
                        SetLongTextPropertyRow(longTextRow, ++longTextId, version.VersionId, propertyTypeId, longTextData.Value);
                        longTexts.Rows.Add(longTextRow);
                    }
                }
                if (props?.ReferenceProperties != null)
                {
                    foreach (var referenceData in props.ReferenceProperties)
                    {
                        var propertyTypeId =
                            data.Schema.PropertyTypes.FirstOrDefault(x => x.Name == referenceData.Key.Name)?.Id ?? 0;
                        foreach (var value in referenceData.Value)
                        {
                            var refPropRow = refProps.NewRow();
                            SetReferencePropertyRow(refPropRow, ++refPropId, version.VersionId, propertyTypeId, value);
                            refProps.Rows.Add(refPropRow);
                        }
                    }
                }
                if (props?.BinaryProperties != null)
                {
                    foreach (var binaryPropertyData in props.BinaryProperties)
                    {
                        var binaryPropertyRow = binaryProperties.NewRow();
                        var propertyTypeId =
                            data.Schema.PropertyTypes.FirstOrDefault(x => x.Name == binaryPropertyData.Key.Name)?.Id ?? 0;
                        SetBinaryPropertyRow(binaryPropertyRow, version.VersionId, propertyTypeId, binaryPropertyData.Value);
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

        private static void SetPropertyTypeRow(DataRow row, PropertyTypeData propertyType, MsSqlDataProvider dataProvider)
        {
            row["PropertyTypeId"] = propertyType.Id;
            row["Name"] = propertyType.Name;
            row["DataType"] = propertyType.DataType.ToString();
            row["Mapping"] = propertyType.Mapping;
            row["IsContentListProperty"] = propertyType.IsContentListProperty ? Yes : No;
        }
        private static void SetNodeTypeRow(DataRow row, NodeTypeData nodeType, List<NodeTypeData> allNodeTypes, MsSqlDataProvider dataProvider)
        {
            row["NodeTypeId"] = nodeType.Id;
            row["Name"] = nodeType.Name;
            row["ParentId"] = (object)allNodeTypes.FirstOrDefault(x => x.Name == nodeType.ParentName)?.Id ?? DBNull.Value;
            row["ClassName"] = nodeType.ClassName;
            row["Properties"] = string.Join(" ", nodeType.Properties);
        }
        private static void SetNodeRow(DataRow row, NodeHeadData node, MsSqlDataProvider dataProvider)
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
        private static void SetVersionRow(DataRow row, VersionData version, IDictionary<PropertyType, object> dynamicProperties, MsSqlDataProvider dataProvider)
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
            row["DynamicProperties"] = dynamicProperties == null ? null : dataProvider.SerializeDynamicProperties(dynamicProperties);
        }
        private static void SetLongTextPropertyRow(DataRow row, int id, int versionId, int propertyTypeId, string value)
        {
            row["LongTextPropertyId"] = id;
            row["VersionId"] = versionId;
            row["PropertyTypeId"] = propertyTypeId;
            row["Length"] = value?.Length;
            row["Value"] = value;
        }
        private static void SetReferencePropertyRow(DataRow row, int id, int versionId, int propertyTypeId, int value)
        {
            row["ReferencePropertyId"] = id;
            row["VersionId"] = versionId;
            row["PropertyTypeId"] = propertyTypeId;
            row["ReferredNodeId"] = value;
        }
        private static void SetBinaryPropertyRow(DataRow row, int versionId, int propertyTypeId, BinaryDataValue data)
        {
            row["BinaryPropertyId"] = data.Id;
            row["VersionId"] = versionId;
            row["PropertyTypeId"] = propertyTypeId;
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

            row["FileId"] = data.FileId;
            row["ContentType"] = data.ContentType;
            row["FileNameWithoutExtension"] = data.FileName.FileNameWithoutExtension;
            row["Extension"] = data.FileName.Extension;
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

        private static DateTime AlignDateTime(DateTime dateTime, MsSqlDataProvider dataProvider)
        {
            if (dateTime > dataProvider.DateTimeMaxValue)
                dateTime = dataProvider.DateTimeMaxValue;
            if (dateTime < dataProvider.DateTimeMinValue)
                dateTime = dataProvider.DateTimeMinValue;
            return dateTime;
        }

        /* ==================================================================================================== Writing */

        private async Task WriteToDatabaseAsync(DataSet dataSet, string connectionString, CancellationToken cancellationToken)
        {
            await BulkInsertAsync(dataSet, TableName.PropertyTypes, connectionString, cancellationToken).ConfigureAwait(false);
            await BulkInsertAsync(dataSet, TableName.NodeTypes, connectionString, cancellationToken).ConfigureAwait(false);
            await BulkInsertAsync(dataSet, TableName.Nodes, connectionString, cancellationToken).ConfigureAwait(false);
            await BulkInsertAsync(dataSet, TableName.Versions, connectionString, cancellationToken).ConfigureAwait(false);
            await BulkInsertAsync(dataSet, TableName.LongTextProperties, connectionString, cancellationToken).ConfigureAwait(false);
            await BulkInsertAsync(dataSet, TableName.ReferenceProperties, connectionString, cancellationToken).ConfigureAwait(false);
            await BulkInsertAsync(dataSet, TableName.BinaryProperties, connectionString, cancellationToken).ConfigureAwait(false);
            await BulkInsertAsync(dataSet, TableName.Files, connectionString, cancellationToken).ConfigureAwait(false);
            //await BulkInsertAsync(dataSet, TableName.Entities, connectionString, cancellationToken).ConfigureAwait(false);
        }
        private async Task BulkInsertAsync(DataSet dataSet, string tableName, string connectionString,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace($"BulkInsert: deleting from table {tableName}");

            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "DELETE " + tableName;
                connection.Open();
                cancellationToken.ThrowIfCancellationRequested();
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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

                    _logger.LogTrace($"BulkInsert: instering {table.Rows.Count} records into table {tableName}.");

                    foreach (var name in _columnNames[tableName])
                        bulkCopy.ColumnMappings.Add(name, name);

                    cancellationToken.ThrowIfCancellationRequested();
                    await bulkCopy.WriteToServerAsync(table, cancellationToken).ConfigureAwait(false);
                }
                connection.Close();
            }
        }        
    }
}
