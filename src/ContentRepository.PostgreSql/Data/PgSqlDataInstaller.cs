using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using STT=System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.PgSqlClient
{    
    public class PgSqlDataInstaller : IDataInstaller
    {
        private static readonly byte Yes = 1;
        private static readonly byte No = 0;

        private ILogger _logger;
        private ConnectionStringOptions ConnectionStrings { get; }

        public PgSqlDataInstaller(IOptions<ConnectionStringOptions> connectionOptions,
            ILogger<PgSqlDataInstaller> logger)
        {
            ConnectionStrings = connectionOptions?.Value ?? new ConnectionStringOptions();
            _logger = logger;
        }

        public async STT.Task InstallInitialDataAsync(InitialData data, DataProvider dataProvider, CancellationToken cancel)
        {
            if (dataProvider is not PgSqlDataProvider pgdp)
                throw new InvalidOperationException("PgSqlDataInstaller error: data provider is expected to be PgSqlDataProvider.");

            var connectionString = ConnectionStrings.Repository;

            // Install schema types first
            await BulkInsertPropertyTypesAsync(data.Schema.PropertyTypes, connectionString, cancel, pgdp).ConfigureAwait(false);
            await BulkInsertNodeTypesAsync(data.Schema.NodeTypes, connectionString, cancel, pgdp).ConfigureAwait(false);

            // Install nodes
            var now = DateTime.UtcNow;
            foreach (var node in data.Nodes)
            {
                node.CreationDate = now;
                node.ModificationDate = now;
            }
            await BulkInsertNodesAsync(data.Nodes.ToList(), connectionString, cancel, pgdp).ConfigureAwait(false);

            // Install versions and related data
            var longTextId = 0;
            var refPropId = 0;
            var allLongTexts = new List<(int Id, int VersionId, int PropertyTypeId, string Value)>();
            var allRefProps = new List<(int Id, int VersionId, int PropertyTypeId, int ReferredNodeId)>();
            var allBinaryProps = new List<(int BinaryPropertyId, int VersionId, int PropertyTypeId, int FileId)>();
            var allFiles = new List<(int FileId, string ContentType, string FileNameWithoutExtension, string Extension, long Size, byte[] Stream, string BlobProvider, string BlobProviderData)>();
            var allVersions = new List<(VersionData Version, IDictionary<PropertyType, object> DynamicProperties)>();

            foreach (var version in data.Versions)
            {
                version.CreationDate = now;
                version.ModificationDate = now;

                var props = data.DynamicProperties.FirstOrDefault(x => x.VersionId == version.VersionId);
                allVersions.Add((version, props?.DynamicProperties));

                if (props?.LongTextProperties != null)
                {
                    foreach (var longTextData in props.LongTextProperties)
                    {
                        var propertyTypeId = data.Schema.PropertyTypes.FirstOrDefault(x => x.Name == longTextData.Key.Name)?.Id ?? 0;
                        allLongTexts.Add((++longTextId, version.VersionId, propertyTypeId, longTextData.Value));
                    }
                }
                if (props?.ReferenceProperties != null)
                {
                    foreach (var referenceData in props.ReferenceProperties)
                    {
                        var propertyTypeId = data.Schema.PropertyTypes.FirstOrDefault(x => x.Name == referenceData.Key.Name)?.Id ?? 0;
                        foreach (var value in referenceData.Value)
                        {
                            allRefProps.Add((++refPropId, version.VersionId, propertyTypeId, value));
                        }
                    }
                }
                if (props?.BinaryProperties != null)
                {
                    foreach (var binaryPropertyData in props.BinaryProperties)
                    {
                        var propertyTypeId = data.Schema.PropertyTypes.FirstOrDefault(x => x.Name == binaryPropertyData.Key.Name)?.Id ?? 0;
                        allBinaryProps.Add((binaryPropertyData.Value.Id, version.VersionId, propertyTypeId, binaryPropertyData.Value.FileId));

                        byte[] buffer = null;
                        var providerName = binaryPropertyData.Value.BlobProviderName;
                        var providerData = binaryPropertyData.Value.BlobProviderData;
                        if (providerName == null && providerData != null &&
                            providerData.StartsWith("/Root", StringComparison.OrdinalIgnoreCase))
                        {
                            buffer = data.GetBlobBytes(providerData, binaryPropertyData.Key.Name);
                            providerData = null;
                        }

                        allFiles.Add((
                            binaryPropertyData.Value.FileId,
                            binaryPropertyData.Value.ContentType,
                            binaryPropertyData.Value.FileName.FileNameWithoutExtension,
                            binaryPropertyData.Value.FileName.Extension,
                            buffer?.Length ?? binaryPropertyData.Value.Size,
                            buffer,
                            providerName,
                            providerData));
                    }
                }
            }

            await BulkInsertVersionsAsync(allVersions, connectionString, cancel, pgdp).ConfigureAwait(false);
            await BulkInsertLongTextPropertiesAsync(allLongTexts, connectionString, cancel).ConfigureAwait(false);
            await BulkInsertReferencePropertiesAsync(allRefProps, connectionString, cancel).ConfigureAwait(false);
            await BulkInsertFilesAsync(allFiles, connectionString, cancel).ConfigureAwait(false);
            await BulkInsertBinaryPropertiesAsync(allBinaryProps, connectionString, cancel).ConfigureAwait(false);

            // Re-enable foreign key triggers
            await ReEnableTriggersAsync(connectionString, cancel).ConfigureAwait(false);

            // Resync all SERIAL sequences to max(id) after bulk inserts with explicit IDs
            await ResyncSequencesAsync(connectionString, cancel).ConfigureAwait(false);
        }

        /// <summary>
        /// Re-enables triggers that were disabled before data loading.
        /// </summary>
        private async STT.Task ReEnableTriggersAsync(string connectionString, CancellationToken cancel)
        {
            _logger.LogTrace("Re-enabling triggers after bulk insert.");
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancel).ConfigureAwait(false);

            var sql = @"
ALTER TABLE ""BinaryProperties"" ENABLE TRIGGER ALL;
ALTER TABLE ""Nodes"" ENABLE TRIGGER ALL;
ALTER TABLE ""ReferenceProperties"" ENABLE TRIGGER ALL;
ALTER TABLE ""LongTextProperties"" ENABLE TRIGGER ALL;
ALTER TABLE ""Versions"" ENABLE TRIGGER ALL;
";
            await using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);
        }

        /// <summary>
        /// After bulk inserts with explicit ID values, PostgreSQL SERIAL sequences are out of sync.
        /// This resets each sequence to MAX(id)+1 so subsequent inserts get correct IDs.
        /// </summary>
        private async STT.Task ResyncSequencesAsync(string connectionString, CancellationToken cancel)
        {
            _logger.LogTrace("Resyncing SERIAL sequences after bulk insert.");
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancel).ConfigureAwait(false);

            // Each entry: (table, id_column)
            var tables = new[]
            {
                ("NodeTypes", "NodeTypeId"),
                ("PropertyTypes", "PropertyTypeId"),
                ("Nodes", "NodeId"),
                ("Versions", "VersionId"),
                ("Files", "FileId"),
                ("BinaryProperties", "BinaryPropertyId"),
                ("ReferenceProperties", "ReferencePropertyId"),
                ("LongTextProperties", "LongTextPropertyId"),
            };

            foreach (var (table, idCol) in tables)
            {
                // pg_get_serial_sequence returns the sequence name for a SERIAL column
                var sql = $@"SELECT setval(pg_get_serial_sequence('""{table}""', '{idCol}'),
                             COALESCE((SELECT MAX(""{idCol}"") FROM ""{table}""), 0) + 1, false);";
                await using var cmd = new NpgsqlCommand(sql, connection);
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);
            }
        }

        /* ==================================================================================================== Bulk insert methods */

        private async STT.Task BulkInsertPropertyTypesAsync(List<PropertyTypeData> propertyTypes, string connectionString,
            CancellationToken cancel, PgSqlDataProvider dataProvider)
        {
            _logger.LogTrace("BulkInsert: deleting from table PropertyTypes");
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancel).ConfigureAwait(false);

            await using (var cmd = new NpgsqlCommand(@"DELETE FROM ""PropertyTypes""", connection))
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);

            foreach (var pt in propertyTypes)
            {
                await using var cmd = new NpgsqlCommand(
                    @"INSERT INTO ""PropertyTypes"" (""PropertyTypeId"", ""Name"", ""DataType"", ""Mapping"", ""IsContentListProperty"")
                      VALUES (@Id, @Name, @DataType, @Mapping, @IsContentListProperty)", connection);
                cmd.Parameters.AddWithValue("@Id", pt.Id);
                cmd.Parameters.AddWithValue("@Name", pt.Name);
                cmd.Parameters.AddWithValue("@DataType", pt.DataType.ToString());
                cmd.Parameters.AddWithValue("@Mapping", pt.Mapping);
                cmd.Parameters.AddWithValue("@IsContentListProperty", pt.IsContentListProperty ? Yes : No);
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);
            }

            _logger.LogTrace($"BulkInsert: inserted {propertyTypes.Count} records into table PropertyTypes.");
        }

        private async STT.Task BulkInsertNodeTypesAsync(List<NodeTypeData> nodeTypes, string connectionString,
            CancellationToken cancel, PgSqlDataProvider dataProvider)
        {
            _logger.LogTrace("BulkInsert: deleting from table NodeTypes");
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancel).ConfigureAwait(false);

            await using (var cmd = new NpgsqlCommand(@"DELETE FROM ""NodeTypes""", connection))
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);

            foreach (var nt in nodeTypes)
            {
                await using var cmd = new NpgsqlCommand(
                    @"INSERT INTO ""NodeTypes"" (""NodeTypeId"", ""ParentId"", ""Name"", ""ClassName"", ""Properties"")
                      VALUES (@Id, @ParentId, @Name, @ClassName, @Properties)", connection);
                cmd.Parameters.AddWithValue("@Id", nt.Id);
                var parentId = nodeTypes.FirstOrDefault(x => x.Name == nt.ParentName)?.Id;
                cmd.Parameters.AddWithValue("@ParentId", (object)parentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Name", nt.Name);
                cmd.Parameters.AddWithValue("@ClassName", nt.ClassName);
                cmd.Parameters.AddWithValue("@Properties", string.Join(" ", nt.Properties));
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);
            }

            _logger.LogTrace($"BulkInsert: inserted {nodeTypes.Count} records into table NodeTypes.");
        }

        private async STT.Task BulkInsertNodesAsync(List<NodeHeadData> nodes, string connectionString,
            CancellationToken cancel, PgSqlDataProvider dataProvider)
        {
            _logger.LogTrace("BulkInsert: deleting from table Nodes");
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancel).ConfigureAwait(false);

            await using (var cmd = new NpgsqlCommand(@"DELETE FROM ""Nodes""", connection))
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);

            foreach (var node in nodes)
            {
                await using var cmd = new NpgsqlCommand(
                    @"INSERT INTO ""Nodes"" (""NodeId"", ""NodeTypeId"", ""CreatingInProgress"", ""IsDeleted"", ""IsInherited"",
                      ""ParentNodeId"", ""Name"", ""Path"", ""Index"", ""Locked"", ""ETag"", ""LockType"", ""LockTimeout"",
                      ""LockDate"", ""LockToken"", ""LastLockUpdate"", ""LastMinorVersionId"", ""LastMajorVersionId"",
                      ""CreationDate"", ""CreatedById"", ""ModificationDate"", ""ModifiedById"", ""IsSystem"", ""OwnerId"", ""SavingState"")
                      VALUES (@NodeId, @NodeTypeId, @CreatingInProgress, @IsDeleted, @IsInherited,
                      @ParentNodeId, @Name, @Path, @Index, @Locked, @ETag, @LockType, @LockTimeout,
                      @LockDate, @LockToken, @LastLockUpdate, @LastMinorVersionId, @LastMajorVersionId,
                      @CreationDate, @CreatedById, @ModificationDate, @ModifiedById, @IsSystem, @OwnerId, @SavingState)", connection);
                cmd.Parameters.AddWithValue("@NodeId", node.NodeId);
                cmd.Parameters.AddWithValue("@NodeTypeId", node.NodeTypeId);
                cmd.Parameters.AddWithValue("@CreatingInProgress", node.CreatingInProgress ? Yes : No);
                cmd.Parameters.AddWithValue("@IsDeleted", node.IsDeleted ? Yes : No);
                cmd.Parameters.AddWithValue("@IsInherited", (byte)0);
                cmd.Parameters.AddWithValue("@ParentNodeId", node.ParentNodeId);
                cmd.Parameters.AddWithValue("@Name", node.Name);
                cmd.Parameters.AddWithValue("@Path", node.Path);
                cmd.Parameters.AddWithValue("@Index", node.Index);
                cmd.Parameters.AddWithValue("@Locked", node.Locked ? Yes : No);
                cmd.Parameters.AddWithValue("@ETag", (object)(node.ETag ?? string.Empty));
                cmd.Parameters.AddWithValue("@LockType", node.LockType);
                cmd.Parameters.AddWithValue("@LockTimeout", node.LockTimeout);
                cmd.Parameters.AddWithValue("@LockDate", AlignDateTime(node.LockDate, dataProvider));
                cmd.Parameters.AddWithValue("@LockToken", (object)(node.LockToken ?? string.Empty));
                cmd.Parameters.AddWithValue("@LastLockUpdate", AlignDateTime(node.LastLockUpdate, dataProvider));
                cmd.Parameters.AddWithValue("@LastMinorVersionId", node.LastMinorVersionId);
                cmd.Parameters.AddWithValue("@LastMajorVersionId", node.LastMajorVersionId);
                cmd.Parameters.AddWithValue("@CreationDate", AlignDateTime(node.CreationDate, dataProvider));
                cmd.Parameters.AddWithValue("@CreatedById", node.CreatedById);
                cmd.Parameters.AddWithValue("@ModificationDate", AlignDateTime(node.ModificationDate, dataProvider));
                cmd.Parameters.AddWithValue("@ModifiedById", node.ModifiedById);
                cmd.Parameters.AddWithValue("@IsSystem", node.IsSystem ? Yes : No);
                cmd.Parameters.AddWithValue("@OwnerId", node.OwnerId);
                cmd.Parameters.AddWithValue("@SavingState", (int)node.SavingState);
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);
            }

            _logger.LogTrace($"BulkInsert: inserted {nodes.Count} records into table Nodes.");
        }

        private async STT.Task BulkInsertVersionsAsync(
            List<(VersionData Version, IDictionary<PropertyType, object> DynamicProperties)> versions,
            string connectionString, CancellationToken cancel, PgSqlDataProvider dataProvider)
        {
            _logger.LogTrace("BulkInsert: deleting from table Versions");
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancel).ConfigureAwait(false);

            await using (var cmd = new NpgsqlCommand(@"DELETE FROM ""Versions""", connection))
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);

            foreach (var (version, dynamicProperties) in versions)
            {
                await using var cmd = new NpgsqlCommand(
                    @"INSERT INTO ""Versions"" (""VersionId"", ""NodeId"", ""MajorNumber"", ""MinorNumber"", ""Status"",
                      ""CreationDate"", ""CreatedById"", ""ModificationDate"", ""ModifiedById"",
                      ""IndexDocument"", ""ChangedData"", ""DynamicProperties"")
                      VALUES (@VersionId, @NodeId, @MajorNumber, @MinorNumber, @Status,
                      @CreationDate, @CreatedById, @ModificationDate, @ModifiedById,
                      @IndexDocument, @ChangedData, @DynamicProperties)", connection);
                cmd.Parameters.AddWithValue("@VersionId", version.VersionId);
                cmd.Parameters.AddWithValue("@NodeId", version.NodeId);
                cmd.Parameters.AddWithValue("@MajorNumber", (short)version.Version.Major);
                cmd.Parameters.AddWithValue("@MinorNumber", (short)version.Version.Minor);
                cmd.Parameters.AddWithValue("@Status", (short)version.Version.Status);
                cmd.Parameters.AddWithValue("@CreationDate", AlignDateTime(version.CreationDate, dataProvider));
                cmd.Parameters.AddWithValue("@CreatedById", version.CreatedById);
                cmd.Parameters.AddWithValue("@ModificationDate", AlignDateTime(version.ModificationDate, dataProvider));
                cmd.Parameters.AddWithValue("@ModifiedById", version.ModifiedById);
                cmd.Parameters.AddWithValue("@IndexDocument", DBNull.Value);
                cmd.Parameters.AddWithValue("@ChangedData", DBNull.Value);
                var dynPropValue = dynamicProperties == null ? null : dataProvider.SerializeDynamicProperties(dynamicProperties);
                cmd.Parameters.AddWithValue("@DynamicProperties", (object)dynPropValue ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);
            }

            _logger.LogTrace($"BulkInsert: inserted {versions.Count} records into table Versions.");
        }

        private async STT.Task BulkInsertLongTextPropertiesAsync(
            List<(int Id, int VersionId, int PropertyTypeId, string Value)> longTexts,
            string connectionString, CancellationToken cancel)
        {
            _logger.LogTrace("BulkInsert: deleting from table LongTextProperties");
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancel).ConfigureAwait(false);

            await using (var cmd = new NpgsqlCommand(@"DELETE FROM ""LongTextProperties""", connection))
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);

            foreach (var lt in longTexts)
            {
                await using var cmd = new NpgsqlCommand(
                    @"INSERT INTO ""LongTextProperties"" (""LongTextPropertyId"", ""VersionId"", ""PropertyTypeId"", ""Length"", ""Value"")
                      VALUES (@Id, @VersionId, @PropertyTypeId, @Length, @Value)", connection);
                cmd.Parameters.AddWithValue("@Id", lt.Id);
                cmd.Parameters.AddWithValue("@VersionId", lt.VersionId);
                cmd.Parameters.AddWithValue("@PropertyTypeId", lt.PropertyTypeId);
                cmd.Parameters.AddWithValue("@Length", (object)lt.Value?.Length ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Value", (object)lt.Value ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);
            }

            _logger.LogTrace($"BulkInsert: inserted {longTexts.Count} records into table LongTextProperties.");
        }

        private async STT.Task BulkInsertReferencePropertiesAsync(
            List<(int Id, int VersionId, int PropertyTypeId, int ReferredNodeId)> refProps,
            string connectionString, CancellationToken cancel)
        {
            _logger.LogTrace("BulkInsert: deleting from table ReferenceProperties");
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancel).ConfigureAwait(false);

            await using (var cmd = new NpgsqlCommand(@"DELETE FROM ""ReferenceProperties""", connection))
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);

            foreach (var rp in refProps)
            {
                await using var cmd = new NpgsqlCommand(
                    @"INSERT INTO ""ReferenceProperties"" (""ReferencePropertyId"", ""VersionId"", ""PropertyTypeId"", ""ReferredNodeId"")
                      VALUES (@Id, @VersionId, @PropertyTypeId, @ReferredNodeId)", connection);
                cmd.Parameters.AddWithValue("@Id", rp.Id);
                cmd.Parameters.AddWithValue("@VersionId", rp.VersionId);
                cmd.Parameters.AddWithValue("@PropertyTypeId", rp.PropertyTypeId);
                cmd.Parameters.AddWithValue("@ReferredNodeId", rp.ReferredNodeId);
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);
            }

            _logger.LogTrace($"BulkInsert: inserted {refProps.Count} records into table ReferenceProperties.");
        }

        private async STT.Task BulkInsertFilesAsync(
            List<(int FileId, string ContentType, string FileNameWithoutExtension, string Extension, long Size, byte[] Stream, string BlobProvider, string BlobProviderData)> files,
            string connectionString, CancellationToken cancel)
        {
            _logger.LogTrace("BulkInsert: deleting from table Files");
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancel).ConfigureAwait(false);

            await using (var cmd = new NpgsqlCommand(@"DELETE FROM ""Files""", connection))
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);

            foreach (var file in files)
            {
                await using var cmd = new NpgsqlCommand(
                    @"INSERT INTO ""Files"" (""FileId"", ""ContentType"", ""FileNameWithoutExtension"", ""Extension"",
                      ""Size"", ""Stream"", ""CreationDate"", ""BlobProvider"", ""BlobProviderData"")
                      VALUES (@FileId, @ContentType, @FileNameWithoutExtension, @Extension,
                      @Size, @Stream, @CreationDate, @BlobProvider, @BlobProviderData)", connection);
                cmd.Parameters.AddWithValue("@FileId", file.FileId);
                cmd.Parameters.AddWithValue("@ContentType", file.ContentType);
                cmd.Parameters.AddWithValue("@FileNameWithoutExtension", (object)file.FileNameWithoutExtension ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Extension", file.Extension);
                cmd.Parameters.AddWithValue("@Size", file.Size);
                cmd.Parameters.Add(new NpgsqlParameter("@Stream", NpgsqlDbType.Bytea) { Value = (object)file.Stream ?? DBNull.Value });
                cmd.Parameters.AddWithValue("@CreationDate", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@BlobProvider", (object)file.BlobProvider ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BlobProviderData", (object)file.BlobProviderData ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);
            }

            _logger.LogTrace($"BulkInsert: inserted {files.Count} records into table Files.");
        }

        private async STT.Task BulkInsertBinaryPropertiesAsync(
            List<(int BinaryPropertyId, int VersionId, int PropertyTypeId, int FileId)> binaryProps,
            string connectionString, CancellationToken cancel)
        {
            _logger.LogTrace("BulkInsert: deleting from table BinaryProperties");
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancel).ConfigureAwait(false);

            await using (var cmd = new NpgsqlCommand(@"DELETE FROM ""BinaryProperties""", connection))
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);

            foreach (var bp in binaryProps)
            {
                await using var cmd = new NpgsqlCommand(
                    @"INSERT INTO ""BinaryProperties"" (""BinaryPropertyId"", ""VersionId"", ""PropertyTypeId"", ""FileId"")
                      VALUES (@BinaryPropertyId, @VersionId, @PropertyTypeId, @FileId)", connection);
                cmd.Parameters.AddWithValue("@BinaryPropertyId", bp.BinaryPropertyId);
                cmd.Parameters.AddWithValue("@VersionId", bp.VersionId);
                cmd.Parameters.AddWithValue("@PropertyTypeId", bp.PropertyTypeId);
                cmd.Parameters.AddWithValue("@FileId", bp.FileId);
                await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);
            }

            _logger.LogTrace($"BulkInsert: inserted {binaryProps.Count} records into table BinaryProperties.");
        }

        /* ==================================================================================================== Tools */

        private static DateTime AlignDateTime(DateTime dateTime, PgSqlDataProvider dataProvider)
        {
            if (dateTime > dataProvider.DateTimeMaxValue)
                dateTime = dataProvider.DateTimeMaxValue;
            if (dateTime < dataProvider.DateTimeMinValue)
                dateTime = dataProvider.DateTimeMinValue;
            return dateTime;
        }
    }
}
