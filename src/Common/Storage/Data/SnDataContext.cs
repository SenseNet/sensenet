using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using SenseNet.Diagnostics;
using IsolationLevel = System.Data.IsolationLevel;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    public abstract class SnDataContext : IDisposable
    {
        private DbConnection _connection;
        private TransactionWrapper _transaction;
        private readonly CancellationToken _cancellationToken;

        public DbConnection Connection => _connection;
        public TransactionWrapper Transaction => _transaction;
        public CancellationToken CancellationToken => _cancellationToken;

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

        public abstract DbConnection CreateConnection();
        public abstract DbCommand CreateCommand();
        public abstract DbParameter CreateParameter();

        public DbParameter CreateParameter(string name, DbType dbType, object value)
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

        public abstract TransactionWrapper WrapTransaction(DbTransaction underlyingTransaction,
            CancellationToken cancellationToken, TimeSpan timeout = default(TimeSpan));

        protected DbConnection OpenConnection()
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
            _transaction = WrapTransaction(transaction, _cancellationToken, timeout)
                           ?? new TransactionWrapper(transaction, _cancellationToken, timeout);
            return _transaction;
        }

        public async Task<int> ExecuteNonQueryAsync(string script, Action<DbCommand> setParams = null)
        {
            using (var op = SnTrace.Database.StartOperation(GetOperationMessage("ExecuteNonQueryAsync", script)))
            {
                using (var cmd = CreateCommand())
                {
                    cmd.Connection = OpenConnection();
                    cmd.CommandTimeout = Configuration.Data.DbCommandTimeout;
                    cmd.CommandText = script;
                    cmd.CommandType = CommandType.Text;
                    cmd.Transaction = _transaction?.Transaction;

                    setParams?.Invoke(cmd);

                    var cancellationToken = _transaction?.CancellationToken ?? _cancellationToken;
                    var result = await cmd.ExecuteNonQueryAsync(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    op.Successful = true;
                    return result;
                }
            }
        }

        public async Task<object> ExecuteScalarAsync(string script, Action<DbCommand> setParams = null)
        {
            using (var op = SnTrace.Database.StartOperation(GetOperationMessage("ExecuteScalarAsync", script)))
            {
                using (var cmd = CreateCommand())
                {
                    cmd.Connection = OpenConnection();
                    cmd.CommandTimeout = Configuration.Data.DbCommandTimeout;
                    cmd.CommandText = script;
                    cmd.CommandType = CommandType.Text;
                    cmd.Transaction = _transaction?.Transaction;

                    setParams?.Invoke(cmd);

                    var cancellationToken = _transaction?.CancellationToken ?? _cancellationToken;
                    var result = await cmd.ExecuteScalarAsync(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    op.Successful = true;
                    return result;
                }
            }
        }

        public Task<T> ExecuteReaderAsync<T>(string script, Func<DbDataReader, CancellationToken, Task<T>> callback)
        {
            return ExecuteReaderAsync(script, null, callback);
        }
        public async Task<T> ExecuteReaderAsync<T>(string script, Action<DbCommand> setParams,
            Func<DbDataReader, CancellationToken, Task<T>> callbackAsync)
        {
            using (var op = SnTrace.Database.StartOperation(GetOperationMessage("ExecuteReaderAsync", script)))
            {
                try
                {
                    using (var cmd = CreateCommand())
                    {
                        cmd.Connection = OpenConnection();
                        cmd.CommandTimeout = Configuration.Data.DbCommandTimeout;
                        cmd.CommandText = script;
                        cmd.CommandType = CommandType.Text;
                        cmd.Transaction = _transaction?.Transaction;

                        setParams?.Invoke(cmd);

                        var cancellationToken = _transaction?.CancellationToken ?? _cancellationToken;
                        using (var reader = (DbDataReader)await cmd.ExecuteReaderAsync(cancellationToken))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var result = await callbackAsync(reader, cancellationToken);
                            op.Successful = true;
                            return result;
                        }
                    }
                }
                catch (Exception e)
                {
                    SnTrace.WriteError(e.ToString());
                    throw;
                }
                using (var cmd = CreateCommand())
                {
                    cmd.Connection = OpenConnection();
                    cmd.CommandTimeout = Configuration.Data.DbCommandTimeout;
                    cmd.CommandText = script;
                    cmd.CommandType = CommandType.Text;
                    cmd.Transaction = _transaction?.Transaction;

                    setParams?.Invoke(cmd);

                    var cancellationToken = _transaction?.CancellationToken ?? _cancellationToken;
                    using (var reader = (DbDataReader)await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var result = await callbackAsync(reader, cancellationToken);
                        op.Successful = true;
                        return result;
                    }
                }
            }
        }

        protected string GetOperationMessage(string name, string script)
        {
            const int maxLength = 80;
            return $"{this.GetType().Name}.{name}: {(script.Length < maxLength ? script : script.Substring(0, maxLength))}";
        }
    }
}
