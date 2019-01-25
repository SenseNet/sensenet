using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    public class SqlAccessTokenDataProvider : IAccessTokenDataProviderExtension
    {
        public DataProvider MetadataProvider { get; set; }

        private string _accessTokenValueCollationName;
        private string AccessTokenValueCollationName
        {
            get
            {
                if (_accessTokenValueCollationName == null)
                {
                    using (var proc = new SqlProcedure())
                    {
                        proc.CommandText = "SELECT c.collation_name, c.* FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id " +
                                           "WHERE t.name = 'AccessTokens' AND c.name = N'Value'";

                        proc.CommandType = CommandType.Text;

                        var result = proc.ExecuteScalar();
                        var originalCollation = Convert.ToString(result);
                        _accessTokenValueCollationName = originalCollation.Replace("_CI_", "_CS_");
                    }
                }
                return _accessTokenValueCollationName;
            }
        }

        private AccessToken GetAccessTokenFromReader(SqlDataReader reader)
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
            using (var proc = new SqlProcedure())
            {
                proc.CommandText = "TRUNCATE TABLE AccessTokens";
                proc.CommandType = CommandType.Text;
                proc.ExecuteNonQuery();
            }
        }

        public void SaveAccessToken(AccessToken token)
        {
            using (var proc = new SqlProcedure())
            {
                proc.CommandText = "INSERT INTO [dbo].[AccessTokens] " +
                                   "([Value],[UserId],[ContentId],[Feature],[CreationDate],[ExpirationDate]) VALUES " +
                                   "(@Value, @UserId, @ContentId, @Feature, @CreationDate, @ExpirationDate)" +
                                   "SELECT @@IDENTITY";

                proc.CommandType = CommandType.Text;
                proc.Parameters.Add("@Value", SqlDbType.NVarChar, 1000).Value = token.Value;
                proc.Parameters.Add("@UserId", SqlDbType.Int).Value = token.UserId;
                proc.Parameters.Add("@ContentId", SqlDbType.Int).Value = token.ContentId != 0 ? (object)token.ContentId : DBNull.Value;
                proc.Parameters.Add("@Feature", SqlDbType.NVarChar, 1000).Value = token.Feature != null ? (object)token.Feature : DBNull.Value;
                proc.Parameters.Add("@CreationDate", SqlDbType.DateTime).Value = token.CreationDate;
                proc.Parameters.Add("@ExpirationDate", SqlDbType.DateTime).Value = token.ExpirationDate;

                var result = proc.ExecuteScalar();
                token.Id = Convert.ToInt32(result);
            }
        }

        public AccessToken LoadAccessTokenById(int accessTokenId)
        {
            using (var proc = new SqlProcedure())
            {
                var sql = $"SELECT TOP 1 * FROM [dbo].[AccessTokens] WHERE [AccessTokenId] = @Id";

                proc.CommandText = sql;
                proc.CommandType = CommandType.Text;
                proc.Parameters.Add("@Id", SqlDbType.Int).Value = accessTokenId;

                using (var reader = proc.ExecuteReader())
                    return reader.Read() ? GetAccessTokenFromReader(reader) : null;
            }
        }

        public AccessToken LoadAccessToken(string tokenValue, int contentId, string feature)
        {
            using (var proc = new SqlProcedure())
            {
                var sql = "SELECT TOP 1 * FROM [dbo].[AccessTokens] " +
                          $"WHERE [Value] = @Value COLLATE {AccessTokenValueCollationName} AND [ExpirationDate] > @Now AND " +
                          (contentId != 0 ? $"ContentId = {contentId} AND " : "ContentId IS NULL AND ") +
                          (feature != null ? $"Feature = '{feature}'" : "Feature IS NULL");

                proc.CommandText = sql;
                proc.CommandType = CommandType.Text;
                proc.Parameters.Add("@Value", SqlDbType.NVarChar, 1000).Value = tokenValue;
                proc.Parameters.Add("@Now", SqlDbType.DateTime).Value = DateTime.UtcNow;

                using (var reader = proc.ExecuteReader())
                    return reader.Read() ? GetAccessTokenFromReader(reader) : null;
            }
        }

        public AccessToken[] LoadAccessTokens(int userId)
        {
            using (var proc = new SqlProcedure())
            {
                var sql = "SELECT * FROM [dbo].[AccessTokens] WHERE [UserId] = @UserId AND [ExpirationDate] > @Now";

                proc.CommandText = sql;
                proc.CommandType = CommandType.Text;
                proc.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                proc.Parameters.Add("@Now", SqlDbType.DateTime).Value = DateTime.UtcNow;

                var tokens = new List<AccessToken>();
                using (var reader = proc.ExecuteReader())
                    while (reader.Read())
                        tokens.Add(GetAccessTokenFromReader(reader));

                return tokens.ToArray();
            }
        }

        public void UpdateAccessToken(string tokenValue, DateTime newExpirationDate)
        {
            using (var proc = new SqlProcedure())
            {
                proc.CommandText = "UPDATE [AccessTokens] SET [ExpirationDate] = @NewExpirationDate OUTPUT INSERTED.AccessTokenId" +
                                   $" WHERE [Value] = @Value COLLATE {AccessTokenValueCollationName} AND [ExpirationDate] > @Now";

                proc.CommandType = CommandType.Text;
                proc.Parameters.Add("@Value", SqlDbType.NVarChar, 1000).Value = tokenValue;
                proc.Parameters.Add("@Now", SqlDbType.DateTime).Value = DateTime.UtcNow;
                proc.Parameters.Add("@NewExpirationDate", SqlDbType.DateTime).Value = newExpirationDate;

                var result = proc.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    throw new InvalidAccessTokenException("Token not found or it is expired.");
            }

        }

        public void DeleteAccessToken(string tokenValue)
        {
            using (var proc = new SqlProcedure())
            {
                proc.CommandText = $"DELETE FROM [dbo].[AccessTokens] WHERE [Value] = @Value COLLATE {AccessTokenValueCollationName}";
                proc.Parameters.Add("@Value", SqlDbType.NVarChar, 1000).Value = tokenValue;
                proc.CommandType = CommandType.Text;
                proc.ExecuteNonQuery();
            }
        }

        public void DeleteAccessTokensByUser(int userId)
        {
            using (var proc = new SqlProcedure())
            {
                proc.CommandText = $"DELETE FROM [dbo].[AccessTokens] WHERE [UserId] = @UserId";
                proc.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                proc.CommandType = CommandType.Text;
                proc.ExecuteNonQuery();
            }
        }

        public void DeleteAccessTokensByContent(int contentId)
        {
            using (var proc = new SqlProcedure())
            {
                proc.CommandText = $"DELETE FROM [dbo].[AccessTokens] WHERE [ContentId] = @ContentId";
                proc.Parameters.Add("@ContentId", SqlDbType.Int).Value = contentId;
                proc.CommandType = CommandType.Text;
                proc.ExecuteNonQuery();
            }
        }
    }
}
