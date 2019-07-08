using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using IsolationLevel = System.Data.IsolationLevel;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    //UNDONE:DB: ASYNC API: CancellationToken is not used in this class.
    public class SnDataContext : IDisposable
    {
        private readonly IDbCommandFactory _commandFactory;
        private readonly DbConnection _connection;
        private TransactionWrapper _transaction;

        public CancellationToken CancellationToken { get; }

        //UNDONE:DB: ? Transaction timeout handling idea:
        //CancellationToken GetTimeoutCancellationToken()
        //{
        //    if (_transaction == null)
        //        return CancellationToken;
        //    var timeoutSrc = new CancellationTokenSource(TimeSpan.FromMinutes(10)).Token;
        //    if (CancellationToken == CancellationToken.None)
        //        return timeoutSrc;
        //    return CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, timeoutSrc).Token;
        //}

        public SnDataContext(IDbCommandFactory commandFactory, CancellationToken cancellationToken = default(CancellationToken))
        {
            _commandFactory = commandFactory;
            CancellationToken = cancellationToken;
            _connection = _commandFactory.CreateConnection();
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
            _transaction = _commandFactory.WrapTransaction(transaction) ?? new TransactionWrapper(transaction);
            return _transaction;
        }

        //UNDONE:DB@@@@ Handle Command/Connection/Transaction Timeout
        public async Task<int> ExecuteNonQueryAsync(string script, Action<DbCommand> setParams = null)
        {
            using (var cmd = _commandFactory.CreateCommand())
            {
                cmd.Connection = _connection;
                cmd.CommandTimeout = Configuration.Data.SqlCommandTimeout;
                cmd.CommandText = script;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = _transaction?.Transaction;

                setParams?.Invoke(cmd);

                return await cmd.ExecuteNonQueryAsync(CancellationToken);
            }
        }
        public async Task<object> ExecuteScalarAsync(string script, Action<DbCommand> setParams = null)
        {
            using (var cmd = _commandFactory.CreateCommand())
            {
                cmd.Connection = _connection;
                cmd.CommandTimeout = Configuration.Data.SqlCommandTimeout;
                cmd.CommandText = script;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = _transaction?.Transaction;

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
            using (var cmd = _commandFactory.CreateCommand())
            {
                cmd.Connection = _connection;
                cmd.CommandTimeout = Configuration.Data.SqlCommandTimeout;
                cmd.CommandText = script;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = _transaction?.Transaction;

                setParams?.Invoke(cmd);

                using (var reader = await cmd.ExecuteReaderAsync(CancellationToken))
                    return await callbackAsync(reader);
            }
        }

        public DbParameter CreateParameter(string name, DbType dbType, object value)
        {
            var prm = _commandFactory.CreateParameter();
            prm.ParameterName = name;
            prm.DbType = dbType;
            prm.Value = value;
            return prm;
        }
        public DbParameter CreateParameter(string name, DbType dbType, int size, object value)
        {
            var prm = _commandFactory.CreateParameter();
            prm.ParameterName = name;
            prm.DbType = dbType;
            prm.Size = size;
            prm.Value = value;
            return prm;
        }

    }
}
