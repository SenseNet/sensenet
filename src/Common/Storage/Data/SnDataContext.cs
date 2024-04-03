using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Tools;
using IsolationLevel = System.Data.IsolationLevel;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    public abstract class SnDataContext : IDisposable
    {
        private DbConnection _connection;
        private TransactionWrapper _transaction;
        private readonly IRetrier _retrier;
        private readonly CancellationToken _cancellationToken;
        protected DataOptions DataOptions { get; }

        public DbConnection Connection => _connection;
        public TransactionWrapper Transaction => _transaction;
        public CancellationToken CancellationToken => _cancellationToken;

        /// <summary>
        /// Used only test purposes.
        /// </summary>
        internal bool IsDisposed { get; private set; }

        public bool NeedToCleanupFiles { get; set; }

        protected SnDataContext(DataOptions options, IRetrier retrier, CancellationToken cancel = default)
        {
            _retrier = retrier ?? new DefaultRetrier(Options.Create(new RetrierOptions()), NullLogger<DefaultRetrier>.Instance);
            _cancellationToken = cancel;
            DataOptions = options ?? new DataOptions();
        }

        public virtual void Dispose()
        {
            if (_transaction?.Status == TransactionStatus.Active)
                _transaction.Rollback();
            _connection?.Dispose();
            IsDisposed = true;
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
            if (_connection?.State == ConnectionState.Closed || _connection?.State == ConnectionState.Broken)
            {
                _connection.Dispose();
                _connection = null;
            }
            if (_connection == null)
            {
                _connection = CreateConnection();
                _connection.Open();
            }
            return _connection;
        }

        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            TimeSpan timeout = default)
        {
            _transaction = RetryAsync(() =>
            {
                SnTrace.Database.Write($"SnDataContext: BeginTransaction.");
                var transaction = OpenConnection().BeginTransaction(isolationLevel);
                var wrappedTransaction = WrapTransaction(transaction, _cancellationToken, timeout)
                               ?? new TransactionWrapper(transaction, DataOptions, timeout, _cancellationToken);
                return Task.FromResult(wrappedTransaction);

            }, CancellationToken.None).GetAwaiter().GetResult();
            
            return _transaction;
        }

        public async Task<int> ExecuteNonQueryAsync(string script, Action<DbCommand> setParams = null)
        {
            var nonQueryResult = await RetryAsync(async () =>
            {
                using (var cmd = CreateCommand())
                {
                    cmd.Connection = OpenConnection();
                    cmd.CommandTimeout = DataOptions.DbCommandTimeout;
                    cmd.CommandText = script;
                    cmd.CommandType = CommandType.Text;
                    cmd.Transaction = _transaction?.Transaction;

                    setParams?.Invoke(cmd);

                    var cancellationToken = _transaction?.CancellationToken ?? _cancellationToken;
                    var result = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    return result;
                }
            }, _transaction?.CancellationToken ?? _cancellationToken);

            return nonQueryResult;
        }

        public async Task<object> ExecuteScalarAsync(string script, Action<DbCommand> setParams = null)
        {
            var scalarResult = await RetryAsync(async () =>
            {
                using (var cmd = CreateCommand())
                {
                    cmd.Connection = OpenConnection();
                    cmd.CommandTimeout = DataOptions.DbCommandTimeout;
                    cmd.CommandText = script;
                    cmd.CommandType = CommandType.Text;
                    cmd.Transaction = _transaction?.Transaction;

                    setParams?.Invoke(cmd);

                    var cancellationToken = _transaction?.CancellationToken ?? _cancellationToken;
                    var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    return result;
                }
            }, _transaction?.CancellationToken ?? _cancellationToken);

            return scalarResult;
        }

        public Task<T> ExecuteReaderAsync<T>(string script, Func<DbDataReader, CancellationToken, Task<T>> callback)
        {
            return ExecuteReaderAsync(script, null, callback);
        }

        public async Task<T> ExecuteReaderAsync<T>(string script, Action<DbCommand> setParams,
            Func<DbDataReader, CancellationToken, Task<T>> callbackAsync)
        {
            var readerResult = await RetryAsync(async () =>
            {
                using (var cmd = CreateCommand())
                {
                    cmd.Connection = OpenConnection();
                    cmd.CommandTimeout = DataOptions.DbCommandTimeout;
                    cmd.CommandText = script;
                    cmd.CommandType = CommandType.Text;
                    cmd.Transaction = _transaction?.Transaction;

                    setParams?.Invoke(cmd);

                    var cancellationToken = _transaction?.CancellationToken ?? _cancellationToken;
                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var result = await callbackAsync(reader, cancellationToken).ConfigureAwait(false);
                        return result;
                    }
                }
            }, _transaction?.CancellationToken ?? _cancellationToken);

            return readerResult;
        }

        internal static bool ShouldRetryOnError(Exception ex)
        {
            //TODO: generalize the expression by relying on error codes instead of hardcoded message texts
            return (ex is InvalidOperationException && ex.Message.Contains("connection from the pool")) ||
                   (ex is SqlException && ex.Message.Contains("A network-related or instance-specific error occurred"));
        }

        protected string GetOperationMessage(string name, string script)
        {
            const int maxLength = 80;
            return $"{this.GetType().Name}.{name}: {(script.Length < maxLength ? script : script.Substring(0, maxLength))}";
        }
        
        public Task RetryAsync(Func<Task> action, CancellationToken cancel)
        {
            return RetryAsync<object>(async () =>
            {
                await action().ConfigureAwait(false);
                return null;
            }, cancel);
        }
        public Task<T> RetryAsync<T>(Func<Task<T>> action, CancellationToken cancel)
        {
            return _retrier.RetryAsync(action,
                shouldRetryOnError: (ex, _) => ShouldRetryOnError(ex),
                onAfterLastIteration: (_, ex, i) =>
                {
                    SnTrace.Database.WriteError(
                        $"Data layer error: {ex.Message}. Retry cycle ended after {i} iterations.");
                    throw new InvalidOperationException("Data layer timeout occurred.", ex);
                },
                cancel: cancel);
        }
    }
}
