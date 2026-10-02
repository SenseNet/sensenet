using System;
using System.Data;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SenseNet.Tools.Configuration;

namespace SenseNet.Storage.Data.PgSqlClient
{
    [Serializable]
    public class PgDbCreationException : Exception
    {
        public PgDbCreationException() { }
        public PgDbCreationException(string message) : base(message) { }
        public PgDbCreationException(string message, Exception inner) : base(message, inner) { }
        protected PgDbCreationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Options for configuring PostgreSQL database installation.
    /// </summary>
    [OptionsClass(sectionName: "sensenet:install:postgres")]
    public class PgSqlDatabaseInstallationOptions
    {
        /// <summary>
        /// Allows the application to install the database schema and initial data on first run.
        /// </summary>
        public bool EnableFirstInstallDB { get; set; } = false;
        /// <summary>
        /// Database server host name.
        /// </summary>
        public string Server { get; set; }
        /// <summary>
        /// Database server port.
        /// </summary>
        public int Port { get; set; } = 5432;
        /// <summary>
        /// Database name.
        /// </summary>
        public string DatabaseName { get; set; }
        /// <summary>
        /// The user used for creating the database (superuser or createdb role).
        /// </summary>
        public string DbCreatorUserName { get; set; }
        /// <summary>
        /// The password of the user used for creating the database.
        /// </summary>
        public string DbCreatorPassword { get; set; }
        /// <summary>
        /// The user who will be the owner of the database.
        /// </summary>
        public string DbOwnerUserName { get; set; }
        /// <summary>
        /// The password of the user who will be the owner of the database.
        /// </summary>
        public string DbOwnerPassword { get; set; }
    }

    public class PgSqlDatabaseInstaller
    {
        private readonly ILogger<PgSqlDatabaseInstaller> _logger;
        private readonly PgSqlDatabaseInstallationOptions _options;

        public PgSqlDatabaseInstaller(IOptions<PgSqlDatabaseInstallationOptions> options, ILogger<PgSqlDatabaseInstaller> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async System.Threading.Tasks.Task InstallAsync()
        {
            ValidateParameters(_options);
            var targetConnectionString = GetConnectionString(_options);
            var maintenanceConnectionString = GetMaintenanceConnectionString(_options);
            var isIntegratedCustomer = string.IsNullOrEmpty(_options.DbOwnerUserName);

            if (!isIntegratedCustomer)
                await EnsureRoleAsync(_options.DbOwnerUserName, _options.DbOwnerPassword, maintenanceConnectionString)
                    .ConfigureAwait(false);

            await EnsureDatabaseAsync(_options.DatabaseName, _options.DbOwnerUserName, maintenanceConnectionString)
                .ConfigureAwait(false);

            // Ensure citext extension is available in the new database
            await EnsureCitextExtensionAsync(targetConnectionString).ConfigureAwait(false);
        }

        public void ValidateParameters(PgSqlDatabaseInstallationOptions options)
        {
            if (string.IsNullOrEmpty(options.DatabaseName))
                throw new ArgumentException("DatabaseName cannot be null or empty.");
        }

        public string GetConnectionString(PgSqlDatabaseInstallationOptions options)
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = string.IsNullOrEmpty(options.Server) ? "localhost" : options.Server,
                Port = options.Port > 0 ? options.Port : 5432,
                Database = options.DatabaseName
            };

            if (!string.IsNullOrEmpty(options.DbCreatorUserName))
            {
                builder.Username = options.DbCreatorUserName;
                builder.Password = options.DbCreatorPassword;
            }

            return builder.ConnectionString;
        }

        public string GetMaintenanceConnectionString(PgSqlDatabaseInstallationOptions options)
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = string.IsNullOrEmpty(options.Server) ? "localhost" : options.Server,
                Port = options.Port > 0 ? options.Port : 5432,
                Database = "postgres" // Connect to maintenance database
            };

            if (!string.IsNullOrEmpty(options.DbCreatorUserName))
            {
                builder.Username = options.DbCreatorUserName;
                builder.Password = options.DbCreatorPassword;
            }

            return builder.ConnectionString;
        }

        private async System.Threading.Tasks.Task EnsureRoleAsync(string userName, string password, string connectionString)
        {
            _logger.LogTrace($"Ensure role: {userName}");

            var roleExists = await QueryRoleExistsAsync(userName, connectionString).ConfigureAwait(false);
            if (!roleExists)
                await CreateRoleAsync(userName, password, connectionString).ConfigureAwait(false);
        }

        private async Task<bool> QueryRoleExistsAsync(string userName, string connectionString)
        {
            var sql = $"SELECT 1 FROM pg_roles WHERE rolname = '{userName}'";
            var result = false;
            await ExecuteSqlQueryAsync(sql, connectionString, reader =>
            {
                result = true;
                return false;
            });
            return result;
        }

        public async System.Threading.Tasks.Task CreateRoleAsync(string userName, string password, string connectionString)
        {
            try
            {
                // In PostgreSQL, a LOGIN role is equivalent to a SQL Server LOGIN
                var sql = $"CREATE ROLE \"{userName}\" WITH LOGIN PASSWORD '{password}'";
                await ExecuteSqlCommandAsync(sql, connectionString);
            }
            catch (PostgresException e)
            {
                if (e.SqlState == "42710") // duplicate_object: role already exists
                    return;
                throw new PgDbCreationException($"Cannot create role on the server. {e.Message}", e);
            }
        }

        private async System.Threading.Tasks.Task EnsureDatabaseAsync(string databaseName, string ownerUserName, string connectionString)
        {
            _logger.LogTrace($"Querying database: {databaseName}");
            var isExist = await QueryDatabaseExistsAsync(databaseName, connectionString).ConfigureAwait(false);
            if (!isExist)
                await CreateDatabaseAsync(databaseName, ownerUserName, connectionString).ConfigureAwait(false);
        }

        private async Task<bool> QueryDatabaseExistsAsync(string databaseName, string connectionString)
        {
            var result = false;
            var sql = $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'";
            await ExecuteSqlQueryAsync(sql, connectionString, reader =>
            {
                result = true;
                return false;
            });
            return result;
        }

        public async System.Threading.Tasks.Task CreateDatabaseAsync(string databaseName, string ownerUserName, string connectionString)
        {
            var ownerClause = string.IsNullOrEmpty(ownerUserName) ? "" : $" OWNER \"{ownerUserName}\"";
            var sql = $@"CREATE DATABASE ""{databaseName}""{ownerClause}
    ENCODING 'UTF8'
    LC_COLLATE 'en_US.UTF-8'
    LC_CTYPE 'en_US.UTF-8'
    TEMPLATE template0";

            _logger.LogTrace($"Creating database: {databaseName}");

            try
            {
                await ExecuteSqlCommandAsync(sql, connectionString);
            }
            catch (PostgresException e)
            {
                if (e.SqlState == "42P04") // duplicate_database: database already exists
                    return;
                // Try a simpler CREATE DATABASE without locale settings
                try
                {
                    var simpleSql = $@"CREATE DATABASE ""{databaseName}""{ownerClause}";
                    await ExecuteSqlCommandAsync(simpleSql, connectionString);
                }
                catch (PostgresException e2)
                {
                    if (e2.SqlState == "42P04")
                        return;
                    throw new PgDbCreationException($"Cannot create database. {e2.Message}", e2);
                }
            }
        }

        private async System.Threading.Tasks.Task EnsureCitextExtensionAsync(string connectionString)
        {
            try
            {
                await ExecuteSqlCommandAsync("CREATE EXTENSION IF NOT EXISTS citext", connectionString);
            }
            catch (PostgresException e)
            {
                _logger.LogWarning($"Could not create citext extension: {e.Message}");
            }
        }

        /* =================================================================================== */

        private async System.Threading.Tasks.Task ExecuteSqlCommandAsync(string sql, string connectionString)
        {
            await using var cn = new NpgsqlConnection(connectionString);
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.CommandType = CommandType.Text;
            await cn.OpenAsync().ConfigureAwait(false);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        private async System.Threading.Tasks.Task ExecuteSqlQueryAsync(string sql, string connectionString, Func<NpgsqlDataReader, bool> processRow)
        {
            await using var cn = new NpgsqlConnection(connectionString);
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.CommandType = CommandType.Text;
            await cn.OpenAsync().ConfigureAwait(false);
            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false) && processRow(reader)) ;
        }
    }
}
