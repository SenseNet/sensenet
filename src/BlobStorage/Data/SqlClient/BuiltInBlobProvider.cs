using System;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Threading.Tasks;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    /// <summary>
    /// The built-in provider is responsible for saving bytes directly 
    /// to the Files table (varbinary or SQL filestream column). This
    /// provider cannot be removed or replaced by an external provider.
    /// </summary>
    internal class BuiltInBlobProvider : IBlobProvider
    {
        public object ParseData(string providerData)
        {
            return BlobStorageContext.DeserializeBlobProviderData<BuiltinBlobProviderData>(providerData);
        }

        public void Allocate(BlobStorageContext context)
        {
            // Never used in our algorithms.
            throw new NotSupportedException();
        }

        internal static void AddStream(BlobStorageContext context, Stream stream)
        {
            SqlProcedure cmd = null;
            try
            {
                // We have to work with an integer since SQL does not support
                // binary values bigger than [Int32.MaxValue].
                var streamSize = Convert.ToInt32(stream.Length);

                cmd = new SqlProcedure { CommandText = "proc_BinaryProperty_WriteStream" };
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = context.FileId;

                var offsetParameter = cmd.Parameters.Add("@Offset", SqlDbType.Int);
                var valueParameter = cmd.Parameters.Add("@Value", SqlDbType.VarBinary, streamSize);

                var offset = 0;
                byte[] buffer = null;
                stream.Seek(0, SeekOrigin.Begin);

                // The 'while' loop is misleading here, because we write the whole
                // stream at once. Bigger files should go to another blob provider.
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

        internal static void UpdateStream(BlobStorageContext context, Stream stream)
        {
            var fileId = context.FileId;

            SqlProcedure cmd = null;
            try
            {
                // We have to work with an integer since SQL does not support
                // binary values bigger than [Int32.MaxValue].
                var streamSize = Convert.ToInt32(stream.Length);

                cmd = new SqlProcedure { CommandText = "proc_BinaryProperty_WriteStream" };
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = fileId;

                var offsetParameter = cmd.Parameters.Add("@Offset", SqlDbType.Int);
                var valueParameter = cmd.Parameters.Add("@Value", SqlDbType.VarBinary, streamSize);

                var offset = 0;
                byte[] buffer = null;
                stream.Seek(0, SeekOrigin.Begin);

                // The 'while' loop is misleading here, because we write the whole
                // stream at once. Bigger files should go to another blob provider.
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

        public Stream GetStreamForRead(BlobStorageContext context)
        {
            return new RepositoryStream(context.FileId, context.Length);
        }

        public Stream CloneStream(BlobStorageContext context, Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var repoStream = stream as RepositoryStream;
            if (repoStream != null)
                return new RepositoryStream(repoStream.FileId, repoStream.Length);

            throw new InvalidOperationException("Unknown stream type: " + stream.GetType().Name);
        }

        public void Delete(BlobStorageContext context)
        {
            // do nothing
        }

        #region LoadBinaryFragmentScript

        private const string LoadBinaryFragmentScript = @"SELECT SUBSTRING([Stream], @Position, @Count) FROM dbo.Files WHERE FileId = @FileId";
        #endregion
        internal static byte[] ReadRandom(BlobStorageContext context, long offset, int count)
        {
            var commandText = LoadBinaryFragmentScript;

            byte[] result;

            using (var cmd = new SqlProcedure { CommandText = commandText })
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
            using (var cmd = GetWriteChunkToSqlProcedure(context, offset, buffer))
            {
                cmd.ExecuteNonQuery();
            }
        }
        public async Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer)
        {
            using (var cmd = GetWriteChunkToSqlProcedure(context, offset, buffer))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        #region UpdateStreamWriteChunkScript
        private static readonly string UpdateStreamWriteChunkScript = MsSqlBlobMetaDataProvider.UpdateStreamWriteChunkSecurityCheckScript + @"
-- init for .WRITE
UPDATE Files SET [Stream] = (CONVERT(varbinary, N'')) WHERE FileId = @FileId AND [Stream] IS NULL
-- fill to offset
DECLARE @StreamLength bigint
SELECT @StreamLength = DATALENGTH([Stream]) FROM Files WHERE FileId = @FileId
IF @StreamLength < @Offset
	UPDATE Files SET [Stream].WRITE(CONVERT( varbinary, REPLICATE(0x00, (@Offset - DATALENGTH([Stream])))), NULL, 0)
		WHERE FileId = @FileId
-- write payload
UPDATE Files SET [Stream].WRITE(@Data, @Offset, DATALENGTH(@Data)) WHERE FileId = @FileId";
        #endregion

        // ReSharper disable once SuggestBaseTypeForParameter
        private static SqlProcedure GetWriteChunkToSqlProcedure(BlobStorageContext context, long offset, byte[] buffer)
        {
            // This is a helper method to aid both the sync and async version of the write chunk operation.

            var cmd = new SqlProcedure { CommandText = UpdateStreamWriteChunkScript, CommandType = CommandType.Text };

            cmd.Parameters.Add("@FileId", SqlDbType.Int).Value = context.FileId;
            cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = context.VersionId;
            cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = context.PropertyTypeId;
            cmd.Parameters.Add("@Data", SqlDbType.VarBinary).Value = buffer;
            cmd.Parameters.Add("@Offset", SqlDbType.BigInt).Value = offset;

            return cmd;
        }

        /// <summary>
        /// THROWS NOTSUPPORTEDEXCEPTION
        /// </summary>
        public Stream GetStreamForWrite(BlobStorageContext context) //UNDONE: Test GetStreamForWrite: it is forbidden in this provider.
        {
            throw new NotSupportedException();
        }
    }
}
