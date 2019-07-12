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
    public abstract class SnDataContext<TConnection, TCommand, TParameter, TReader> : IDataPlatform<TConnection, TCommand, TParameter>, IDisposable
        where TConnection : DbConnection
        where TCommand : DbCommand
        where TParameter : DbParameter
        where TReader : DbDataReader
    {
        private TConnection _connection;
        private TransactionWrapper _transaction;

        public TConnection Connection => _connection;
        public TransactionWrapper Transaction => _transaction;
        private readonly CancellationToken _cancellationToken;


        protected SnDataContext(CancellationToken cancellationToken = default(CancellationToken))
        {
            _cancellationToken = cancellationToken;
        }


        public virtual void Dispose()
        {
            if (_transaction?.Status == TransactionStatus.Active)
                _transaction.Rollback();
            _connection?.Dispose();
        }

        public abstract TConnection CreateConnection();
        public abstract TCommand CreateCommand();
        public abstract TParameter CreateParameter();

        public abstract TransactionWrapper WrapTransaction(DbTransaction underlyingTransaction,
            TimeSpan timeout = default(TimeSpan));

        private TConnection OpenConnection()
        {
            if (_connection == null)
            {
                _connection = CreateConnection();
                _connection.Open();
            }
            return _connection;
        }

        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            TimeSpan timeout = default(TimeSpan))
        {

            var transaction = OpenConnection().BeginTransaction(isolationLevel);
            _transaction = WrapTransaction(transaction, timeout) ?? new TransactionWrapper(transaction, timeout);
            return _transaction;
        }

        public async Task<int> ExecuteNonQueryAsync(string script, Action<TCommand> setParams = null)
        {
            using (var cmd = CreateCommand())
            {
                cmd.Connection = OpenConnection();
                cmd.CommandTimeout = Configuration.Data.SqlCommandTimeout;
                cmd.CommandText = script;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = _transaction?.Transaction;

                setParams?.Invoke(cmd);

                var cancellationToken = GetCancellationToken();
                var result = await cmd.ExecuteNonQueryAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }
        }
        public async Task<object> ExecuteScalarAsync(string script, Action<TCommand> setParams = null)
        {
            using (var cmd = CreateCommand())
            {
                cmd.Connection = OpenConnection();
                cmd.CommandTimeout = Configuration.Data.SqlCommandTimeout;
                cmd.CommandText = script;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = _transaction?.Transaction;

                setParams?.Invoke(cmd);

                var cancellationToken = GetCancellationToken();
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }
        }
        public Task<T> ExecuteReaderAsync<T>(string script, Func<TReader, CancellationToken, Task<T>> callback)
        {
            return ExecuteReaderAsync(script, null, callback);
        }
        public async Task<T> ExecuteReaderAsync<T>(string script, Action<TCommand> setParams, Func<TReader, CancellationToken, Task<T>> callbackAsync)
        {
            using (var cmd = CreateCommand())
            {
                cmd.Connection = OpenConnection();
                cmd.CommandTimeout = Configuration.Data.SqlCommandTimeout;
                cmd.CommandText = script;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = _transaction?.Transaction;

                setParams?.Invoke(cmd);

                var cancellationToken = GetCancellationToken();
                using (var reader = (TReader) await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await callbackAsync(reader, cancellationToken);
                }
            }
        }

        private CancellationToken GetCancellationToken() //UNDONE:DB@@@ Create only one!
        {
            if (_transaction == null)
                return _cancellationToken;

            if (_transaction.Timeout == default(TimeSpan))
                return _cancellationToken;

            var timeoutSrc = new CancellationTokenSource(_transaction.Timeout).Token;
            if (_cancellationToken == CancellationToken.None)
                return timeoutSrc;
            return CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, timeoutSrc).Token;
        }

        public TParameter CreateParameter(string name, DbType dbType, object value)
        {
            var prm = CreateParameter();
            prm.ParameterName = name;
            prm.DbType = dbType;
            prm.Value = value;
            return prm;
        }
        public DbParameter CreateParameter(string name, DbType dbType, int size, object value)
        {
            var prm = CreateParameter();
            prm.ParameterName = name;
            prm.DbType = dbType;
            prm.Size = size;
            prm.Value = value;
            return prm;
        }
    }

}
