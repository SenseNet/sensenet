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
    public class SnDataContext : IDisposable
    {
        private class TransactionWrapper : IDbTransaction
        {
            public DbTransaction Transaction { get; }

            public IDbConnection Connection => Transaction.Connection;
            public IsolationLevel IsolationLevel => Transaction.IsolationLevel;
            public TransactionStatus Status { get; private set; }

            public TransactionWrapper(DbTransaction transaction)
            {
                Status = TransactionStatus.Active;
                Transaction = transaction;
            }

            public void Dispose()
            {
                Transaction.Dispose();
            }
            public void Commit()
            {
                Transaction.Commit();
                Status = TransactionStatus.Committed;
            }
            public void Rollback()
            {
                Transaction.Rollback();
                Status = TransactionStatus.Aborted;
            }
        }

        private readonly DataProvider2 _dataProvider;
        private readonly DbConnection _connection;
        private TransactionWrapper _transaction;

        public CancellationToken CancellationToken { get; }

        public SnDataContext(DataProvider2 dataProvider, CancellationToken cancellationToken = default(CancellationToken))
        {
            _dataProvider = dataProvider;
            CancellationToken = cancellationToken;
            _connection = _dataProvider.CreateConnection();
            _connection.Open();
        }

        public void Dispose()
        {
            if(_transaction?.Status == TransactionStatus.Active)
                _transaction.Rollback();
            _connection.Dispose();
        }

        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            var transaction = _connection.BeginTransaction(isolationLevel);
            _transaction = new TransactionWrapper(transaction);
            return _transaction;
        }

        public async Task<int> ExecuteNonQueryAsync(string script, Action<DbCommand> setParams = null)
        {
            using (var cmd = _dataProvider.CreateCommand())
            {
                cmd.Connection = _connection;
                cmd.CommandText = script;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = _transaction?.Transaction;

                setParams?.Invoke(cmd);

                return await cmd.ExecuteNonQueryAsync(CancellationToken);
            }
        }
        public async Task<object> ExecuteScalarAsync(string script, Action<DbCommand> setParams = null)
        {
            using (var cmd = _dataProvider.CreateCommand())
            {
                cmd.Connection = _connection;
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
            using (var cmd = _dataProvider.CreateCommand())
            {
                cmd.Connection = _connection;
                cmd.CommandText = script;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = _transaction?.Transaction;

                setParams?.Invoke(cmd);

                using (var reader = await cmd.ExecuteReaderAsync(CancellationToken))
                    return await callbackAsync(reader);
            }
        }

    }
}
