using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.Tools;
using TransactionScope = SenseNet.ContentRepository.Storage.TransactionScope;

namespace SenseNet.MsSqlFsBlobProvider
{
    /// <summary>
    /// Contains the MS SQL-specific implementation of the IBlobStorageMetaDataProvider interface that
    /// is responsible for binary-related operations in the main metadata database.
    /// </summary>
    public class SqlFileStreamBlobMetaDataProvider : IBlobStorageMetaDataProvider
    {
        internal static bool IsBuiltInOrSqlFileStreamProvider(IBlobProvider provider)
        {
            return provider == BlobStorageBase.BuiltInProvider || provider is SqlFileStreamBlobProvider;
        }

        private static string ValidateExtension(string originalExtension)
        {
            return originalExtension.Length == 0
                ? string.Empty
                : string.Concat(".", originalExtension);
        }

        #region ClearFileStreamByFileIdScript, GetBlobContextDataFileStreamScript

        private const string GetBlobContextDataFileStreamScript = @"  SELECT Size, BlobProvider, BlobProviderData, FileStream.PathName() AS Path, GET_FILESTREAM_TRANSACTION_CONTEXT() AS TransactionContext
FROM  dbo.Files WHERE FileId = @FileId
";

        private const string ClearFileStreamByFileIdScript = @"UPDATE Files SET Stream = NULL, FileStream = CONVERT(varbinary, N'') WHERE FileId = @FileId AND FileStream IS NULL;
";

        #endregion

        /// <summary>
        /// Returns a context object that holds MsSql-specific data (e.g. FileStream info) for blob storage operations.
        /// </summary>
        /// <param name="fileId">File identifier.</param>
        /// <param name="clearStream">Whether the blob provider should clear the stream during assembling the context.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        public BlobStorageContext GetBlobStorageContext(int fileId, bool clearStream, int versionId, int propertyTypeId)
        {
            using (var cmd = GetBlobContextProcedure(fileId, clearStream, versionId, propertyTypeId))
            using (var reader = cmd.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SingleResult))
            if (reader.Read())
                return GetBlobStorageContextPrivate(reader, fileId, versionId, propertyTypeId);
            return null;
        }
        /// <summary>
        /// Returns a context object that holds MsSql-specific data (e.g. FileStream info) for blob storage operations.
        /// </summary>
        /// <param name="fileId">File identifier.</param>
        /// <param name="clearStream">Whether the blob provider should clear the stream during assembling the context.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        public async Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, bool clearStream, int versionId, int propertyTypeId)
        {
            using (var cmd = GetBlobContextProcedure(fileId, clearStream, versionId, propertyTypeId))
            using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SingleResult))
            if (await reader.ReadAsync())
                return GetBlobStorageContextPrivate(reader, fileId, versionId, propertyTypeId);
            return null;
        }

        private static SqlProcedure GetBlobContextProcedure(int fileId, bool clearStream, int versionId, int propertyTypeId)
        {
            // this is a helper method to aid both the sync and 
            // async version of the GetBlobContext operation

            var sql = GetBlobContextDataFileStreamScript;

            // add clear stream prefix of necessary
            if (clearStream)
                sql = ClearFileStreamByFileIdScript + sql;

            var cmd = new SqlProcedure {CommandText = sql, CommandType = CommandType.Text};

            cmd.Parameters.Add("@FileId", SqlDbType.Int).Value = fileId;

            // security check: the given fileid must belong to the given version id and propertytypeid
            if (clearStream)
            {
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyTypeId;
            }

            return cmd;
        }
        private static BlobStorageContext GetBlobStorageContextPrivate(SqlDataReader reader, int fileId, int versionId, int propertyTypeId)
        {
            // this is a helper method to aid both the sync and 
            // async version of the GetBlobContext operation

            var length = reader.GetSafeInt64(0);
            var providerName = reader.GetSafeString(1);
            var providerData = reader.GetSafeString(2);

            var fsData = new SqlFileStreamData
            {
                Path = reader.GetSafeString(3),
                TransactionContext = reader.GetSqlBytes(4).Buffer
            };
            var useFileStream = fsData.Path != null;

            var provider = BlobStorageBase.GetProvider(providerName);
            object blobProviderData;
            if (IsBuiltInOrSqlFileStreamProvider(provider))
            {
                if (useFileStream) // based on db column
                {
                    blobProviderData = new SqlFileStreamBlobProviderData {FileStreamData = fsData};
                    // Name of the SqlFS and BuiltIn are the same: null
                    //   so currently need to change to the SqlFS provider.
                    provider = BlobStorageBase.GetProvider(length);
                }
                else
                    blobProviderData = new BuiltinBlobProviderData();
            }
            else
            {
                blobProviderData = provider.ParseData(providerData);
            }

            return new BlobStorageContext(provider, providerData)
            {
                VersionId = versionId,
                PropertyTypeId = propertyTypeId,
                FileId = fileId,
                Length = length,
                BlobProviderData = blobProviderData
            };
        }

        #region DeleteBinaryPropertyScript, InsertBinaryPropertyScript, InsertBinaryPropertyFilestreamScript
        internal const string DeleteBinaryPropertyScript =
    @"DELETE BinaryProperties WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId
";

        private const string InsertBinaryPropertyScript = @"INSERT INTO Files" +
@" (ContentType, FileNameWithoutExtension, Extension, [Size], [BlobProvider], [BlobProviderData], [Checksum])
VALUES (@ContentType, @FileNameWithoutExtension, @Extension, @Size, @BlobProvider, @BlobProviderData,
	CASE @Size WHEN 0 THEN NULL ELSE @Checksum END);
DECLARE @FileId int; SELECT @FileId = @@IDENTITY;

INSERT INTO BinaryProperties (VersionId, PropertyTypeId, FileId) VALUES (@VersionId, @PropertyTypeId, @FileId);
DECLARE @BinPropId int; SELECT @BinPropId = @@IDENTITY;

SELECT @BinPropId, @FileId, [Timestamp] FROM Files WHERE FileId = @FileId;
";

        private const string InsertBinaryPropertyFilestreamScript = @"INSERT INTO Files" +
@" (ContentType, FileNameWithoutExtension, Extension, [Size], [BlobProvider], [BlobProviderData], [Checksum], [FileStream])
VALUES (@ContentType, @FileNameWithoutExtension, @Extension, @Size, @BlobProvider, @BlobProviderData, 
	CASE @Size WHEN 0 THEN NULL ELSE @Checksum END, CASE @Size WHEN 0 THEN NULL ELSE (0x) END);
DECLARE @FileId int; SELECT @FileId = @@IDENTITY;

INSERT INTO BinaryProperties (VersionId, PropertyTypeId, FileId) VALUES (@VersionId, @PropertyTypeId, @FileId);
DECLARE @BinPropId int; SELECT @BinPropId = @@IDENTITY;

SELECT @BinPropId, @FileId, [Timestamp], FileStream.PathName(), GET_FILESTREAM_TRANSACTION_CONTEXT() FROM Files WHERE FileId = @FileId;
";

        private const string DeleteAndInsertBinaryProperty = DeleteBinaryPropertyScript + InsertBinaryPropertyScript;
        private const string DeleteAndInsertBinaryPropertyFilestream = DeleteBinaryPropertyScript + InsertBinaryPropertyFilestreamScript;
        #endregion

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
            var useFileStream = SqlFileStreamBlobProvider.UseFileStream(blobProvider, streamLength);
            var ctx = new BlobStorageContext(blobProvider) { VersionId = versionId, PropertyTypeId = propertyTypeId, FileId = 0, Length = streamLength };

            // In case of an external provider allocate the place for bytes and
            // write the stream beforehand and get the generated provider data.
            // Note that the external provider does not need an existing record
            // in the Files table to work, it just stores the bytes. 
            if (!IsBuiltInOrSqlFileStreamProvider(blobProvider) && streamLength > 0)
            {
                    blobProvider.Allocate(ctx);

                using (var stream = blobProvider.GetStreamForWrite(ctx))
                    value.Stream?.CopyTo(stream);

                value.BlobProviderName = ctx.Provider.GetType().FullName;
                value.BlobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);
            }

            SqlProcedure cmd = null;
            SqlFileStreamData fileStreamData = null;
            try
            {
                cmd = useFileStream
                    ? new SqlProcedure { CommandText = isNewNode ? InsertBinaryPropertyFilestreamScript : DeleteAndInsertBinaryPropertyFilestream, CommandType = CommandType.Text }
                    : new SqlProcedure { CommandText = isNewNode ? InsertBinaryPropertyScript : DeleteAndInsertBinaryProperty, CommandType = CommandType.Text };

                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId != 0 ? (object)versionId : DBNull.Value;
                cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyTypeId != 0 ? (object)propertyTypeId : DBNull.Value;
                cmd.Parameters.Add("@ContentType", SqlDbType.NVarChar, 450).Value = value.ContentType;
                cmd.Parameters.Add("@FileNameWithoutExtension", SqlDbType.NVarChar, 450).Value = value.FileName.FileNameWithoutExtension == null ? DBNull.Value : (object)value.FileName.FileNameWithoutExtension;
                cmd.Parameters.Add("@Extension", SqlDbType.NVarChar, 50).Value = ValidateExtension(value.FileName.Extension);
                cmd.Parameters.Add("@Size", SqlDbType.BigInt).Value = Math.Max(0, value.Size);
                cmd.Parameters.Add("@BlobProvider", SqlDbType.NVarChar, 450).Value = value.BlobProviderName != null ? (object)value.BlobProviderName : DBNull.Value;
                cmd.Parameters.Add("@BlobProviderData", SqlDbType.NVarChar, int.MaxValue).Value = value.BlobProviderData != null ? (object)value.BlobProviderData : DBNull.Value;
                cmd.Parameters.Add("@Checksum", SqlDbType.VarChar, 200).Value = value.Checksum != null ? (object)value.Checksum : DBNull.Value;

                // insert binary and file rows and retrieve file path and transaction context for the Filestream column
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();

                    value.Id = Convert.ToInt32(reader[0]);
                    value.FileId = Convert.ToInt32(reader[1]);
                    value.Timestamp = Utility.Convert.BytesToLong((byte[])reader.GetValue(2));
                    if (useFileStream)
                    {
                        fileStreamData = new SqlFileStreamData
                        {
                            Path = reader.GetString(3),
                            TransactionContext = reader.GetSqlBytes(4).Buffer
                        };
                    }
                }

            }
            finally
            {
                cmd.Dispose();
            }

            // The BuiltIn blob provider saves the stream after the record 
            // was saved into the Files table, because simple varbinary
            // and sql filestream columns must exist before we can write a
            // stream into the record.
            // ReSharper disable once InvertIf
            if (blobProvider == BlobStorageBase.BuiltInProvider && value.Stream != null && value.Stream.Length > 0)
            {
                ctx.FileId = value.FileId;
                ctx.BlobProviderData = new SqlFileStreamBlobProviderData { FileStreamData = fileStreamData };

                BuiltInBlobProvider.AddStream(ctx, value.Stream);
            }
            else if (blobProvider is SqlFileStreamBlobProvider && value.Stream != null && value.Stream.Length > 0)
            {
                ctx.FileId = value.FileId;
                ctx.BlobProviderData = new SqlFileStreamBlobProviderData { FileStreamData = fileStreamData };

                SqlFileStreamBlobProvider.AddStream(ctx, value.Stream);
            }
        }

        #region InsertBinaryPropertyWithKnownFileIdScript
        private const string InsertBinaryPropertyWithKnownFileIdScript = @"INSERT INTO BinaryProperties
    (VersionId, PropertyTypeId, FileId) VALUES (@VersionId, @PropertyTypeId, @FileId)
SELECT CAST(@@IDENTITY AS int)
";
        private const string DeleteAndInsertBinaryPropertyWithKnownFileId = DeleteBinaryPropertyScript + InsertBinaryPropertyWithKnownFileIdScript;
        #endregion

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
            SqlProcedure cmd = null;
            int id;

            try
            {
                cmd = new SqlProcedure
                {
                    CommandText = isNewNode ? InsertBinaryPropertyWithKnownFileIdScript : DeleteAndInsertBinaryPropertyWithKnownFileId,
                    CommandType = CommandType.Text
                };
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId != 0 ? (object)versionId : DBNull.Value;
                cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyTypeId != 0 ? (object)propertyTypeId : DBNull.Value;
                cmd.Parameters.Add("@FileId", SqlDbType.Int).Value = value.FileId;
                id = (int)cmd.ExecuteScalar();
            }
            finally
            {
                cmd?.Dispose();
            }

            value.Id = id;
        }

        #region UpdateBinarypropertyNewFilerowFilestreamScript

        private const string UpdateBinarypropertyNewFilerowFilestreamScript = @"DECLARE @FileId int
INSERT INTO Files (ContentType, FileNameWithoutExtension, Extension, [Size], [BlobProvider], [BlobProviderData], [Checksum], [Stream], [FileStream])
    VALUES (@ContentType, @FileNameWithoutExtension, @Extension, @Size, @BlobProvider, @BlobProviderData,
		CASE WHEN (@Size <= 0) THEN NULL ELSE @Checksum END,
		NULL, CASE WHEN (@Size <= 0) THEN NULL ELSE CONVERT(varbinary, '') END)
SELECT @FileId = @@IDENTITY
UPDATE BinaryProperties SET FileId = @FileId WHERE BinaryPropertyId = @BinaryPropertyId
SELECT @FileId, FileStream.PathName(), GET_FILESTREAM_TRANSACTION_CONTEXT() FROM Files WHERE FileId = @FileId
";
        #endregion

        /// <summary>
        /// Updates an existing binary property value in the database and the blob storage.
        /// </summary>
        /// <param name="blobProvider">Blob storage provider.</param>
        /// <param name="value">Binary data to update.</param>
        public void UpdateBinaryProperty(IBlobProvider blobProvider, BinaryDataValue value)
        {
            var streamLength = value.Stream?.Length ?? 0;
            if (!IsBuiltInOrSqlFileStreamProvider(blobProvider) && streamLength > 0)
            {
                var ctx = new BlobStorageContext(blobProvider, value.BlobProviderData)
                {
                    VersionId = 0,
                    PropertyTypeId = 0,
                    FileId = value.FileId,
                    Length = streamLength,
                };

                blobProvider.Allocate(ctx);
                using (var stream = blobProvider.GetStreamForWrite(ctx))
                    value.Stream?.CopyTo(stream);

                value.BlobProviderName = ctx.Provider.GetType().FullName;
                value.BlobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);
            }
            else
            {
                value.BlobProviderName = null;
                value.BlobProviderData = null;
            }

            var isRepositoryStream = value.Stream is RepositoryStream || value.Stream is SenseNetSqlFileStream;
            var hasStream = isRepositoryStream || value.Stream is MemoryStream;
            if (!hasStream)
                // do not do any database operation if the stream is not modified
                return;

            SqlFileStreamData fileStreamData = null;
            SqlProcedure cmd = null;
            try
            {
                string sql;
                CommandType commandType;
                if (IsBuiltInOrSqlFileStreamProvider(blobProvider))
                {
                    commandType = CommandType.StoredProcedure;
                    sql = "proc_BinaryProperty_Update";
                }
                else
                {
                    commandType = CommandType.Text;
                    sql = UpdateBinarypropertyNewFilerowFilestreamScript;
                }

                cmd = new SqlProcedure { CommandText = sql, CommandType = commandType };
                cmd.Parameters.Add("@BinaryPropertyId", SqlDbType.Int).Value = value.Id;
                cmd.Parameters.Add("@ContentType", SqlDbType.NVarChar, 450).Value = value.ContentType;
                cmd.Parameters.Add("@FileNameWithoutExtension", SqlDbType.NVarChar, 450).Value = value.FileName.FileNameWithoutExtension == null ? DBNull.Value : (object)value.FileName.FileNameWithoutExtension;
                cmd.Parameters.Add("@Extension", SqlDbType.NVarChar, 50).Value = ValidateExtension(value.FileName.Extension);
                cmd.Parameters.Add("@Size", SqlDbType.BigInt).Value = value.Size;
                cmd.Parameters.Add("@Checksum", SqlDbType.VarChar, 200).Value = value.Checksum != null ? (object)value.Checksum : DBNull.Value;
                cmd.Parameters.Add("@BlobProvider", SqlDbType.NVarChar, 450).Value = value.BlobProviderName != null ? (object)value.BlobProviderName : DBNull.Value;
                cmd.Parameters.Add("@BlobProviderData", SqlDbType.NVarChar, int.MaxValue).Value = value.BlobProviderData != null ? (object)value.BlobProviderData : DBNull.Value;

                int fileId;
                string path;
                byte[] transactionContext;

                // Update row and retrieve file path and 
                // transaction context for the Filestream column
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();

                    fileId = reader.GetInt32(0);
                    path = reader.GetSafeString(1);
                    transactionContext = reader.IsDBNull(2) ? null : reader.GetSqlBytes(2).Buffer;
                }

                if (!string.IsNullOrEmpty(path))
                    fileStreamData = new SqlFileStreamData { Path = path, TransactionContext = transactionContext };

                if (fileId > 0 && fileId != value.FileId)
                    value.FileId = fileId;
            }
            finally
            {
                cmd?.Dispose();
            }

            // ReSharper disable once InvertIf
            if (blobProvider == BlobStorageBase.BuiltInProvider && !isRepositoryStream && streamLength > 0)
            {
                // Stream exists and is loaded -> write it
                var ctx = new BlobStorageContext(blobProvider, value.BlobProviderData)
                {
                    VersionId = 0,
                    PropertyTypeId = 0,
                    FileId = value.FileId,
                    Length = streamLength,
                    BlobProviderData = new SqlFileStreamBlobProviderData { FileStreamData = fileStreamData }
                };

                BuiltInBlobProvider.UpdateStream(ctx, value.Stream);
            }
            if (blobProvider is SqlFileStreamBlobProvider && !isRepositoryStream && streamLength > 0)
            {
                // Stream exists and is loaded -> write it
                var ctx = new BlobStorageContext(blobProvider, value.BlobProviderData)
                {
                    VersionId = 0,
                    PropertyTypeId = 0,
                    FileId = value.FileId,
                    Length = streamLength,
                    BlobProviderData = new SqlFileStreamBlobProviderData { FileStreamData = fileStreamData }
                };

                SqlFileStreamBlobProvider.UpdateStream(ctx, value.Stream);
            }
        }

        /// <summary>
        /// Deletes a binary property value from the metadata database, making the corresponding blbo storage entry orphaned.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        public void DeleteBinaryProperty(int versionId, int propertyTypeId)
        {
            SqlProcedure cmd = null;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_BinaryProperty_Delete" };
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyTypeId;
                cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd?.Dispose();
            }
        }

        #region LoadBinaryCacheentityFormatScript

        private const string LoadBinaryCacheentityFormatScript = @"SELECT F.Size, B.BinaryPropertyId, F.FileId, F.BlobProvider, F.BlobProviderData, CASE  WHEN Size < {0} AND F.FileStream IS NOT NULL THEN F.FileStream
                    WHEN Size < {0} AND F.FileStream IS NULL THEN F.Stream
		            ELSE null
	            END AS Stream,
                CASE
		            WHEN F.FileStream IS NULL THEN 0
		            ELSE 1
	            END AS UseFileStream,
                F.FileStream.PathName() AS Path,
                GET_FILESTREAM_TRANSACTION_CONTEXT() AS TransactionContext
            FROM dbo.BinaryProperties B
                JOIN Files F ON B.FileId = F.FileId
            WHERE B.VersionId = @VersionId AND B.PropertyTypeId = @PropertyTypeId AND F.Staging IS NULL";

        #endregion

        /// <summary>
        /// Loads a cache item into memory that either contains the raw binary (if its size fits into the limit) or
        /// just the blob metadata pointing to the blob storage.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        public BinaryCacheEntity LoadBinaryCacheEntity(int versionId, int propertyTypeId)
        {
            var commandText = string.Format(LoadBinaryCacheentityFormatScript, BlobStorage.BinaryCacheSize);
            using (var cmd = new SqlProcedure { CommandText = commandText })
            {
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyTypeId;
                cmd.CommandType = CommandType.Text;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SingleResult))
                {
                    if (!reader.HasRows || !reader.Read())
                        return null;

                    var length = reader.GetInt64(0);
                    var binaryPropertyId = reader.GetInt32(1);
                    var fileId = reader.GetInt32(2);

                    var providerName = reader.GetSafeString(3);
                    var providerTextData = reader.GetSafeString(4);

                    byte[] rawData;
                    if (reader.IsDBNull(5))
                        rawData = null;
                    else
                        rawData = (byte[])reader.GetValue(5);


                    SqlFileStreamData fileStreamData = null;
                    var useFileStream = reader.GetInt32(6) == 1;
                    if (useFileStream)
                    {
                        // fill Filestream info if we really need it
                        fileStreamData = new SqlFileStreamData
                        {
                            Path = reader.GetSafeString(7),
                            TransactionContext = reader.GetSqlBytes(8).Buffer
                        };
                    }

                    var provider = useFileStream
                        ? new SqlFileStreamBlobProvider()
                        : BlobStorageBase.GetProvider(providerName);

                    var context = new BlobStorageContext(provider, providerTextData)
                    {
                        VersionId = versionId,
                        PropertyTypeId = propertyTypeId,
                        FileId = fileId,
                        Length = length
                    };

                    if (provider == BlobStorageBase.BuiltInProvider)
                        context.BlobProviderData = new BuiltinBlobProviderData();
                    else if (provider is SqlFileStreamBlobProvider)
                        context.BlobProviderData = new SqlFileStreamBlobProviderData { FileStreamData = fileStreamData };

                    return new BinaryCacheEntity
                    {
                        Length = length,
                        RawData = rawData,
                        BinaryPropertyId = binaryPropertyId,
                        FileId = fileId,
                        Context = context
                    };
                }
            }
        }

        #region InsertStagingBinaryScript

        private const string InsertStagingBinaryScript = @"
DECLARE @ContentType varchar(50);
DECLARE @FileNameWithoutExtension varchar(450);
DECLARE @Extension varchar(50);
DECLARE @FileId int;

BEGIN TRAN

-- select existing stream metadata values
SELECT TOP(1) @ContentType = F.ContentType, @FileNameWithoutExtension = F.FileNameWithoutExtension, @Extension = F.Extension
FROM BinaryProperties B JOIN Files F ON B.FileId = F.FileId
WHERE B.VersionId = @VersionId AND B.PropertyTypeId = @PropertyTypeId;

-- no existing binary/file relation
IF (@ContentType IS NULL)
BEGIN
    SET @ContentType = '';
    SET @FileNameWithoutExtension = '';
    SET @Extension = '';
END

INSERT INTO Files ([ContentType],[FileNameWithoutExtension],[Extension],[Size],[Checksum],[CreationDate], [Staging], [StagingVersionId], [StagingPropertyTypeId], [BlobProvider], [BlobProviderData], [FileStream])
VALUES (@ContentType, @FileNameWithoutExtension, @Extension, @Size, NULL, GETUTCDATE(), 1, @VersionId, @PropertyTypeId, @BlobProvider, @BlobProviderData, CASE @UseSqlFileStream WHEN 0 THEN NULL ELSE (0x) END);

SET @FileId = @@IDENTITY;

-- lazy binary row creation
IF NOT EXISTS (SELECT 1 FROM BinaryProperties WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId)
BEGIN
    INSERT INTO BinaryProperties ([VersionId],[PropertyTypeId], [FileId])
    VALUES (@VersionId, @PropertyTypeId, @FileId);
END

SELECT BinaryPropertyId, @FileId
FROM BinaryProperties
WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId

COMMIT TRAN";

        #endregion

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
            var isLocalTransaction = !TransactionScope.IsActive;
            if (isLocalTransaction)
                TransactionScope.Begin();

            var ctx = new BlobStorageContext(blobProvider) { VersionId = versionId, PropertyTypeId = propertyTypeId, FileId = 0, Length = fullSize };
            string blobProviderName = null;
            string blobProviderData = null;
            bool useSqlFileStream;
            if (IsBuiltInOrSqlFileStreamProvider(blobProvider))
            {
                useSqlFileStream = SqlFileStreamBlobProvider.UseFileStream(blobProvider, fullSize);
            }
            else
            {
                useSqlFileStream = false;
                blobProvider.Allocate(ctx);
                blobProviderName = blobProvider.GetType().FullName;
                blobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);
            }

            try
            {
                using (var cmd = new SqlProcedure { CommandText = InsertStagingBinaryScript, CommandType = CommandType.Text })
                {
                    cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                    cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyTypeId;
                    cmd.Parameters.Add("@Size", SqlDbType.BigInt).Value = fullSize;
                    cmd.Parameters.Add("@UseSqlFileStream", SqlDbType.TinyInt).Value = useSqlFileStream ? 2 : 0;
                    cmd.Parameters.Add("@BlobProvider", SqlDbType.NVarChar, 450).Value = blobProviderName != null ? (object)blobProviderName : DBNull.Value;
                    cmd.Parameters.Add("@BlobProviderData", SqlDbType.NVarChar, int.MaxValue).Value = blobProviderData != null ? (object)blobProviderData : DBNull.Value;

                    int binaryPropertyId;
                    int fileId;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            binaryPropertyId = reader.GetSafeInt32(0);
                            fileId = reader.GetSafeInt32(1);
                        }
                        else
                        {
                            throw new DataException("File row could not be inserted.");
                        }
                    }

                    ctx.FileId = fileId;

                    return new ChunkToken
                    {
                        VersionId = versionId,
                        PropertyTypeId = propertyTypeId,
                        BinaryPropertyId = binaryPropertyId,
                        FileId = fileId
                    }.GetToken();
                }
            }
            catch (Exception ex)
            {
                if (isLocalTransaction && TransactionScope.IsActive)
                    TransactionScope.Rollback();

                throw new DataException("Error during saving binary chunk to SQL Server.", ex);
            }
            finally
            {
                if (isLocalTransaction && TransactionScope.IsActive)
                    TransactionScope.Commit();
            }
        }

        #region UpdateStreamWriteChunkSecurityCheckScript, CommitChunkScript
        internal static readonly string UpdateStreamWriteChunkSecurityCheckScript = @"
-- security check: if the versionid in the token matches the version that this staging row belongs to
IF NOT EXISTS (SELECT 1 FROM Files WHERE FileId = @FileId AND StagingVersionId = @VersionId AND StagingPropertyTypeId = @PropertyTypeId)
BEGIN
    RAISERROR (N'FileId and versionid and propertytypeid mismatch.', 12, 1);
END
";
        private static readonly string CommitChunkScript = UpdateStreamWriteChunkSecurityCheckScript +
@"UPDATE Files SET [Size] = @Size, [Checksum] = @Checksum, ContentType = @ContentType, FileNameWithoutExtension = @FileNameWithoutExtension, Extension = @Extension, Staging = NULL, StagingVersionId = NULL, StagingPropertyTypeId = NULL
    WHERE FileId = @FileId

UPDATE BinaryProperties SET FileId = @FileId
    WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId;";
        #endregion

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
            // start a new transaction here if needed
            var isLocalTransaction = !TransactionScope.IsActive;
            if (isLocalTransaction)
                TransactionScope.Begin();

            try
            {
                // commit the process: set the final full size and checksum
                using (var cmd = new SqlProcedure { CommandText = CommitChunkScript, CommandType = CommandType.Text })
                {
                    cmd.Parameters.Add("@FileId", SqlDbType.Int).Value = fileId;
                    cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                    cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyTypeId;
                    cmd.Parameters.Add("@Size", SqlDbType.BigInt).Value = fullSize;
                    cmd.Parameters.Add("@Checksum", SqlDbType.VarChar, 200).Value = DBNull.Value;

                    cmd.Parameters.Add("@ContentType", SqlDbType.NVarChar, 50).Value = source != null ? source.ContentType : string.Empty;
                    cmd.Parameters.Add("@FileNameWithoutExtension", SqlDbType.NVarChar, 450).Value = source != null
                        ? source.FileName.FileNameWithoutExtension == null
                            ? DBNull.Value
                            : (object)source.FileName.FileNameWithoutExtension
                        : DBNull.Value;

                    cmd.Parameters.Add("@Extension", SqlDbType.NVarChar, 50).Value = source != null ? ValidateExtension(source.FileName.Extension) : string.Empty;

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // rollback the transaction if it was opened locally
                if (isLocalTransaction && TransactionScope.IsActive)
                    TransactionScope.Rollback();

                throw new DataException("Error during committing binary chunk to file stream.", ex);
            }
            finally
            {
                // commit the transaction if it was opened locally
                if (isLocalTransaction && TransactionScope.IsActive)
                    TransactionScope.Commit();
            }
        }
        
        #region CleanupFileSetIsdeletedScript
        // this is supposed to be faster than using LEFT JOIN
        private const string CleanupFileSetIsdeletedScript = @"UPDATE [Files] SET IsDeleted = 1
WHERE [Staging] IS NULL AND CreationDate < DATEADD(minute, -30, GETUTCDATE()) AND FileId NOT IN (SELECT FileId FROM [BinaryProperties])";

        #endregion

        /// <summary>
        /// Marks orphaned file records (the ones that do not have a referencing binary record anymore) as Deleted.
        /// </summary>
        public void CleanupFilesSetDeleteFlag()
        {
            var isLocalTransaction = false;

            if (!TransactionScope.IsActive)
            {
                TransactionScope.Begin();
                isLocalTransaction = true;
            }

            try
            {
                using (var proc = new SqlProcedure { CommandText = CleanupFileSetIsdeletedScript, CommandType = CommandType.Text })
                {
                    proc.CommandType = CommandType.Text;
                    proc.ExecuteNonQuery();
                }

                if (isLocalTransaction && TransactionScope.IsActive)
                    TransactionScope.Commit();
            }
            catch (Exception ex)
            {
                if (isLocalTransaction && TransactionScope.IsActive)
                    TransactionScope.Rollback();

                throw new DataException("Error during setting deleted flag on files.", ex);
            }
        }
        
        #region CleanupFileScript
        private const string CleanupFileScript = @"DELETE TOP(1) FROM Files
OUTPUT DELETED.FileId, DELETED.Size, DELETED.BlobProvider, DELETED.BlobProviderData 
WHERE IsDeleted = 1";
        #endregion

        /// <summary>
        /// Deletes file records that are marked as deleted from the metadata database and also from the blob storage.
        /// </summary>
        /// <returns>Whether there was at least one row that was deleted.</returns>
        public bool CleanupFiles()
        {
            using (var proc = new SqlProcedure { CommandText = CleanupFileScript, CommandType = CommandType.Text })
            {
                proc.CommandType = CommandType.Text;

                try
                {
                    var deleted = false;
                    using (var reader = proc.ExecuteReader())
                    {
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
                        // Delete algorithm is same in the BuiltIn and SqlFs providers.
                        var provider = BlobStorageBase.GetProvider(providerName);
                        var ctx = new BlobStorageContext(provider, providerData) { VersionId = 0, PropertyTypeId = 0, FileId = fileId, Length = size };

                        ctx.Provider.Delete(ctx);
                    }
                    return deleted;
                }
                catch (Exception ex)
                {
                    throw new DataException("Error during binary cleanup.", ex);
                }
            }
        }
    }
}
