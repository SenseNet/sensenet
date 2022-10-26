﻿using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
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
            var disposed = IsDisposed;
            var closeConnection = (_connection?.State ?? ConnectionState.Closed) == ConnectionState.Open;
            if (_transaction?.Status == TransactionStatus.Active)
                _transaction.Rollback();
            _connection?.Dispose();
            IsDisposed = true;
            if (!disposed)
            {
                if (closeConnection)
                {
                    Interlocked.Decrement(ref _connectionCount);
                    SnTrace.Database.Write($"SnDataContext TMP connection CLOSED {ContextId} === COUNT: {_connectionCount}");
                }
                else
                {
                    if (OpenConnections.TryGetValue(ContextId, out var st))
                    {
                        SnLog.WriteInformation($"SnDataContext TMP DISPOSED WITHOUT CONNECTION {ContextId} === COUNT: {_connectionCount} {st}");
                    }
                    else
                    {
                        SnTrace.Database.Write($"SnDataContext TMP DISPOSED WITHOUT CONNECTION {ContextId} === COUNT: {_connectionCount}");
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
            if (_connection == null)
            {
                _connection = CreateConnection();
                _connection.Open();

                Interlocked.Increment(ref _connectionCount);
                SnTrace.Database.Write($"SnDataContext TMP connection OPEN {ContextId} === COUNT: {_connectionCount}");
                OpenConnections[ContextId] = Environment.StackTrace;
            }
            return _connection;
        }

        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            TimeSpan timeout = default)
        {
            var transaction = OpenConnection().BeginTransaction(isolationLevel);
            _transaction = WrapTransaction(transaction, _cancellationToken, timeout)
                           ?? new TransactionWrapper(transaction, DataOptions, timeout, _cancellationToken);
            return _transaction;
        }

        public async Task<int> ExecuteNonQueryAsync(string script, Action<DbCommand> setParams = null)
        {
            using (var op = SnTrace.Database.StartOperation(GetOperationMessage("ExecuteNonQueryAsync", script)))
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
            }
        }

        public async Task<object> ExecuteScalarAsync(string script, Action<DbCommand> setParams = null)
        {
            using (var op = SnTrace.Database.StartOperation(GetOperationMessage("ExecuteScalarAsync", script)))
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
                    if (i == 1 || !(ex is InvalidOperationException && ex.Message.Contains("connection from the pool")))
                        throw ex;

                    // continue the cycle
                    return false;
                });

                return readerResult;
            }
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
    }
}
