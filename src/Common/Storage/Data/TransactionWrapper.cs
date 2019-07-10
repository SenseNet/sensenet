using System;
using System.Data;
using System.Data.Common;
using System.Transactions;
using IsolationLevel = System.Data.IsolationLevel;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    public class TransactionWrapper : IDbTransaction
    {
        public DbTransaction Transaction { get; }
        public TimeSpan Timeout { get; }

        public IDbConnection Connection => Transaction.Connection;
        public IsolationLevel IsolationLevel => Transaction.IsolationLevel;
        public TransactionStatus Status { get; private set; }

        public TransactionWrapper(DbTransaction transaction, TimeSpan timeout = default(TimeSpan))
        {
            Status = TransactionStatus.Active;
            Transaction = transaction;
            Timeout = timeout;
        }

        public virtual void Dispose()
        {
            Transaction.Dispose();
        }
        public virtual void Commit()
        {
            Transaction.Commit();
            Status = TransactionStatus.Committed;
        }
        public virtual void Rollback()
        {
            Transaction.Rollback();
            Status = TransactionStatus.Aborted;
        }
    }
}
