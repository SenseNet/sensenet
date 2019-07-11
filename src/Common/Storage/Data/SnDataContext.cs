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
    [Obsolete("##", true)]
    public class SnDataContext : IDisposable
    {
        private readonly IDbCommandFactory _commandFactory;
        private readonly DbConnection _connection;
        private TransactionWrapper _transaction;

        public CancellationToken CancellationToken { get; }

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

        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            TimeSpan timeout = default(TimeSpan))
        {
            var transaction = _connection.BeginTransaction(isolationLevel);
            _transaction = _commandFactory.WrapTransaction(transaction, timeout)
                ?? new TransactionWrapper(transaction, timeout);
            return _transaction;
        }

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

                var cancellationToken = GetCancellationToken();
                var result =  await cmd.ExecuteNonQueryAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException();
                return result;
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

                var cancellationToken = GetCancellationToken();
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException();
                return result;
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

                //UNDONE:DB@@@@@ Transaction timeout in ExecuteReaderAsync
                using (var reader = await cmd.ExecuteReaderAsync(GetCancellationToken()))
                    return await callbackAsync(reader);
            }
        }

        private CancellationToken GetCancellationToken()
        {
            if (_transaction == null)
                return CancellationToken;

            if (_transaction.Timeout == default(TimeSpan))
                return CancellationToken;

            var timeoutSrc = new CancellationTokenSource(_transaction.Timeout).Token;
            if (CancellationToken == CancellationToken.None)
                return timeoutSrc;
            return CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, timeoutSrc).Token;
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



    //UNDONE:DB@@@@@@ Refactor: Rename SnDctx to SnDataContext
    public abstract class SnDctx<TConnection, TCommand, TParameter, TReader> : IDataPlatform<TConnection, TCommand, TParameter>, IDisposable
        where TConnection : DbConnection
        where TCommand : DbCommand
        where TParameter : DbParameter
        where TReader : DbDataReader
    {
        private TConnection _connection;
        private TransactionWrapper _transaction;

        public TConnection Connection => _connection;
        public TransactionWrapper Transaction => _transaction;
        public CancellationToken CancellationToken { get; }


        protected SnDctx(CancellationToken cancellationToken = default(CancellationToken))
        {
            CancellationToken = cancellationToken;
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

        public virtual TransactionWrapper WrapTransaction(DbTransaction underlyingTransaction, TimeSpan timeout = default(TimeSpan))
        {
            return null;
        }

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

                return await cmd.ExecuteNonQueryAsync(CancellationToken);
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

                return await cmd.ExecuteScalarAsync(CancellationToken);
            }
        }
        public Task<T> ExecuteReaderAsync<T>(string script, Func<TReader, Task<T>> callback)
        {
            return ExecuteReaderAsync(script, null, callback);
        }
        public async Task<T> ExecuteReaderAsync<T>(string script, Action<TCommand> setParams, Func<TReader, Task<T>> callbackAsync)
        {
            using (var cmd = CreateCommand())
            {
                cmd.Connection = OpenConnection();
                cmd.CommandTimeout = Configuration.Data.SqlCommandTimeout;
                cmd.CommandText = script;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = _transaction?.Transaction;

                setParams?.Invoke(cmd);

                using (var reader = (TReader)await cmd.ExecuteReaderAsync(CancellationToken))
                    return await callbackAsync(reader);
            }
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
