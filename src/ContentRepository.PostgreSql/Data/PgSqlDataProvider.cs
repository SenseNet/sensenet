using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Querying;
using SenseNet.ContentRepository.Storage.Data.PgSqlClient;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Storage.Data.PgSqlClient;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.PgSqlClient
{
    /// <summary>
    /// PostgreSQL implementation of the relational data provider.
    /// </summary>
    public partial class PgSqlDataProvider : RelationalDataProviderBase
    {
        private readonly IOptions<ConnectionStringOptions> _connectionOptions;
        private readonly DataOptions _dataOptions;
        private readonly PgSqlDatabaseInstallationOptions _dbInstallerOptions;
        private readonly PgSqlDatabaseInstaller _databaseInstaller;
        private readonly IDataInstaller _dataInstaller;
        private readonly ILogger _logger;
        private readonly IRetrier _retrier;

        public PgSqlDataProvider(
            IOptions<DataOptions> dataOptions,
            IOptions<ConnectionStringOptions> connectionOptions,
            IOptions<PgSqlDatabaseInstallationOptions> dbInstallerOptions,
            PgSqlDatabaseInstaller databaseInstaller,
            IDataInstaller dataInstaller,
            ILogger<PgSqlDataProvider> logger,
            IRetrier retrier)
        {
            _connectionOptions = connectionOptions;
            _dataOptions = dataOptions?.Value;
            _dbInstallerOptions = dbInstallerOptions?.Value ?? new PgSqlDatabaseInstallationOptions();
            _databaseInstaller = databaseInstaller;
            _dataInstaller = dataInstaller ?? throw new ArgumentNullException(nameof(dataInstaller));
            _logger = logger;
            _retrier = retrier;
        }

        public override SnDataContext CreateDataContext(CancellationToken cancellationToken)
        {
            return new PgSqlDataContext(_connectionOptions.Value.Repository, _dataOptions, _retrier, cancellationToken);
        }

        public override SchemaWriter CreateSchemaWriter()
        {
            return new PgSqlSchemaWriter(_connectionOptions);
        }

        // =============================================================================================== Queries

        public override async Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(
            int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name,
            CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation(() => "PgSqlDataProvider: " +
                $"QueryNodesByTypeAndPathAndName(nodeTypeIds: {nodeTypeIds?.ToTrace() ?? "null"}, " +
                $"pathStart: {pathStart?.ToTrace() ?? "null"}, orderByPath: {orderByPath}, name: {name})");

            var sql = new System.Text.StringBuilder(
                "SELECT N.\"NodeId\" FROM \"Nodes\" N");

            if (nodeTypeIds != null && nodeTypeIds.Length > 0)
            {
                sql.Append(" JOIN \"NodeTypes\" T ON N.\"NodeTypeId\" = T.\"NodeTypeId\" WHERE T.\"NodeTypeId\" IN (");
                sql.Append(string.Join(", ", Enumerable.Range(0, nodeTypeIds.Length).Select(i => "@TypeId" + i)));
                sql.Append(")");
            }
            else
            {
                sql.Append(" WHERE 1=1");
            }

            if (pathStart != null && pathStart.Length > 0)
            {
                sql.Append(" AND (");
                for (var i = 0; i < pathStart.Length; i++)
                {
                    if (i > 0) sql.Append(" OR ");
                    sql.Append($"N.\"Path\" LIKE @Path{i} || '/%' OR N.\"Path\" = @Path{i}");
                }
                sql.Append(")");
            }

            if (name != null)
                sql.Append(" AND N.\"Name\" = @Name");

            if (orderByPath)
                sql.Append(" ORDER BY N.\"Path\"");

            using var ctx = CreateDataContext(cancellationToken);
            var heads = await ctx.ExecuteReaderAsync(sql.ToString(), cmd =>
            {
                if (nodeTypeIds != null)
                    for (var i = 0; i < nodeTypeIds.Length; i++)
                        cmd.Parameters.Add(ctx.CreateParameter("@TypeId" + i, DbType.Int32, nodeTypeIds[i]));
                if (pathStart != null)
                    for (var i = 0; i < pathStart.Length; i++)
                        cmd.Parameters.Add(ctx.CreateParameter("@Path" + i, DbType.String, pathStart[i].TrimEnd('/')));
                if (name != null)
                    cmd.Parameters.Add(ctx.CreateParameter("@Name", DbType.String, 450, name));
            }, async (reader, cancel) =>
            {
                cancel.ThrowIfCancellationRequested();
                var result = new List<int>();
                while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    result.Add(reader.GetInt32(0));
                return (IEnumerable<int>)result;
            }).ConfigureAwait(false);

            op.Successful = true;
            return heads;
        }

        public override async Task<IEnumerable<int>> QueryNodesByTypeAndPathAndPropertyAsync(
            int[] nodeTypeIds, string pathStart, bool orderByPath,
            List<QueryPropertyData> properties, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation(() => "PgSqlDataProvider: " +
                $"QueryNodesByTypeAndPathAndProperty(nodeTypeIds: {nodeTypeIds.ToTrace()}, " +
                $"pathStart: {pathStart}, orderByPath: {orderByPath}, properties: {properties.Count})");

            var sql = new System.Text.StringBuilder(
                "SELECT N.\"NodeId\" FROM \"Nodes\" N JOIN \"NodeTypes\" T ON N.\"NodeTypeId\" = T.\"NodeTypeId\"" +
                " JOIN \"Versions\" V ON N.\"NodeId\" = V.\"NodeId\" AND V.\"VersionId\" = N.\"LastMinorVersionId\"" +
                " WHERE T.\"NodeTypeId\" IN (");
            sql.Append(string.Join(", ", Enumerable.Range(0, nodeTypeIds.Length).Select(i => "@TypeId" + i)));
            sql.Append(")");

            if (!string.IsNullOrEmpty(pathStart))
                sql.Append(" AND (N.\"Path\" LIKE @Path || '/%' OR N.\"Path\" = @Path)");

            var paramIndex = 0;
            foreach (var prop in properties)
            {
                if (prop.QueryOperator == Operator.Equal)
                {
                    sql.Append($" AND V.\"DynamicProperties\" LIKE '%' || @PropName{paramIndex} || ':' || @PropValue{paramIndex} || E'\\r\\n%'");
                }
                paramIndex++;
            }

            if (orderByPath)
                sql.Append(" ORDER BY N.\"Path\"");

            using var ctx = CreateDataContext(cancellationToken);
            var heads = await ctx.ExecuteReaderAsync(sql.ToString(), cmd =>
            {
                for (var i = 0; i < nodeTypeIds.Length; i++)
                    cmd.Parameters.Add(ctx.CreateParameter("@TypeId" + i, DbType.Int32, nodeTypeIds[i]));
                if (!string.IsNullOrEmpty(pathStart))
                    cmd.Parameters.Add(ctx.CreateParameter("@Path", DbType.String, 450, pathStart.TrimEnd('/')));
                paramIndex = 0;
                foreach (var prop in properties)
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@PropName" + paramIndex, DbType.String, 450, prop.PropertyName));
                    cmd.Parameters.Add(ctx.CreateParameter("@PropValue" + paramIndex, DbType.String, 450,
                        prop.Value?.ToString() ?? string.Empty));
                    paramIndex++;
                }
            }, async (reader, cancel) =>
            {
                cancel.ThrowIfCancellationRequested();
                var result = new List<int>();
                while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    result.Add(reader.GetInt32(0));
                return (IEnumerable<int>)result;
            }).ConfigureAwait(false);

            op.Successful = true;
            return heads;
        }

        // =============================================================================================== Exception handling

        protected override Exception GetException(Exception e, string msg = null)
        {
            if (e is ContentNotFoundException)
                return e;

            if (e is NodeIsOutOfDateException)
                return e;

            if (e is NodeAlreadyExistsException)
                return e;

            if (e is NpgsqlException npgEx)
            {
                if (msg == null)
                    msg = "A database exception occured during execution of the operation." +
                          " See InnerException for details.";

                // 23505 = unique_violation
                if (npgEx.SqlState == "23505")
                    return new NodeAlreadyExistsException(msg, npgEx);
            }

            return base.GetException(e, msg);
        }

        public override bool IsDeadlockException(Exception e)
        {
            // 40P01 = deadlock_detected
            return e is NpgsqlException npgEx && npgEx.SqlState == "40P01";
        }

        // =============================================================================================== Installation

        public override async System.Threading.Tasks.Task InstallInitialDataAsync(InitialData initialData, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlDataProvider: InstallInitialData.");

            await _dataInstaller.InstallInitialDataAsync(initialData, this, cancellationToken).ConfigureAwait(false);

            op.Successful = true;
        }

        public override async System.Threading.Tasks.Task InstallDatabaseAsync(CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlDataProvider: InstallDatabase.");

            if (!_dbInstallerOptions.EnableFirstInstallDB)
            {
                _logger.LogTrace("EnableFirstInstallDB is disabled. Skipping database installation.");
                op.Successful = true;
                return;
            }

            if (await IsDatabaseAlreadyInstalledAsync(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogWarning("Database already contains data. Skipping installation to prevent data loss.");
                op.Successful = true;
                return;
            }

            if (!string.IsNullOrEmpty(_dbInstallerOptions.DatabaseName))
            {
                _logger.LogTrace($"Executing installer for database {_dbInstallerOptions.DatabaseName}.");

                await _databaseInstaller.InstallAsync().ConfigureAwait(false);

                // warmup: we have to wait a short period before the new db becomes usable
                await Tools.Retrier.RetryAsync(15, 2000, async () =>
                {
                    _logger.LogTrace("Trying to connect to the new database...");

                    using var ctx = CreateDataContext(cancellationToken);
                    await ctx.ExecuteNonQueryAsync("SELECT 1 FROM pg_catalog.pg_tables LIMIT 1").ConfigureAwait(false);
                }, (i, ex) =>
                {
                    if (ex == null)
                    {
                        _logger.LogTrace("Successfully connected to the newly created database.");
                        return true;
                    }

                    if (i == 1)
                        _logger.LogError($"Could not connect to the database {_dbInstallerOptions.DatabaseName} after several retries.");

                    return false;
                }, cancellationToken);
            }
            else
            {
                _logger.LogTrace("Install database name is not configured, moving on to schema installation.");
            }

            _logger.LogTrace("Executing security schema script.");
            await ExecuteEmbeddedNonQueryScriptAsync(
                    "SenseNet.ContentRepository.Scripts.PgSqlInstall_Security.sql", cancellationToken)
                .ConfigureAwait(false);

            _logger.LogTrace("Executing database schema script.");
            await ExecuteEmbeddedNonQueryScriptAsync(
                    "SenseNet.ContentRepository.Scripts.PgSqlInstall_Schema.sql", cancellationToken)
                .ConfigureAwait(false);

            op.Successful = true;
        }

        private IBlobStorage BlobStorage => Providers.Instance.BlobStorage;

        private async System.Threading.Tasks.Task<bool> IsDatabaseAlreadyInstalledAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var ctx = CreateDataContext(cancellationToken);
                var result = await ctx.ExecuteScalarAsync(@"
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM information_schema.tables WHERE table_name = 'Nodes'
                    ) THEN (SELECT COUNT(1) FROM ""Nodes"") ELSE 0 END").ConfigureAwait(false);

                var count = Convert.ToInt32(result);
                if (count > 0)
                    _logger.LogTrace($"IsDatabaseAlreadyInstalledAsync: Nodes table contains {count} rows, database is already installed.");

                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "IsDatabaseAlreadyInstalledAsync: Could not determine database state. Assuming not installed.");
                return false;
            }
        }

        public override async Task<bool> IsDatabaseReadyAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var ctx = CreateDataContext(cancellationToken);
                var result = await ctx.ExecuteScalarAsync(@"
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public' AND table_name = 'Nodes'
                    ) THEN true ELSE false END").ConfigureAwait(false);
                return Convert.ToBoolean(result);
            }
            catch (NpgsqlException ex) when (
                ex.SqlState == "3D000" /* invalid_catalog_name (database doesn't exist) */ ||
                ex.SqlState == "08006" /* connection_failure */ ||
                ex.SqlState == "08001" /* sqlclient_unable_to_establish_sqlconnection */)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =============================================================================================== Timestamp

        protected override long ConvertTimestampToInt64(object timestamp)
        {
            if (timestamp == null || timestamp == DBNull.Value)
                return 0L;
            if (timestamp is long l)
                return l;
            if (timestamp is int i)
                return i;
            if (timestamp is byte[] bytes)
            {
                // byte[] may come from GetSafeByteArray which converts long via HostToNetworkOrder
                if (bytes.Length == 8)
                    return System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt64(bytes, 0));
                // Fallback: manual big-endian conversion (same as MSSQL)
                var result = 0L;
                for (int j = 0; j < bytes.Length; j++)
                    result = (result << 8) + bytes[j];
                return result;
            }
            return Convert.ToInt64(timestamp);
        }

        protected override object ConvertInt64ToTimestamp(long timestamp)
        {
            return timestamp;
        }

        // =============================================================================================== GetAppModelScript

        protected override string GetAppModelScript(IEnumerable<string> paths, bool resolveAll, bool resolveChildren)
        {
            // Build path values for CTE
            var sb = new System.Text.StringBuilder();
            var index = 0;
            foreach (var path in paths)
            {
                if (index > 0) sb.Append(" UNION ALL ");
                sb.AppendFormat("SELECT {0} AS \"Id\", '{1}' AS \"Path\"", ++index,
                    path.Replace("'", "''").Replace("/*", "**").Replace("--", "**"));
            }

            var pathsCte = sb.ToString();

            if (resolveAll)
            {
                if (resolveChildren)
                {
                    // Return NodeId and Path of child nodes
                    return $@"WITH _paths AS ({pathsCte})
SELECT N.""NodeId"", N.""Path""::TEXT FROM ""Nodes"" N
WHERE N.""ParentNodeId"" IN (
    SELECT N2.""NodeId"" FROM _paths C
    LEFT OUTER JOIN ""Nodes"" N2 ON C.""Path"" = N2.""Path""
    WHERE N2.""Path"" IS NOT NULL
)";
                }
                else
                {
                    // Return all matching NodeIds
                    return $@"WITH _paths AS ({pathsCte})
SELECT N.""NodeId"" FROM _paths C
LEFT OUTER JOIN ""Nodes"" N ON C.""Path"" = N.""Path""
WHERE N.""Path"" IS NOT NULL
ORDER BY C.""Id""";
                }
            }
            else
            {
                // Return first matching NodeId only
                return $@"WITH _paths AS ({pathsCte})
SELECT N.""NodeId"" FROM _paths C
LEFT OUTER JOIN ""Nodes"" N ON C.""Path"" = N.""Path""
WHERE N.""Path"" IS NOT NULL
ORDER BY C.""Id""
LIMIT 1";
            }
        }

        // =============================================================================================== Provider Tools

        public override DateTime RoundDateTime(DateTime d)
        {
            return new DateTime(d.Ticks / 100000 * 100000);
        }

        // =============================================================================================== Health

        public override object GetConfigurationForHealthDashboard()
        {
            return new
            {
                Repository = _connectionOptions.Value.Repository != null
                    ? "(configured)" : "(not configured)",
                _dataOptions.DbCommandTimeout,
                _dataOptions.TransactionTimeout,
                _dataOptions.LongTransactionTimeout
            };
        }

        public override async Task<HealthResult> GetHealthAsync(CancellationToken cancel)
        {
            object data = null;
            string error = null;
            TimeSpan? elapsed = null;

            try
            {
                var timer = System.Diagnostics.Stopwatch.StartNew();
                var sql = "SELECT \"Path\"::TEXT FROM \"Nodes\" WHERE \"NodeId\" = 1";
                using var ctx = CreateDataContext(cancel);
                data = await ctx.ExecuteScalarAsync(sql).ConfigureAwait(false);
                timer.Stop();
                elapsed = timer.Elapsed;
            }
            catch (Exception e)
            {
                error = e.Message;
            }

            HealthResult result;
            if (error != null)
            {
                result = new HealthResult
                {
                    Color = HealthColor.Red,
                    Reason = $"ERROR: {error}",
                    Method = "Trying to load first Node's Path."
                };
            }
            else if (data == null || data == DBNull.Value)
            {
                result = new HealthResult
                {
                    Color = HealthColor.Yellow,
                    Reason = "Invalid data",
                    Method = "Trying to interpret the loaded first Node's Path."
                };
            }
            else
            {
                result = new HealthResult
                {
                    Color = HealthColor.Green,
                    ResponseTime = elapsed,
                    Method = "Measure time of loading first Node's Path in secs."
                };
            }

            return result;
        }

        // =============================================================================================== Script execution

        private async System.Threading.Tasks.Task ExecuteEmbeddedNonQueryScriptAsync(string scriptName, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlDataProvider: " +
                "ExecuteEmbeddedNonQueryScript(scriptName: {0})", scriptName);

            using var stream = GetType().Assembly.GetManifestResourceStream(scriptName);
            if (stream == null)
                throw new InvalidOperationException($"Embedded resource {scriptName} not found.");

            using var sr = new System.IO.StreamReader(stream);
            var script = await sr.ReadToEndAsync().ConfigureAwait(false);

            using var ctx = CreateDataContext(cancellationToken);
            await ctx.ExecuteNonQueryAsync(script).ConfigureAwait(false);

            op.Successful = true;
        }

        // =============================================================================================== Retry

        protected override bool ShouldRetryOnError(Exception ex)
        {
            if (ex is InvalidOperationException && ex.Message.Contains("connection from the pool"))
                return true;
            if (ex is NpgsqlException npgEx && (
                npgEx.IsTransient ||
                npgEx.SqlState == "08006" /* connection_failure */ ||
                npgEx.SqlState == "08001" /* sqlclient_unable_to_establish_sqlconnection */ ||
                npgEx.SqlState == "57P01" /* admin_shutdown */))
                return true;
            return false;
        }
    }
}
