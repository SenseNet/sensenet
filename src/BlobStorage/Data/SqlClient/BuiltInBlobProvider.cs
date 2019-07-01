using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using SenseNet.Common.Storage.Data;
using SenseNet.Common.Storage.Data.MsSqlClient;
using SenseNet.Configuration;
// ReSharper disable AccessToDisposedClosure
// ReSharper disable AccessToModifiedClosure

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    /// <summary>
    /// The built-in provider is responsible for saving bytes directly 
    /// to the Files table (varbinary column). This
    /// provider cannot be removed or replaced by an external provider.
    /// </summary>
    public class BuiltInBlobProvider : IBlobProvider
    {
        /// <inheritdoc />
        public object ParseData(string providerData)
        {
            return BlobStorageContext.DeserializeBlobProviderData<BuiltinBlobProviderData>(providerData);
        }

        /// <summary>
        /// Throws NotSupportedException. Our algorithms do not use this methon of this type.
        /// </summary>
        public void Allocate(BlobStorageContext context)
        {
            // Never used in our algorithms.
            throw new NotSupportedException();
        }

        private static readonly string AddStreamScript = @"-- BuiltInBlobProvider.AddStream
UPDATE Files SET Stream = @Value WHERE FileId = @Id;"; // proc_BinaryProperty_WriteStream

        /// <summary>
        /// DO NOT USE DIRECTLY THIS METHOD FROM YOUR CODE.
        /// Writes the stream in the appropriate row of the Files table specified by the context.
        /// </summary>
        public static void AddStream(BlobStorageContext context, Stream stream)
        {
            // We have to work with an integer since SQL does not support
            // binary values bigger than [Int32.MaxValue].
            var bufferSize = Convert.ToInt32(stream.Length);

            // Read bytes from the source
            var buffer = new byte[bufferSize];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(buffer, 0, bufferSize);

            using (var ctx = new MsSqlDataContext())
            {
                ctx.ExecuteNonQueryAsync(AddStreamScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Id", SqlDbType.Int, context.FileId),
                        ctx.CreateParameter("@Value", SqlDbType.VarBinary, bufferSize, buffer),
                    });
                }).Wait();
            }
        }

        /// <summary>
        /// DO NOT USE DIRECTLY THIS METHOD FROM YOUR CODE.
        /// Updates the stream in the appropriate row of the Files table specified by the context.
        /// </summary>
        public static void UpdateStream(BlobStorageContext context, Stream stream)
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

                if (stream.Length == 0)
                {
                    offsetParameter.Value = offset;
                    valueParameter.Value = new byte[0];
                    cmd.ExecuteNonQuery();
                }
                else
                {
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
            }
            finally
            {
                cmd?.Dispose();
            }
        }

        /// <inheritdoc />
        public Stream GetStreamForRead(BlobStorageContext context)
        {
            return new RepositoryStream(context.FileId, context.Length);
        }

        /// <inheritdoc />
        public Stream CloneStream(BlobStorageContext context, Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var repoStream = stream as RepositoryStream;
            if (repoStream != null)
                return new RepositoryStream(repoStream.FileId, repoStream.Length);

            throw new InvalidOperationException("Unknown stream type: " + stream.GetType().Name);
        }

        /// <inheritdoc />
        public void Delete(BlobStorageContext context)
        {
            // do nothing
        }

        #region LoadBinaryFragmentScript

        private const string LoadBinaryFragmentScript = @"SELECT SUBSTRING([Stream], @Position, @Count) FROM dbo.Files WHERE FileId = @FileId";
        #endregion
        internal static byte[] ReadRandom(BlobStorageContext context, long offset, int count)
        {
            using (var ctx = new MsSqlDataContext())
            {
                return (byte[])ctx.ExecuteScalarAsync(LoadBinaryFragmentScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@FileId", SqlDbType.Int, context.FileId),
                        ctx.CreateParameter("@Position", SqlDbType.BigInt, offset + 1),
                        ctx.CreateParameter("@Count", SqlDbType.Int, count),
                    });
                }).Result;
            }
        }

        /// <inheritdoc />
        public void Write(BlobStorageContext context, long offset, byte[] buffer)
        {
            WriteAsync(context, offset, buffer).Wait();
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

        /// <inheritdoc />
        public async Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer)
        {
            using (var ctx = new MsSqlDataContext())
            {
                await ctx.ExecuteNonQueryAsync(UpdateStreamWriteChunkScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@FileId", SqlDbType.Int, context.FileId),
                        ctx.CreateParameter("@VersionId", SqlDbType.Int, context.VersionId),
                        ctx.CreateParameter("@PropertyTypeId", SqlDbType.Int, context.PropertyTypeId),
                        ctx.CreateParameter("@Data", SqlDbType.VarBinary, buffer),
                        ctx.CreateParameter("@Offset", SqlDbType.BigInt, offset),
                    });

                });
            }
        }


        /// <summary>
        /// Throws NotSupportedException. Our algorithms do not use this methon of this type.
        /// </summary>
        public Stream GetStreamForWrite(BlobStorageContext context)
        {
            throw new NotSupportedException();
        }
    }
}
