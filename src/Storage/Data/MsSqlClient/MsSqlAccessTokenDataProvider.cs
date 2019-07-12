using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    //UNDONE:DB: ASYNC API: CancellationToken is not used in this class.
    public class MsSqlAccessTokenDataProvider : IAccessTokenDataProviderExtension
    {
        private RelationalDataProviderBase _dataProvider;
        private RelationalDataProviderBase MainProvider => _dataProvider ?? (_dataProvider = (RelationalDataProviderBase)DataStore.DataProvider);

        private string _accessTokenValueCollationName;
        private async Task<string> GetAccessTokenValueCollationNameAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            const string sql = "SELECT c.collation_name, c.* " +
                               "FROM sys.columns c " +
                               "  JOIN sys.tables t ON t.object_id = c.object_id " +
                               "WHERE t.name = 'AccessTokens' AND c.name = N'Value'";

            if (_accessTokenValueCollationName == null)
            {
                using (var ctx = new RelationalDbDataContext(MainProvider.GetPlatform(), cancellationToken))
                {
                    var result = await ctx.ExecuteScalarAsync(sql);
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

        public async Task DeleteAllAccessTokensAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var ctx = new RelationalDbDataContext(MainProvider.GetPlatform(), cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync("TRUNCATE TABLE AccessTokens");
            }
        }

        public async Task SaveAccessTokenAsync(AccessToken token, CancellationToken cancellationToken = default(CancellationToken))
        {
            const string sql = "INSERT INTO [dbo].[AccessTokens] " +
                               "([Value],[UserId],[ContentId],[Feature],[CreationDate],[ExpirationDate]) VALUES " +
                               "(@Value, @UserId, @ContentId, @Feature, @CreationDate, @ExpirationDate)" +
                               "SELECT @@IDENTITY";

            using (var ctx = new RelationalDbDataContext(MainProvider.GetPlatform(), cancellationToken))
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
                });

                token.Id = Convert.ToInt32(result);
            }
        }

        public async Task<AccessToken> LoadAccessTokenByIdAsync(int accessTokenId, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var ctx = new RelationalDbDataContext(MainProvider.GetPlatform(), cancellationToken))
            {
                return await ctx.ExecuteReaderAsync("SELECT TOP 1 * FROM AccessTokens WHERE [AccessTokenId] = @Id",
                    cmd => { cmd.Parameters.Add(ctx.CreateParameter("@Id", DbType.Int32, accessTokenId)); },
                    async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        return await reader.ReadAsync(cancel)
                            ? GetAccessTokenFromReader(reader)
                            : null;
                    });
            }
        }

        public async Task<AccessToken> LoadAccessTokenAsync(string tokenValue, int contentId, string feature, CancellationToken cancellationToken = default(CancellationToken))
        {
            var sql = "SELECT TOP 1 * FROM [dbo].[AccessTokens] " +
                      $"WHERE [Value] = @Value COLLATE {await GetAccessTokenValueCollationNameAsync(cancellationToken)} AND [ExpirationDate] > GETUTCDATE() AND " +
                      (contentId != 0 ? $"ContentId = {contentId} AND " : "ContentId IS NULL AND ") +
                      (feature != null ? $"Feature = '{feature}'" : "Feature IS NULL");

            using (var ctx = new RelationalDbDataContext(MainProvider.GetPlatform(), cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(sql,
                    cmd => { cmd.Parameters.Add(ctx.CreateParameter("@Value", DbType.String, tokenValue)); },
                    async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        return await reader.ReadAsync(cancel)
                            ? GetAccessTokenFromReader(reader)
                            : null;
                    });
            }
        }

        public async Task<AccessToken[]> LoadAccessTokensAsync(int userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            const string sql = "SELECT * FROM [dbo].[AccessTokens] " +
                               "WHERE [UserId] = @UserId AND [ExpirationDate] > GETUTCDATE()";

            using (var ctx = new RelationalDbDataContext(MainProvider.GetPlatform(), cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(sql,
                    cmd => { cmd.Parameters.Add(ctx.CreateParameter("@UserId", DbType.Int32, userId)); },
                    async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        var tokens = new List<AccessToken>();
                        while (await reader.ReadAsync(cancel))
                            tokens.Add(GetAccessTokenFromReader(reader));

                        return tokens.ToArray();
                    }
                );
            }
        }

        public async Task UpdateAccessTokenAsync(string tokenValue, DateTime newExpirationDate, CancellationToken cancellationToken = default(CancellationToken))
        {
            var sql = "UPDATE [AccessTokens] " +
                      "SET [ExpirationDate] = @NewExpirationDate " +
                      "   OUTPUT INSERTED.AccessTokenId" +
                      " WHERE [Value] = @Value " +
                      $" COLLATE {await GetAccessTokenValueCollationNameAsync(cancellationToken)} AND [ExpirationDate] > GETUTCDATE()";

            using (var ctx = new RelationalDbDataContext(MainProvider.GetPlatform(), cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Value", DbType.String, tokenValue),
                        ctx.CreateParameter("@NewExpirationDate", DbType.DateTime2, newExpirationDate)
                    });
                });

                if (result == null || result == DBNull.Value)
                    throw new InvalidAccessTokenException("Token not found or it is expired.");
            }
        }

        public async Task DeleteAccessTokenAsync(string tokenValue, CancellationToken cancellationToken = default(CancellationToken))
        {
            var sql = "DELETE FROM [dbo].[AccessTokens] " +
                      $"WHERE [Value] = @Value COLLATE {await GetAccessTokenValueCollationNameAsync(cancellationToken)}";

            using (var ctx = new RelationalDbDataContext(MainProvider.GetPlatform(), cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync(sql,
                    cmd => { cmd.Parameters.Add(ctx.CreateParameter("@Value", DbType.String, tokenValue)); });
            }
        }

        public async Task DeleteAccessTokensByUserAsync(int userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var ctx = new RelationalDbDataContext(MainProvider.GetPlatform(), cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync("DELETE FROM [dbo].[AccessTokens] WHERE [UserId] = @UserId",
                    cmd => { cmd.Parameters.Add(ctx.CreateParameter("@UserId", DbType.Int32, userId)); });
            }
        }

        public async Task DeleteAccessTokensByContentAsync(int contentId, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var ctx = new RelationalDbDataContext(MainProvider.GetPlatform(), cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync("DELETE FROM [dbo].[AccessTokens] WHERE [ContentId] = @ContentId",
                    cmd => { cmd.Parameters.Add(ctx.CreateParameter("@ContentId", DbType.Int32, contentId)); });
            }
        }

        public async Task CleanupAccessTokensAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            const string sql = "DELETE FROM [dbo].[AccessTokens] WHERE [ExpirationDate] < DATEADD(MINUTE, -30, GETUTCDATE())";

            using (var ctx = new RelationalDbDataContext(MainProvider.GetPlatform(), cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync(sql);
            }
        }
    }
}
