using System;
using System.Data;

namespace SenseNet.ContentRepository.Storage.Data
{
    public interface ITransactionProvider : IDisposable
    {
        /// <summary>
        /// Unique transaction identifier.
        /// </summary>
        long Id { get; }
        /// <summary>
        /// Transaction start time.
        /// </summary>
        DateTime Started { get; }
        /// <summary>
        /// Transaction isolation level.
        /// </summary>
        IsolationLevel IsolationLevel { get; }

        /// <summary>
        /// Starts database transaction with the specified isolation level.
        /// </summary>
        /// <param name="isolationLevel">Transaction isolation level.</param>
        void Begin(IsolationLevel isolationLevel);
        /// <summary>
        /// Starts database transaction with the specified isolation level and timeout.
        /// </summary>
        /// <param name="isolationLevel">Transaction isolation level.</param>
        /// <param name="timeout">Timeout for the transaction.</param>
        void Begin(IsolationLevel isolationLevel, TimeSpan timeout);
        /// <summary>
        /// Commits the database transaction.
        /// </summary>
        void Commit();
        /// <summary>
        /// Rolls back the transaction.
        /// </summary>
        void Rollback();
    }
}