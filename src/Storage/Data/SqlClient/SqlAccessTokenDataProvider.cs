using System;
using System.Collections.Generic;
using System.Data;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    public class SqlAccessTokenDataProvider : IAccessTokenDataProviderExtension
    {
        private DataProvider _mainProvider;
        public DataProvider MainProvider => _mainProvider ?? (_mainProvider = DataProvider.Instance);

        private string _accessTokenValueCollationName;
        private string AccessTokenValueCollationName
        {
            get
            {
                if (_accessTokenValueCollationName == null)
                {
                    using (var proc = MainProvider.CreateDataProcedure("SELECT c.collation_name, c.* " +
                                                                           "FROM sys.columns c " +
                                                                           "  JOIN sys.tables t ON t.object_id = c.object_id " +
                                                                           "WHERE t.name = 'AccessTokens' AND c.name = N'Value'"))
                    {
                        proc.CommandType = CommandType.Text;

                        var result = proc.ExecuteScalar();
                        var originalCollation = Convert.ToString(result);
                        _accessTokenValueCollationName = originalCollation.Replace("_CI_", "_CS_");
                    }
                }
                return _accessTokenValueCollationName;
            }
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
            using (var proc = MainProvider.CreateDataProcedure("TRUNCATE TABLE AccessTokens"))
            {
                proc.CommandType = CommandType.Text;
                proc.ExecuteNonQuery();
            }
        }

        public void SaveAccessToken(AccessToken token)
        {
            using (var proc = MainProvider.CreateDataProcedure(
                "INSERT INTO [dbo].[AccessTokens] " +
                "([Value],[UserId],[ContentId],[Feature],[CreationDate],[ExpirationDate]) VALUES " +
                "(@Value, @UserId, @ContentId, @Feature, @CreationDate, @ExpirationDate)" +
                "SELECT @@IDENTITY")
                .AddParameter("@Value", token.Value, DbType.String, 1000)
                .AddParameter("@UserId", token.UserId)
                .AddParameter("@ContentId", token.ContentId != 0 ? (object)token.ContentId : DBNull.Value, DbType.Int32)
                .AddParameter("@Feature", token.Feature != null ? (object)token.Feature : DBNull.Value, DbType.String, 1000)
                .AddParameter("@CreationDate", token.CreationDate)
                .AddParameter("@ExpirationDate", token.ExpirationDate))
            {
                proc.CommandType = CommandType.Text;

                var result = proc.ExecuteScalar();
                token.Id = Convert.ToInt32(result);
            }
        }

        public AccessToken LoadAccessTokenById(int accessTokenId)
        {
            using (var proc = MainProvider.CreateDataProcedure("SELECT TOP 1 * FROM [dbo].[AccessTokens] WHERE [AccessTokenId] = @Id")
                .AddParameter("@Id", accessTokenId))
            {
                proc.CommandType = CommandType.Text;

                using (var reader = proc.ExecuteReader())
                    return reader.Read() ? GetAccessTokenFromReader(reader) : null;
            }
        }

        public AccessToken LoadAccessToken(string tokenValue, int contentId, string feature)
        {
            var sql = "SELECT TOP 1 * FROM [dbo].[AccessTokens] " +
                      $"WHERE [Value] = @Value COLLATE {AccessTokenValueCollationName} AND [ExpirationDate] > GETUTCDATE() AND " +
                      (contentId != 0 ? $"ContentId = {contentId} AND " : "ContentId IS NULL AND ") +
                      (feature != null ? $"Feature = '{feature}'" : "Feature IS NULL");

            using (var proc = MainProvider.CreateDataProcedure(sql)
                .AddParameter("@Value", tokenValue))
            {
                proc.CommandType = CommandType.Text;

                using (var reader = proc.ExecuteReader())
                    return reader.Read() ? GetAccessTokenFromReader(reader) : null;
            }
        }

        public AccessToken[] LoadAccessTokens(int userId)
        {
            using (var proc = MainProvider.CreateDataProcedure("SELECT * FROM [dbo].[AccessTokens] " +
                                                                   "WHERE [UserId] = @UserId AND [ExpirationDate] > GETUTCDATE()")
                .AddParameter("@UserId", userId))
            {
                proc.CommandType = CommandType.Text;

                var tokens = new List<AccessToken>();
                using (var reader = proc.ExecuteReader())
                    while (reader.Read())
                        tokens.Add(GetAccessTokenFromReader(reader));

                return tokens.ToArray();
            }
        }

        public void UpdateAccessToken(string tokenValue, DateTime newExpirationDate)
        {
            using (var proc = MainProvider.CreateDataProcedure("UPDATE [AccessTokens] " +
                                                                   "SET [ExpirationDate] = @NewExpirationDate " +
                                                                   "   OUTPUT INSERTED.AccessTokenId" +
                                                                   " WHERE [Value] = @Value " +
                                                                   $" COLLATE {AccessTokenValueCollationName} AND [ExpirationDate] > GETUTCDATE()")
                .AddParameter("@Value", tokenValue)
                .AddParameter("@NewExpirationDate", newExpirationDate))
            {
                proc.CommandType = CommandType.Text;

                var result = proc.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    throw new InvalidAccessTokenException("Token not found or it is expired.");
            }

        }

        public void DeleteAccessToken(string tokenValue)
        {
            using (var proc = MainProvider.CreateDataProcedure("DELETE FROM [dbo].[AccessTokens] " +
                                                                   $"WHERE [Value] = @Value COLLATE {AccessTokenValueCollationName}")
                .AddParameter("@Value", tokenValue))
            {
                proc.CommandType = CommandType.Text;
                proc.ExecuteNonQuery();
            }
        }

        public void DeleteAccessTokensByUser(int userId)
        {
            using (var proc = MainProvider.CreateDataProcedure("DELETE FROM [dbo].[AccessTokens] WHERE [UserId] = @UserId")
                .AddParameter("@UserId", userId))
            {
                proc.CommandType = CommandType.Text;
                proc.ExecuteNonQuery();
            }
        }

        public void DeleteAccessTokensByContent(int contentId)
        {
            using (var proc = MainProvider.CreateDataProcedure("DELETE FROM [dbo].[AccessTokens] WHERE [ContentId] = @ContentId")
                .AddParameter("@ContentId", contentId))
            {
                proc.CommandType = CommandType.Text;
                proc.ExecuteNonQuery();
            }
        }

        public void CleanupAccessTokens()
        {
            var sql = "DELETE FROM [dbo].[AccessTokens] WHERE [ExpirationDate] < DATEADD(MINUTE, -30, GETUTCDATE())";
            using (var proc = MainProvider.CreateDataProcedure(sql))
            {
                proc.CommandType = CommandType.Text;
                proc.ExecuteNonQuery();
            }
        }
    }
}
