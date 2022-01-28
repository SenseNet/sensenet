using System;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SenseNet.Storage.Data.MsSqlClient
{
    [Serializable]
    public class DbCreationException : Exception
    {
        public DbCreationException() { }
        public DbCreationException(string message) : base(message) { }
        public DbCreationException(string message, Exception inner) : base(message, inner) { }
        protected DbCreationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class MsSqlDatabaseInstallationParameters
    {
        public string Server { get; set; }
        public string ExpectedDatabaseName { get; set; }
        public string DbCreatorUserName { get; set; }
        public string DbCreatorPassword { get; set; }
        public string DbOwnerUserName { get; set; }
        public string DbOwnerPassword { get; set; }
    }

    public class MsSqlDbInstallationStatus
    {
        public bool IsDatabaseExist { get; set; }
        public string DbOwner { get; set; }
        public bool DbOwnerOk { get; set; }
        public bool DbOwnerRoleOk { get; set; }
        public bool CreatorLoginExists { get; set; }
        public bool CreatorLoginEnabled { get; set; }
        public bool CustomerLoginExists { get; set; }
        public bool CustomerLoginEnabled { get; set; }
    }

    public class MsSqlDatabaseInstaller
    {
        private readonly ILogger<MsSqlDatabaseInstaller> _logger;
        private readonly MsSqlDatabaseInstallationParameters _options;

        public MsSqlDatabaseInstaller(IOptions<MsSqlDatabaseInstallationParameters> options, ILogger<MsSqlDatabaseInstaller> logger)
        {
            _options = options.Value;
            _logger = logger;
        }
        // WARNING: Server authentication mode need to be "SQL Server and Windows Authentication mode".
        //     (see on the Security tab of the Database server properties in the SSMS)

        public async Task InstallAsync()
        {
            ValidateParameters(_options);
            var targetConnectionString = GetConnectionString(_options);
            var masterConnectionString =
                new SqlConnectionStringBuilder(targetConnectionString) { InitialCatalog = "master" }.ConnectionString;
            var isIntegratedCustomer = string.IsNullOrEmpty(_options.DbOwnerUserName);

            if (!isIntegratedCustomer)
                await EnsureCustomerLoginAsync(_options.DbOwnerUserName, _options.DbOwnerPassword, masterConnectionString)
                    .ConfigureAwait(false);

            await EnsureDatabaseAsync(_options.ExpectedDatabaseName, masterConnectionString).ConfigureAwait(false);

            if (isIntegratedCustomer)
                return;

            var targetDbOwner = await GetDbOwner(_options.ExpectedDatabaseName, masterConnectionString);

            if (targetDbOwner.Equals(_options.DbOwnerUserName, StringComparison.OrdinalIgnoreCase))
                return;

            await EnsureDatabaseUserAsync(_options.DbOwnerUserName, targetConnectionString)
                .ConfigureAwait(false);
            await EnsureDbOwnerRoleAsync(_options.DbOwnerUserName, targetConnectionString)
                .ConfigureAwait(false);
        }
        public void ValidateParameters(MsSqlDatabaseInstallationParameters parameters)
        {
            if (string.IsNullOrEmpty(parameters.ExpectedDatabaseName))
                throw new ArgumentException("ExpectedDatabaseName cannot be null or empty.");
        }
        public string GetConnectionString(MsSqlDatabaseInstallationParameters parameters)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = string.IsNullOrEmpty(parameters.Server) ? "(local)" : parameters.Server,
                InitialCatalog = parameters.ExpectedDatabaseName
            };

            if (string.IsNullOrEmpty(parameters.DbCreatorUserName))
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = parameters.DbCreatorUserName;
                builder.Password = parameters.DbCreatorPassword;
            }

            return builder.ConnectionString;
        }


        private async Task EnsureCustomerLoginAsync(string userName, string password, string connectionString)
        {
            _logger.LogTrace($"Ensure customer login: {userName}");

            var (isExist, isEnabled) = await QueryLoginAsync(userName, connectionString).ConfigureAwait(false);
            if (isExist)
            {
                if (isEnabled)
                    return;
                throw new DbCreationException("User is disabled on this server.");
            }

            await CreateLoginAsync(userName, password, connectionString).ConfigureAwait(false);
        }
        private async Task<(bool IsExist, bool IsEnabled)> QueryLoginAsync(string userName, string connectionString)
        {
            var sql = $"SELECT [name], [type], [type_desc], [is_disabled] FROM sys.server_principals WHERE [name] = '{userName}'";
            var isExist = false;
            var isEnabled = false;
            await ExecuteSqlQueryAsync(sql, connectionString, reader =>
            {
                isExist = true;
                isEnabled = !reader.GetBoolean(reader.GetOrdinal("is_disabled"));
                return false;
            });
            return (isExist, isEnabled);
        }
        public async Task CreateLoginAsync(string userName, string password, string connectionString)
        {
            try
            {
                var sql = $"CREATE LOGIN {userName} WITH PASSWORD=N'{password}', " +
                          $"DEFAULT_DATABASE = master, DEFAULT_LANGUAGE = us_english";
                await ExecuteSqlCommandAsync(sql, connectionString);
            }
            catch (SqlException e)
            {
                if (e.Number == 15025) // The server principal '{??}' already exists.
                    return;
                throw new DbCreationException($"Cannot create user on the server. {e.Message}", e);
            }
        }


        private async Task EnsureDatabaseAsync(string databaseName, string connectionString)
        {
            _logger.LogTrace($"Querying database: {databaseName}");
            var isExist = await QueryDatabaseAsync(databaseName, connectionString).ConfigureAwait(false);
            if (!isExist)
                await CreateDatabaseAsync(databaseName, connectionString).ConfigureAwait(false);
        }
        private async Task<bool> QueryDatabaseAsync(string databaseName, string connectionString)
        {
            var result = false;
            var sql = $"SELECT [name] FROM sys.databases WHERE name = N'{databaseName}'";
            await ExecuteSqlQueryAsync(sql, connectionString, reader =>
            {
                result = true;
                return false;
            });
            return result;
        }
        public async Task CreateDatabaseAsync(string databaseName, string connectionString)
        {
            var sql = @$"CREATE DATABASE [{databaseName}]
ALTER DATABASE [{databaseName}] SET ANSI_NULL_DEFAULT OFF
ALTER DATABASE [{databaseName}] SET ANSI_NULLS OFF
ALTER DATABASE [{databaseName}] SET ANSI_PADDING OFF
ALTER DATABASE [{databaseName}] SET ANSI_WARNINGS OFF
ALTER DATABASE [{databaseName}] SET ARITHABORT OFF
ALTER DATABASE [{databaseName}] SET AUTO_CLOSE OFF
ALTER DATABASE [{databaseName}] SET AUTO_CREATE_STATISTICS ON
ALTER DATABASE [{databaseName}] SET AUTO_SHRINK OFF
ALTER DATABASE [{databaseName}] SET AUTO_UPDATE_STATISTICS ON
ALTER DATABASE [{databaseName}] SET CURSOR_CLOSE_ON_COMMIT OFF
ALTER DATABASE [{databaseName}] SET CURSOR_DEFAULT  GLOBAL
ALTER DATABASE [{databaseName}] SET CONCAT_NULL_YIELDS_NULL OFF
ALTER DATABASE [{databaseName}] SET NUMERIC_ROUNDABORT OFF
ALTER DATABASE [{databaseName}] SET QUOTED_IDENTIFIER OFF
ALTER DATABASE [{databaseName}] SET RECURSIVE_TRIGGERS OFF
ALTER DATABASE [{databaseName}] SET  ENABLE_BROKER
ALTER DATABASE [{databaseName}] SET AUTO_UPDATE_STATISTICS_ASYNC OFF
ALTER DATABASE [{databaseName}] SET DATE_CORRELATION_OPTIMIZATION OFF
--ALTER DATABASE [{databaseName}] SET TRUSTWORTHY OFF /* MSDN: 'To set this option, you must be a member of the sysadmin fixed server role.' */
ALTER DATABASE [{databaseName}] SET ALLOW_SNAPSHOT_ISOLATION OFF
ALTER DATABASE [{databaseName}] SET PARAMETERIZATION SIMPLE
ALTER DATABASE [{databaseName}] SET  READ_WRITE
ALTER DATABASE [{databaseName}] SET RECOVERY SIMPLE
ALTER DATABASE [{databaseName}] SET  MULTI_USER
ALTER DATABASE [{databaseName}] SET PAGE_VERIFY CHECKSUM
ALTER DATABASE [{databaseName}] SET DB_CHAINING OFF";

            _logger.LogTrace($"Creating database: {databaseName}");

            try
            {
                await ExecuteSqlCommandAsync(sql, connectionString);
            }
            catch (SqlException e)
            {
                if (e.Number == 1801) // Database '{??}' already exists. Choose a different database name.
                    return;
                throw new DbCreationException($"Cannot create database. {e.Message}", e);
            }
        }


        private async Task EnsureDatabaseUserAsync(string userName, string connectionString)
        {
            _logger.LogTrace($"Ensure database user: {userName}");

            var isExist = await QueryUserAsync(userName, connectionString).ConfigureAwait(false);
            if (!isExist)
                await CreateUserAsync(userName, connectionString).ConfigureAwait(false);
        }
        private async Task<string> GetDbOwner(string databaseName, string connectionString)
        {
            string result = null;
            var sql = $"SELECT suser_sname(owner_sid) DbOwner/*, [name] DbName*/ FROM sys.databases WHERE [name] = '{databaseName}'";
            await ExecuteSqlQueryAsync(sql, connectionString, reader =>
            {
                result = reader.GetString(reader.GetOrdinal("DbOwner"));
                return false;
            });
            return result;
        }
        private async Task<bool> QueryUserAsync(string userName, string connectionString)
        {
            var result = false;
            var sql = $"SELECT [name], [type], [type_desc], [authentication_type], [authentication_type_desc]" +
                      $" FROM sys.database_principals WHERE[name] = '{userName}'";
            await ExecuteSqlQueryAsync(sql, connectionString, reader =>
            {
                result = true;
                return false;
            });
            return result;
        }
        public async Task CreateUserAsync(string userName, string connectionString)
        {
            var sql = $"CREATE USER {userName} FOR LOGIN {userName} WITH DEFAULT_SCHEMA = [DBO]";
            try
            {
                await ExecuteSqlCommandAsync(sql, connectionString);
            }
            catch (SqlException e)
            {
                if (e.Number == 15023) // User, group, or role '{??}' already exists in the current database.
                    return;
                throw new DbCreationException($"Cannot create owner user of the database. {e.Message}", e);
            }
        }


        private async Task EnsureDbOwnerRoleAsync(string userName, string connectionString)
        {
            _logger.LogTrace($"Ensure db owner role: {userName}");

            var isInRole = await QueryDbOwnerRoleAsync(userName, connectionString).ConfigureAwait(false);
            if (!isInRole)
                await AddDbOwnerRoleAsync(userName, connectionString).ConfigureAwait(false);
        }
        private async Task<bool> QueryDbOwnerRoleAsync(string userName, string connectionString)
        {
            var result = false;
            var sql = @$"SELECT roles.name role, members.name member FROM sys.database_role_members AS database_role_members  
JOIN sys.database_principals AS roles ON database_role_members.role_principal_id = roles.principal_id  
JOIN sys.database_principals AS members ON database_role_members.member_principal_id = members.principal_id
WHERE members.name = '{userName}' AND roles.name = 'db_owner'";
            await ExecuteSqlQueryAsync(sql, connectionString, reader =>
            {
                result = true;
                return false;
            });
            return result;
        }
        public async Task AddDbOwnerRoleAsync(string userName, string connectionString)
        {
            var sql = $"EXEC  sp_addrolemember 'db_owner', '{userName}'";
            try
            {
                await ExecuteSqlCommandAsync(sql, connectionString);
            }
            catch (SqlException e)
            {
                throw new DbCreationException($"Cannot add 'dbowner' role to the Customer. {e.Message}", e);
            }
        }


        /* =================================================================================== */

        private async Task ExecuteSqlCommandAsync(string sql, string connectionString)
        {
            using (var cn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.CommandType = CommandType.Text;
                cn.Open();
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private async Task ExecuteSqlQueryAsync(string sql, string connectionString, Func<SqlDataReader, bool> processRow)
        {
            using (var cn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.CommandType = CommandType.Text;
                cn.Open();
                using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                    while (await reader.ReadAsync().ConfigureAwait(false) && processRow(reader)) ;
            }
        }
    }
}
