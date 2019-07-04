using System;
using System.Data;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage
{
    public class TransactionTimedOutEventArgs : EventArgs
    {
        public object Sender { get; private set; }
        public object State { get; private set; }
        public TransactionTimedOutEventArgs(object sender, object state)
        {
            this.Sender = sender;
            this.State = state;
        }
    }

    /// <summary>
    /// Utility class for maintaining the current transaction. For starting and commiting 
    /// transactions please use the SnTransaction class instead in a using statement.
    /// </summary>
    [Obsolete("##", true)]
    public static class TransactionScope
	{
        private static bool _notSupported;

        // ================================================================================ Static properties

        /// <summary>
        /// Gets a value indicating whether this instance is active.
        /// </summary>
        /// <value><c>true</c> if this instance is active; otherwise, <c>false</c>.</value>
        public static bool IsActive
        {
            get
            {
                return (ContextHandler.GetTransaction() != null);
            }
        }
        /// <summary>
        /// Gets the transaction identifier if there is an active transaction.
        /// </summary>
        public static long CurrentId
        {
            get
            {
                var tran = ContextHandler.GetTransaction();
                return tran?.Id ?? 0L;
            }
        }

        /// <summary>
        /// Gets the isolation level.
        /// </summary>
        /// <value>The isolation level.</value>
        public static IsolationLevel IsolationLevel
        {
            get
            {
                var tran = ContextHandler.GetTransaction();
                return tran?.IsolationLevel ?? IsolationLevel.Unspecified;
            }
        }

        /// <summary>
        /// Gets the transaction provider.
        /// </summary>
        /// <value>The transaction provider.</value>
        public static ITransactionProvider Provider => ContextHandler.GetTransaction();

        // ================================================================================ Static Methods

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        public static void Begin()
        {
            Begin(IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(Configuration.Data.TransactionTimeout));
        }
        /// <summary>
        /// Begins the transaction with the specified timeout in seconds.
        /// </summary>
        /// <param name="timeout">The timeout in seconds.</param>         
        public static void Begin(TimeSpan timeout)
        {
            Begin(IsolationLevel.ReadCommitted, timeout);
        }
        /// <summary>
        /// Begins the transaction with the specified isolation level.
        /// </summary>
        /// <param name="isolationLevel">The isolation level.</param>             
        public static void Begin(IsolationLevel isolationLevel)
        {
            Begin(isolationLevel, TimeSpan.FromSeconds(Configuration.Data.TransactionTimeout));
        }
        /// <summary>
        /// Begins the transaction with the specified isolation level and timeout in seconds.
        /// </summary>
        /// <param name="isolationLevel">The isolation level.</param>   
        /// <param name="timeout">The timeout in seconds.</param>      
        public static void Begin(IsolationLevel isolationLevel, TimeSpan timeout)
        {
            if (IsActive)
                throw new InvalidOperationException(); // Transaction is already Active (Parallel transactions msg..).

            ITransactionProvider tran = null;
            try
            {
                tran = CommonComponents.TransactionFactory?.CreateTransaction();
                if (tran == null)
                {
                    // Transactions are not supported at the current provider.
                    _notSupported = true;
                    return;
                }

                tran.Begin(isolationLevel, timeout);
            }
            catch // rethrow
            {
                if (tran != null)
                    tran.Dispose();

                throw;
            }

            SnTrace.Database.Write("Transaction BEGIN: {0}.", tran.Id);
            ContextHandler.SetTransaction(tran);
            OnBeginTransaction(tran, EventArgs.Empty);
        }

        /// <summary>
        /// Add a new participant to the transaction queue.
        /// </summary>
        /// <param name="participant"></param>
        public static void Participate(ITransactionParticipant participant)
		{
			if (!IsActive)
				return;

			var queue = ContextHandler.GetTransactionQueue();
			if (queue == null)
			{
				queue = new TransactionQueue();
				ContextHandler.SetTransactionQueue(queue);
			}
            queue.Add(participant);
		}

        /// <summary>
        /// Commits the transaction and all its participants.
        /// </summary>
        public static void Commit()
        {
            if(_notSupported)
                return;

            var tran = ContextHandler.GetTransaction();
            if(tran == null) // !IsActive
                throw new InvalidOperationException(); // Transaction is not Active.

            try
            {
                tran.Commit();

                var queue = ContextHandler.GetTransactionQueue();
                queue?.Commit();

                OnCommitTransaction(tran, EventArgs.Empty);
            }
            finally
            {
                SnTrace.Database.Write("Transaction COMMIT: {0}. Running time: {1}", tran.Id, (DateTime.UtcNow - tran.Started));
                ContextHandler.SetTransaction(null);
                ContextHandler.SetTransactionQueue(null);
                tran.Dispose();
            }
        }

        /// <summary>
        /// Rollbacks the current transaction and all its participants.
        /// </summary>
        public static void Rollback()
        {
            if(_notSupported)
                return;

            var tran = ContextHandler.GetTransaction();
            if(tran == null)
                throw new InvalidOperationException("Transaction is not active");

            try
            {
                tran.Rollback();

				var queue = ContextHandler.GetTransactionQueue();
                queue?.Rollback();

                OnRollbackTransaction(tran, EventArgs.Empty);
            }
            finally
            {
                SnTrace.Database.Write("Transaction ROLLBACK: {0}. Running time: {1}", tran.Id, (DateTime.UtcNow - tran.Started));
                ContextHandler.SetTransaction(null);
                ContextHandler.SetTransactionQueue(null);
                tran.Dispose();
            }
        }

        // ================================================================================ Static Events

        /// <summary>
        /// Occurs when transaction begins.
        /// </summary>
        public static event EventHandler BeginTransaction;
        /// <summary>
        /// Occurs when transaction commits.
        /// </summary>
		public static event EventHandler CommitTransaction;
        /// <summary>
        /// Occurs when transaction rollbacks.
        /// </summary>
        public static event EventHandler RollbackTransaction;
        /// <summary>
        /// Occurs when transaction timed out.
        /// </summary>
        public static event EventHandler<TransactionTimedOutEventArgs> TransactionTimedOut;

		internal static void OnBeginTransaction(object sender, EventArgs e)
		{
		    BeginTransaction?.Invoke(sender, e);
		}

        internal static void OnCommitTransaction(object sender, EventArgs e)
        {
            CommitTransaction?.Invoke(sender, e);
        }

        internal static void OnRollbackTransaction(object sender, EventArgs e)
        {
            RollbackTransaction?.Invoke(sender, e);
        }

        internal static void OnTransactionTimedOut(object sender, TransactionTimedOutEventArgs e)
        {
            TransactionTimedOut?.Invoke(sender, e);
        }
	}
}