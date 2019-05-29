using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data.SqlClient;

namespace SenseNet.Common.Storage.Data.MsSqlClient
{
    public class MsSqlProcedure : IDisposable
    {
        public void Dispose()
        {
            //UNDONE:DB@@@@ not implemented
        }

        public static Task<T> ExecuteReaderAsync<T>(string sql, Func<SqlDataReader, T> callback)
        {
            return ExecuteReaderAsync(sql, null, callback);
        }
        public static async Task<T> ExecuteReaderAsync<T>(string sql, Action<SqlProcedure> setParams, Func<SqlDataReader, T> callback)
        {
            using (var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text })
            {
                setParams?.Invoke(cmd);
                using (var reader = await cmd.ExecuteReaderAsync())
                    return callback(reader);
            }
        }

        public static async Task<T> ExecuteScalarAsync<T>(string sql, Func<object, T> callback)
        {
            return await ExecuteScalarAsync(sql, null, callback);
        }
        public static async Task<T> ExecuteScalarAsync<T>(string sql, Action<SqlProcedure> setParams, Func<object, T> callback)
        {
            using (var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text })
            {
                setParams?.Invoke(cmd);
                var value = await cmd.ExecuteScalarAsync();
                return callback(value);
            }
        }

        public static async Task<int> ExecuteNonQueryAsync(string sql)
        {
            return await ExecuteNonQueryAsync(sql, null);
        }
        public static async Task<int> ExecuteNonQueryAsync(string sql, Action<SqlProcedure> setParams)
        {
            using (var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text })
            {
                setParams?.Invoke(cmd);
                return await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
