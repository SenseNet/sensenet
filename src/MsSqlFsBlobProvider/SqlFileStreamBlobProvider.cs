using System;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;

namespace SenseNet.MsSqlFsBlobProvider
{
    /// <summary>
    /// The built-in provider is responsible for saving bytes directly 
    /// to the Files table (varbinary or SQL filestream column). This
    /// provider cannot be removed or replaced by an external provider.
    /// </summary>
    internal class SqlFileStreamBlobProvider : IBlobProvider
    {
        internal static bool UseFileStream(IBlobProvider blobProvider, long fullSize)
        {
            if (!(blobProvider is SqlFileStreamBlobProvider))
                return false;
            return fullSize > BlobStorage.MinimumSizeForBlobProviderInBytes;
        }

        public object ParseData(string providerData)
        {
            return BlobStorageContext.DeserializeBlobProviderData<SqlFileStreamBlobProviderData>(providerData);
        }

        public void Allocate(BlobStorageContext context)
        {
            // do nothing
        }

        internal static void AddStream(BlobStorageContext context, Stream stream)
        {
            SqlFileStreamData fileStreamData = null;
            if (context.BlobProviderData is SqlFileStreamBlobProviderData providerData)
                fileStreamData = providerData.FileStreamData;

            if (!UseFileStream(context.Provider, stream.Length))
                throw new NotSupportedException("Assertion failed.");

            WriteSqlFileStream(stream, context.FileId, fileStreamData);
        }

        internal static void UpdateStream(BlobStorageContext context, Stream stream)
        {
            var fileId = context.FileId;
            var fileStreamData = ((SqlFileStreamBlobProviderData)context.BlobProviderData).FileStreamData;

            SqlProcedure cmd = null;
            try
            {
                // if possible, write the stream using the special Filestream technology
                if (UseFileStream(context.Provider, stream.Length))
                {
                    WriteSqlFileStream(stream, fileId, fileStreamData);
                    return;
                }

                // We have to work with an integer since SQL does not support
                // binary values bigger than [Int32.MaxValue].
                var streamSize = Convert.ToInt32(stream.Length);

                cmd = new SqlProcedure { CommandText = "proc_BinaryProperty_WriteStream" };
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = fileId;

                var offsetParameter = cmd.Parameters.Add("@Offset", SqlDbType.Int);
                var valueParameter = cmd.Parameters.Add("@Value", SqlDbType.VarBinary, streamSize);

                cmd.Parameters.Add("@UseFileStream", SqlDbType.TinyInt).Value = false;

                var offset = 0;
                byte[] buffer = null;
                stream.Seek(0, SeekOrigin.Begin);

                // The 'while' loop is misleading here, because we write the whole
                // stream at once. Bigger files should go to the Filestream
                // column anyway.
                while (offset < streamSize)
                {
                    // Buffer size may be less at the end os the stream than the limit
                    var bufferSize = streamSize - offset;

                    if (buffer == null || buffer.Length != bufferSize)
                        buffer = new byte[bufferSize];

                    // Read bytes from the source
                    stream.Read(buffer, 0, bufferSize);

                    offsetParameter.Value = offset;
                    valueParameter.Value = buffer;

                    // Write full stream
                    cmd.ExecuteNonQuery();

                    offset += bufferSize;
                }
            }
            finally
            {
                cmd?.Dispose();
            }
        }

        #region UpdateBinaryPropertyFileStreamScript
        private const string UpdateBinaryPropertyFileStreamScript = @"UPDATE Files SET Stream = NULL WHERE FileId = @Id;
    SELECT FileStream.PathName(), GET_FILESTREAM_TRANSACTION_CONTEXT() 
    FROM Files WHERE [FileId] = @Id
";
        #endregion
        private static void WriteSqlFileStream(Stream stream, int fileId, SqlFileStreamData fileStreamData = null)
        {
            SqlProcedure cmd = null;

            try
            {
                // if we did not receive a path and transaction context, retrieve it now from the database
                if (fileStreamData == null)
                {
                    cmd = new SqlProcedure { CommandText = UpdateBinaryPropertyFileStreamScript, CommandType = CommandType.Text };
                    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = fileId;

                    string path;
                    byte[] transactionContext;

                    // Set Stream column to NULL and retrieve file path and 
                    // transaction context for the Filestream column.
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();

                        path = reader.GetString(0);
                        transactionContext = reader.GetSqlBytes(1).Buffer;
                    }

                    fileStreamData = new SqlFileStreamData { Path = path, TransactionContext = transactionContext };
                }

                stream.Seek(0, SeekOrigin.Begin);

                using (var fs = new SqlFileStream(fileStreamData.Path, fileStreamData.TransactionContext, FileAccess.Write))
                {
                    // default buffer size is 4096
                    stream.CopyTo(fs);
                }
            }
            finally
            {
                cmd?.Dispose();
            }
        }

        public Stream GetStreamForRead(BlobStorageContext context)
        {
            var data = (SqlFileStreamBlobProviderData)context.BlobProviderData;
            return new SenseNetSqlFileStream(context.Length, context.FileId, data.FileStreamData);
        }

        public Stream CloneStream(BlobStorageContext context, Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var repoStream = stream as RepositoryStream;
            if (repoStream != null)
                return new RepositoryStream(repoStream.FileId, repoStream.Length);

            var snFileStream = stream as SenseNetSqlFileStream;
            if (snFileStream != null)
                return new SenseNetSqlFileStream(snFileStream.Length, snFileStream.FileId);

            throw new InvalidOperationException("Unknown stream type: " + stream.GetType().Name);
        }

        public void Delete(BlobStorageContext context)
        {
            // do nothing
        }

        #region LoadBinaryFragmentFilestreamScript

        private const string LoadBinaryFragmentFilestreamScript = @"SELECT 
	            CASE WHEN FileStream IS NULL
                    THEN SUBSTRING([Stream], @Position, @Count)
                    ELSE SUBSTRING([FileStream], @Position, @Count)
                END AS Stream
            FROM dbo.Files
            WHERE FileId = @FileId";

        #endregion
        internal static byte[] ReadRandom(BlobStorageContext context, long offset, int count)
        {
            byte[] result;

            using (var cmd = new SqlProcedure { CommandText = LoadBinaryFragmentFilestreamScript })
            {
                cmd.Parameters.Add("@FileId", SqlDbType.Int).Value = context.FileId;
                cmd.Parameters.Add("@Position", SqlDbType.BigInt).Value = offset + 1;
                cmd.Parameters.Add("@Count", SqlDbType.Int).Value = count;
                cmd.CommandType = CommandType.Text;

                result = (byte[])cmd.ExecuteScalar();
            }

            return result;
        }

        public void Write(BlobStorageContext context, long offset, byte[] buffer)
        {
            if (UseFileStream(context.Provider, context.Length))
                WriteChunkToFilestream(context, offset, buffer);
            else
                WriteChunkToSql(context, offset, buffer);
        }
        public async Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer)
        {
            if (UseFileStream(context.Provider, context.Length))
                await WriteChunkToFilestreamAsync(context, offset, buffer);
            else
                await WriteChunkToSqlAsync(context, offset, buffer);
        }
        private static void WriteChunkToFilestream(BlobStorageContext context, long offset, byte[] buffer)
        {
            using (var fs = GetAndExtendFileStream(context, offset))
            {
                // no offset is needed here, the stream is already at the correct position
                fs.Write(buffer, 0, buffer.Length);
            }
        }
        private static async Task WriteChunkToFilestreamAsync(BlobStorageContext context, long offset, byte[] buffer)
        {
            using (var fs = GetAndExtendFileStream(context, offset))
            {
                // no offset is needed here, the stream is already at the correct position
                await fs.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// Loads a SqlFileStream object for the binary in the provided context and sets it to the required position. 
        /// If the filestream is shorter than the required offset, it extends the stream with empty bytes.
        /// </summary>
        private static SqlFileStream GetAndExtendFileStream(BlobStorageContext context, long offset)
        {
            var fsd = ((SqlFileStreamBlobProviderData)context.BlobProviderData).FileStreamData;
            if (fsd == null)
                throw new InvalidOperationException("File row not found. FileId: " + context.FileId);

            var fs = new SqlFileStream(fsd.Path, fsd.TransactionContext, FileAccess.ReadWrite, FileOptions.SequentialScan, 0);

            // if the current stream is smaller than the position where we want to write the bytes
            if (fs.Length < offset)
            {
                // go to the end of the existing stream
                fs.Seek(0, SeekOrigin.End);

                // calculate the size of the gap (warning: fs.Length changes during the write below!)
                var gapSize = offset - fs.Length;

                // fill the gap with empty bytes (one-by-one, because this gap could be huge)
                for (var i = 0; i < gapSize; i++)
                {
                    fs.WriteByte(0x00);
                }
            }
            else if (offset > 0)
            {
                // otherwise we will append to the end or overwrite existing bytes
                fs.Seek(offset, SeekOrigin.Begin);
            }

            return fs;
        }

        #region UpdateStreamWriteChunkFsScript
        private static readonly string UpdateStreamWriteChunkFsScript = SqlFileStreamBlobMetaDataProvider.UpdateStreamWriteChunkSecurityCheckScript + @"
-- init for .WRITE
UPDATE Files SET [Stream] = (CONVERT(varbinary, N'')) WHERE FileId = @FileId AND [Stream] IS NULL
-- fill to offset
DECLARE @StreamLength bigint
SELECT @StreamLength = DATALENGTH([Stream]) FROM Files WHERE FileId = @FileId
IF @StreamLength < @Offset
	UPDATE Files SET [Stream].WRITE(CONVERT( varbinary, REPLICATE(0x00, (@Offset - DATALENGTH([Stream])))), NULL, 0)
		WHERE FileId = @FileId
-- write payload
UPDATE Files SET [Stream].WRITE(@Data, @Offset, DATALENGTH(@Data)), [FileStream] = NULL WHERE FileId = @FileId";

        #endregion

        private static void WriteChunkToSql(BlobStorageContext context, long offset, byte[] buffer)
        {
            using (var cmd = GetWriteChunkToSqlProcedure(context, offset, buffer))
            {
                cmd.ExecuteNonQuery();
            }
        }
        private static async Task WriteChunkToSqlAsync(BlobStorageContext context, long offset, byte[] buffer)
        {
            using (var cmd = GetWriteChunkToSqlProcedure(context, offset, buffer))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }
        // ReSharper disable once SuggestBaseTypeForParameter
        private static SqlProcedure GetWriteChunkToSqlProcedure(BlobStorageContext context, long offset, byte[] buffer)
        {
            // This is a helper method to aid both the sync and async version of the write chunk operation.

            var cmd = new SqlProcedure { CommandText = UpdateStreamWriteChunkFsScript, CommandType = CommandType.Text };

            cmd.Parameters.Add("@FileId", SqlDbType.Int).Value = context.FileId;
            cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = context.VersionId;
            cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = context.PropertyTypeId;
            cmd.Parameters.Add("@Data", SqlDbType.VarBinary).Value = buffer;
            cmd.Parameters.Add("@Offset", SqlDbType.BigInt).Value = offset;

            return cmd;
        }

        public Stream GetStreamForWrite(BlobStorageContext context)
        {
            if (!UseFileStream(context.Provider, context.Length))
                throw new NotSupportedException();

            var fsd = ((SqlFileStreamBlobProviderData)context.BlobProviderData).FileStreamData;
            if (fsd == null)
                throw new InvalidOperationException("File row not found. FileId: " + context.FileId);

            return new SqlFileStream(fsd.Path, fsd.TransactionContext, FileAccess.ReadWrite,
                FileOptions.SequentialScan, 0);
        }
    }
}
