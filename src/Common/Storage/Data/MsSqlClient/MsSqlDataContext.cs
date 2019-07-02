using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using SenseNet.Configuration;
using IsolationLevel = System.Data.IsolationLevel;

namespace SenseNet.Common.Storage.Data.MsSqlClient
{
    public class MsSqlDataContext : IDisposable
    {
        private readonly SqlConnection _connection;
        private TransactionWrapper _transaction;

        public CancellationToken CancellationToken { get; }

        public MsSqlDataContext(CancellationToken cancellationToken = default(CancellationToken))
        {
            CancellationToken = cancellationToken;
            _connection = new SqlConnection(ConnectionStrings.ConnectionString);
            _connection.Open();
        }

        public void Dispose()
        {
            if (_transaction?.Status == TransactionStatus.Active)
                _transaction.Rollback();
            _connection.Dispose();
        }

        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            var transaction = _connection.BeginTransaction(isolationLevel);
            _transaction = new TransactionWrapper(transaction);
            return _transaction;
        }

        //UNDONE:DB: Handle Command/Connection/Transaction Timeout
        public async Task<int> ExecuteNonQueryAsync(string script, Action<DbCommand> setParams = null)
        {
            using (var cmd = new SqlCommand())
            {
                cmd.Connection = _connection;
                cmd.CommandTimeout = Configuration.Data.SqlCommandTimeout;
                cmd.CommandText = script;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = (SqlTransaction)_transaction?.Transaction;

                setParams?.Invoke(cmd);

                return await cmd.ExecuteNonQueryAsync(CancellationToken);
            }
        }
        public async Task<object> ExecuteScalarAsync(string script, Action<DbCommand> setParams = null)
        {
            using (var cmd = new SqlCommand())
            {
                cmd.Connection = _connection;
                cmd.CommandTimeout = Configuration.Data.SqlCommandTimeout;
                cmd.CommandText = script;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = (SqlTransaction)_transaction?.Transaction;

                setParams?.Invoke(cmd);

                return await cmd.ExecuteScalarAsync(CancellationToken);
            }
        }
        public Task<T> ExecuteReaderAsync<T>(string script, Func<DbDataReader, Task<T>> callback)
        {
            return ExecuteReaderAsync(script, null, callback);
        }
        public async Task<T> ExecuteReaderAsync<T>(string script, Action<DbCommand> setParams, Func<DbDataReader, Task<T>> callbackAsync)
        {
            using (var cmd = new SqlCommand())
            {
                cmd.Connection = _connection;
                cmd.CommandTimeout = Configuration.Data.SqlCommandTimeout;
                cmd.CommandText = script;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = (SqlTransaction)_transaction?.Transaction;

                setParams?.Invoke(cmd);

                using (var reader = await cmd.ExecuteReaderAsync(CancellationToken))
                    return await callbackAsync(reader);
            }
        }

        public SqlParameter CreateParameter(string name, SqlDbType dbType, object value)
        {
            return new SqlParameter
            {
                ParameterName = name,
                SqlDbType = dbType,
                Value = value
            };
        }
        public SqlParameter CreateParameter(string name, SqlDbType dbType, int size, object value)
        {
            return new SqlParameter
            {
                ParameterName = name,
                SqlDbType = dbType,
                Size = size,
                Value = value
            };
        }
    }
}
