using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
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
        private readonly CancellationToken _cancellationToken;
        protected DataOptions DataOptions { get; }

        public DbConnection Connection => _connection;
        public TransactionWrapper Transaction => _transaction;
        public CancellationToken CancellationToken => _cancellationToken;

        private static int _connectionCount;
        private string ContextId { get; } = Guid.NewGuid().ToString();

        public static ConcurrentDictionary<string, string>
            OpenConnections = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Used only test purposes.
        /// </summary>
        internal bool IsDisposed { get; private set; }

        public bool NeedToCleanupFiles { get; set; }

        [Obsolete("Use the constructor that expects data options instead.", true)]
        protected SnDataContext(CancellationToken cancellationToken) : this(new DataOptions(), cancellationToken)
        {
        }
        protected SnDataContext(DataOptions options, CancellationToken cancel = default)
        {
            _cancellationToken = cancel;
            DataOptions = options ?? new DataOptions();
        }

        public virtual void Dispose()
        {
            //if (IsDisposed || _connection?.State != ConnectionState.Open ||
            //    (_transaction != null && _transaction.Status != TransactionStatus.Committed))
            //{
                SnTrace.Database.Write($"SnDataContext #{ContextId}: DISPOSE" +
                                       $" _connection: {_connection?.State.ToString() ?? "null"}," +
                                       $" _transaction: {_transaction?.Status.ToString() ?? "null"}," +
                                       $" IsDisposed: {IsDisposed}," /*+
                                       $" StackTrace: {Environment.StackTrace}"*/);
            //}

            var disposed = IsDisposed;
            var closeConnection = (_connection?.State ?? ConnectionState.Closed) == ConnectionState.Open;
            if (_transaction?.Status == TransactionStatus.Active)
                _transaction.Rollback();
            _connection?.Close();
            _connection?.Dispose();
            IsDisposed = true;
            if (!disposed)
            {
                if (closeConnection)
                {
                    Interlocked.Decrement(ref _connectionCount);
                    SnTrace.Database.Write($"SnDataContext #{ContextId}: connection closed === COUNT: {_connectionCount}");
                }
                else
                {
                    if (OpenConnections.TryGetValue(ContextId, out var st))
                    {
                        SnLog.WriteInformation($"SnDataContext #{ContextId}: DISPOSED WITHOUT CONNECTION === COUNT: {_connectionCount} STACK: {st}");
                    }
                    else
                    {
                        SnTrace.Database.Write($"SnDataContext #{ContextId} DISPOSED WITHOUT CONNECTION === COUNT: {_connectionCount}");
                    }
                }

                OpenConnections.TryRemove(ContextId, out _);
            }
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
                SnTrace.Database.Write($"SnDataContext #{ContextId}: previous connection was forcibly disposed === COUNT: {_connectionCount}");
            }
            if (_connection == null)
            {
                _connection = CreateConnection();
                _connection.Open();

                Interlocked.Increment(ref _connectionCount);
                SnTrace.Database.Write($"SnDataContext #{ContextId}: open connection === COUNT: {_connectionCount}");
                OpenConnections[ContextId] = Environment.StackTrace;
            }
            return _connection;
        }

        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            TimeSpan timeout = default)
        {
            _transaction = RetryAsync(() =>
            {
                SnTrace.Database.Write($"SnDataContext #{ContextId} BeginTransaction.");
                var transaction = OpenConnection().BeginTransaction(isolationLevel);
                var wrappedTransaction = WrapTransaction(transaction, _cancellationToken, timeout)
                               ?? new TransactionWrapper(transaction, DataOptions, timeout, _cancellationToken);
                return Task.FromResult(wrappedTransaction);

            }).GetAwaiter().GetResult();
            
            return _transaction;
        }

        public async Task<int> ExecuteNonQueryAsync(string script, Action<DbCommand> setParams = null)
        {
            using (var op = SnTrace.Database.StartOperation(GetOperationMessage("ExecuteNonQueryAsync", script)))
            {
                var nonQueryResult = await Retrier.RetryAsync(10, 1000, async () =>
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

                        op.Successful = true;
                        return result;
                    }
                }, (res, i, ex) =>
                {
                    if (ex == null)
                    {
                        return true;
                    }

                    // last iteration
                    if (i == 1)
                    {
                        SnTrace.WriteError(ex.ToString());
                        LogOpenConnections();
                    }

                    // if we do not recognize the error, throw it immediately
                    if (i == 1 || !RetriableException(ex))
                        throw ex;

                    // continue the cycle
                    return false;
                });

                return nonQueryResult;
            }
        }

        public async Task<object> ExecuteScalarAsync(string script, Action<DbCommand> setParams = null)
        {
            using (var op = SnTrace.Database.StartOperation(GetOperationMessage("ExecuteScalarAsync", script)))
            {
                var scalarResult = await Retrier.RetryAsync(10, 1000, async () =>
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

                        op.Successful = true;
                        return result;
                    }
                }, (res, i, ex) =>
                {
                    if (ex == null)
                    {
                        return true;
                    }

                    // last iteration
                    if (i == 1)
                    {
                        SnTrace.WriteError(ex.ToString());
                        LogOpenConnections();
                    }

                    // if we do not recognize the error, throw it immediately
                    if (i == 1 || !RetriableException(ex))
                        throw ex;

                    // continue the cycle
                    return false;
                });

                return scalarResult;
                
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
                var readerResult = await Retrier.RetryAsync(10, 1000, async () =>
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
                        using (var reader = (DbDataReader)await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var result = await callbackAsync(reader, cancellationToken).ConfigureAwait(false);
                            op.Successful = true;
                            return result;
                        }
                    }
                }, (res, i, ex) =>
                {
                    if (ex == null)
                    {
                        //_logger.LogTrace("Successfully connected to the newly created database.");
                        return true;
                    }
                    
                    // last iteration
                    if (i == 1)
                    {
                        SnTrace.WriteError(ex.ToString());
                        LogOpenConnections();
                    }

                    // if we do not recognize the error, throw it immediately
                    if (i == 1 || !RetriableException(ex))
                        throw ex;

                    // continue the cycle
                    return false;
                });

                return readerResult;
            }
        }

        private static bool RetriableException(Exception ex)
        {
            return (ex is InvalidOperationException && ex.Message.Contains("connection from the pool")) ||
                   (ex is SqlException && ex.Message.Contains("A network-related or instance-specific error occurred"));
        }

        protected string GetOperationMessage(string name, string script)
        {
            const int maxLength = 80;
            return $"{this.GetType().Name}.{name}: {(script.Length < maxLength ? script : script.Substring(0, maxLength))}";
        }

        private void LogOpenConnections()
        {
            SnLog.WriteWarning($"SnDataContext TMP contextid {ContextId} === COUNT: {_connectionCount}" +
                               $"STUCKSTACK: {string.Join(" STUCKSTACK ", OpenConnections.Values)}");
        }

        public static Task<T> RetryAsync<T>(Func<Task<T>> action)
        {
            return Retrier.RetryAsync(30, 1000, action,
                (result, i, ex) =>
                {
                    if (ex == null)
                        return true;

                    // if we do not recognize the error, throw it immediately
                    if (!RetriableException(ex))
                        throw ex;

                    if (i == 1)
                    {
                        SnLog.WriteException(ex, $"Data layer error: {ex.Message}. Retry cycle ended.");
                        throw new InvalidOperationException("Data layer timeout occurred.", ex);
                    }

                    // continue the cycle
                    return false;
                });
        }
    }
}
