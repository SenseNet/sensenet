using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using STT=System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
// ReSharper disable AccessToDisposedClosure
// ReSharper disable CheckNamespace

namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    /// <summary> 
    /// This is an MS SQL implementation of the <see cref="IAccessTokenDataProvider"/> interface.
    /// It requires the main data provider to be a <see cref="RelationalDataProviderBase"/>.
    /// </summary>
    public class MsSqlAccessTokenDataProvider : IAccessTokenDataProvider
    {
        private readonly RelationalDataProviderBase _mainProvider;

        public MsSqlAccessTokenDataProvider(DataProvider mainProvider)
        {
            if (mainProvider == null)
                return;
            if (!(mainProvider is RelationalDataProviderBase relationalDataProviderBase))
                throw new ArgumentException("The mainProvider need to be RelationalDataProviderBase.");
            _mainProvider = relationalDataProviderBase;
        }

        private string _accessTokenValueCollationName;
        private async STT.Task<string> GetAccessTokenValueCollationNameAsync(CancellationToken cancellationToken)
        {
            const string sql = "SELECT c.collation_name, c.* " +
                               "FROM sys.columns c " +
                               "  JOIN sys.tables t ON t.object_id = c.object_id " +
                               "WHERE t.name = 'AccessTokens' AND c.name = N'Value'";

            if (_accessTokenValueCollationName == null)
            {
                using var op = SnTrace.Database.StartOperation(
                    "MsSqlAccessTokenDataProvider: GetAccessTokenValueCollationName()");
                using var ctx = _mainProvider.CreateDataContext(cancellationToken);
                var result = await ctx.ExecuteScalarAsync(sql).ConfigureAwait(false);
                var originalCollation = Convert.ToString(result);
                _accessTokenValueCollationName = originalCollation.Replace("_CI_", "_CS_");
                op.Successful = true;
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

        public async STT.Task DeleteAllAccessTokensAsync(CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlAccessTokenDataProvider: DeleteAllAccessTokens()");
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            await ctx.ExecuteNonQueryAsync("TRUNCATE TABLE AccessTokens").ConfigureAwait(false);
            op.Successful = true;
        }

        public async STT.Task SaveAccessTokenAsync(AccessToken token, CancellationToken cancellationToken)
        {
            const string sql = "INSERT INTO [dbo].[AccessTokens] " +
                               "([Value],[UserId],[ContentId],[Feature],[CreationDate],[ExpirationDate]) VALUES " +
                               "(@Value, @UserId, @ContentId, @Feature, @CreationDate, @ExpirationDate)" +
                               "SELECT @@IDENTITY";

            using var op = SnTrace.Database.StartOperation("MsSqlAccessTokenDataProvider: SaveAccessToken: " +
                "UserId: {0}, ContentId: {1}, Feature: {2}, " +
                "CreationDate: {3:yyyy-MM-dd HH:mm:ss.fffff}, ExpirationDate: {4:yyyy-MM-dd HH:mm:ss.fffff}",
                token?.UserId, token?.ContentId, token?.Feature, token?.CreationDate, token?.ExpirationDate);

            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
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
            op.Successful = true;
        }

        public async STT.Task<AccessToken> LoadAccessTokenByIdAsync(int accessTokenId, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlAccessTokenDataProvider: " +
                "LoadAccessTokenById(accessTokenId: {0})", accessTokenId);

            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            var result = await ctx.ExecuteReaderAsync("SELECT TOP 1 * FROM AccessTokens WHERE [AccessTokenId] = @Id",
                cmd => { cmd.Parameters.Add(ctx.CreateParameter("@Id", DbType.Int32, accessTokenId)); },
                async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    return await reader.ReadAsync(cancel).ConfigureAwait(false)
                        ? GetAccessTokenFromReader(reader)
                        : null;
                }).ConfigureAwait(false);
            op.Successful = true;

            return result;
        }

        public async STT.Task<AccessToken> LoadAccessTokenAsync(string tokenValue, int contentId, string feature, CancellationToken cancellationToken)
        {
            var sql = "SELECT TOP 1 * FROM [dbo].[AccessTokens] " +
                      $"WHERE [Value] = @Value COLLATE {await GetAccessTokenValueCollationNameAsync(cancellationToken).ConfigureAwait(false)} AND [ExpirationDate] > GETUTCDATE() AND " +
                      (contentId != 0 ? $"ContentId = {contentId} AND " : "ContentId IS NULL AND ") +
                      (feature != null ? $"Feature = '{feature}'" : "Feature IS NULL");

            using var op = SnTrace.Database.StartOperation("MsSqlAccessTokenDataProvider: " +
                "LoadAccessToken(tokenValue: {0}, contentId: {1}, feature: {2})", GetAccessTokenForLog(tokenValue), contentId, feature);

            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            var result = await ctx.ExecuteReaderAsync(sql,
                cmd => { cmd.Parameters.Add(ctx.CreateParameter("@Value", DbType.String, tokenValue)); },
                async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    return await reader.ReadAsync(cancel).ConfigureAwait(false)
                        ? GetAccessTokenFromReader(reader)
                        : null;
                }).ConfigureAwait(false);
            op.Successful = true;

            return result;
        }
        private string GetAccessTokenForLog(string tokenValue)
        {
            return tokenValue.Substring(0, 3) + "...";
        }

        public async STT.Task<AccessToken[]> LoadAccessTokensAsync(int userId, CancellationToken cancellationToken)
        {
            const string sql = "SELECT * FROM [dbo].[AccessTokens] " +
                               "WHERE [UserId] = @UserId AND [ExpirationDate] > GETUTCDATE()";

            using var op = SnTrace.Database.StartOperation("MsSqlAccessTokenDataProvider: " +
                "LoadAccessTokens(userId: {0})", userId);

            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            var result = await ctx.ExecuteReaderAsync(sql,
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
            op.Successful = true;

            return result;
        }

        public async STT.Task UpdateAccessTokenAsync(string tokenValue, DateTime newExpirationDate, CancellationToken cancellationToken)
        {
            var sql = "UPDATE [AccessTokens] " +
                      "SET [ExpirationDate] = @NewExpirationDate " +
                      "   OUTPUT INSERTED.AccessTokenId" +
                      " WHERE [Value] = @Value " +
                      $" COLLATE {await GetAccessTokenValueCollationNameAsync(cancellationToken).ConfigureAwait(false)} AND [ExpirationDate] > GETUTCDATE()";

            using var op = SnTrace.Database.StartOperation("MsSqlAccessTokenDataProvider: " +
                "UpdateAccessToken(tokenValue: {0}, newExpirationDate: {1:yyyy-MM-dd HH:mm:ss.fffff})", GetAccessTokenForLog(tokenValue), newExpirationDate);

            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
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

            op.Successful = true;
        }

        public async STT.Task DeleteAccessTokenAsync(string tokenValue, CancellationToken cancellationToken)
        {
            var sql = "DELETE FROM [dbo].[AccessTokens] " +
                      $"WHERE [Value] = @Value COLLATE {await GetAccessTokenValueCollationNameAsync(cancellationToken).ConfigureAwait(false)}";

            using var op = SnTrace.Database.StartOperation("MsSqlAccessTokenDataProvider: " +
                "DeleteAccessToken(tokenValue: {0})", GetAccessTokenForLog(tokenValue));
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            await ctx.ExecuteNonQueryAsync(sql,
                cmd => { cmd.Parameters.Add(ctx.CreateParameter("@Value", DbType.String, tokenValue)); }).ConfigureAwait(false);
            op.Successful = true;
        }

        public async STT.Task DeleteAccessTokensByUserAsync(int userId, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlAccessTokenDataProvider: " +
                "DeleteAccessTokensByUser(userId: {0})", userId);
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            await ctx.ExecuteNonQueryAsync("DELETE FROM [dbo].[AccessTokens] WHERE [UserId] = @UserId",
                cmd => { cmd.Parameters.Add(ctx.CreateParameter("@UserId", DbType.Int32, userId)); }).ConfigureAwait(false);
            op.Successful = true;
        }

        public async STT.Task DeleteAccessTokensByContentAsync(int contentId, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlAccessTokenDataProvider: " +
                "DeleteAccessTokensByContent(contentId: {0})", contentId);
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            await ctx.ExecuteNonQueryAsync("DELETE FROM [dbo].[AccessTokens] WHERE [ContentId] = @ContentId",
                cmd => { cmd.Parameters.Add(ctx.CreateParameter("@ContentId", DbType.Int32, contentId)); }).ConfigureAwait(false);
            op.Successful = true;
        }

        public async STT.Task DeleteAccessTokensAsync(int userId, int contentId, string feature, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlAccessTokenDataProvider: " +
                "DeleteAccessTokens(userId: {0}, contentId: {1}, feature: {2})", userId, contentId, feature);
            if (userId == 0 && contentId == 0 && string.IsNullOrEmpty(feature))
            {
                await DeleteAllAccessTokensAsync(cancellationToken).ConfigureAwait(false);
                op.Successful = true;
                return;
            }

            var expressions = userId != 0 ? "UserId = @UserId" : string.Empty;
            expressions = AddExpressions(expressions, contentId != 0 ? "ContentId = @ContentId" : string.Empty);
            expressions = AddExpressions(expressions, !string.IsNullOrEmpty(feature) ? "Feature = @Feature" : string.Empty);

            var sql = "DELETE FROM [dbo].[AccessTokens] WHERE " + expressions;

            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
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
            op.Successful = true;
        }

        private static string AddExpressions(string sqlA, string sqlB)
        {
            return sqlA + (!string.IsNullOrEmpty(sqlA) && !string.IsNullOrEmpty(sqlB) ? " AND " : string.Empty) + sqlB;
        }

        public async STT.Task CleanupAccessTokensAsync(CancellationToken cancellationToken)
        {
            const string sql = "DELETE FROM [dbo].[AccessTokens] WHERE [ExpirationDate] < DATEADD(MINUTE, -30, GETUTCDATE())";

            using var op = SnTrace.Database.StartOperation("MsSqlAccessTokenDataProvider: CleanupAccessTokens().");
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            await ctx.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
            op.Successful = true;
        }
    }
}
