using System;
using System.Data;

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Disposable tool class for handling transactions, typically used in a 'using' statement. 
    /// Starts the transaction automatically (if there is no existing transaction) and rolls
    /// it back during disposing if the Commit method was not called previously.
    /// </summary>
    public class SnTransaction : IDisposable
    {
        private bool _isLocalTransaction;
        private bool _committed;

        /// <summary>
        /// Creates a new SnTransaction object and starts a db transaction if the
        /// TransactionScope is not yet active. Call this in a 'using' statement.
        /// </summary>
        /// <param name="isolationLevel">Transaction isolation level.</param>
        /// <param name="timeout">Timeout for the transaction.</param>
        /// <returns>A disposable SnTransaction object.</returns>
        public static SnTransaction Begin(IsolationLevel? isolationLevel = null, TimeSpan? timeout = null)
        {
            var tran = new SnTransaction
            {
                _isLocalTransaction = !TransactionScope.IsActive
            };

            if (tran._isLocalTransaction)
                TransactionScope.Begin(
                    isolationLevel ?? IsolationLevel.ReadCommitted,
                    timeout ?? TimeSpan.FromSeconds(Configuration.Data.TransactionTimeout));

            return tran;
        }

        /// <summary>
        /// Marks the SnTransaction object as commitable. The actual commit operation
        /// will be performed when the object gets disposed. Call this at the end of
        /// the using statement.
        /// If you do not call Commit, a rollback operation will be performed automatically
        /// during dispose.
        /// </summary>
        public void Commit()
        {
            _committed = true;
        }

        public void Dispose()
        {
            if (_isLocalTransaction && TransactionScope.IsActive)
            {
                if (_committed)
                    TransactionScope.Commit();
                else
                    TransactionScope.Rollback();
            }
        }
    }
}
