using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    public class MsSqlDataContext : SnDataContext
    {
        private readonly string _connectionString;

        public MsSqlDataContext(CancellationToken cancellationToken = default(CancellationToken)) : base(cancellationToken) { }
        public MsSqlDataContext(string connectionString, CancellationToken cancellationToken = default(CancellationToken)) :
            base(cancellationToken)
        {
            //UNDONE:DB: TEST: not tested (packaging)
            _connectionString = connectionString;
        }
        public MsSqlDataContext(ConnectionInfo connectionInfo, CancellationToken cancellationToken = default(CancellationToken)) :
            base(cancellationToken)
        {
            //UNDONE:DB: TEST: not tested (packaging)
            _connectionString = GetConnectionString(connectionInfo);
        }

        public static string GetConnectionString(ConnectionInfo connectionInfo)
        {
            string cnstr;

            if (string.IsNullOrEmpty(connectionInfo.ConnectionName))
                cnstr = ConnectionStrings.ConnectionString;
            else
            if (!ConnectionStrings.AllConnectionStrings.TryGetValue(connectionInfo.ConnectionName, out cnstr)
                || cnstr == null)
                throw new InvalidOperationException("Unknown connection name: " + connectionInfo.ConnectionName);

            var connectionBuilder = new SqlConnectionStringBuilder(cnstr);
            switch (connectionInfo.InitialCatalog)
            {
                case InitialCatalog.Initial:
                    break;
                case InitialCatalog.Master:
                    connectionBuilder.InitialCatalog = "master";
                    break;
                default:
                    throw new NotSupportedException("Unknown InitialCatalog");
            }

            if (!string.IsNullOrEmpty(connectionInfo.DataSource))
                connectionBuilder.DataSource = connectionInfo.DataSource;

            if (!string.IsNullOrEmpty(connectionInfo.InitialCatalogName))
                connectionBuilder.InitialCatalog = connectionInfo.InitialCatalogName;

            if (!string.IsNullOrWhiteSpace(connectionInfo.UserName))
            {
                if (string.IsNullOrWhiteSpace(connectionInfo.Password))
                    throw new NotSupportedException("Invalid credentials.");
                connectionBuilder.UserID = connectionInfo.UserName;
                connectionBuilder.Password = connectionInfo.Password;
                connectionBuilder.IntegratedSecurity = false;
            }
            return connectionBuilder.ToString();
        }

        public override DbConnection CreateConnection()
        {
            return CreateSqlConnection();
        }
        public override DbCommand CreateCommand()
        {
            return CreateSqlCommand();
        }
        public override DbParameter CreateParameter()
        {
            return CreateSqlParameter();
        }

        public SqlParameter CreateParameter(string name, SqlDbType dbType, object value)
        {
            return new SqlParameter
            {
                ParameterName = name,
                SqlDbType = dbType,
                Value = value
            };
        }
        public SqlParameter CreateParameter(string name, SqlDbType dbType, int size, object value)
        {
            return new SqlParameter
            {
                ParameterName = name,
                SqlDbType = dbType,
                Size = size,
                Value = value
            };
        }

        public virtual SqlConnection CreateSqlConnection()
        {
            return new SqlConnection(_connectionString ?? ConnectionStrings.ConnectionString);
        }
        public virtual SqlCommand CreateSqlCommand()
        {
            return new SqlCommand();
        }
        public virtual SqlParameter CreateSqlParameter()
        {
            return new SqlParameter();
        }
        public override TransactionWrapper WrapTransaction(DbTransaction underlyingTransaction,
            CancellationToken cancellationToken, TimeSpan timeout = default(TimeSpan))
        {
            return null;
        }


        public async Task<int> ExecuteNonQueryAsync(string script, Action<SqlCommand> setParams = null)
        {
            using (var op = SnTrace.Database.StartOperation(GetOperationMessage("ExecuteNonQueryAsync", script)))
            {
                using (var cmd = CreateSqlCommand())
                {
                    SqlTransaction transaction = null;
                    var cancellationToken = CancellationToken;
                    if (Transaction != null)
                    {
                        transaction = (SqlTransaction) Transaction.Transaction;
                        cancellationToken = Transaction.CancellationToken;
                    }

                    cmd.Connection = (SqlConnection) OpenConnection();
                    cmd.CommandTimeout = Configuration.Data.DbCommandTimeout;
                    cmd.CommandText = script;
                    cmd.CommandType = CommandType.Text;
                    cmd.Transaction = transaction;

                    setParams?.Invoke(cmd);

                    var result = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    op.Successful = true;
                    return result;
                }
            }
        }
        public async Task<object> ExecuteScalarAsync(string script, Action<SqlCommand> setParams = null)
        {
            using (var op = SnTrace.Database.StartOperation(GetOperationMessage("ExecuteScalarAsync", script)))
            {
                using (var cmd = CreateSqlCommand())
                {
                    SqlTransaction transaction = null;
                    var cancellationToken = CancellationToken;
                    if (Transaction != null)
                    {
                        transaction = (SqlTransaction) Transaction.Transaction;
                        cancellationToken = Transaction.CancellationToken;
                    }

                    cmd.Connection = (SqlConnection) OpenConnection();
                    cmd.CommandTimeout = Configuration.Data.DbCommandTimeout;
                    cmd.CommandText = script;
                    cmd.CommandType = CommandType.Text;
                    cmd.Transaction = transaction;

                    setParams?.Invoke(cmd);

                    var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    op.Successful = true;
                    return result;
                }
            }
        }
        public Task<T> ExecuteReaderAsync<T>(string script, Func<SqlDataReader, CancellationToken, Task<T>> callback)
        {
            return ExecuteReaderAsync(script, null, callback);
        }
        public async Task<T> ExecuteReaderAsync<T>(string script, Action<SqlCommand> setParams,
            Func<SqlDataReader, CancellationToken, Task<T>> callbackAsync)
        {
            using (var op = SnTrace.Database.StartOperation(GetOperationMessage("ExecuteReaderAsync", script)))
            {
                try
                {
                    using (var cmd = CreateSqlCommand())
                    {
                        SqlTransaction transaction = null;
                        var cancellationToken = CancellationToken;
                        if (Transaction != null)
                        {
                            transaction = (SqlTransaction)Transaction.Transaction;
                            cancellationToken = Transaction.CancellationToken;
                        }

                        cmd.Connection = (SqlConnection)OpenConnection();
                        cmd.CommandTimeout = Configuration.Data.DbCommandTimeout;
                        cmd.CommandText = script;
                        cmd.CommandType = CommandType.Text;
                        cmd.Transaction = transaction;

                        setParams?.Invoke(cmd);

                        using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var result = await callbackAsync(reader, cancellationToken).ConfigureAwait(false);
                            op.Successful = true;
                            return result;
                        }
                    }
                }
                catch (Exception e)
                {
                    SnTrace.WriteError(e.ToString());
                    throw;
                }
            }
        }
    }
}
