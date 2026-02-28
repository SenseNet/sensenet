using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.PgSqlClient
{
    public class PgSqlAccessTokenDataProvider : IAccessTokenDataProvider
    {
        private readonly IRetrier _retrier;
        private DataOptions DataOptions { get; }
        private ConnectionStringOptions ConnectionStrings { get; }

        public PgSqlAccessTokenDataProvider(IOptions<DataOptions> dataOptions,
            IOptions<ConnectionStringOptions> connectionOptions, IRetrier retrier)
        {
            _retrier = retrier;
            DataOptions = dataOptions?.Value ?? new DataOptions();
            ConnectionStrings = connectionOptions?.Value ?? new ConnectionStringOptions();
        }

        public async System.Threading.Tasks.Task DeleteAllAccessTokensAsync(CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            await ctx.ExecuteNonQueryAsync(
                @"TRUNCATE TABLE ""AccessTokens""").ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task SaveAccessTokenAsync(AccessToken token, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlAccessTokenDataProvider: " +
                "SaveAccessToken: UserId: {0}, ContentId: {1}", token.UserId, token.ContentId);
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            var result = await ctx.ExecuteReaderAsync(
                @"INSERT INTO ""AccessTokens"" (""Value"", ""UserId"", ""ContentId"", ""Feature"", ""CreationDate"", ""ExpirationDate"")
VALUES (@Value, @UserId, @ContentId, @Feature, @CreationDate, @ExpirationDate)
RETURNING ""AccessTokenId""", cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Value", DbType.String, 1000, token.Value),
                        ctx.CreateParameter("@UserId", DbType.Int32, token.UserId),
                        ctx.CreateParameter("@ContentId", DbType.Int32, token.ContentId != 0 ? (object)token.ContentId : DBNull.Value),
                        ctx.CreateParameter("@Feature", DbType.String, 1000, token.Feature != null ? (object)token.Feature : DBNull.Value),
                        ctx.CreateParameter("@CreationDate", DbType.DateTime2, token.CreationDate),
                        ctx.CreateParameter("@ExpirationDate", DbType.DateTime2, token.ExpirationDate),
                    });
                }, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    if (await reader.ReadAsync(cancel).ConfigureAwait(false))
                        token.Id = reader.GetInt32(0);
                    return true;
                }).ConfigureAwait(false);
            op.Successful = true;
        }

        public async Task<AccessToken> LoadAccessTokenByIdAsync(int accessTokenId, CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            var result = await ctx.ExecuteReaderAsync(
                @"SELECT ""AccessTokenId"", ""Value"", ""UserId"", ""ContentId"", ""Feature"",
       ""CreationDate"", ""ExpirationDate""
FROM ""AccessTokens"" WHERE ""AccessTokenId"" = @Id AND ""ExpirationDate"" > @Now", cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Id", DbType.Int32, accessTokenId),
                        ctx.CreateParameter("@Now", DbType.DateTime2, DateTime.UtcNow),
                    });
                }, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    return await reader.ReadAsync(cancel).ConfigureAwait(false)
                        ? GetAccessTokenFromReader(reader) : null;
                }).ConfigureAwait(false);
            return result;
        }

        public async Task<AccessToken> LoadAccessTokenAsync(string tokenValue, int contentId, string feature,
            CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            var sql = @"SELECT ""AccessTokenId"", ""Value"", ""UserId"", ""ContentId"", ""Feature"",
       ""CreationDate"", ""ExpirationDate""
FROM ""AccessTokens""
WHERE ""Value"" = @Value AND ""ExpirationDate"" > @Now";

            if (contentId > 0)
                sql += @" AND ""ContentId"" = @ContentId";
            else
                sql += @" AND ""ContentId"" IS NULL";

            if (feature != null)
                sql += @" AND ""Feature"" = @Feature";
            else
                sql += @" AND ""Feature"" IS NULL";

            var result = await ctx.ExecuteReaderAsync(sql, cmd =>
            {
                cmd.Parameters.Add(ctx.CreateParameter("@Value", DbType.String, 1000, tokenValue));
                cmd.Parameters.Add(ctx.CreateParameter("@Now", DbType.DateTime2, DateTime.UtcNow));
                if (contentId > 0)
                    cmd.Parameters.Add(ctx.CreateParameter("@ContentId", DbType.Int32, contentId));
                if (feature != null)
                    cmd.Parameters.Add(ctx.CreateParameter("@Feature", DbType.String, 1000, feature));
            }, async (reader, cancel) =>
            {
                cancel.ThrowIfCancellationRequested();
                return await reader.ReadAsync(cancel).ConfigureAwait(false)
                    ? GetAccessTokenFromReader(reader) : null;
            }).ConfigureAwait(false);
            return result;
        }

        public async Task<AccessToken[]> LoadAccessTokensAsync(int userId, CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            var result = await ctx.ExecuteReaderAsync(
                @"SELECT ""AccessTokenId"", ""Value"", ""UserId"", ""ContentId"", ""Feature"",
       ""CreationDate"", ""ExpirationDate""
FROM ""AccessTokens"" WHERE ""UserId"" = @UserId AND ""ExpirationDate"" > @Now", cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@UserId", DbType.Int32, userId),
                        ctx.CreateParameter("@Now", DbType.DateTime2, DateTime.UtcNow),
                    });
                }, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    var tokens = new List<AccessToken>();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                        tokens.Add(GetAccessTokenFromReader(reader));
                    return tokens.ToArray();
                }).ConfigureAwait(false);
            return result;
        }

        public async System.Threading.Tasks.Task UpdateAccessTokenAsync(string tokenValue, DateTime newExpirationDate,
            CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            var count = await ctx.ExecuteNonQueryAsync(
                @"UPDATE ""AccessTokens"" SET ""ExpirationDate"" = @NewExpiration
WHERE ""Value"" = @Value AND ""ExpirationDate"" > @Now", cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Value", DbType.String, 1000, tokenValue),
                        ctx.CreateParameter("@NewExpiration", DbType.DateTime2, newExpirationDate),
                        ctx.CreateParameter("@Now", DbType.DateTime2, DateTime.UtcNow),
                    });
                }).ConfigureAwait(false);
            if (count == 0)
                throw new InvalidAccessTokenException("Token not found or it is expired.");
        }

        public async System.Threading.Tasks.Task DeleteAccessTokenAsync(string tokenValue, CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            await ctx.ExecuteNonQueryAsync(
                @"DELETE FROM ""AccessTokens"" WHERE ""Value"" = @Value", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@Value", DbType.String, 1000, tokenValue));
                }).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task DeleteAccessTokensByUserAsync(int userId, CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            await ctx.ExecuteNonQueryAsync(
                @"DELETE FROM ""AccessTokens"" WHERE ""UserId"" = @UserId", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@UserId", DbType.Int32, userId));
                }).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task DeleteAccessTokensByContentAsync(int contentId, CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            await ctx.ExecuteNonQueryAsync(
                @"DELETE FROM ""AccessTokens"" WHERE ""ContentId"" = @ContentId", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@ContentId", DbType.Int32, contentId));
                }).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task CleanupAccessTokensAsync(CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            await ctx.ExecuteNonQueryAsync(
                @"DELETE FROM ""AccessTokens"" WHERE ""ExpirationDate"" < @Now", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@Now", DbType.DateTime2, DateTime.UtcNow));
                }).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task DeleteAccessTokensAsync(int userId, int contentId, string feature,
            CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);

            var sql = @"DELETE FROM ""AccessTokens"" WHERE 1=1";
            if (userId > 0)
                sql += @" AND ""UserId"" = @UserId";
            if (contentId > 0)
                sql += @" AND ""ContentId"" = @ContentId";
            if (feature != null)
                sql += @" AND ""Feature"" = @Feature";

            await ctx.ExecuteNonQueryAsync(sql, cmd =>
            {
                if (userId > 0)
                    cmd.Parameters.Add(ctx.CreateParameter("@UserId", DbType.Int32, userId));
                if (contentId > 0)
                    cmd.Parameters.Add(ctx.CreateParameter("@ContentId", DbType.Int32, contentId));
                if (feature != null)
                    cmd.Parameters.Add(ctx.CreateParameter("@Feature", DbType.String, 1000, feature));
            }).ConfigureAwait(false);
        }

        private static AccessToken GetAccessTokenFromReader(DbDataReader reader)
        {
            return new AccessToken
            {
                Id = reader.GetInt32(reader.GetOrdinal("AccessTokenId")),
                Value = reader.GetString(reader.GetOrdinal("Value")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                ContentId = reader.IsDBNull(reader.GetOrdinal("ContentId"))
                    ? 0 : reader.GetInt32(reader.GetOrdinal("ContentId")),
                Feature = reader.IsDBNull(reader.GetOrdinal("Feature"))
                    ? null : reader.GetString(reader.GetOrdinal("Feature")),
                CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                ExpirationDate = reader.GetDateTime(reader.GetOrdinal("ExpirationDate")),
            };
        }
    }
}
