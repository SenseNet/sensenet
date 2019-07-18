using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.Tools;
// ReSharper disable AccessToDisposedClosure

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    /// <summary>
    /// Contains the MS SQL-specific implementation of the IBlobStorageMetaDataProvider interface that
    /// is responsible for binary-related operations in the main metadata database.
    /// </summary>
    public partial class MsSqlBlobMetaDataProvider : IBlobStorageMetaDataProvider
    {
        public IDataPlatform<DbConnection, DbCommand, DbParameter> GetPlatform()
        {
            return new MsSqlDataContext_OLD();
        }

        /* ======================================================================================= IBlobStorageMetaDataProvider */

        private static string ValidateExtension(string originalExtension)
        {
            return originalExtension.Length == 0
                ? string.Empty
                : string.Concat(".", originalExtension);
        }


        /// <summary>
        /// Returns a context object that holds MsSql-specific data for blob storage operations.
        /// </summary>
        /// <param name="fileId">File identifier.</param>
        /// <param name="clearStream">Whether the blob provider should clear the stream during assembling the context.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        public BlobStorageContext GetBlobStorageContext(int fileId, bool clearStream, int versionId, int propertyTypeId)
        {
            return GetBlobStorageContextAsync(fileId, clearStream, versionId, propertyTypeId).Result;
        }
        /// <summary>
        /// Returns a context object that holds MsSql-specific data for blob storage operations.
        /// </summary>
        /// <param name="fileId">File identifier.</param>
        /// <param name="clearStream">Whether the blob provider should clear the stream during assembling the context.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        public async Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, bool clearStream, int versionId, int propertyTypeId)
        {
            var sql = GetBlobStorageContextScript;
            if (clearStream)
                sql = ClearStreamByFileIdScript + sql;

            using (var ctx = new MsSqlDataContext())
            {
                return await ctx.ExecuteReaderAsync/*UNDONE*/(sql, cmd =>
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
                    if (!await reader.ReadAsync(cancel))
                        return null;

                    var length = reader.GetSafeInt64(0);
                    var providerName = reader.GetSafeString(1);
                    var providerData = reader.GetSafeString(2);
                    var provider = BlobStorageBase.GetProvider(providerName);

                    return new BlobStorageContext(provider, providerData)
                    {
                        VersionId = versionId,
                        PropertyTypeId = propertyTypeId,
                        FileId = fileId,
                        Length = length,
                        BlobProviderData = provider == BlobStorageBase.BuiltInProvider
                            ? new BuiltinBlobProviderData()
                            : provider.ParseData(providerData)
                    };
                });
            }
        }


        /// <summary>
        /// Inserts a new binary property value into the metadata database and the blob storage, 
        /// removing the previous one if the content is not new.
        /// </summary>
        /// <param name="blobProvider">Blob storage provider.</param>
        /// <param name="value">Binary data to insert.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="isNewNode">Whether this value belongs to a new or an existing node.</param>
        public void InsertBinaryProperty(IBlobProvider blobProvider, BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode)
        {
            var streamLength = value.Stream?.Length ?? 0;
            var ctx = new BlobStorageContext(blobProvider) { VersionId = versionId, PropertyTypeId = propertyTypeId, FileId = 0, Length = streamLength };

            // In case of an external provider allocate the place for bytes and
            // write the stream beforehand and get the generated provider data.
            // Note that the external provider does not need an existing record
            // in the Files table to work, it just stores the bytes. 
            if (blobProvider != BlobStorageBase.BuiltInProvider)
            {
                blobProvider.Allocate(ctx);

                using (var stream = blobProvider.GetStreamForWrite(ctx))
                    value.Stream?.CopyTo(stream);

                value.BlobProviderName = ctx.Provider.GetType().FullName;
                value.BlobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);
            }

            using (var dctx = new MsSqlDataContext())
            {
                var sql = isNewNode ? InsertBinaryPropertyScript : DeleteAndInsertBinaryPropertyScript;
                dctx.ExecuteReaderAsync/*UNDONE*/(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        dctx.CreateParameter("@VersionId", DbType.Int32, versionId != 0 ? (object)versionId : DBNull.Value),
                        dctx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyTypeId != 0 ? (object)propertyTypeId : DBNull.Value),
                        dctx.CreateParameter("@ContentType", DbType.String, 450, value.ContentType),
                        dctx.CreateParameter("@FileNameWithoutExtension", DbType.String, 450, value.FileName.FileNameWithoutExtension == null ? DBNull.Value : (object)value.FileName.FileNameWithoutExtension),
                        dctx.CreateParameter("@Extension", DbType.String, 50, ValidateExtension(value.FileName.Extension)),
                        dctx.CreateParameter("@Size", DbType.Int64, Math.Max(0, value.Size)),
                        dctx.CreateParameter("@BlobProvider", DbType.String, 450, value.BlobProviderName != null ? (object)value.BlobProviderName : DBNull.Value),
                        dctx.CreateParameter("@BlobProviderData", DbType.String, int.MaxValue, value.BlobProviderData != null ? (object)value.BlobProviderData : DBNull.Value),
                        dctx.CreateParameter("@Checksum", DbType.AnsiString, 200, value.Checksum != null ? (object)value.Checksum : DBNull.Value),
                    });
                }, (reader, cancel) =>
                {
                    if (reader.Read())
                    {
                        value.Id = Convert.ToInt32(reader[0]);
                        value.FileId = Convert.ToInt32(reader[1]);
                        value.Timestamp = Utility.Convert.BytesToLong((byte[])reader.GetValue(2));
                    }
                    return Task.FromResult(true);
                }).Wait();
            }

            // The BuiltIn blob provider saves the stream after the record 
            // was saved into the Files table, because simple varbinary
            // column must exist before we can write a stream into the record.
            // ReSharper disable once InvertIf
            if (blobProvider == BlobStorageBase.BuiltInProvider && value.Stream != null)
            {
                ctx.FileId = value.FileId;
                ctx.BlobProviderData = new BuiltinBlobProviderData();

                BuiltInBlobProvider.AddStream(ctx, value.Stream);
            }
        }

        /// <summary>
        /// Inserts a new binary record into the metadata database containing an already exising file id,
        /// removing the previous record if the content is not new.
        /// </summary>
        /// <param name="value">Binary data to insert.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="isNewNode">Whether this value belongs to a new or an existing node.</param>
        public void InsertBinaryPropertyWithFileId(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode)
        {
            var sql = isNewNode ? InsertBinaryPropertyWithKnownFileIdScript : DeleteAndInsertBinaryPropertyWithKnownFileIdScript;
            using (var ctx = new MsSqlDataContext())
            {
                value.Id = (int) ctx.ExecuteScalarAsync/*UNDONE*/(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@VersionId", DbType.Int32, versionId != 0 ? (object) versionId : DBNull.Value),
                        ctx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyTypeId != 0 ? (object) propertyTypeId : DBNull.Value),
                        ctx.CreateParameter("@FileId", DbType.Int32, value.FileId),
                    });
                }).Result;
            }
        }

        /// <summary>
        /// Updates an existing binary property value in the database and the blob storage.
        /// </summary>
        /// <param name="blobProvider">Blob storage provider.</param>
        /// <param name="value">Binary data to update.</param>
        public void UpdateBinaryProperty(IBlobProvider blobProvider, BinaryDataValue value)
        {
            var streamLength = value.Stream?.Length ?? 0;
            var isExternal = false;
            if (blobProvider != BlobStorageBase.BuiltInProvider)
            {
                // BlobProviderData parameter is irrelevant because it will be overridden in the Allocate method
                var ctx = new BlobStorageContext(blobProvider)
                {
                    VersionId = 0,
                    PropertyTypeId = 0,
                    FileId = value.FileId,
                    Length = streamLength,
                };

                blobProvider.Allocate(ctx);
                isExternal = true;

                value.BlobProviderName = ctx.Provider.GetType().FullName;
                value.BlobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);
            }
            else
            {
                value.BlobProviderName = null;
                value.BlobProviderData = null;
            }

            if (blobProvider == BlobStorageBase.BuiltInProvider)
            {
                // MS-SQL does not support stream size over [Int32.MaxValue].
                if (streamLength > int.MaxValue)
                    throw new NotSupportedException();
            }

            var isRepositoryStream = value.Stream is RepositoryStream;
            var hasStream = isRepositoryStream || value.Stream is MemoryStream;
            if (!isExternal && !hasStream)
                // do not do any database operation if the stream is not modified
                return;

            using (var dctx = new MsSqlDataContext())
            {
                var sql = blobProvider == BlobStorageBase.BuiltInProvider
                    ? UpdateBinaryPropertyScript
                    : UpdateBinaryPropertyNewFilerowScript;
                var fileId = (int)dctx.ExecuteScalarAsync/*UNDONE*/(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        dctx.CreateParameter("@BinaryPropertyId", DbType.Int32, value.Id),
                        dctx.CreateParameter("@ContentType", DbType.String, 450, value.ContentType),
                        dctx.CreateParameter("@FileNameWithoutExtension", DbType.String, 450, value.FileName.FileNameWithoutExtension == null ? DBNull.Value : (object)value.FileName.FileNameWithoutExtension),
                        dctx.CreateParameter("@Extension", DbType.String, 50, ValidateExtension(value.FileName.Extension)),
                        dctx.CreateParameter("@Size", DbType.Int64, value.Size),
                        dctx.CreateParameter("@Checksum", DbType.AnsiString, 200, value.Checksum != null ? (object)value.Checksum : DBNull.Value),
                        dctx.CreateParameter("@BlobProvider", DbType.String, 450, value.BlobProviderName != null ? (object)value.BlobProviderName : DBNull.Value),
                        dctx.CreateParameter("@BlobProviderData", DbType.String, int.MaxValue, value.BlobProviderData != null ? (object)value.BlobProviderData : DBNull.Value),
                    });
                }).Result;

                if (fileId > 0 && fileId != value.FileId)
                    value.FileId = fileId;
            }

            if (blobProvider == BlobStorageBase.BuiltInProvider)
            {
                // Stream exists and is loaded -> write it
                var ctx = new BlobStorageContext(blobProvider, value.BlobProviderData)
                {
                    VersionId = 0,
                    PropertyTypeId = 0,
                    FileId = value.FileId,
                    Length = streamLength,
                    BlobProviderData = new BuiltinBlobProviderData()
                };

                BuiltInBlobProvider.UpdateStream(ctx, value.Stream);
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

                using (var stream = blobProvider.GetStreamForWrite(ctx))
                    value.Stream?.CopyTo(stream);
            }
        }

        /// <summary>
        /// Deletes a binary property value from the metadata database, making the corresponding blbo storage entry orphaned.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        public void DeleteBinaryProperty(int versionId, int propertyTypeId)
        {
            using (var ctx = new MsSqlDataContext())
            {
                ctx.ExecuteNonQueryAsync/*UNDONE*/(DeleteBinaryPropertyScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@VersionId", DbType.Int32, versionId),
                        ctx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyTypeId),
                    });
                }).Wait();
            }
        }

        public void DeleteBinaryProperties(IEnumerable<int> versionIds, SnDataContext dataContext = null)
        {
            void DeleteBinaryPropertiesLogic(IEnumerable<int> versionIdSet, MsSqlDataContext ctx)
            {
                var idsParam = string.Join(",", versionIdSet.Select(x => x.ToString()));
                ctx.ExecuteNonQueryAsync/*UNDONE*/(DeleteBinaryPropertiesScript, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@VersionIds", DbType.String, idsParam.Length, idsParam));
                })
                    .Wait();
            }

            if (dataContext == null)
            {
                using (var ctx = new MsSqlDataContext())
                    DeleteBinaryPropertiesLogic(versionIds, ctx);
            }
            else
            {
                DeleteBinaryPropertiesLogic(versionIds, (MsSqlDataContext)dataContext);
            }
        }

        public BinaryDataValue LoadBinaryProperty(int versionId, int propertyTypeId, SnDataContext dataContext = null)
        {
            async Task<BinaryDataValue> LoadBinaryPropertyLogic(MsSqlDataContext ctx)
            {
                return await ctx.ExecuteReaderAsync(LoadBinaryPropertyScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@VersionId", DbType.Int32, versionId),
                        ctx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyTypeId),
                    });
                }, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    if (!await reader.ReadAsync(cancel))
                        return null;

                    var size = reader.GetInt64("Size");
                    var binaryPropertyId = reader.GetInt32("BinaryPropertyId");
                    var fileId = reader.GetInt32("FileId");
                    var providerName = reader.GetSafeString("BlobProvider");
                    var providerTextData = reader.GetSafeString("BlobProviderData");
                    var provider = BlobStorageBase.GetProvider(providerName);
                    var context = new BlobStorageContext(provider, providerTextData)
                    {
                        VersionId = versionId,
                        PropertyTypeId = propertyTypeId,
                        FileId = fileId,
                        Length = size
                    };
                    Stream stream = null;
                    if (provider == BlobStorageBase.BuiltInProvider)
                    {
                        context.BlobProviderData = new BuiltinBlobProviderData();
                        var streamIndex = reader.GetOrdinal("Stream");
                        if (!reader.IsDBNull(streamIndex))
                        {
                            var rawData = (byte[])reader.GetValue(streamIndex);
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
                });
            }

            if (dataContext == null)
                using (var ctx = new MsSqlDataContext(CancellationToken.None)) //UNDONE:DB: CancellationToken.None
                    return LoadBinaryPropertyLogic(ctx).Result;

            if (!(dataContext is MsSqlDataContext sqlCtx))
                throw new PlatformNotSupportedException();
            return LoadBinaryPropertyLogic(sqlCtx).Result;
        }

        /// <summary>
        /// Loads a cache item into memory that either contains the raw binary (if its size fits into the limit) or
        /// just the blob metadata pointing to the blob storage.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        public BinaryCacheEntity LoadBinaryCacheEntity(int versionId, int propertyTypeId)
        {
            using (var ctx = new MsSqlDataContext())
            {
                return ctx.ExecuteReaderAsync/*UNDONE*/(LoadBinaryCacheEntityScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@MaxSize", DbType.Int32, BlobStorage.BinaryCacheSize),
                        ctx.CreateParameter("@VersionId", DbType.Int32, versionId),
                        ctx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyTypeId),
                    });
                }, (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    if (!reader.HasRows || !reader.Read())
                        return null;

                    var length = reader.GetInt64(0);
                    var binaryPropertyId = reader.GetInt32(1);
                    var fileId = reader.GetInt32(2);

                    var providerName = reader.GetSafeString(3);
                    var providerTextData = reader.GetSafeString(4);

                    byte[] rawData = null;

                    var provider = BlobStorageBase.GetProvider(providerName);
                    var context = new BlobStorageContext(provider, providerTextData) { VersionId = versionId, PropertyTypeId = propertyTypeId, FileId = fileId, Length = length };
                    if (provider == BlobStorageBase.BuiltInProvider)
                    {
                        context.BlobProviderData = new BuiltinBlobProviderData();
                        if (!reader.IsDBNull(5))
                            rawData = (byte[])reader.GetValue(5);
                    }

                    return Task.FromResult(new BinaryCacheEntity
                    {
                        Length = length,
                        RawData = rawData,
                        BinaryPropertyId = binaryPropertyId,
                        FileId = fileId,
                        Context = context
                    });

                }).Result;
            }
        }

        /// <summary>
        /// Starts a chunked save operation on an existing content. It does not write any binary data 
        /// to the storage, it only makes prerequisite operations - e.g. allocates a new slot in the storage.
        /// </summary>
        /// <param name="blobProvider">Blob storage provider.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="fullSize">Full size (stream length) of the binary value.</param>
        /// <returns>A token containing all the information (db record ids) that identify a single entry in the blob storage.</returns>
        public string StartChunk(IBlobProvider blobProvider, int versionId, int propertyTypeId, long fullSize)
        {
            var ctx = new BlobStorageContext(blobProvider) { VersionId = versionId, PropertyTypeId = propertyTypeId, FileId = 0, Length = fullSize };
            string blobProviderName = null;
            string blobProviderData = null;
            if (blobProvider != BlobStorageBase.BuiltInProvider)
            {
                blobProvider.Allocate(ctx);
                blobProviderName = blobProvider.GetType().FullName;
                blobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);
            }
            try
            {
                using (var dctx = new MsSqlDataContext())
                {
                    using (var transaction = dctx.BeginTransaction())
                    {
                        var result = dctx.ExecuteReaderAsync/*UNDONE*/(InsertStagingBinaryScript, cmd =>
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
                            if (await reader.ReadAsync(cancel))
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
                        }).Result;
                        transaction.Commit();
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error during saving binary chunk to SQL Server.", ex);
            }
        }

        /// <summary>
        /// Finalizes a chunked save operation.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="fileId">File identifier.</param>
        /// <param name="fullSize">Full size (stream length) of the binary value.</param>
        /// <param name="source">Binary data containing metadata (e.g. content type).</param>
        public void CommitChunk(int versionId, int propertyTypeId, int fileId, long fullSize, BinaryDataValue source = null)
        {
            try
            {
                using (var ctx = new MsSqlDataContext())
                {
                    using (var transaction = ctx.BeginTransaction())
                    {
                        ctx.ExecuteNonQueryAsync/*UNDONE*/(CommitChunkScript, cmd =>
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
                        }).Wait();
                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataException("Error during committing binary chunk to file stream.", ex);
            }
        }

        /// <summary>
        /// Marks orphaned file records (the ones that do not have a referencing binary record anymore) as Deleted.
        /// </summary>
        public void CleanupFilesSetDeleteFlag()
        {
            using (var ctx = new MsSqlDataContext())
            {
                using (var transaction = ctx.BeginTransaction())
                {
                    try
                    {
                        ctx.ExecuteNonQueryAsync/*UNDONE*/(CleanupFileSetIsdeletedScript).Wait();
                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        throw new DataException("Error during setting deleted flag on files.", e);
                    }
                }
            }
        }

        /// <summary>
        /// Deletes file records that are marked as deleted from the metadata database and also from the blob storage.
        /// </summary>
        /// <returns>Whether there was at least one row that was deleted.</returns>
        public bool CleanupFiles()
        {
            using (var dctx = new MsSqlDataContext())
            {
                return dctx.ExecuteReaderAsync/*UNDONE*/(CleanupFileScript, (reader, cancel) =>
                {
                    try
                    {
                        var deleted = false;
                        var fileId = 0;
                        var size = 0L;
                        string providerName = null;
                        string providerData = null;
                        // We do not care about the number of deleted rows, 
                        // we only want to know if a row was deleted or not.
                        if (reader.Read())
                        {
                            deleted = true;
                            fileId = reader.GetSafeInt32(reader.GetOrdinal("FileId"));
                            size = reader.GetSafeInt64(reader.GetOrdinal("Size"));
                            providerName = reader.GetSafeString(reader.GetOrdinal("BlobProvider"));
                            providerData = reader.GetSafeString(reader.GetOrdinal("BlobProviderData"));
                        }

                        // delete bytes from the blob storage
                        var provider = BlobStorageBase.GetProvider(providerName);
                        var ctx = new BlobStorageContext(provider, providerData) { VersionId = 0, PropertyTypeId = 0, FileId = fileId, Length = size };

                        ctx.Provider.Delete(ctx);

                        return Task.FromResult(deleted);
                    }
                    catch (Exception ex)
                    {
                        throw new DataException("Error during binary cleanup.", ex);
                    }
                }).Result;
            }
        }

    }
}
