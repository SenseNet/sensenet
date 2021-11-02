﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Security;
// ReSharper disable AccessToDisposedClosure
// ReSharper disable CheckNamespace

namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    /// <summary> 
    /// This is an MS SQL implementation of the <see cref="IAccessTokenDataProviderExtension"/> interface.
    /// It requires the main data provider to be a <see cref="RelationalDataProviderBase"/>.
    /// </summary>
    public class MsSqlAccessTokenDataProvider : IAccessTokenDataProviderExtension
    {
        private RelationalDataProviderBase _dataProvider;
        private RelationalDataProviderBase MainProvider => _dataProvider ??= (RelationalDataProviderBase)Providers.Instance.DataProvider;

        private string _accessTokenValueCollationName;
        private async Task<string> GetAccessTokenValueCollationNameAsync(CancellationToken cancellationToken)
        {
            const string sql = "SELECT c.collation_name, c.* " +
                               "FROM sys.columns c " +
                               "  JOIN sys.tables t ON t.object_id = c.object_id " +
                               "WHERE t.name = 'AccessTokens' AND c.name = N'Value'";

            if (_accessTokenValueCollationName == null)
            {
                using (var ctx = MainProvider.CreateDataContext(cancellationToken))
                {
                    var result = await ctx.ExecuteScalarAsync(sql).ConfigureAwait(false);
                    var originalCollation = Convert.ToString(result);
                    _accessTokenValueCollationName = originalCollation.Replace("_CI_", "_CS_");
                }
            }

            return _accessTokenValueCollationName;
        }

        private static AccessToken GetAccessTokenFromReader(IDataReader reader)
        {
            return new AccessToken
            {
                Id = reader.GetInt32(reader.GetOrdinal("AccessTokenId")),
                Value = reader.GetString(reader.GetOrdinal("Value")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                ContentId = reader.GetSafeInt32(reader.GetOrdinal("ContentId")),
                Feature = reader.GetSafeString(reader.GetOrdinal("Feature")),
                CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                ExpirationDate = reader.GetDateTime(reader.GetOrdinal("ExpirationDate")),
            };
        }

        public async Task DeleteAllAccessTokensAsync(CancellationToken cancellationToken)
        {
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync("TRUNCATE TABLE AccessTokens").ConfigureAwait(false);
            }
        }

        public async Task SaveAccessTokenAsync(AccessToken token, CancellationToken cancellationToken)
        {
            const string sql = "INSERT INTO [dbo].[AccessTokens] " +
                               "([Value],[UserId],[ContentId],[Feature],[CreationDate],[ExpirationDate]) VALUES " +
                               "(@Value, @UserId, @ContentId, @Feature, @CreationDate, @ExpirationDate)" +
                               "SELECT @@IDENTITY";

            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Value", DbType.String, token.Value),
                        ctx.CreateParameter("@UserId", DbType.Int32, token.UserId),
                        ctx.CreateParameter("@ContentId", DbType.Int32, token.ContentId != 0 ? (object)token.ContentId : DBNull.Value),
                        ctx.CreateParameter("@Feature", DbType.String, token.Feature != null ? (object)token.Feature : DBNull.Value),
                        ctx.CreateParameter("@CreationDate", DbType.DateTime2, token.CreationDate),
                        ctx.CreateParameter("@ExpirationDate", DbType.DateTime2, token.ExpirationDate)
                    });
                }).ConfigureAwait(false);

                token.Id = Convert.ToInt32(result);
            }
        }

        public async Task<AccessToken> LoadAccessTokenByIdAsync(int accessTokenId, CancellationToken cancellationToken)
        {
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync("SELECT TOP 1 * FROM AccessTokens WHERE [AccessTokenId] = @Id",
                    cmd => { cmd.Parameters.Add(ctx.CreateParameter("@Id", DbType.Int32, accessTokenId)); },
                    async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        return await reader.ReadAsync(cancel).ConfigureAwait(false)
                            ? GetAccessTokenFromReader(reader)
                            : null;
                    }).ConfigureAwait(false);
            }
        }

        public async Task<AccessToken> LoadAccessTokenAsync(string tokenValue, int contentId, string feature, CancellationToken cancellationToken)
        {
            var sql = "SELECT TOP 1 * FROM [dbo].[AccessTokens] " +
                      $"WHERE [Value] = @Value COLLATE {await GetAccessTokenValueCollationNameAsync(cancellationToken).ConfigureAwait(false)} AND [ExpirationDate] > GETUTCDATE() AND " +
                      (contentId != 0 ? $"ContentId = {contentId} AND " : "ContentId IS NULL AND ") +
                      (feature != null ? $"Feature = '{feature}'" : "Feature IS NULL");

            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(sql,
                    cmd => { cmd.Parameters.Add(ctx.CreateParameter("@Value", DbType.String, tokenValue)); },
                    async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        return await reader.ReadAsync(cancel).ConfigureAwait(false)
                            ? GetAccessTokenFromReader(reader)
                            : null;
                    }).ConfigureAwait(false);
            }
        }

        public async Task<AccessToken[]> LoadAccessTokensAsync(int userId, CancellationToken cancellationToken)
        {
            const string sql = "SELECT * FROM [dbo].[AccessTokens] " +
                               "WHERE [UserId] = @UserId AND [ExpirationDate] > GETUTCDATE()";

            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(sql,
                    cmd => { cmd.Parameters.Add(ctx.CreateParameter("@UserId", DbType.Int32, userId)); },
                    async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        var tokens = new List<AccessToken>();
                        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                            tokens.Add(GetAccessTokenFromReader(reader));

                        return tokens.ToArray();
                    }
                ).ConfigureAwait(false);
            }
        }

        public async Task UpdateAccessTokenAsync(string tokenValue, DateTime newExpirationDate, CancellationToken cancellationToken)
        {
            var sql = "UPDATE [AccessTokens] " +
                      "SET [ExpirationDate] = @NewExpirationDate " +
                      "   OUTPUT INSERTED.AccessTokenId" +
                      " WHERE [Value] = @Value " +
                      $" COLLATE {await GetAccessTokenValueCollationNameAsync(cancellationToken).ConfigureAwait(false)} AND [ExpirationDate] > GETUTCDATE()";

            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Value", DbType.String, tokenValue),
                        ctx.CreateParameter("@NewExpirationDate", DbType.DateTime2, newExpirationDate)
                    });
                }).ConfigureAwait(false);

                if (result == null || result == DBNull.Value)
                    throw new InvalidAccessTokenException("Token not found or it is expired.");
            }
        }

        public async Task DeleteAccessTokenAsync(string tokenValue, CancellationToken cancellationToken)
        {
            var sql = "DELETE FROM [dbo].[AccessTokens] " +
                      $"WHERE [Value] = @Value COLLATE {await GetAccessTokenValueCollationNameAsync(cancellationToken).ConfigureAwait(false)}";

            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync(sql,
                    cmd => { cmd.Parameters.Add(ctx.CreateParameter("@Value", DbType.String, tokenValue)); }).ConfigureAwait(false);
            }
        }

        public async Task DeleteAccessTokensByUserAsync(int userId, CancellationToken cancellationToken)
        {
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync("DELETE FROM [dbo].[AccessTokens] WHERE [UserId] = @UserId",
                    cmd => { cmd.Parameters.Add(ctx.CreateParameter("@UserId", DbType.Int32, userId)); }).ConfigureAwait(false);
            }
        }

        public async Task DeleteAccessTokensByContentAsync(int contentId, CancellationToken cancellationToken)
        {
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync("DELETE FROM [dbo].[AccessTokens] WHERE [ContentId] = @ContentId",
                    cmd => { cmd.Parameters.Add(ctx.CreateParameter("@ContentId", DbType.Int32, contentId)); }).ConfigureAwait(false);
            }
        }

        public async Task DeleteAccessTokensAsync(int userId, int contentId, string feature, CancellationToken cancellationToken)
        {
            if (userId == 0 && contentId == 0 && string.IsNullOrEmpty(feature))
            {
                await DeleteAllAccessTokensAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var expressions = userId != 0 ? "UserId = @UserId" : string.Empty;
            expressions = AddExpressions(expressions, contentId != 0 ? "ContentId = @ContentId" : string.Empty);
            expressions = AddExpressions(expressions, !string.IsNullOrEmpty(feature) ? "Feature = @Feature" : string.Empty);

            var sql = "DELETE FROM [dbo].[AccessTokens] WHERE " + expressions;

            using var ctx = MainProvider.CreateDataContext(cancellationToken);
            await ctx.ExecuteNonQueryAsync(sql,
                cmd =>
                {
                    if (userId != 0)
                        cmd.Parameters.Add(ctx.CreateParameter("@UserId", DbType.Int32, userId));
                    if (contentId != 0)
                        cmd.Parameters.Add(ctx.CreateParameter("@ContentId", DbType.Int32, contentId));
                    if (!string.IsNullOrEmpty(feature))
                        cmd.Parameters.Add(ctx.CreateParameter("@Feature", DbType.String, feature));
                }).ConfigureAwait(false);
        }

        private static string AddExpressions(string sqlA, string sqlB)
        {
            return sqlA + (!string.IsNullOrEmpty(sqlA) && !string.IsNullOrEmpty(sqlB) ? " AND " : string.Empty) + sqlB;
        }

        public async Task CleanupAccessTokensAsync(CancellationToken cancellationToken)
        {
            const string sql = "DELETE FROM [dbo].[AccessTokens] WHERE [ExpirationDate] < DATEADD(MINUTE, -30, GETUTCDATE())";

            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
            }
        }
    }
}
