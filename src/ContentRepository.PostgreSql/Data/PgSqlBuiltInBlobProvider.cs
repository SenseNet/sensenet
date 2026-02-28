using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;
using SenseNet.Tools;
// ReSharper disable AccessToDisposedClosure
// ReSharper disable AccessToModifiedClosure

namespace SenseNet.ContentRepository.Storage.Data.PgSqlClient
{
    /// <summary>
    /// The built-in provider for PostgreSQL is responsible for saving bytes directly 
    /// to the Files table (bytea column).
    /// </summary>
    public class PgSqlBuiltInBlobProvider : IBuiltInBlobProvider
    {
        private readonly IRetrier _retrier;

        protected DataOptions DataOptions { get; }
        private string _connectionString;

        public IBlobStorage BlobStorage { get; set; }

        public PgSqlBuiltInBlobProvider(IOptions<DataOptions> options, IOptions<ConnectionStringOptions> connectionOptions, IRetrier retrier)
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
        /// Throws NotSupportedException. Our algorithms do not use this method of this type.
        /// </summary>
        public System.Threading.Tasks.Task AllocateAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        private static readonly string WriteStreamScript = @"-- PgSqlBuiltInBlobProvider.WriteStream
UPDATE ""Files"" SET ""Stream"" = @Value WHERE ""FileId"" = @Id;";

        /// <summary>
        /// DO NOT USE DIRECTLY THIS METHOD FROM YOUR CODE.
        /// </summary>
        public void AddStream(BlobStorageContext context, Stream stream)
        {
            if (stream == null || stream.Length == 0L)
                return;
            UpdateStream(context, stream);
        }
        public static System.Threading.Tasks.Task AddStreamAsync(BlobStorageContext context, Stream stream, PgSqlDataContext dataContext)
        {
            if (stream == null || stream.Length == 0L)
                return System.Threading.Tasks.Task.CompletedTask;
            return UpdateStreamAsync(context, stream, dataContext);
        }

        /// <summary>
        /// DO NOT USE DIRECTLY THIS METHOD FROM YOUR CODE.
        /// </summary>
        public void UpdateStream(BlobStorageContext context, Stream stream)
        {
            var bufferSize = Convert.ToInt32(stream.Length);

            using (var op = SnTrace.Database.StartOperation("PgSqlBuiltInBlobProvider: " +
                "UpdateStream: FileId: {0}, length: {1}.", context.FileId, bufferSize))
            {
                var buffer = new byte[bufferSize];
                if (bufferSize > 0)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.Read(buffer, 0, bufferSize);
                }

                using (var ctx = new PgSqlDataContext(_connectionString, DataOptions, _retrier, CancellationToken.None))
                {
                    ctx.ExecuteNonQueryAsync(WriteStreamScript, cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@Id", DbType.Int32, context.FileId),
                            ctx.CreateParameter("@Value", NpgsqlDbType.Bytea, bufferSize, buffer),
                        });
                    }).GetAwaiter().GetResult();
                }
                op.Successful = true;
            }
        }
        public static async System.Threading.Tasks.Task UpdateStreamAsync(BlobStorageContext context, Stream stream, PgSqlDataContext dataContext)
        {
            var bufferSize = Convert.ToInt32(stream.Length);

            using (var op = SnTrace.Database.StartOperation("PgSqlBuiltInBlobProvider: " +
                "UpdateStreamAsync: FileId: {0}, length: {1}.", context.FileId, bufferSize))
            {
                var buffer = new byte[bufferSize];
                if (bufferSize > 0)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.Read(buffer, 0, bufferSize);
                }

                await dataContext.ExecuteNonQueryAsync(WriteStreamScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        dataContext.CreateParameter("@Id", DbType.Int32, context.FileId),
                        dataContext.CreateParameter("@Value", NpgsqlDbType.Bytea, bufferSize, buffer),
                    });
                }).ConfigureAwait(false);
                op.Successful = true;
            }
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task ClearAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.CompletedTask;
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
        public System.Threading.Tasks.Task DeleteAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        #region LoadBinaryFragmentScript
        private const string LoadBinaryFragmentScript = @"SELECT SUBSTRING(""Stream"" FROM @Position::int FOR @Count) FROM ""Files"" WHERE ""FileId"" = @FileId";
        #endregion
        public byte[] ReadRandom(BlobStorageContext context, long offset, int count)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlBuiltInBlobProvider: " +
                "ReadRandom: FileId: {0}, offset: {1}, count: {2}", context.FileId, offset, count);
            using var ctx = new PgSqlDataContext(_connectionString, DataOptions, _retrier, CancellationToken.None);
            var result = (byte[])ctx.ExecuteScalarAsync(LoadBinaryFragmentScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@FileId", DbType.Int32, context.FileId),
                    ctx.CreateParameter("@Position", DbType.Int64, offset + 1), // PostgreSQL SUBSTRING is 1-based
                    ctx.CreateParameter("@Count", DbType.Int32, count),
                });
            }).GetAwaiter().GetResult();
            op.Successful = true;

            return result;
        }

        #region UpdateStreamWriteChunkScript
        private static readonly string UpdateStreamWriteChunkScript = PgSqlBlobMetaDataProvider.UpdateStreamWriteChunkSecurityCheckScript + @"
-- init for streaming write
UPDATE ""Files"" SET ""Stream"" = ''::bytea WHERE ""FileId"" = @FileId AND ""Stream"" IS NULL;
-- fill to offset if needed
UPDATE ""Files"" SET ""Stream"" = ""Stream"" || repeat(E'\\000', greatest(0, @Offset::int - octet_length(""Stream"")))::bytea
    WHERE ""FileId"" = @FileId AND octet_length(""Stream"") < @Offset;
-- write payload using overlay
UPDATE ""Files"" SET ""Stream"" = overlay(""Stream"" placing @Data from (@Offset + 1)::int for octet_length(@Data))
    WHERE ""FileId"" = @FileId;";
        #endregion

        /// <inheritdoc />
        public async System.Threading.Tasks.Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlBuiltInBlobProvider: " +
                "WriteAsync: FileId: {0}, VersionId: {1}, PropertyTypeId: {2}, offset: {3}, buffer length: {4}",
                context.FileId, context.VersionId, context.PropertyTypeId, offset, buffer.Length);
            using var ctx = new PgSqlDataContext(_connectionString, DataOptions, _retrier, cancellationToken);
            await ctx.ExecuteNonQueryAsync(UpdateStreamWriteChunkScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@FileId", DbType.Int32, context.FileId),
                    ctx.CreateParameter("@VersionId", DbType.Int32, context.VersionId),
                    ctx.CreateParameter("@PropertyTypeId", DbType.Int32, context.PropertyTypeId),
                    ctx.CreateParameter("@Data", NpgsqlDbType.Bytea, buffer.Length, buffer),
                    ctx.CreateParameter("@Offset", DbType.Int64, offset),
                });
            }).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Throws NotSupportedException.
        /// </summary>
        public Stream GetStreamForWrite(BlobStorageContext context)
        {
            throw new NotSupportedException();
        }
    }
}
