using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using Tasks=System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.PgSqlClient;
using SenseNet.Diagnostics;
using SenseNet.Tools;
// ReSharper disable AccessToDisposedClosure

namespace SenseNet.ContentRepository.Security.Clients
{
    public class PgSqlClientStoreDataProvider : IClientStoreDataProvider
    {
        private readonly IRetrier _retrier;
        private DataOptions DataOptions { get; }
        private ConnectionStringOptions ConnectionStrings { get; }

        public PgSqlClientStoreDataProvider(IOptions<DataOptions> dataOptions,
            IOptions<ConnectionStringOptions> connectionOptions, IRetrier retrier)
        {
            _retrier = retrier;
            DataOptions = dataOptions?.Value ?? new DataOptions();
            ConnectionStrings = connectionOptions?.Value ?? new ConnectionStringOptions();
        }

        /* =============================================================================================== LOAD */

        private static readonly string LoadClientsByRepositorySql = @"-- PgSqlClientStoreDataProvider.LoadClientsByRepository
SELECT * FROM ""ClientApps"" WHERE ""Repository"" = @Repository;
SELECT S.* FROM ""ClientSecrets"" S JOIN ""ClientApps"" A ON S.""ClientId"" = A.""ClientId"" WHERE A.""Repository"" = @Repository;
";

        public async Tasks.Task<Client[]> LoadClientsByRepositoryAsync(string repositoryHost,
            CancellationToken cancellation)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlClientStoreDataProvider: " +
                "LoadClientsByRepository(repositoryHost: {0})", repositoryHost);

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellation);
            var result = await ctx.ExecuteReaderAsync(LoadClientsByRepositorySql, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@Repository", DbType.String, 450, repositoryHost));
                },
                async (reader, cancel) => await GetClientsFromReader(reader, cancel)).ConfigureAwait(false);
            op.Successful = true;

            return result;
        }

        private static readonly string LoadClientsByAuthoritySql = @"-- PgSqlClientStoreDataProvider.LoadClientsByAuthority
SELECT * FROM ""ClientApps"" WHERE ""Authority"" = @Authority;
SELECT S.* FROM ""ClientSecrets"" S JOIN ""ClientApps"" A ON S.""ClientId"" = A.""ClientId"" WHERE A.""Authority"" = @Authority;
";

        public async Tasks.Task<Client[]> LoadClientsByAuthorityAsync(string authority,
            CancellationToken cancellation)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlClientStoreDataProvider: " +
                "LoadClientsByAuthority(authority: {0})", authority);

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellation);
            var result = await ctx.ExecuteReaderAsync(LoadClientsByAuthoritySql, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@Authority", DbType.String, 450, authority));
                },
                async (reader, cancel) => await GetClientsFromReader(reader, cancel)).ConfigureAwait(false);
            op.Successful = true;

            return result;
        }

        private async Tasks.Task<Client[]> GetClientsFromReader(DbDataReader reader, CancellationToken cancel)
        {
            var clients = new List<Client>();
            while (await reader.ReadAsync(cancel).ConfigureAwait(false))
            {
                cancel.ThrowIfCancellationRequested();
                clients.Add(new Client
                {
                    ClientId = reader.GetSafeString(reader.GetOrdinal("ClientId")),
                    Name = reader.GetSafeString(reader.GetOrdinal("Name")),
                    Repository = reader.GetSafeString(reader.GetOrdinal("Repository")),
                    UserName = reader.GetSafeString(reader.GetOrdinal("UserName")),
                    Authority = reader.GetSafeString(reader.GetOrdinal("Authority")),
                    Type = (ClientType)reader.GetInt32(reader.GetOrdinal("Type")),
                });
            }
            await reader.NextResultAsync(cancel);
            while (await reader.ReadAsync(cancel).ConfigureAwait(false))
            {
                cancel.ThrowIfCancellationRequested();
                var clientId = reader.GetString(reader.GetOrdinal("ClientId"));
                var client = clients.First(x => x.ClientId == clientId);
                client.Secrets.Add(new ClientSecret
                {
                    Id = reader.GetString(reader.GetOrdinal("Id")),
                    Value = reader.GetString(reader.GetOrdinal("Value")),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    ValidTill = reader.GetDateTime(reader.GetOrdinal("ValidTill")),
                });
            }

            return clients.ToArray();
        }

        /* =============================================================================================== SAVE */

        private static readonly string UpsertClientSql = @"-- PgSqlClientStoreDataProvider.SaveClient
INSERT INTO ""ClientApps"" (""ClientId"", ""Name"", ""Repository"", ""UserName"", ""Authority"", ""Type"")
VALUES (@ClientId, @Name, @Repository, @UserName, @Authority, @Type)
ON CONFLICT (""ClientId"") DO UPDATE SET
    ""Name"" = EXCLUDED.""Name"", ""Repository"" = EXCLUDED.""Repository"",
    ""UserName"" = EXCLUDED.""UserName"", ""Authority"" = EXCLUDED.""Authority"",
    ""Type"" = EXCLUDED.""Type""
";

        private static readonly string DeleteSecretsByClientSql = @"-- PgSqlClientStoreDataProvider.DeleteSecrets
DELETE FROM ""ClientSecrets"" WHERE ""ClientId"" = @ClientId
";

        public async Tasks.Task SaveClientAsync(Client client, CancellationToken cancellation)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlClientStoreDataProvider: " +
                "SaveClient: ClientId/Name: {0}, Repository: {1}, UserName: {2}, Authority: {3}, Type: {4}({5})",
                client?.ClientId, client?.Repository, client?.UserName,
                client?.Authority, client?.Type, (int)(client?.Type ?? 0));

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellation);
            using var transaction = ctx.BeginTransaction();

            // UPSERT CLIENT
            await ctx.ExecuteNonQueryAsync(UpsertClientSql, cmd =>
            {
                cmd.Parameters.Add(ctx.CreateParameter("@ClientId", DbType.AnsiString, 50, client.ClientId));
                cmd.Parameters.Add(ctx.CreateParameter("@Name", DbType.String, 450, client.Name ?? client.ClientId));
                cmd.Parameters.Add(ctx.CreateParameter("@Repository", DbType.String, 450, client.Repository));
                cmd.Parameters.Add(ctx.CreateParameter("@UserName", DbType.String, 450,
                    (object)client.UserName ?? DBNull.Value));
                cmd.Parameters.Add(ctx.CreateParameter("@Authority", DbType.String, 450, client.Authority));
                cmd.Parameters.Add(ctx.CreateParameter("@Type", DbType.Int32, (int)client.Type));
            }).ConfigureAwait(false);

            // DELETE ALL RELATED SECRETS
            await ctx.ExecuteNonQueryAsync(DeleteSecretsByClientSql, cmd =>
            {
                cmd.Parameters.Add(ctx.CreateParameter("@ClientId", DbType.AnsiString, 50, client.ClientId));
            }).ConfigureAwait(false);

            // INSERT SECRETS
            foreach (var secret in client.Secrets)
                await SaveSecretInternalAsync(client.ClientId, secret, false, ctx);

            transaction.Commit();
            op.Successful = true;
        }

        public async Tasks.Task SaveSecretAsync(string clientId, ClientSecret secret,
            CancellationToken cancellation)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellation);
            await SaveSecretInternalAsync(clientId, secret, true, ctx);
        }

        private async Tasks.Task SaveSecretInternalAsync(string clientId, ClientSecret secret,
            bool deleteBefore, PgSqlDataContext ctx)
        {
            var sql = deleteBefore
                ? @"-- PgSqlClientStoreDataProvider.SaveSecret (delete and insert)
DELETE FROM ""ClientSecrets"" WHERE ""Id"" = @Id;
INSERT INTO ""ClientSecrets"" (""Id"", ""ClientId"", ""Value"", ""CreationDate"", ""ValidTill"")
VALUES (@Id, @ClientId, @Value, @CreationDate, @ValidTill)
"
                : @"-- PgSqlClientStoreDataProvider.SaveSecret (insert only)
INSERT INTO ""ClientSecrets"" (""Id"", ""ClientId"", ""Value"", ""CreationDate"", ""ValidTill"")
VALUES (@Id, @ClientId, @Value, @CreationDate, @ValidTill)
";
            using var op = SnTrace.Database.StartOperation("PgSqlClientStoreDataProvider: " +
                "SaveSecret: ClientId: {0}, Id: {1}, Value: {2}," +
                " CreationDate: {3:yyyy-MM-dd HH:mm:ss.fffff}, ValidTill: {4:yyyy-MM-dd HH:mm:ss.fffff}",
                clientId, secret?.Id, secret?.Value, secret?.CreationDate, secret?.ValidTill);

            await ctx.ExecuteNonQueryAsync(sql, cmd =>
            {
                cmd.Parameters.Add(ctx.CreateParameter("@ClientId", DbType.AnsiString, 50, clientId));
                cmd.Parameters.Add(ctx.CreateParameter("@Id", DbType.String, 450, secret.Id));
                cmd.Parameters.Add(ctx.CreateParameter("@Value", DbType.String, 450, secret.Value));
                cmd.Parameters.Add(ctx.CreateParameter("@CreationDate", DbType.DateTime2, secret.CreationDate));
                cmd.Parameters.Add(ctx.CreateParameter("@ValidTill", DbType.DateTime2, secret.ValidTill));
            }).ConfigureAwait(false);

            op.Successful = true;
        }

        /* =============================================================================================== DELETE */

        private static readonly string DeleteClientSql = @"-- PgSqlClientStoreDataProvider.DeleteClient
DELETE FROM ""ClientApps"" WHERE ""ClientId"" = @ClientId
";

        public async Tasks.Task DeleteClientAsync(string clientId, CancellationToken cancellation)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlClientStoreDataProvider: " +
                "DeleteClient(clientId: {0})", clientId);

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellation);
            using var transaction = ctx.BeginTransaction();

            // DELETE ALL RELATED SECRETS
            await ctx.ExecuteNonQueryAsync(DeleteSecretsByClientSql, cmd =>
            {
                cmd.Parameters.Add(ctx.CreateParameter("@ClientId", DbType.AnsiString, 50, clientId));
            }).ConfigureAwait(false);

            await ctx.ExecuteNonQueryAsync(DeleteClientSql, cmd =>
            {
                cmd.Parameters.Add(ctx.CreateParameter("@ClientId", DbType.AnsiString, 50, clientId));
            }).ConfigureAwait(false);

            transaction.Commit();
            op.Successful = true;
        }

        private static readonly string DeleteSecretByHostSql = @"-- PgSqlClientStoreDataProvider.DeleteSecretByHost
DELETE FROM ""ClientSecrets"" WHERE ""ClientId"" IN (SELECT ""ClientId"" FROM ""ClientApps"" WHERE ""Repository"" = @Repository)
";
        private static readonly string DeleteClientByHostSql = @"-- PgSqlClientStoreDataProvider.DeleteClientByHost
DELETE FROM ""ClientApps"" WHERE ""Repository"" = @Repository
";

        public async Tasks.Task DeleteClientByRepositoryHostAsync(string repositoryHost,
            CancellationToken cancellation)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlClientStoreDataProvider: " +
                "DeleteClientByRepositoryHost(repositoryHost: {0})", repositoryHost);

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellation);
            using var transaction = ctx.BeginTransaction();

            // DELETE ALL RELATED SECRETS
            await ctx.ExecuteNonQueryAsync(DeleteSecretByHostSql, cmd =>
            {
                cmd.Parameters.Add(ctx.CreateParameter("@Repository", DbType.String, 450, repositoryHost));
            }).ConfigureAwait(false);

            await ctx.ExecuteNonQueryAsync(DeleteClientByHostSql, cmd =>
            {
                cmd.Parameters.Add(ctx.CreateParameter("@Repository", DbType.String, 450, repositoryHost));
            }).ConfigureAwait(false);

            transaction.Commit();
            op.Successful = true;
        }

        private static readonly string DeleteSecretSql = @"-- PgSqlClientStoreDataProvider.DeleteSecret
DELETE FROM ""ClientSecrets"" WHERE ""ClientId"" = @ClientId AND ""Id"" = @SecretId
";

        public async Tasks.Task DeleteSecretAsync(string clientId, string secretId,
            CancellationToken cancellation)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlClientStoreDataProvider: " +
                "DeleteSecret(clientId: {0}, secretId: {1})", clientId, secretId);

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellation);
            await ctx.ExecuteNonQueryAsync(DeleteSecretSql, cmd =>
            {
                cmd.Parameters.Add(ctx.CreateParameter("@ClientId", DbType.AnsiString, 50, clientId));
                cmd.Parameters.Add(ctx.CreateParameter("@SecretId", DbType.AnsiString, 50, secretId));
            }).ConfigureAwait(false);

            op.Successful = true;
        }

        // =============================================================================================== Installation

        public static readonly string DropAndCreateTablesSql = @"-- PgSqlClientStoreDataProvider.CreateTables
DROP TABLE IF EXISTS ""ClientSecrets"" CASCADE;
DROP TABLE IF EXISTS ""ClientApps"" CASCADE;

CREATE TABLE IF NOT EXISTS ""ClientApps"" (
    ""ClientId"" VARCHAR(50) NOT NULL PRIMARY KEY,
    ""Name"" VARCHAR(450) NULL,
    ""Repository"" VARCHAR(450) NULL,
    ""UserName"" VARCHAR(450) NULL,
    ""Authority"" VARCHAR(450) NULL,
    ""Type"" INT NULL
);

CREATE INDEX IF NOT EXISTS ""IX_ClientApps_Repository"" ON ""ClientApps"" (""Repository"");
CREATE INDEX IF NOT EXISTS ""IX_ClientApps_Authority"" ON ""ClientApps"" (""Authority"");

CREATE TABLE IF NOT EXISTS ""ClientSecrets"" (
    ""Id"" VARCHAR(50) NOT NULL PRIMARY KEY,
    ""ClientId"" VARCHAR(50) NOT NULL REFERENCES ""ClientApps"" (""ClientId""),
    ""Value"" VARCHAR(450) NOT NULL,
    ""CreationDate"" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    ""ValidTill"" TIMESTAMP WITHOUT TIME ZONE NOT NULL
);

CREATE INDEX IF NOT EXISTS ""IX_ClientSecrets_ClientId"" ON ""ClientSecrets"" (""ClientId"");
";
    }
}
