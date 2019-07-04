using SenseNet.Diagnostics;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    [Obsolete("##", true)]
    public class SqlProcedure : IDataProcedure
    {
        private bool _disposed;
        private SqlCommand _cmd;
        private SqlConnection _conn;
        private bool _useTransaction;

        private readonly string _connectionName;
        private readonly InitialCatalog _initialCatalog;
        private readonly string _initialCatalogName;
        private readonly string _dataSource;
        private readonly string _userName;
        private readonly string _password;

        /// <summary>
        /// Gets or sets a value indicating how the CommandText property is to be interpreted.
        /// </summary>
        public CommandType CommandType
        {
            get
            {
                if (_cmd == null)
                    _cmd = CreateCommand();
                return _cmd.CommandType;
            }
            set
            {
                if (_cmd == null)
                    _cmd = CreateCommand();
                _cmd.CommandType = value;
            }
        }
        /// <summary>
        /// Gets or sets the Transact-SQL statement or stored procedure to execute at the data source.
        /// </summary>
        public string CommandText
        {
            get
            {
                if (_cmd == null)
                    _cmd = CreateCommand();
                return _cmd.CommandText;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (_cmd == null)
                    _cmd = CreateCommand();
                _cmd.CommandText = value;
            }
        }
        /// <summary>
        /// Gets the SqlParameterCollection of the underlying command.
        /// </summary>
        public SqlParameterCollection Parameters
        {
            get
            {
                if (_cmd == null)
                    _cmd = CreateCommand();
                return _cmd.Parameters;
            }
        }

        /// <summary>
        /// Initializes a new instance of the SqlProcedure class.
        /// </summary>
        public SqlProcedure() { }
        /// <summary>
        /// Initializes a new instance of the SqlProcedure class with the name of the connection and the database.
        /// </summary>
        /// <param name="connectionName">Name of the configured connection string.</param>
        /// <param name="initialCatalog">Database name.</param>
        public SqlProcedure(string connectionName, InitialCatalog initialCatalog)
        {
            _connectionName = connectionName;
            _initialCatalog = initialCatalog;
        }
        public SqlProcedure(ConnectionInfo connectionInfo) : this(connectionInfo.ConnectionName, connectionInfo.InitialCatalog)
        {
            _dataSource = connectionInfo.DataSource;
            _initialCatalogName = connectionInfo.InitialCatalogName;
            _userName = connectionInfo.UserName;
            _password = connectionInfo.Password;
        }

        private SqlCommand CreateCommand()
        {
            SqlTransaction tran = null;
            var provider = (Transaction)TransactionScope.Provider;
            var tranConn = provider?.Connection;
            if (tranConn != null)
            {
                _conn = tranConn;
                tran = provider.Tran;
                _useTransaction = true;
            }
            else
            {
                string cnstr;

                if (!string.IsNullOrEmpty(_connectionName))
                {
                    cnstr = Configuration.ConnectionStrings.AllConnectionStrings[_connectionName];
                    if (cnstr == null)
                        throw new InvalidOperationException("Unknown connection name: " + _connectionName);
                }
                else
                {
                    cnstr = Configuration.ConnectionStrings.ConnectionString;
                }

                var connectionBuilder = new SqlConnectionStringBuilder(cnstr);
                switch (_initialCatalog)
                {
                    case InitialCatalog.Initial:
                        break;
                    case InitialCatalog.Master:
                        connectionBuilder.InitialCatalog = "master";
                        break;
                    default:
                        throw new NotSupportedException("Unknown InitialCatalog");
                }

                // if there is a custom data source (SQL server name)
                if (!string.IsNullOrEmpty(_dataSource))
                {
                    connectionBuilder.DataSource = _dataSource;
                    //cnstr = new SqlConnectionStringBuilder(cnstr) { DataSource = _dataSource }.ToString();
                }
                // if there is a custom initial catalog (database name)
                if (!string.IsNullOrEmpty(_initialCatalogName))
                {
                    connectionBuilder.InitialCatalog = _initialCatalogName;
                    //cnstr = new SqlConnectionStringBuilder(cnstr) { InitialCatalog = _initialCatalogName }.ToString();
                }
                // if it is an SQL authentication
                if (!string.IsNullOrWhiteSpace(_userName))
                {
                    if (string.IsNullOrWhiteSpace(_password))
                    {
                        throw new NotSupportedException("Invalid credentials.");
                    }
                    connectionBuilder.UserID = _userName;
                    connectionBuilder.Password = _password;
                    connectionBuilder.IntegratedSecurity = false;
                }
                cnstr = connectionBuilder.ToString();
                _conn = new SqlConnection(cnstr);
            }

            var cmd = new SqlCommand
            {
                Connection = _conn,
                Transaction = tran,
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = Configuration.Data.SqlCommandTimeout
            };

            return cmd;
        }
        private void StartConnection()
        {
            if (_conn == null)
                throw new InvalidOperationException("SqlProcedure has been closed.");

            if (_conn.State == ConnectionState.Closed)
                _conn.Open();
        }

        /// <summary>
        /// Sends the CommandText to the connection and builds a SqlDataReader.
        /// </summary>
        public virtual SqlDataReader ExecuteReader()
        {
            using (var op = SnTrace.Database.StartOperation(GetTraceData("ExecuteReader")))
            {
                StartConnection();
                var result = _cmd.ExecuteReader();
                op.Successful = true;
                return result;
            }
        }
        /// <summary>
        /// An asynchronous version of ExecuteReader that sends the CommandText to the 
        /// connection and builds a SqlDataReader.
        /// </summary>
        public async Task<SqlDataReader> ExecuteReaderAsync()
        {
            using (var op = SnTrace.Database.StartOperation(GetTraceData("ExecuteReader")))
            {
                StartConnection();
                var result = await _cmd.ExecuteReaderAsync();
                op.Successful = true;
                return result;
            }
        }
        /// <summary>
        /// Sends the CommandText to the connection and builds a SqlDataReader using the provided command behavior.
        /// </summary>
        public virtual SqlDataReader ExecuteReader(CommandBehavior behavior)
        {
            using (var op = SnTrace.Database.StartOperation(GetTraceData("ExecuteReader(" + behavior + ")")))
            {
                StartConnection();
                var result = _cmd.ExecuteReader(behavior);
                op.Successful = true;
                return result;
            }
        }
        /// <summary>
        /// An asynchronous version of ExecuteReader that sends the CommandText to the 
        /// connection and builds a SqlDataReader using the provided command behavior.
        /// </summary>
        public async Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior behavior)
        {
            using (var op = SnTrace.Database.StartOperation(GetTraceData("ExecuteReader(" + behavior + ")")))
            {
                StartConnection();
                var result = await _cmd.ExecuteReaderAsync(behavior);
                op.Successful = true;
                return result;
            }
        }
        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set.
        /// </summary>
        public virtual object ExecuteScalar()
        {
            using (var op = SnTrace.Database.StartOperation(GetTraceData("ExecuteScalar")))
            {
                StartConnection();
                var result = _cmd.ExecuteScalar();
                op.Successful = true;
                return result;
            }
        }
        /// <summary>
        /// An asynchronous version of ExecuteScalar that executes the query, and returns the first column of the first row in the result set.
        /// </summary>
        public async Task<object> ExecuteScalarAsync()
        {
            using (var op = SnTrace.Database.StartOperation(GetTraceData("ExecuteScalar")))
            {
                StartConnection();
                var result = await _cmd.ExecuteScalarAsync();
                op.Successful = true;
                return result;
            }
        }
        /// <summary>
        /// Executes a Transact-SQL statement against the connection and returns the number of rows affected.
        /// </summary>
        public virtual int ExecuteNonQuery()
        {
            using (var op = SnTrace.Database.StartOperation(GetTraceData("ExecuteNonQuery")))
            {
                StartConnection();
                var result = _cmd.ExecuteNonQuery();
                op.Successful = true;
                return result;
            }
        }

        /// <summary>
        /// Returns a new SqlParameter instance.
        /// </summary>
        public IDataParameter CreateParameter()
        {
            return new SqlParameter();
        }

        /// <summary>
        /// An asynchronous version of ExecuteNonQuery that executes a Transact-SQL statement 
        /// against the connection and returns the number of rows affected.
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync()
        {
            using (var op = SnTrace.Database.StartOperation(GetTraceData("ExecuteNonQuery")))
            {
                StartConnection();
                var result = await _cmd.ExecuteNonQueryAsync();
                op.Successful = true;
                return result;
            }
        }
        private void Close()
        {
            _cmd.Dispose();
            if (!_useTransaction)
            {
                if (_conn != null && _conn.State == ConnectionState.Open)
                    _conn.Close();
                _conn = null;
            }
            _cmd = null;
        }

        private string GetTraceData(string method)
        {
            if (!SnTrace.Database.Enabled)
                return string.Empty;
           
            return $"SqlProcedure.{method} (tran:{TransactionScope.CurrentId}): Command: {GetSqlForTrace(this.CommandText)}";
        }
        private static string GetSqlForTrace(string cmd)
        {
            var s = cmd.Trim().Split('\r', '\n');
            if (s.Length == 0)
                return string.Empty;

            var p = 0;
            var line = string.Empty;
            while ((p < s.Length) && ((line = s[p++].Trim()).Length == 0))
            {
            }

            var suffix = s.Length > p ? "..." : string.Empty;
            return (line.Length > 500 ? line.Substring(0, 500) : line) + suffix;
        }

        // ====================================================================== IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (!this._disposed)
                if (disposing)
                    this.Close();
            _disposed = true;
        }
        ~SqlProcedure()
        {
            Dispose(false);
        }

        // ====================================================================== IDataProcedure Members

        CommandType IDataProcedure.CommandType
        {
            get { return this.CommandType; }
            set { this.CommandType = value; }
        }
        string IDataProcedure.CommandText
        {
            get { return this.CommandText; }
            set { this.CommandText = value; }
        }
        System.Data.Common.DbParameterCollection IDataProcedure.Parameters
        {
            get { return this.Parameters; }
        }

        System.Data.Common.DbDataReader IDataProcedure.ExecuteReader()
        {
            return ExecuteReader();
        }
        System.Data.Common.DbDataReader IDataProcedure.ExecuteReader(CommandBehavior behavior)
        {
            return ExecuteReader(behavior);
        }
        object IDataProcedure.ExecuteScalar()
        {
            return ExecuteScalar();
        }
        int IDataProcedure.ExecuteNonQuery()
        {
            return ExecuteNonQuery();
        }
    }
}