using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Storage.Data.MsSqlClient;
using SenseNet.Tests.Core.Implementations;

namespace SenseNet.IntegrationTests.Infrastructure
{
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public class MsSqlTestingDataProvider : ITestingDataProvider
    {
        private readonly RelationalDataProviderBase _mainProvider;
        ConnectionStringOptions _connectionStrings;
        public MsSqlTestingDataProvider(DataProvider mainProvider, IOptions<ConnectionStringOptions> connectionStrings)
        {
            if (mainProvider == null)
                return;
            if (!(mainProvider is RelationalDataProviderBase relationalDataProviderBase))
                throw new ArgumentException("The mainProvider need to be RelationalDataProviderBase.");
            _mainProvider = relationalDataProviderBase;
            _connectionStrings = connectionStrings.Value;
        }

        public void InitializeForTests()
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
            {
                ctx.ExecuteNonQueryAsync(@"
ALTER TABLE [BinaryProperties] CHECK CONSTRAINT ALL
ALTER TABLE [FlatProperties] CHECK CONSTRAINT ALL
ALTER TABLE [Nodes] CHECK CONSTRAINT ALL
ALTER TABLE [ReferenceProperties] CHECK CONSTRAINT ALL
ALTER TABLE [TextPropertiesNText] CHECK CONSTRAINT ALL
ALTER TABLE [TextPropertiesNVarchar] CHECK CONSTRAINT ALL
ALTER TABLE [Versions] CHECK CONSTRAINT ALL
").GetAwaiter().GetResult();
            }
        }

        public string GetSecurityControlStringForTests()
        {
            var sql = "SELECT NodeId, ParentNodeId, [OwnerId] FROM Nodes ORDER BY NodeId";
            var securityEntitiesArray = new List<object>();
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
            {
                ctx.ExecuteReaderAsync(sql, (reader, cancel) =>
                {
                    var count = 0;
                    while (reader.Read())
                    {
                        securityEntitiesArray.Add(new
                        {
                            NodeId = reader.GetSafeInt32(0),
                            ParentId = reader.GetSafeInt32(1),
                            OwnerId = reader.GetSafeInt32(2),
                        });

                        count++;
                        // it is neccessary to examine the number of Nodes, because loading a too big security structure may require too much resource
                        if (count > 200000)
                            throw new ApplicationException("Too many Nodes");
                    }
                    return Task.FromResult(0);
                }).GetAwaiter().GetResult();
            }
            return JsonConvert.SerializeObject(securityEntitiesArray);
        }

        public int GetPermissionLogEntriesCountAfterMoment(DateTime moment)
        {
            var sql = $"SELECT COUNT(1) FROM LogEntries WHERE Title = 'PermissionChanged' AND" +
                        $" LogDate>='{moment:yyyy-MM-dd HH:mm:ss}'";

            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
            {
                return ctx.ExecuteReaderAsync(sql, (reader, cancel) =>
                {
                    var count = 0;
                    if (reader.ReadAsync(cancel).GetAwaiter().GetResult())
                        count = reader.GetSafeInt32(0);
                    return Task.FromResult(count);
                }).GetAwaiter().GetResult();
            }
        }

        public AuditLogEntry[] LoadLastAuditLogEntries(int count)
        {
            throw new NotImplementedException();
        }

        public void CheckScript(string commandText)
        {
            // c:\Program Files\Microsoft SQL Server\90\SDK\Assemblies\Microsoft.SqlServer.Smo.dll
            // c:\Program Files\Microsoft SQL Server\90\SDK\Assemblies\Microsoft.SqlServer.ConnectionInfo.dll

            // The code maybe equivalent to this script:
            // SET NOEXEC ON
            // GO
            // SELECT * FROM Nodes
            // GO
            // SET NOEXEC OFF
            // GO

            throw new NotImplementedException();
        }

        public async Task<int> GetLastNodeIdAsync()
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
                return (int)await ctx.ExecuteScalarAsync(
                    "SELECT i.last_value FROM sys.identity_columns i JOIN sys.tables t ON i.object_id = t.object_id WHERE t.name = 'Nodes'");
        }

        public void SetContentHandler(string contentTypeName, string handler)
        {
            throw new NotImplementedException();
        }

        public void AddField(string contentTypeName, string fieldName, string fieldType = null, string fieldHandler = null)
        {
            throw new NotImplementedException();
        }

        public async Task<int[]> GetChildNodeIdsByParentNodeIdAsync(int parentNodeId)
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
            {
                return await ctx.ExecuteReaderAsync(
                    "SELECT * FROM Nodes WHERE ParentNodeId = @ParentNodeId",
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@ParentNodeId", DbType.Int32, parentNodeId)
                        });
                    },
                    async (reader, cancel) =>
                    {
                        var result = new List<int>();
                        while (await reader.ReadAsync(cancel))
                            result.Add(reader.GetSafeInt32(0));
                        return result.Count == 0 ? null : result.ToArray();
                    });
            }
        }

        public async Task<NodeHeadData> GetNodeHeadDataAsync(int nodeId)
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
            {
                return await ctx.ExecuteReaderAsync("SELECT * FROM Nodes WHERE NodeId = @NodeId", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@NodeId", DbType.Int32, nodeId));
                }, async (reader, cancel) =>
                {
                    if (!await reader.ReadAsync(cancel))
                        return null;

                    return new NodeHeadData
                    {
                        NodeId = DataReaderExtension.GetInt32(reader, "NodeId"),
                        NodeTypeId = DataReaderExtension.GetInt32(reader, "NodeTypeId"),
                        ContentListTypeId = reader.GetSafeInt32("ContentListTypeId"),
                        ContentListId = reader.GetSafeInt32("ContentListId"),
                        CreatingInProgress = reader.GetSafeBooleanFromByte("CreatingInProgress"),
                        IsDeleted = reader.GetSafeBooleanFromByte("IsDeleted"),
                        ParentNodeId = reader.GetSafeInt32("ParentNodeId"),
                        Name = DataReaderExtension.GetString(reader, "Name"),
                        DisplayName = reader.GetSafeString("DisplayName"),
                        Path = DataReaderExtension.GetString(reader, "Path"),
                        Index = reader.GetSafeInt32("Index"),
                        Locked = reader.GetSafeBooleanFromByte("Locked"),
                        LockedById = reader.GetSafeInt32("LockedById"),
                        ETag = DataReaderExtension.GetString(reader, "ETag"),
                        LockType = reader.GetSafeInt32("LockType"),
                        LockTimeout = reader.GetSafeInt32("LockTimeout"),
                        LockDate = reader.GetDateTimeUtc("LockDate"),
                        LockToken = DataReaderExtension.GetString(reader, "LockToken"),
                        LastLockUpdate = reader.GetDateTimeUtc("LastLockUpdate"),
                        LastMinorVersionId = reader.GetSafeInt32("LastMinorVersionId"),
                        LastMajorVersionId = reader.GetSafeInt32("LastMajorVersionId"),
                        CreationDate = reader.GetDateTimeUtc("CreationDate"),
                        CreatedById = reader.GetSafeInt32("CreatedById"),
                        ModificationDate = reader.GetDateTimeUtc("ModificationDate"),
                        ModifiedById = reader.GetSafeInt32("ModifiedById"),
                        IsSystem = reader.GetSafeBooleanFromByte("IsSystem"),
                        OwnerId = reader.GetSafeInt32("OwnerId"),
                        SavingState = reader.GetSavingState("SavingState"),
                        Timestamp = reader.GetSafeLongFromBytes("Timestamp"),
                    };
                });
            }
        }

        public async Task<VersionData> GetVersionDataAsync(int versionId)
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
            {
                return await ctx.ExecuteReaderAsync("SELECT * FROM Versions WHERE VersionId = @VersionId", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@VersionId", DbType.Int32, versionId));
                }, async (reader, cancel) =>
                {
                    if (!await reader.ReadAsync(cancel))
                        return null;

                    return new VersionData
                    {
                        VersionId = DataReaderExtension.GetInt32(reader, "VersionId"),
                        NodeId = DataReaderExtension.GetInt32(reader, "NodeId"),
                        Version = new VersionNumber(DataReaderExtension.GetInt16(reader, "MajorNumber"), DataReaderExtension.GetInt16(reader, "MinorNumber"),
                            (VersionStatus)DataReaderExtension.GetInt16(reader, "Status")),
                        CreationDate = reader.GetDateTimeUtc("CreationDate"),
                        CreatedById = reader.GetSafeInt32("CreatedById"),
                        ModificationDate = reader.GetDateTimeUtc("ModificationDate"),
                        ModifiedById = reader.GetSafeInt32("ModifiedById"),
                        ChangedData = reader.GetChangedData("ChangedData"),
                        Timestamp = reader.GetSafeLongFromBytes("Timestamp"),
                    };
                });
            }
        }

        public async Task<int> GetBinaryPropertyCountAsync(string path)
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
                return (int)await ctx.ExecuteScalarAsync("SELECT COUNT (1) FROM BinaryProperties NOLOCK", cmd => { });
        }

        public async Task<int> GetFileCountAsync(string path)
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
                return (int)await ctx.ExecuteScalarAsync("SELECT COUNT (1) FROM Files NOLOCK", cmd => { });
        }

        public async Task<int> GetLongTextCountAsync(string path)
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
                return (int)await ctx.ExecuteScalarAsync("SELECT COUNT (1) FROM LongTextProperties NOLOCK", cmd => { });
        }

        public async Task<long> GetAllFileSize()
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
                return (long)await ctx.ExecuteScalarAsync("SELECT SUM(Size) FROM Files");
        }
        public async Task<long> GetAllFileSizeInSubtree(string path)
        {
            var sql = @$"SELECT SUM(Size) FROM Files f
    JOIN BinaryProperties b ON b.FileId = f.FileId
    JOIN Versions v ON v.VersionId = b.VersionId
    JOIN Nodes n on n.NodeId = v.NodeId
WHERE Path LIKE '{path}%'";

            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
                return (long)await ctx.ExecuteScalarAsync(sql);
        }
        public async Task<long> GetFileSize(string path)
        {
            var sql = $@"SELECT SUM(Size) FROM Files f
    JOIN BinaryProperties b ON b.FileId = f.FileId
    JOIN Versions v ON v.VersionId = b.VersionId
    JOIN Nodes n on n.NodeId = v.NodeId
WHERE Path = '{path}'";

            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
                return (long)await ctx.ExecuteScalarAsync(sql);

        }

        public async Task<object> GetPropertyValueAsync(int versionId, string name)
        {
            var propertyType = Providers.Instance.StorageSchema.PropertyTypes[name];
            if (propertyType == null)
                throw new ArgumentException("Unknown property");

            switch (propertyType.DataType)
            {
                case DataType.Binary:
                    return await GetBinaryPropertyValueAsync(versionId, propertyType);
                case DataType.Reference:
                    return await GetReferencePropertyValueAsync(versionId, propertyType);
                case DataType.Text:
                    return await GetLongTextValueAsync(versionId, propertyType);
                default:
                    return await GetDynamicPropertyValueAsync(versionId, propertyType);
            }
        }
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private Task<object> GetBinaryPropertyValueAsync(int versionId, PropertyType propertyType)
        {
            throw new NotImplementedException();
        }
        private async Task<object> GetReferencePropertyValueAsync(int versionId, PropertyType propertyType)
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
            {
                return await ctx.ExecuteReaderAsync(
                    "SELECT ReferredNodeId FROM ReferenceProperties " +
                    "WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId",
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@VersionId", DbType.Int32, versionId),
                            ctx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyType.Id),
                        });
                    },
                    async (reader, cancel) =>
                    {
                        var result = new List<int>();
                        while (await reader.ReadAsync(cancel))
                            result.Add(reader.GetSafeInt32(0));
                        return result.Count == 0 ? null : result.ToArray();
                    });
            }
        }
        private async Task<object> GetLongTextValueAsync(int versionId, PropertyType propertyType)
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
            {
                return (string)await ctx.ExecuteScalarAsync(
                    "SELECT TOP 1 Value FROM LongTextProperties " +
                    "WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId",
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@VersionId", DbType.Int32, versionId),
                            ctx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyType.Id),
                        });
                    });
            }
        }
        private async Task<object> GetDynamicPropertyValueAsync(int versionId, PropertyType propertyType)
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
            {
                var result = (string)await ctx.ExecuteScalarAsync(
                    "SELECT TOP 1 DynamicProperties FROM Versions WHERE VersionId = @VersionId",
                    cmd =>
                    {
                        cmd.Parameters.Add(ctx.CreateParameter("@VersionId", DbType.Int32, versionId));
                    });

                var properties = _mainProvider.DeserializeDynamicProperties(result);
                if (properties.TryGetValue(propertyType, out var value))
                    return value;
                return null;
            }
        }


        public async Task UpdateDynamicPropertyAsync(int versionId, string name, object value)
        {
            var pt = Providers.Instance.StorageSchema.PropertyTypes[name];
            switch (pt.DataType)
            {
                case DataType.Text:
                    var stringValue = (string)value;
                    using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
                    {
                        await ctx.ExecuteNonQueryAsync(
                            "UPDATE LongTextProperties SET Length = @Length, Value = @Value " +
                            "WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId",
                            cmd =>
                            {
                                cmd.Parameters.AddRange(new[]
                                {
                                    ctx.CreateParameter("@VersionId", DbType.Int32, versionId),
                                    ctx.CreateParameter("@PropertyTypeId", DbType.Int32, pt.Id),
                                    ctx.CreateParameter("@Length", DbType.Int32, stringValue.Length),
                                    ctx.CreateParameter("@Value", DbType.String, stringValue.Length, stringValue),
                                });
                            });
                    }
                    break;
                case DataType.String:
                case DataType.Int:
                case DataType.Currency:
                case DataType.DateTime:
                case DataType.Binary:
                case DataType.Reference:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task SetFileStagingAsync(int fileId, bool staging)
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
            {
                await ctx.ExecuteNonQueryAsync(
                    "UPDATE Files SET Staging = @Staging WHERE FileId = @FileId",
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@FileId", DbType.Int32, fileId),
                            ctx.CreateParameter("@Staging", DbType.Boolean, staging),
                        });
                    });
            }
        }

        public async Task DeleteFileAsync(int fileId)
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
            {
                await ctx.ExecuteNonQueryAsync(
                    "DELETE FROM Files WHERE FileId = @FileId",
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@FileId", DbType.Int32, fileId),
                        });
                    });
            }
        }

        public async Task EnsureOneUnlockedSchemaLockAsync()
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
                await ctx.ExecuteNonQueryAsync(@"DELETE FROM SchemaModification
INSERT INTO SchemaModification (ModificationDate) VALUES (GETUTCDATE())
");
        }

        public DateTime GetSharedLockCreationDate(int nodeId)
        {
            const string sql = "SELECT [CreationDate] FROM [dbo].[SharedLocks] WHERE [ContentId] = @ContentId";

            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
            {
                var result = ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ContentId", DbType.Int32, nodeId)
                    });
                }).GetAwaiter().GetResult();
                return result == DBNull.Value ? DateTime.MinValue : (DateTime)result;
            }
        }

        public void SetSharedLockCreationDate(int nodeId, DateTime value)
        {
            const string sql = "UPDATE [dbo].[SharedLocks] SET [CreationDate] = @CreationDate WHERE [ContentId] = @ContentId";

            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
            {
                var unused = ctx.ExecuteNonQueryAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ContentId", DbType.Int32, nodeId),
                        ctx.CreateParameter("@CreationDate", DbType.DateTime2, value)
                    });
                }).GetAwaiter().GetResult();
            }
        }

        public DataProvider CreateCannotCommitDataProvider(DataProvider mainDataProvider)
        {
            return new MsSqlCannotCommitDataProvider(_connectionStrings.Repository);
        }

        public async Task DeleteAllStatisticalDataAsync(IStatisticalDataProvider dataProvider)
        {
            const string sql = @"DELETE FROM StatisticalData
DELETE FROM StatisticalAggregations
";
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
                await ctx.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
        }

        public async Task<IEnumerable<IStatisticalDataRecord>> LoadAllStatisticalDataRecords(IStatisticalDataProvider dataProvider)
        {
            const string sql = "SELECT * FROM StatisticalData";
            var records = new List<IStatisticalDataRecord>();
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
            {
                await ctx.ExecuteReaderAsync(sql, (reader, cancel) =>
                {
                    while (reader.Read())
                    {
                        var durationIndex = reader.GetOrdinal("Duration");
                        records.Add(new StatisticalDataRecord
                        {
                            Id = reader.GetSafeInt32(reader.GetOrdinal("Id")),
                            DataType = reader.GetSafeString(reader.GetOrdinal("DataType")),
                            WrittenTime = reader.GetDateTimeUtc(reader.GetOrdinal("WrittenTime")),
                            CreationTime = reader.GetDateTimeUtc(reader.GetOrdinal("CreationTime")),
                            Duration = reader.IsDBNull(durationIndex) ? (TimeSpan?)null : TimeSpan.FromTicks(reader.GetInt64(durationIndex)),
                            RequestLength = reader.GetLongOrNull("RequestLength"),
                            ResponseLength = reader.GetLongOrNull("ResponseLength"),
                            ResponseStatusCode = reader.GetIntOrNull("ResponseStatusCode"),
                            Url = reader.GetStringOrNull("Url"),
                            TargetId = reader.GetIntOrNull("TargetId"),
                            ContentId = reader.GetIntOrNull("ContentId"),
                            EventName = reader.GetStringOrNull("EventName"),
                            ErrorMessage = reader.GetStringOrNull("ErrorMessage"),
                            GeneralData = reader.GetStringOrNull("GeneralData"),
                        });
                    }
                    return Task.FromResult(0);
                }).ConfigureAwait(false);
            }
            return records;
        }

        public async Task<IEnumerable<Aggregation>> LoadAllStatisticalDataAggregations(IStatisticalDataProvider dataProvider)
        {
            const string sql = "SELECT * FROM StatisticalAggregations";
            var aggregations = new List<Aggregation>();
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
            {
                await ctx.ExecuteReaderAsync(sql, async (reader, cancel) =>
                {
                    while (await reader.ReadAsync(cancel))
                    {
                        aggregations.Add(new Aggregation
                        {
                            DataType = reader.GetString(reader.GetOrdinal("DataType")),
                            Date = reader.GetDateTimeUtc(reader.GetOrdinal("Date")),
                            Resolution = Enum.Parse<TimeResolution>( reader.GetString(reader.GetOrdinal("Resolution"))),
                            Data = reader.GetStringOrNull("Data"),
                        });
                    }
                    return true;
                }).ConfigureAwait(false);
            }
            return aggregations;
        }

        #region MsSqlCannotCommitDataProvider classes
        private class MsSqlCannotCommitDataProvider : MsSqlDataProvider
        {
            private readonly string _connectionString;
            public MsSqlCannotCommitDataProvider(string connectionString) : base(Options.Create(DataOptions.GetLegacyConfiguration()),
                Options.Create(new ConnectionStringOptions
            {
                Repository = connectionString
            }), Options.Create(new MsSqlDatabaseInstallationOptions()), 
                new MsSqlDatabaseInstaller(Options.Create(new MsSqlDatabaseInstallationOptions()),
                    NullLoggerFactory.Instance.CreateLogger<MsSqlDatabaseInstaller>()),
                new MsSqlDataInstaller(Options.Create(new ConnectionStringOptions
            {
                Repository = connectionString
            }), NullLoggerFactory.Instance.CreateLogger<MsSqlDataInstaller>()),
                NullLoggerFactory.Instance.CreateLogger<MsSqlDataProvider>())
            {
                _connectionString = connectionString;
            }
            public override SnDataContext CreateDataContext(CancellationToken token)
            {
                return new MsSqlCannotCommitDataContext(_connectionString, new DataOptions(), token);
            }
        }
        private class MsSqlCannotCommitDataContext : MsSqlDataContext
        {
            public MsSqlCannotCommitDataContext(string connectionString, DataOptions options, CancellationToken cancellationToken)
                : base(connectionString, options, cancellationToken)
            {

            }
            public override TransactionWrapper WrapTransaction(DbTransaction underlyingTransaction,
                CancellationToken cancellationToken, TimeSpan timeout = default(TimeSpan))
            {
                return new MsSqlCannotCommitTransaction(underlyingTransaction, new DataOptions(), cancellationToken);
            }
        }
        private class MsSqlCannotCommitTransaction : TransactionWrapper
        {
            public MsSqlCannotCommitTransaction(DbTransaction transaction, DataOptions options, CancellationToken cancellationToken)
                : base(transaction, options, cancellationToken) { }
            public override void Commit()
            {
                throw new NotSupportedException("This transaction cannot commit anything.");
            }
        }
        #endregion

        public async Task ClearIndexingActivitiesAsync()
        {
            using (var ctx = _mainProvider.CreateDataContext(CancellationToken.None))
                await ctx.ExecuteNonQueryAsync(@"DELETE FROM IndexingActivities");
        }

        public IEnumerable<IndexIntegrityCheckerItem> GetTimestampDataForOneNodeIntegrityCheck(string path, int[] excludedNodeTypeIds)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IndexIntegrityCheckerItem> GetTimestampDataForRecursiveIntegrityCheck(string path, int[] excludedNodeTypeIds)
        {
            throw new NotImplementedException();
        }
    }
}
