﻿using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Tools;
// ReSharper disable AccessToDisposedClosure
// ReSharper disable AccessToModifiedClosure

namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    /// <summary>
    /// Interface for pointing out the special built-in blob provider implementation.
    /// </summary>
    public interface IBuiltInBlobProvider : IBlobProvider
    {
        IBlobStorage BlobStorage { get; set; }
        byte[] ReadRandom(BlobStorageContext context, long offset, int count);
    }

    /// <summary>
    /// The built-in provider is responsible for saving bytes directly 
    /// to the Files table (varbinary column). This
    /// provider cannot be removed or replaced by an external provider.
    /// </summary>
    public class BuiltInBlobProvider : IBuiltInBlobProvider
    {
        private readonly IRetrier _retrier;

        protected DataOptions DataOptions { get; }
        // Do not make readonly this field because tests write her.
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private string _connectionString;

        // This property injection is a workaround for the service circular reference caused
        // by the built-in blob provider. It requires a BlobStorage instance to be able to
        // create RepositoryStream instances.
        // Ideally blob provider instances should not need a backreference to BlobStorage.
        public IBlobStorage BlobStorage { get; set; }

        public BuiltInBlobProvider(IOptions<DataOptions> options, IOptions<ConnectionStringOptions> connectionOptions, IRetrier retrier)
        {
            _retrier = retrier;
            DataOptions = options?.Value ?? new DataOptions();
            _connectionString = connectionOptions?.Value.Repository;
        }

        /// <inheritdoc />
        public object ParseData(string providerData)
        {
            return BlobStorageContext.DeserializeBlobProviderData<BuiltinBlobProviderData>(providerData);
        }

        /// <summary>
        /// Throws NotSupportedException. Our algorithms do not use this methon of this type.
        /// </summary>
        public Task AllocateAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            // Never used in our algorithms.
            throw new NotSupportedException();
        }

        private static readonly string WriteStreamScript = @"-- BuiltInBlobProvider.WriteStream
UPDATE Files SET Stream = @Value WHERE FileId = @Id;"; // proc_BinaryProperty_WriteStream

        /// <summary>
        /// DO NOT USE DIRECTLY THIS METHOD FROM YOUR CODE.
        /// Writes the stream in the appropriate row of the Files table specified by the context.
        /// </summary>
        public void AddStream(BlobStorageContext context, Stream stream)
        {
            if (stream == null || stream.Length == 0L)
                return;
            UpdateStream(context, stream);
        }
        public static Task AddStreamAsync(BlobStorageContext context, Stream stream, MsSqlDataContext dataContext)
        {
            if (stream == null || stream.Length == 0L)
                return Task.CompletedTask;
            return UpdateStreamAsync(context, stream, dataContext);
        }

        /// <summary>
        /// DO NOT USE DIRECTLY THIS METHOD FROM YOUR CODE.
        /// Updates the stream in the appropriate row of the Files table specified by the context.
        /// </summary>
        public void UpdateStream(BlobStorageContext context, Stream stream)
        {
            // We have to work with an integer since SQL does not support
            // binary values bigger than [Int32.MaxValue].
            var bufferSize = Convert.ToInt32(stream.Length);

            using (var op = SnTrace.Database.StartOperation("BuiltInBlobProvider: " +
                "UpdateStream: FileId: {0}, length: {1}.", context.FileId, bufferSize))
            {
                var buffer = new byte[bufferSize];
                if (bufferSize > 0)
                {
                    // Read bytes from the source
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.Read(buffer, 0, bufferSize);
                }

                using (var ctx = new MsSqlDataContext(_connectionString, DataOptions, _retrier, CancellationToken.None))
                {
                    ctx.ExecuteNonQueryAsync(WriteStreamScript, cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@Id", SqlDbType.Int, context.FileId),
                            ctx.CreateParameter("@Value", SqlDbType.VarBinary, bufferSize, buffer),
                        });
                    }).GetAwaiter().GetResult();
                }
                op.Successful = true;
            }
        }
        public static async Task UpdateStreamAsync(BlobStorageContext context, Stream stream, MsSqlDataContext dataContext)
        {
            // We have to work with an integer since SQL does not support
            // binary values bigger than [Int32.MaxValue].
            var bufferSize = Convert.ToInt32(stream.Length);

            using (var op = SnTrace.Database.StartOperation("BuiltInBlobProvider: " +
                "UpdateStreamAsync: FileId: {0}, length: {1}.", context.FileId, bufferSize))
            {
                var buffer = new byte[bufferSize];
                if (bufferSize > 0)
                {
                    // Read bytes from the source
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.Read(buffer, 0, bufferSize);
                }

                await dataContext.ExecuteNonQueryAsync(WriteStreamScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        dataContext.CreateParameter("@Id", SqlDbType.Int, context.FileId),
                        dataContext.CreateParameter("@Value", SqlDbType.VarBinary, bufferSize, buffer),
                    });
                }).ConfigureAwait(false);
                op.Successful = true;
            }
        }

        /// <inheritdoc />
        public Task ClearAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            // do nothing
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Stream GetStreamForRead(BlobStorageContext context)
        {
            if (BlobStorage == null)
                throw new InvalidOperationException("BlobStorage back reference is not set.");

            return new RepositoryStream(context.FileId, context.Length, BlobStorage);
        }

        /// <inheritdoc />
        public Stream CloneStream(BlobStorageContext context, Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (BlobStorage == null)
                throw new InvalidOperationException("BlobStorage back reference is not set.");

            if (stream is RepositoryStream repoStream)
                return new RepositoryStream(repoStream.FileId, repoStream.Length, BlobStorage);

            throw new InvalidOperationException("Unknown stream type: " + stream.GetType().Name);
        }

        /// <inheritdoc />
        public Task DeleteAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            // do nothing
            return Task.CompletedTask;
        }

        #region LoadBinaryFragmentScript

        private const string LoadBinaryFragmentScript = @"SELECT SUBSTRING([Stream], @Position, @Count) FROM dbo.Files WHERE FileId = @FileId";
        #endregion
        public byte[] ReadRandom(BlobStorageContext context, long offset, int count)
        {
            using var op = SnTrace.Database.StartOperation("BuiltInBlobProvider: " +
                "ReadRandom: FileId: {0}, offset: {1}, count: {2}", context.FileId, offset, count);
            using var ctx = new MsSqlDataContext(_connectionString, DataOptions, _retrier, CancellationToken.None);
            var result = (byte[])ctx.ExecuteScalarAsync(LoadBinaryFragmentScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@FileId", SqlDbType.Int, context.FileId),
                    ctx.CreateParameter("@Position", SqlDbType.BigInt, offset + 1),
                    ctx.CreateParameter("@Count", SqlDbType.Int, count),
                });
            }).GetAwaiter().GetResult();
            op.Successful = true;

            return result;
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
        public async Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("BuiltInBlobProvider: " +
                "WriteAsync: FileId: {0}, VersionId: {1}, PropertyTypeId: {2}, offset: {3}, buffer length: {4}",
                context.FileId, context.VersionId, context.PropertyTypeId, offset, buffer.Length);
            using var ctx = new MsSqlDataContext(_connectionString, DataOptions, _retrier, cancellationToken);
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
            }).ConfigureAwait(false);
            op.Successful = true;
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
