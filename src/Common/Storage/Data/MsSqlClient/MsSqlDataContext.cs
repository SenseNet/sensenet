using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using SenseNet.Configuration;
using IsolationLevel = System.Data.IsolationLevel;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    public class MsSqlDataContext : SnDataContext<SqlConnection, SqlCommand, SqlParameter, SqlDataReader>
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

        private string GetConnectionString(ConnectionInfo connectionInfo)
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

        public override SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString ?? ConnectionStrings.ConnectionString);
        }
        public override SqlCommand CreateCommand()
        {
            return new SqlCommand();
        }
        public override SqlParameter CreateParameter()
        {
            return new SqlParameter();
        }
        public override TransactionWrapper WrapTransaction(DbTransaction underlyingTransaction, TimeSpan timeout = default(TimeSpan))
        {
            return null;
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
    }

}
