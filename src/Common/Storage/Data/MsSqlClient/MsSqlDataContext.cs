using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using SenseNet.Configuration;
using IsolationLevel = System.Data.IsolationLevel;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    //UNDONE:DB: ASYNC API: CancellationToken is not used in this class.
    public class MsSqlDataContext : IDisposable
    {
        private readonly SqlConnection _connection;
        private TransactionWrapper _transaction;

        public CancellationToken CancellationToken { get; }

        public MsSqlDataContext(CancellationToken cancellationToken = default(CancellationToken))
        {
            //UNDONE:DB: TEST: not tested (packaging)
            CancellationToken = cancellationToken;
            _connection = new SqlConnection(ConnectionStrings.ConnectionString);
            _connection.Open();
        }
        public MsSqlDataContext(string connectionString, CancellationToken cancellationToken = default(CancellationToken))
        {
            //UNDONE:DB: TEST: not tested (packaging)
            CancellationToken = cancellationToken;
            _connection = new SqlConnection(connectionString ?? ConnectionStrings.ConnectionString);
            _connection.Open();
        }
        public MsSqlDataContext(ConnectionInfo connectionInfo, CancellationToken cancellationToken = default(CancellationToken))
        {
            //UNDONE:DB: TEST: not tested (packaging)
            CancellationToken = cancellationToken;
            _connection = new SqlConnection(GetConnectionString(connectionInfo) ?? ConnectionStrings.ConnectionString);
            _connection.Open();
        }

        private string GetConnectionString(ConnectionInfo connectionInfo)
        {
            string cnstr;

            if (string.IsNullOrEmpty(connectionInfo.ConnectionName))
                cnstr = ConnectionStrings.ConnectionString;
            else
                if(!ConnectionStrings.AllConnectionStrings.TryGetValue(connectionInfo.ConnectionName, out cnstr)
                        || cnstr == null)
                    throw new InvalidOperationException("Unknown connection name: " + connectionInfo.ConnectionName);

            var connectionBuilder = new SqlConnectionStringBuilder(cnstr);
            switch (connectionInfo.InitialCatalog)
            {
                case InitialCatalog.Initial:
                    break;
                case InitialCatalog.Master:
                    connectionBuilder.InitialCatalog = "master";
                    break;
                default:
                    throw new NotSupportedException("Unknown InitialCatalog");
            }

            if (!string.IsNullOrEmpty(connectionInfo.DataSource))
                connectionBuilder.DataSource = connectionInfo.DataSource;

            if (!string.IsNullOrEmpty(connectionInfo.InitialCatalogName))
                connectionBuilder.InitialCatalog = connectionInfo.InitialCatalogName;

            if (!string.IsNullOrWhiteSpace(connectionInfo.UserName))
            {
                if (string.IsNullOrWhiteSpace(connectionInfo.Password))
                    throw new NotSupportedException("Invalid credentials.");
                connectionBuilder.UserID = connectionInfo.UserName;
                connectionBuilder.Password = connectionInfo.Password;
                connectionBuilder.IntegratedSecurity = false;
            }
            return connectionBuilder.ToString();
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

        //UNDONE:DB@@@@ Handle Command/Connection/Transaction Timeout
        public async Task<int> ExecuteNonQueryAsync(string script, Action<SqlCommand> setParams = null)
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
        public async Task<object> ExecuteScalarAsync(string script, Action<SqlCommand> setParams = null)
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
        public async Task<T> ExecuteReaderAsync<T>(string script, Action<SqlCommand> setParams, Func<SqlDataReader, Task<T>> callbackAsync)
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
