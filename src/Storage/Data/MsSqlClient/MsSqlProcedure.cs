using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;

namespace SenseNet.Storage.Data.MsSqlClient
{
    internal class MsSqlProcedure : IDisposable
    {
        public void Dispose()
        {
            //UNDONE:DB@@@@ not implemented
        }

        public static async Task<T> ExecuteReaderAsync<T>(string sql, Func<SqlDataReader, T> callback)
        {
            return await ExecuteReaderAsync(sql, null, callback);
        }
        public static async Task<T> ExecuteReaderAsync<T>(string sql, Action<SqlProcedure> setParams, Func<SqlDataReader, T> callback)
        {
            var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text };

            setParams?.Invoke(cmd);

            SqlDataReader reader = null;
            try
            {
                reader = await cmd.ExecuteReaderAsync();
                return callback(reader);
            }
            finally
            {
                reader?.Dispose();
                cmd.Dispose();
            }
        }


        public CommandType CommandType { get; set; }

        public string CommandText { get; set; }
    }
}
