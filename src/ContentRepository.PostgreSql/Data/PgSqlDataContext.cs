using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Npgsql;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.PgSqlClient
{
    /// <summary>
    /// PostgreSQL implementation of SnDataContext. Provides connection, command and
    /// parameter factory methods using Npgsql.
    /// </summary>
    public class PgSqlDataContext : SnDataContext
    {
        private readonly string _connectionString;
        private static readonly ConcurrentDictionary<string, NpgsqlDataSource> DataSources = new();

        public PgSqlDataContext(string connectionString, DataOptions options, IRetrier retrier,
            CancellationToken cancellationToken = default)
            : base(options, retrier, cancellationToken)
        {
            _connectionString = connectionString
                ?? throw new ArgumentNullException(nameof(connectionString));
        }

        private static NpgsqlDataSource GetOrCreateDataSource(string connectionString)
        {
            return DataSources.GetOrAdd(connectionString, cs =>
            {
                var builder = new NpgsqlDataSourceBuilder(cs);
                builder.EnableDynamicJson();
                return builder.Build();
            });
        }

        public override DbConnection CreateConnection()
        {
            return GetOrCreateDataSource(_connectionString).CreateConnection();
        }

        public override DbCommand CreateCommand()
        {
            return new NpgsqlCommand();
        }

        public override DbParameter CreateParameter()
        {
            return new NpgsqlParameter();
        }

        /// <summary>
        /// Overrides base CreateParameter to handle Npgsql strict type checking.
        /// Npgsql requires the CLR value type to match the NpgsqlDbType exactly.
        /// </summary>
        public override DbParameter CreateParameter(string name, DbType dbType, object value)
        {
            // PostgreSQL timestamps are BIGINT, not binary rowversion.
            // ConvertInt64ToTimestamp returns long on PgSql, but the base class passes DbType.Binary.
            if (dbType == DbType.Binary && value is long longVal)
            {
                return new NpgsqlParameter
                {
                    ParameterName = name,
                    NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Bigint,
                    Value = longVal
                };
            }

            return base.CreateParameter(name, dbType, CoerceParameterValue(dbType, value));
        }

        /// <summary>
        /// Overrides base CreateParameter to handle Npgsql strict type checking.
        /// </summary>
        public override DbParameter CreateParameter(string name, DbType dbType, int size, object value)
        {
            if (dbType == DbType.Binary && value is long longVal)
            {
                return new NpgsqlParameter
                {
                    ParameterName = name,
                    NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Bigint,
                    Value = longVal
                };
            }

            return base.CreateParameter(name, dbType, size, CoerceParameterValue(dbType, value));
        }

        /// <summary>
        /// Coerces parameter values to match the expected CLR type for the given DbType.
        /// Npgsql (unlike SqlClient) strictly enforces type matching between DbType and CLR value type.
        /// </summary>
        private static object CoerceParameterValue(DbType dbType, object value)
        {
            if (value == null || value == DBNull.Value)
                return value;

            switch (dbType)
            {
                case DbType.Byte:
                    // Npgsql maps DbType.Byte to NpgsqlDbType.Smallint (PostgreSQL int2).
                    // It expects short (Int16), not byte. C# ternary (byte)1 : 0 returns int.
                    if (value is not short)
                        return Convert.ToInt16(value);
                    break;

                case DbType.Int16:
                    // Version.Major/Minor are int, but DbType.Int16 needs short.
                    if (value is not short)
                        return Convert.ToInt16(value);
                    break;
            }

            return value;
        }

        /// <summary>
        /// Creates a parameter with NpgsqlDbType for PostgreSQL-specific types.
        /// </summary>
        public DbParameter CreateParameter(string name, NpgsqlTypes.NpgsqlDbType dbType, object value)
        {
            var prm = new NpgsqlParameter
            {
                ParameterName = name,
                NpgsqlDbType = dbType,
                Value = value ?? DBNull.Value
            };
            return prm;
        }

        /// <summary>
        /// Creates a parameter with NpgsqlDbType and explicit size.
        /// </summary>
        public DbParameter CreateParameter(string name, NpgsqlTypes.NpgsqlDbType dbType, int size, object value)
        {
            var prm = new NpgsqlParameter
            {
                ParameterName = name,
                NpgsqlDbType = dbType,
                Size = size,
                Value = value ?? DBNull.Value
            };
            return prm;
        }

        public override TransactionWrapper WrapTransaction(DbTransaction underlyingTransaction,
            CancellationToken cancellationToken, TimeSpan timeout = default)
        {
            return new TransactionWrapper(underlyingTransaction, DataOptions, timeout, cancellationToken);
        }

        public string GetConnectionString() => _connectionString;
    }
}
