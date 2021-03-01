using SenseNet.Configuration;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
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
        public CancellationToken CancellationToken { get; }

        public TransactionWrapper(DbTransaction transaction, DataOptions options, CancellationToken cancel) :
            this(transaction, options, default, cancel)
        {
        }
        public TransactionWrapper(DbTransaction transaction, DataOptions options, TimeSpan timeout, CancellationToken cancel)
        {
            Status = TransactionStatus.Active;
            Transaction = transaction;
            Timeout = timeout == default
                ? TimeSpan.FromSeconds(options.TransactionTimeout)
                : timeout;
            CancellationToken = CombineCancellationToken(cancel);
        }
        private CancellationToken CombineCancellationToken(CancellationToken cancellationToken)
        {
            var timeoutToken = new CancellationTokenSource(Timeout).Token;
            if (cancellationToken == CancellationToken.None)
                return timeoutToken;

            return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken).Token;
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
