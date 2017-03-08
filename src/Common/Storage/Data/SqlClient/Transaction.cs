using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    internal class ScheduledAction
    {
        private readonly Action _action;
        private Timer _timer;

        private ScheduledAction(TimeSpan timeout, Action action)
        {
            _action = action;
            _timer = new Timer(TimerElapsed, null, timeout, Timeout.InfiniteTimeSpan);
        }

        public static ScheduledAction Start(TimeSpan timeout, Action action)
        {
            return new ScheduledAction(timeout, action);
        }

        private void TimerElapsed(object state)
        {
            Cancel();
            _action();
        }

        internal void Cancel()
        {
            if (_timer == null)
                return;

            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _timer.Dispose();
            _timer = null;
        }
    }

    /// <summary>
    /// Represents a Transact-SQL database transaction with a timeout.
    /// </summary>
    public class Transaction : ITransactionProvider
    {
        private static long _lastId;
        private bool _disposed;
        private ScheduledAction _scheduler;

        /// <summary>
        /// Initializes a new instance of the Transaction class.
        /// </summary>
        public Transaction()
        {
            Id = Interlocked.Increment(ref _lastId);
        }

        internal SqlConnection Connection { get; private set; }
        internal SqlTransaction Tran { get; private set; }

        /*------------------------------------------- ITransactionProvider Members */

        /// <summary>
        /// Unique transaction identifier.
        /// </summary>
        public long Id { get; }
        /// <summary>
        /// Transaction start time.
        /// </summary>
        public DateTime Started { get; private set; }
        /// <summary>
        /// Transaction isolation level.
        /// </summary>
        public IsolationLevel IsolationLevel => Tran?.IsolationLevel ?? IsolationLevel.Unspecified;

        /// <summary>
        /// Starts a database transaction with the specified isolation level.
        /// </summary>
        /// <param name="isolationLevel">Transaction isolation level.</param>
        public void Begin(IsolationLevel isolationLevel)
        {
            Begin(isolationLevel, TimeSpan.FromSeconds(Configuration.Data.TransactionTimeout));
        }
        /// <summary>
        /// Starts a database transaction with the specified isolation level and timeout.
        /// </summary>
        /// <param name="isolationLevel">Transaction isolation level.</param>
        /// <param name="timeout">Timeout for the transaction.</param>
        public void Begin(IsolationLevel isolationLevel, TimeSpan timeout)
        {
            Connection = new SqlConnection(Configuration.ConnectionStrings.ConnectionString);
            Connection.Open();
            Tran = Connection.BeginTransaction(isolationLevel);
            Started = DateTime.UtcNow;
            _scheduler = ScheduledAction.Start(timeout, HandleTimeout);
        }

        private void HandleTimeout()
        {
            if (System.Diagnostics.Debugger.IsAttached)
                return;

            var transactionInfo = GatherLongRunningTransactionInformation();

            var rollbackOk = true;
            var rollbackExceptionInfo = string.Empty;
            try
            {
                const int timeOut = 10 * 1000;
                var tokenSource = new CancellationTokenSource();
                CancellationToken token = tokenSource.Token;
                try
                {
                    using (var task = Task.Factory.StartNew(Rollback, token))
                    {
                        if (!task.Wait(timeOut, token))
                        {
                            tokenSource.Cancel();
                            rollbackOk = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    rollbackExceptionInfo = e.ToString();
                }
            }
            finally
            {
                SnLog.WriteError(
                    $@"Transaction #{Id} timed out ({Configuration.Data.TransactionTimeout} sec). Rollback called on it {(rollbackOk ? "and it is executed successfully." : "but that is timed out too. " + rollbackExceptionInfo)}{Environment.NewLine}{transactionInfo}",
                    EventId.Transaction);
            }
        }
        private string GatherLongRunningTransactionInformation()
        {
            const string sql = @"-- relevant locks
SELECT session_id ,blocking_session_id, [status] ,command ,name ,transaction_id
	,start_time ,wait_type ,wait_time ,last_wait_type ,total_elapsed_time
	,(SELECT TOP 1 SUBSTRING(text,statement_start_offset / 2+1 , 
      ((CASE WHEN statement_end_offset = -1 
         THEN (LEN(CONVERT(nvarchar(max),text)) * 2) 
         ELSE statement_end_offset END)  - statement_start_offset) / 2+1))  AS sql_statement
FROM sys.dm_exec_requests
CROSS APPLY sys.dm_exec_sql_text(sql_handle) s2
LEFT OUTER JOIN sys.objects ON s2.objectid = sys.objects.object_id
WHERE session_id != @@SPID AND database_id = DB_ID()

-- relevant processes
SELECT P.spid, P.program_name, P.hostname, P.loginame, right(convert(varchar, dateadd(ms, datediff(ms, P.last_batch, getdate()), '1900-01-01'), 121), 12) as [duration], status, sqltext.text
FROM SYS.sysprocesses P
CROSS APPLY sys.dm_exec_sql_text(sql_handle) AS sqltext
WHERE spid != @@SPID AND sqltext.dbid = DB_ID() AND (
	(P.status NOT IN ('background', 'sleeping') AND P.cmd NOT IN ('AWAITING COMMAND', 'MIRROR HANDLER', 'LAZY WRITER', 'CHECKPOINT SLEEP', 'RA MANAGER'))
	OR open_tran != 0)
";
            var sb = new StringBuilder();
            using (var connection = new SqlConnection(Configuration.ConnectionStrings.ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        sb.Append("Transaction #").Append(this.Id).AppendLine(" timeout information:");
                        sb.Append("{");
                        sb.AppendLine(" locked = [ ");
                        var first = true;
                        while (reader.Read())
                        {
                            if (first)
                                first = false;
                            else
                                sb.AppendLine(", ");
                            sb.Append("  { session_id: ").Append(reader.GetValue(0)).AppendLine(",");
                            sb.Append("    blocking_session_id: ").Append(reader.GetValue(1)).AppendLine(",");
                            sb.Append("    status: \"").Append(reader.GetValue(2)).AppendLine("\",");
                            sb.Append("    command: \"").Append(reader.GetValue(3)).AppendLine("\",");
                            sb.Append("    name: \"").Append(reader.GetValue(4)).AppendLine("\",");
                            sb.Append("    transaction_id: ").Append(reader.GetValue(5)).AppendLine(",");
                            sb.Append("    start_time: \"").Append(reader.GetValue(6)).AppendLine("\",");
                            sb.Append("    wait_type: \"").Append(reader.GetValue(7)).AppendLine("\",");
                            sb.Append("    wait_time: ").Append(reader.GetValue(8)).AppendLine(",");
                            sb.Append("    last_wait_type: ").Append(reader.GetValue(9)).AppendLine(",");
                            sb.Append("    total_elapsed_time: \"").Append(reader.GetValue(10)).Append("\",");
                            sb.Append("    sql_statement: \"").Append(reader.GetValue(11)).Append("\"}");
                        }
                        sb.AppendLine(" ],");

                        reader.NextResult();

                        sb.Append(" relevant_processes = [ ");
                        first = true;
                        while (reader.Read())
                        {
                            if (first)
                                first = false;
                            else
                                sb.AppendLine(", ");
                            sb.AppendLine("{");
                            sb.Append("  { spid: ").Append(reader.GetValue(0)).AppendLine(",");
                            sb.Append("    program_name: \"").Append(reader.GetValue(1)).AppendLine("\",");
                            sb.Append("    hostname: \"").Append(reader.GetValue(2)).AppendLine("\",");
                            sb.Append("    loginame: \"").Append(reader.GetValue(3)).AppendLine("\",");
                            sb.Append("    duration: ").Append(reader.GetValue(4)).AppendLine(",");
                            sb.Append("    status: \"").Append(reader.GetValue(5)).AppendLine("\",");
                            sb.Append("    text: \"").Append(reader.GetValue(6)).AppendLine("\"}");
                        }
                        sb.AppendLine(" ]");
                        sb.AppendLine("}");
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Commits the database transaction.
        /// </summary>
        public void Commit()
        {
            var scheduler = _scheduler;
            scheduler?.Cancel();

            var tran = Tran;
            tran?.Commit();
        }
        /// <summary>
        /// Rolls back the transaction.
        /// </summary>
        public void Rollback()
        {
            var scheduler = _scheduler;
            scheduler?.Cancel();

            var tran = Tran;
            tran?.Rollback();
        }

        /*------------------------------------------- IDisposable Members */

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Transaction()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this._disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    this.Close();
                }
            }
            _disposed = true;
        }

        /*--------------------------------------*/

        private void Close()
        {
            if (Connection != null)
                Connection.Dispose();

            Connection = null;
            Tran = null;
        }
    }
}