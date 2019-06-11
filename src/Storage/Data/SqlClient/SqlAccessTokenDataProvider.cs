using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using SenseNet.Common.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    //UNDONE: AccessToken: async API + CancellationToken
    public class SqlAccessTokenDataProvider : IAccessTokenDataProviderExtension
    {
        private RelationalDataProviderBase _dataProvider;
        private RelationalDataProviderBase MainProvider => _dataProvider ?? (_dataProvider = (RelationalDataProviderBase)DataStore.DataProvider);

        private string _accessTokenValueCollationName;
        private string GetAccessTokenValueCollationName()
        {
            var sql = "SELECT c.collation_name, c.* " +
                      "FROM sys.columns c " +
                      "  JOIN sys.tables t ON t.object_id = c.object_id " +
                      "WHERE t.name = 'AccessTokens' AND c.name = N'Value'";

            if (_accessTokenValueCollationName == null)
            {
                using (var ctx = new SnDataContext(MainProvider))
                {
                    var result = ctx.ExecuteScalarAsync(sql).Result;
                    var originalCollation = Convert.ToString(result);
                    _accessTokenValueCollationName = originalCollation.Replace("_CI_", "_CS_");
                }
            }

            return _accessTokenValueCollationName;
        }

        private AccessToken GetAccessTokenFromReader(IDataReader reader)
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

        public void DeleteAllAccessTokens()
        {
            using (var ctx = new SnDataContext(MainProvider))
            {
                ctx.ExecuteNonQueryAsync("TRUNCATE TABLE AccessTokens").Wait(ctx.CancellationToken);
            }
        }

        public void SaveAccessToken(AccessToken token)
        {
            const string sql = "INSERT INTO [dbo].[AccessTokens] " +
                               "([Value],[UserId],[ContentId],[Feature],[CreationDate],[ExpirationDate]) VALUES " +
                               "(@Value, @UserId, @ContentId, @Feature, @CreationDate, @ExpirationDate)" +
                               "SELECT @@IDENTITY";

            using (var ctx = new SnDataContext(MainProvider))
            {
                var result = ctx.ExecuteScalarAsync(sql, cmd =>
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
                }).Result;

                token.Id = Convert.ToInt32(result);
            }
        }

        public AccessToken LoadAccessTokenById(int accessTokenId)
        {
            using (var ctx = new SnDataContext(MainProvider))
            {
                return ctx.ExecuteReaderAsync("SELECT TOP 1 * FROM AccessTokens WHERE [AccessTokenId] = @Id", cmd =>
                    {
                        cmd.Parameters.Add(ctx.CreateParameter("@Id", DbType.Int32, accessTokenId));
                    },
                    reader =>
                    {
                        var token = reader.Read() ? GetAccessTokenFromReader(reader) : null;

                        return Task.FromResult(token);
                    }
                ).Result;
            }
        }

        public AccessToken LoadAccessToken(string tokenValue, int contentId, string feature)
        {
            var sql = "SELECT TOP 1 * FROM [dbo].[AccessTokens] " +
                      $"WHERE [Value] = @Value COLLATE {GetAccessTokenValueCollationName()} AND [ExpirationDate] > GETUTCDATE() AND " +
                      (contentId != 0 ? $"ContentId = {contentId} AND " : "ContentId IS NULL AND ") +
                      (feature != null ? $"Feature = '{feature}'" : "Feature IS NULL");

            using (var ctx = new SnDataContext(MainProvider))
            {
                return ctx.ExecuteReaderAsync(sql, cmd =>
                    {
                        cmd.Parameters.Add(ctx.CreateParameter("@Value", DbType.String, tokenValue));
                    },
                    reader =>
                    {
                        var token = reader.Read() ? GetAccessTokenFromReader(reader) : null;

                        return Task.FromResult(token);
                    }
                ).Result;
            }
        }

        public AccessToken[] LoadAccessTokens(int userId)
        {
            const string sql = "SELECT * FROM [dbo].[AccessTokens] " +
                               "WHERE [UserId] = @UserId AND [ExpirationDate] > GETUTCDATE()";

            using (var ctx = new SnDataContext(MainProvider))
            {
                return ctx.ExecuteReaderAsync(sql, cmd =>
                    {
                        cmd.Parameters.Add(ctx.CreateParameter("@UserId", DbType.Int32, userId));
                    },
                    reader =>
                    {
                        var tokens = new List<AccessToken>();
                        while (reader.Read())
                            tokens.Add(GetAccessTokenFromReader(reader));

                        return Task.FromResult(tokens.ToArray());
                    }
                ).Result;
            }
        }

        public void UpdateAccessToken(string tokenValue, DateTime newExpirationDate)
        {
            var sql = "UPDATE [AccessTokens] " +
                      "SET [ExpirationDate] = @NewExpirationDate " +
                      "   OUTPUT INSERTED.AccessTokenId" +
                      " WHERE [Value] = @Value " +
                      $" COLLATE {GetAccessTokenValueCollationName()} AND [ExpirationDate] > GETUTCDATE()";

            using (var ctx = new SnDataContext(MainProvider))
            {
                var result = ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Value", DbType.String, tokenValue),
                        ctx.CreateParameter("@NewExpirationDate", DbType.DateTime2, newExpirationDate)
                    });
                }).Result;

                if (result == null || result == DBNull.Value)
                    throw new InvalidAccessTokenException("Token not found or it is expired.");
            }
        }

        public void DeleteAccessToken(string tokenValue)
        {
            var sql = "DELETE FROM [dbo].[AccessTokens] " +
                      $"WHERE [Value] = @Value COLLATE {GetAccessTokenValueCollationName()}";

            using (var ctx = new SnDataContext(MainProvider))
            {
                ctx.ExecuteNonQueryAsync(sql, cmd =>
                    {
                        cmd.Parameters.Add(ctx.CreateParameter("@Value", DbType.String, tokenValue));
                    }).Wait();
            }
        }

        public void DeleteAccessTokensByUser(int userId)
        {
            using (var ctx = new SnDataContext(MainProvider))
            {
                ctx.ExecuteNonQueryAsync("DELETE FROM [dbo].[AccessTokens] WHERE [UserId] = @UserId", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@UserId", DbType.Int32, userId));
                }).Wait();
            }
        }

        public void DeleteAccessTokensByContent(int contentId)
        {
            using (var ctx = new SnDataContext(MainProvider))
            {
                ctx.ExecuteNonQueryAsync("DELETE FROM [dbo].[AccessTokens] WHERE [ContentId] = @ContentId", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@ContentId", DbType.Int32, contentId));
                }).Wait();
            }
        }

        public void CleanupAccessTokens()
        {
            const string sql = "DELETE FROM [dbo].[AccessTokens] WHERE [ExpirationDate] < DATEADD(MINUTE, -30, GETUTCDATE())";

            using (var ctx = new SnDataContext(MainProvider))
            {
                ctx.ExecuteNonQueryAsync(sql).Wait();
            }
        }
    }
}
