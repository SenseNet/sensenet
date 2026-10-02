using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;
using SenseNet.Tools;
// ReSharper disable AccessToDisposedClosure

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.PgSqlClient
{
    /// <summary>
    /// Contains the PostgreSQL-specific implementation of the IBlobStorageMetaDataProvider interface that
    /// is responsible for binary-related operations in the main metadata database.
    /// </summary>
    public partial class PgSqlBlobMetaDataProvider : IBlobStorageMetaDataProvider
    {
        private readonly IRetrier _retrier;
        private DataOptions DataOptions { get; }
        private BlobStorageOptions BlobStorageOptions { get; }
        private IBlobProviderStore Providers { get; }
        private ConnectionStringOptions ConnectionStrings { get; }

        public PgSqlBlobMetaDataProvider(IBlobProviderStore providers, IOptions<DataOptions> dataOptions,
            IOptions<BlobStorageOptions> blobStorageOptions, IOptions<ConnectionStringOptions> connectionOptions, IRetrier retrier)
        {
            _retrier = retrier;
            Providers = providers;
            DataOptions = dataOptions?.Value ?? new DataOptions();
            BlobStorageOptions = blobStorageOptions?.Value ?? new BlobStorageOptions();
            ConnectionStrings = connectionOptions?.Value ?? new ConnectionStringOptions();
        }

        /* ======================================================================================= IBlobStorageMetaDataProvider */

        private static string ValidateExtension(string originalExtension)
        {
            return originalExtension.Length == 0
                ? string.Empty
                : originalExtension;
        }

        public async Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, bool clearStream, int versionId, int propertyTypeId,
            CancellationToken cancellationToken)
        {
            var sql = GetBlobStorageContextScript;
            if (clearStream)
                sql = ClearStreamByFileIdScript + sql;

            cancellationToken.ThrowIfCancellationRequested();

            using var op = SnTrace.Database.StartOperation("PgSqlBlobMetaDataProvider: " +
                "GetBlobStorageContext(fileId: {0}, clearStream: {1}, versionId: {2}, propertyTypeId: {3})",
                fileId, clearStream, versionId, propertyTypeId);

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            var result = await ctx.ExecuteReaderAsync(sql, cmd =>
            {
                cmd.Parameters.Add(ctx.CreateParameter("@FileId", DbType.Int32, fileId));
                if (clearStream)
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@VersionId", DbType.Int32, versionId));
                    cmd.Parameters.Add(ctx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyTypeId));
                }
            }, async (reader, cancel) =>
            {
                cancel.ThrowIfCancellationRequested();
                if (!await reader.ReadAsync(cancel).ConfigureAwait(false))
                    return null;

                var length = reader.GetSafeInt64(0);
                var providerName = reader.GetSafeString(1);
                var providerData = reader.GetSafeString(2);
                var provider = Providers.GetProvider(providerName);

                return new BlobStorageContext(provider, providerData)
                {
                    VersionId = versionId,
                    PropertyTypeId = propertyTypeId,
                    FileId = fileId,
                    Length = length,
                    BlobProviderData = provider is IBuiltInBlobProvider
                        ? new BuiltinBlobProviderData()
                        : provider.ParseData(providerData)
                };
            }).ConfigureAwait(false);
            op.Successful = true;

            return result;
        }

        public async System.Threading.Tasks.Task InsertBinaryPropertyAsync(IBlobProvider blobProvider, BinaryDataValue value, int versionId, int propertyTypeId,
            bool isNewNode, SnDataContext dataContext)
        {
            var streamLength = value.Stream?.Length ?? 0;

            using var op = SnTrace.Database.StartOperation("PgSqlBlobMetaDataProvider: InsertBinaryPropertyAsync: " +
                "BlobProvider: {0}, ContentType: {1}, FileName: {2}, Size: {3}, VersionId: {4}, PropertyTypeId: {5}, IsNewNode: {6})",
                blobProvider.GetType().Name, value.ContentType, value.FileName, streamLength, versionId, propertyTypeId, isNewNode);

            var ctx = new BlobStorageContext(blobProvider) { VersionId = versionId, PropertyTypeId = propertyTypeId, FileId = 0, Length = streamLength };

            if (!(blobProvider is IBuiltInBlobProvider))
            {
                await blobProvider.AllocateAsync(ctx, dataContext.CancellationToken).ConfigureAwait(false);

                using (var stream = blobProvider.GetStreamForWrite(ctx))
                    value.Stream?.CopyTo(stream);

                value.BlobProviderName = ctx.Provider.GetType().FullName;
                value.BlobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);
            }

            if(!(dataContext is PgSqlDataContext sqlCtx))
                throw new PlatformNotSupportedException();

            var sql = isNewNode ? InsertBinaryPropertyScript : DeleteAndInsertBinaryPropertyScript;
            if (!isNewNode)
                dataContext.NeedToCleanupFiles = true;

            await sqlCtx.ExecuteReaderAsync(sql, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    sqlCtx.CreateParameter("@VersionId", DbType.Int32, versionId != 0 ? (object)versionId : DBNull.Value),
                    sqlCtx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyTypeId != 0 ? (object)propertyTypeId : DBNull.Value),
                    sqlCtx.CreateParameter("@ContentType", DbType.String, 450, value.ContentType),
                    sqlCtx.CreateParameter("@FileNameWithoutExtension", DbType.String, 450, value.FileName.FileNameWithoutExtension == null ? DBNull.Value : (object)value.FileName.FileNameWithoutExtension),
                    sqlCtx.CreateParameter("@Extension", DbType.String, 50, ValidateExtension(value.FileName.Extension)),
                    sqlCtx.CreateParameter("@Size", DbType.Int64, Math.Max(0, value.Size)),
                    sqlCtx.CreateParameter("@BlobProvider", DbType.String, 450, value.BlobProviderName != null ? (object)value.BlobProviderName : DBNull.Value),
                    sqlCtx.CreateParameter("@BlobProviderData", DbType.String, int.MaxValue, value.BlobProviderData != null ? (object)value.BlobProviderData : DBNull.Value),
                    sqlCtx.CreateParameter("@Checksum", DbType.AnsiString, 200, value.Checksum != null ? (object)value.Checksum : DBNull.Value),
                });
            }, async (reader, cancel) =>
            {
                if (await reader.ReadAsync(cancel).ConfigureAwait(false))
                {
                    value.Id = Convert.ToInt32(reader[0]);
                    value.FileId = Convert.ToInt32(reader[1]);
                    value.Timestamp = Convert.ToInt64(reader.GetValue(2));
                }
                return true;
            }).ConfigureAwait(false);
                
            if (blobProvider is IBuiltInBlobProvider && value.Stream != null)
            {
                ctx.FileId = value.FileId;
                ctx.BlobProviderData = new BuiltinBlobProviderData();

                await PgSqlBuiltInBlobProvider.AddStreamAsync(ctx, value.Stream, sqlCtx).ConfigureAwait(false);
            }

            op.Successful = true;
        }

        public async System.Threading.Tasks.Task InsertBinaryPropertyWithFileIdAsync(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode,
            SnDataContext dataContext)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlBlobMetaDataProvider: InsertBinaryPropertyWithFileId: " +
                "VersionId: {0}, PropertyTypeId: {1}, FileId: {2}, IsNewNode: {3}",
                versionId, propertyTypeId, value.FileId, isNewNode);

            var sql = isNewNode ? InsertBinaryPropertyWithKnownFileIdScript : DeleteAndInsertBinaryPropertyWithKnownFileIdScript;
            if (!isNewNode)
                dataContext.NeedToCleanupFiles = true;

            if (!(dataContext is PgSqlDataContext sqlCtx))
                throw new PlatformNotSupportedException();

            value.Id = (int)await sqlCtx.ExecuteScalarAsync(sql, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    sqlCtx.CreateParameter("@VersionId", DbType.Int32, versionId != 0 ? (object) versionId : DBNull.Value),
                    sqlCtx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyTypeId != 0 ? (object) propertyTypeId : DBNull.Value),
                    sqlCtx.CreateParameter("@FileId", DbType.Int32, value.FileId),
                });
            }).ConfigureAwait(false);

            op.Successful = true;
        }

        public async System.Threading.Tasks.Task UpdateBinaryPropertyAsync(IBlobProvider blobProvider, BinaryDataValue value, SnDataContext dataContext)
        {
            var streamLength = value.Stream?.Length ?? 0;
            using var op = SnTrace.Database.StartOperation("PgSqlBlobMetaDataProvider: UpdateBinaryProperty: " +
                "BlobProvider: {0}, BinaryPropertyId: {1}, FileId: {2}, ContentType: {3}, FileName: {4}, Size: {5}",
                blobProvider.GetType().Name, value.Id, value.FileId, value.ContentType, value.FileName, streamLength);

            var isExternal = false;
            if (!(blobProvider is IBuiltInBlobProvider))
            {
                var ctx = new BlobStorageContext(blobProvider)
                {
                    VersionId = 0,
                    PropertyTypeId = 0,
                    FileId = value.FileId,
                    Length = streamLength,
                };

                await blobProvider.AllocateAsync(ctx, dataContext.CancellationToken).ConfigureAwait(false);
                isExternal = true;

                value.BlobProviderName = ctx.Provider.GetType().FullName;
                value.BlobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);
            }
            else
            {
                value.BlobProviderName = null;
                value.BlobProviderData = null;
            }

            if (blobProvider is IBuiltInBlobProvider)
            {
                if (streamLength > int.MaxValue)
                    throw new NotSupportedException();
            }

            var isRepositoryStream = value.Stream is RepositoryStream;
            var hasStream = isRepositoryStream || value.Stream is MemoryStream;
            if (!isExternal && !hasStream)
            {
                SnTrace.Database.Write("Do not do any database operation because the stream is not modified.");
                op.Successful = true;
                return;
            }

            if(!(dataContext is PgSqlDataContext sqlCtx))
                throw new PlatformNotSupportedException();

            // For built-in blob providers we need conditional logic:
            // check if a new Files row is needed, then either insert-new or update-in-place.
            // For external blob providers we always insert a new Files row.
            int fileId;
            if (blobProvider is IBuiltInBlobProvider)
            {
                // Step 1: check if we need a new Files row
                var needNewRow = (bool)await sqlCtx.ExecuteScalarAsync(UpdateBinaryPropertyNeedNewFileRowScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        sqlCtx.CreateParameter("@BinaryPropertyId", DbType.Int32, value.Id),
                        sqlCtx.CreateParameter("@BlobProvider", DbType.String, 450, value.BlobProviderName != null ? (object)value.BlobProviderName : DBNull.Value),
                    });
                }).ConfigureAwait(false);

                // Step 2: either insert a new file row or update in-place
                var updateSql = needNewRow ? UpdateBinaryPropertyNewFilerowScript : UpdateBinaryPropertyInPlaceScript;
                fileId = (int)await sqlCtx.ExecuteScalarAsync(updateSql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        sqlCtx.CreateParameter("@BinaryPropertyId", DbType.Int32, value.Id),
                        sqlCtx.CreateParameter("@ContentType", DbType.String, 450, value.ContentType),
                        sqlCtx.CreateParameter("@FileNameWithoutExtension", DbType.String, 450, value.FileName.FileNameWithoutExtension == null ? DBNull.Value : (object)value.FileName.FileNameWithoutExtension),
                        sqlCtx.CreateParameter("@Extension", DbType.String, 50, ValidateExtension(value.FileName.Extension)),
                        sqlCtx.CreateParameter("@Size", DbType.Int64, value.Size),
                        sqlCtx.CreateParameter("@Checksum", DbType.AnsiString, 200, value.Checksum != null ? (object)value.Checksum : DBNull.Value),
                        sqlCtx.CreateParameter("@BlobProvider", DbType.String, 450, value.BlobProviderName != null ? (object)value.BlobProviderName : DBNull.Value),
                        sqlCtx.CreateParameter("@BlobProviderData", DbType.String, int.MaxValue, value.BlobProviderData != null ? (object)value.BlobProviderData : DBNull.Value),
                    });
                }).ConfigureAwait(false);
            }
            else
            {
                fileId = (int)await sqlCtx.ExecuteScalarAsync(UpdateBinaryPropertyNewFilerowScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        sqlCtx.CreateParameter("@BinaryPropertyId", DbType.Int32, value.Id),
                        sqlCtx.CreateParameter("@ContentType", DbType.String, 450, value.ContentType),
                        sqlCtx.CreateParameter("@FileNameWithoutExtension", DbType.String, 450, value.FileName.FileNameWithoutExtension == null ? DBNull.Value : (object)value.FileName.FileNameWithoutExtension),
                        sqlCtx.CreateParameter("@Extension", DbType.String, 50, ValidateExtension(value.FileName.Extension)),
                        sqlCtx.CreateParameter("@Size", DbType.Int64, value.Size),
                        sqlCtx.CreateParameter("@Checksum", DbType.AnsiString, 200, value.Checksum != null ? (object)value.Checksum : DBNull.Value),
                        sqlCtx.CreateParameter("@BlobProvider", DbType.String, 450, value.BlobProviderName != null ? (object)value.BlobProviderName : DBNull.Value),
                        sqlCtx.CreateParameter("@BlobProviderData", DbType.String, int.MaxValue, value.BlobProviderData != null ? (object)value.BlobProviderData : DBNull.Value),
                    });
                }).ConfigureAwait(false);
            }

            if (fileId > 0 && fileId != value.FileId)
                value.FileId = fileId;

            if (blobProvider is IBuiltInBlobProvider)
            {
                var ctx = new BlobStorageContext(blobProvider, value.BlobProviderData)
                {
                    VersionId = 0,
                    PropertyTypeId = 0,
                    FileId = value.FileId,
                    Length = streamLength,
                    BlobProviderData = new BuiltinBlobProviderData()
                };

                await PgSqlBuiltInBlobProvider.UpdateStreamAsync(ctx, value.Stream, sqlCtx).ConfigureAwait(false);
            }
            else
            {
                var ctx = new BlobStorageContext(blobProvider, value.BlobProviderData)
                {
                    VersionId = 0,
                    PropertyTypeId = 0,
                    FileId = value.FileId,
                    Length = streamLength,
                };
                if (streamLength == 0)
                {
                    await blobProvider.ClearAsync(ctx, dataContext.CancellationToken).ConfigureAwait(false);
                }
                else
                {
                    using (var stream = blobProvider.GetStreamForWrite(ctx))
                        value.Stream?.CopyTo(stream);
                }
            }
            op.Successful = true;
        }

        public async System.Threading.Tasks.Task DeleteBinaryPropertyAsync(int versionId, int propertyTypeId, SnDataContext dataContext)
        {
            if (!(dataContext is PgSqlDataContext sqlCtx))
                throw new PlatformNotSupportedException();

            using var op = SnTrace.Database.StartOperation("PgSqlBlobMetaDataProvider: " +
                "DeleteBinaryProperty(versionId: {0}, propertyTypeId: {1})", versionId, propertyTypeId);
            await sqlCtx.ExecuteNonQueryAsync(DeleteBinaryPropertyScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    sqlCtx.CreateParameter("@VersionId", DbType.Int32, versionId),
                    sqlCtx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyTypeId),
                });
            }).ConfigureAwait(false);
            op.Successful = true;
        }

        public async System.Threading.Tasks.Task DeleteBinaryPropertiesAsync(IEnumerable<int> versionIds, SnDataContext dataContext)
        {
            if (!(dataContext is PgSqlDataContext sqlCtx))
                throw new PlatformNotSupportedException();

            var idsParam = string.Join(",", versionIds.Select(x => x.ToString()));
            using var op = SnTrace.Database.StartOperation("PgSqlBlobMetaDataProvider: " +
                "DeleteBinaryProperties(versionIds: [{0}])", idsParam);
            await sqlCtx.ExecuteNonQueryAsync(DeleteBinaryPropertiesScript, cmd =>
            {
                cmd.Parameters.Add(sqlCtx.CreateParameter("@VersionIds", DbType.String, idsParam.Length, idsParam));
            }).ConfigureAwait(false);
            op.Successful = true;
        }

        public async Task<BinaryDataValue> LoadBinaryPropertyAsync(int versionId, int propertyTypeId, SnDataContext dataContext)
        {
            if (!(dataContext is PgSqlDataContext sqlCtx))
                throw new PlatformNotSupportedException();

            using var op = SnTrace.Database.StartOperation("PgSqlBlobMetaDataProvider: " +
                "LoadBinaryProperty(versionId: {0}, propertyTypeId: {1})", versionId, propertyTypeId);
            var result = await sqlCtx.ExecuteReaderAsync(LoadBinaryPropertyScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    sqlCtx.CreateParameter("@VersionId", DbType.Int32, versionId),
                    sqlCtx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyTypeId),
                });
            }, async (reader, cancel) =>
            {
                cancel.ThrowIfCancellationRequested();
                if (!await reader.ReadAsync(cancel).ConfigureAwait(false))
                    return null;

                var size = reader.GetInt64("Size");
                var binaryPropertyId = reader.GetInt32("BinaryPropertyId");
                var fileId = reader.GetInt32("FileId");
                var providerName = reader.GetSafeString("BlobProvider");
                var providerTextData = reader.GetSafeString("BlobProviderData");
                var provider = Providers.GetProvider(providerName);
                var context = new BlobStorageContext(provider, providerTextData)
                {
                    VersionId = versionId,
                    PropertyTypeId = propertyTypeId,
                    FileId = fileId,
                    Length = size
                };
                Stream stream = null;
                if (provider is IBuiltInBlobProvider)
                {
                    context.BlobProviderData = new BuiltinBlobProviderData();
                    var streamIndex = reader.GetOrdinal("Stream");
                    if (!reader.IsDBNull(streamIndex))
                    {
                        var rawData = (byte[]) reader.GetValue(streamIndex);
                        stream = new MemoryStream(rawData);
                    }
                }

                return new BinaryDataValue
                {
                    Id = binaryPropertyId,
                    FileId = fileId,
                    ContentType = reader.GetSafeString("ContentType"),
                    FileName = new BinaryFileName(
                        reader.GetSafeString("FileNameWithoutExtension") ?? "",
                        reader.GetSafeString("Extension") ?? ""),
                    Size = size,
                    Checksum = reader.GetSafeString("Checksum"),
                    BlobProviderName = providerName,
                    BlobProviderData = providerTextData,
                    Timestamp = reader.GetSafeLongFromBytes("Timestamp"),
                    Stream = stream
                };
            }).ConfigureAwait(false);
            op.Successful = true;

            return result;
        }

        public async Task<BinaryCacheEntity> LoadBinaryCacheEntityAsync(int versionId, int propertyTypeId, CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            return await LoadBinaryCacheEntityAsync(versionId, propertyTypeId, ctx).ConfigureAwait(false);
        }
        public async Task<BinaryCacheEntity> LoadBinaryCacheEntityAsync(int versionId, int propertyTypeId, SnDataContext dataContext)
        {
            if (!(dataContext is PgSqlDataContext sqlCtx))
                throw new PlatformNotSupportedException();

            using var op = SnTrace.Database.StartOperation("PgSqlBlobMetaDataProvider: " +
                "LoadBinaryCacheEntity(versionId: {0}, propertyTypeId: {1})", versionId, propertyTypeId);
            var result = await sqlCtx.ExecuteReaderAsync(LoadBinaryCacheEntityScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    sqlCtx.CreateParameter("@MaxSize", DbType.Int32, BlobStorageOptions.BinaryCacheSize),
                    sqlCtx.CreateParameter("@VersionId", DbType.Int32, versionId),
                    sqlCtx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyTypeId),
                });
            }, async (reader, cancel) =>
            {
                cancel.ThrowIfCancellationRequested();
                if (!reader.HasRows || !await reader.ReadAsync(cancel).ConfigureAwait(false))
                    return null;

                var length = reader.GetInt64(0);
                var binaryPropertyId = reader.GetInt32(1);
                var fileId = reader.GetInt32(2);

                var providerName = reader.GetSafeString(3);
                var providerTextData = reader.GetSafeString(4);

                byte[] rawData = null;

                var provider = Providers.GetProvider(providerName);
                var context = new BlobStorageContext(provider, providerTextData)
                {
                    VersionId = versionId,
                    PropertyTypeId = propertyTypeId,
                    FileId = fileId,
                    Length = length
                };
                if (provider is IBuiltInBlobProvider)
                {
                    context.BlobProviderData = new BuiltinBlobProviderData();
                    if (!reader.IsDBNull(5))
                        rawData = (byte[]) reader.GetValue(5);
                }

                return new BinaryCacheEntity
                {
                    Length = length,
                    RawData = rawData,
                    BinaryPropertyId = binaryPropertyId,
                    FileId = fileId,
                    Context = context
                };
            }).ConfigureAwait(false);
            op.Successful = true;

            return result;
        }

        public async Task<string> StartChunkAsync(IBlobProvider blobProvider, int versionId, int propertyTypeId, long fullSize,
            CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlBlobMetaDataProvider: " +
                "StartChunk(blobProvider: {0}, versionId: {1}, propertyTypeId: {2}, fullSize: {3})",
                blobProvider, versionId, propertyTypeId, fullSize);

            var ctx = new BlobStorageContext(blobProvider) { VersionId = versionId, PropertyTypeId = propertyTypeId, FileId = 0, Length = fullSize };
            string blobProviderName = null;
            string blobProviderData = null;
            if (!(blobProvider is IBuiltInBlobProvider))
            {
                await blobProvider.AllocateAsync(ctx, cancellationToken).ConfigureAwait(false);
                blobProviderName = blobProvider.GetType().FullName;
                blobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);
            }
            try
            {
                using var dctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
                using var transaction = dctx.BeginTransaction();

                // Step 1: Ensure the BinaryProperties row exists (lazy creation).
                await dctx.ExecuteNonQueryAsync(InsertStagingBinaryEnsureBinaryPropertyScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        dctx.CreateParameter("@VersionId", DbType.Int32, versionId),
                        dctx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyTypeId),
                    });
                }).ConfigureAwait(false);

                // Step 2: Insert staging file and return BinaryPropertyId + FileId.
                var result = await dctx.ExecuteReaderAsync(InsertStagingBinaryScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        dctx.CreateParameter("@VersionId", DbType.Int32, versionId),
                        dctx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyTypeId),
                        dctx.CreateParameter("@Size", DbType.Int64, fullSize),
                        dctx.CreateParameter("@BlobProvider", DbType.String, 450, blobProviderName != null ? (object)blobProviderName : DBNull.Value),
                        dctx.CreateParameter("@BlobProviderData", DbType.String, int.MaxValue, blobProviderData != null ? (object)blobProviderData : DBNull.Value),
                    });
                }, async (reader, cancel) =>
                {
                    int binaryPropertyId;
                    int fileId;
                    cancel.ThrowIfCancellationRequested();
                    if (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        binaryPropertyId = reader.GetSafeInt32(0);
                        fileId = reader.GetSafeInt32(1);
                    }
                    else
                    {
                        throw new DataException("File row could not be inserted.");
                    }
                    ctx.FileId = fileId;

                    return new ChunkToken
                    {
                        VersionId = versionId,
                        PropertyTypeId = propertyTypeId,
                        BinaryPropertyId = binaryPropertyId,
                        FileId = fileId
                    }.GetToken();
                }).ConfigureAwait(false);
                transaction.Commit();
                op.Successful = true;
                return result;
            }
            catch (Exception ex)
            {
                throw new DataException("Error during saving binary chunk to PostgreSQL.", ex);
            }
        }

        public async System.Threading.Tasks.Task CommitChunkAsync(int versionId, int propertyTypeId, int fileId, long fullSize, BinaryDataValue source,
            CancellationToken cancellationToken)
        {
            try
            {
                using var op = SnTrace.Database.StartOperation("PgSqlBlobMetaDataProvider: CommitChunk: " +
                    "versionId: {0}, propertyTypeId: {1}, fileId: {2}, fullSize: {3}, contentType: {4}, fileName: {5}",
                    versionId, propertyTypeId, fileId, fullSize, source?.ContentType ?? "<null>", source?.FileName ?? "<null>");
                using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
                using var transaction = ctx.BeginTransaction();
                await ctx.ExecuteNonQueryAsync(CommitChunkScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@FileId", DbType.Int32, fileId),
                        ctx.CreateParameter("@VersionId", DbType.Int32, versionId),
                        ctx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyTypeId),
                        ctx.CreateParameter("@Size", DbType.Int64, fullSize),
                        ctx.CreateParameter("@Checksum", DbType.AnsiString, 200, DBNull.Value),
                        ctx.CreateParameter("@ContentType", DbType.String, 50, source != null ? source.ContentType : string.Empty),
                        ctx.CreateParameter("@FileNameWithoutExtension", DbType.String, 450, source != null
                            ? source.FileName.FileNameWithoutExtension == null
                                ? DBNull.Value
                                : (object) source.FileName.FileNameWithoutExtension
                            : DBNull.Value),
                        ctx.CreateParameter("@Extension", DbType.String, 50,
                            source != null ? ValidateExtension(source.FileName.Extension) : string.Empty),
                    });
                }).ConfigureAwait(false);
                transaction.Commit();
                op.Successful = true;
            }
            catch (Exception ex)
            {
                throw new DataException("Error during committing binary chunk.", ex);
            }
        }

        public System.Threading.Tasks.Task CleanupFilesSetDeleteFlagAsync(CancellationToken cancellationToken)
        {
            return CleanupFilesSetDeleteFlagAsync(CleanupFileSetIsDeletedScript, cancellationToken);
        }
        public System.Threading.Tasks.Task CleanupFilesSetDeleteFlagImmediatelyAsync(CancellationToken cancellationToken)
        {
            return CleanupFilesSetDeleteFlagAsync(CleanupFileSetIsDeletedImmediatelyScript, cancellationToken);
        }
        private async System.Threading.Tasks.Task CleanupFilesSetDeleteFlagAsync(string script, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation(() => "PgSqlBlobMetaDataProvider: " +
                $"CleanupFilesSetDeleteFlag: script: {script.ToTrace()}");

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            using var transaction = ctx.BeginTransaction();
            try
            {
                await ctx.ExecuteNonQueryAsync(script).ConfigureAwait(false);
                transaction.Commit();
            }
            catch (Exception e)
            {
                throw new DataException("Error during setting deleted flag on files.", e);
            }

            op.Successful = true;
        }

        public async Task<bool> CleanupFilesAsync(CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlBlobMetaDataProvider: CleanupFiles()");
            using var dctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            var result = await dctx.ExecuteReaderAsync(CleanupFileScript, async (reader, cancel) =>
            {
                try
                {
                    var deleted = false;
                    var fileId = 0;
                    var size = 0L;
                    string providerName = null;
                    string providerData = null;
                    if (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        deleted = true;
                        fileId = reader.GetSafeInt32(reader.GetOrdinal("FileId"));
                        size = reader.GetSafeInt64(reader.GetOrdinal("Size"));
                        providerName = reader.GetSafeString(reader.GetOrdinal("BlobProvider"));
                        providerData = reader.GetSafeString(reader.GetOrdinal("BlobProviderData"));
                    }

                    var provider = Providers.GetProvider(providerName);
                    var ctx = new BlobStorageContext(provider, providerData) { VersionId = 0, PropertyTypeId = 0, FileId = fileId, Length = size };

                    await ctx.Provider.DeleteAsync(ctx, cancel).ConfigureAwait(false);

                    return deleted;
                }
                catch (Exception ex)
                {
                    throw new DataException("Error during binary cleanup.", ex);
                }
            }).ConfigureAwait(false);
            op.Successful = true;

            return result;
        }

        private int _waitBetweenCleanupFilesMilliseconds = 0;
        public async System.Threading.Tasks.Task CleanupAllFilesAsync(CancellationToken cancellationToken)
        {
            while (await CleanupFilesAsync(cancellationToken).ConfigureAwait(false))
            {
                if(_waitBetweenCleanupFilesMilliseconds != 0)
                    await System.Threading.Tasks.Task.Delay(_waitBetweenCleanupFilesMilliseconds, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<int> GetFirstFileIdAsync(CancellationToken cancel)
        {
            int result;
            var sql = GetFirstFileId;
            cancel.ThrowIfCancellationRequested();
            using var op = SnTrace.Database.StartOperation("PgSqlBlobMetaDataProvider: GetFirstFileIdAsync()");
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancel);
            var scalar = await ctx.ExecuteScalarAsync(sql).ConfigureAwait(false);
            if (scalar is int intValue)
                result = intValue;
            else
                result = 0;
            op.Successful = true;
            return result;
        }
    }
}
