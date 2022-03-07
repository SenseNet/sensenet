using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using Tasks=System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
// ReSharper disable ConvertToUsingDeclaration
// ReSharper disable AccessToDisposedClosure

namespace SenseNet.ContentRepository.Security.Clients
{
    /// <summary>
    /// MsSql implementation of the <see cref="IClientStoreDataProvider"/> interface.
    /// </summary>
    public class MsSqlClientStoreDataProvider : IClientStoreDataProvider
    {
        #region Create scripts
        public static readonly string DropAndCreateTablesSql = @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ClientApps]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ClientApps](
	[ClientId] [varchar](50) NOT NULL,
	[Name] [nvarchar](450) NULL,
	[Repository] [nvarchar](450) NULL,
	[UserName] [nvarchar](450) NULL,
	[Authority] [nvarchar](450) NULL,
	[Type] [int] NULL,
 CONSTRAINT [PK_ClientApps] PRIMARY KEY CLUSTERED 
(
	[ClientId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END

/****** Table [dbo].[ClientSecrets] ******/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ClientSecrets]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ClientSecrets](
	[Id] [varchar](50) NOT NULL,
	[ClientId] [varchar](50) NOT NULL,
	[Value] [nvarchar](450) NOT NULL,
	[CreationDate] [datetime2](7) NOT NULL,
	[ValidTill] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ClientSecrets] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END

/****** Index [IX_ClientApps_Authority] ******/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ClientApps]') AND name = N'IX_ClientApps_Authority')
CREATE NONCLUSTERED INDEX [IX_ClientApps_Authority] ON [dbo].[ClientApps]
(
	[Authority] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]


/****** Index [IX_ClientApps_Repository] ******/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ClientApps]') AND name = N'IX_ClientApps_Repository')
CREATE NONCLUSTERED INDEX [IX_ClientApps_Repository] ON [dbo].[ClientApps]
(
	[Repository] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

/****** Index [IX_ClientSecrets_ClientId] ******/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ClientSecrets]') AND name = N'IX_ClientSecrets_ClientId')
CREATE NONCLUSTERED INDEX [IX_ClientSecrets_ClientId] ON [dbo].[ClientSecrets]
(
	[ClientId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ClientSecrets_ClientApps]') AND parent_object_id = OBJECT_ID(N'[dbo].[ClientSecrets]'))
ALTER TABLE [dbo].[ClientSecrets]  WITH CHECK ADD  CONSTRAINT [FK_ClientSecrets_ClientApps] FOREIGN KEY([ClientId])
REFERENCES [dbo].[ClientApps] ([ClientId])

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ClientSecrets_ClientApps]') AND parent_object_id = OBJECT_ID(N'[dbo].[ClientSecrets]'))
ALTER TABLE [dbo].[ClientSecrets] CHECK CONSTRAINT [FK_ClientSecrets_ClientApps]
";
        #endregion

        private RelationalDataProviderBase DataProvider => (RelationalDataProviderBase) Providers.Instance.DataProvider;

        /* =============================================================================================== LOAD */

        private static readonly string LoadClientsByRepositorySql = @"-- MsSqlClientStoreDataProviderExtension.LoadClientsByRepository
SELECT * FROM ClientApps WHERE Repository = @Repository
SELECT S.* FROM ClientSecrets S JOIN ClientApps A ON S.ClientId = A.ClientId WHERE A.Repository = @Repository
";
        /// <inheritdoc/>
        public async Tasks.Task<Client[]> LoadClientsByRepositoryAsync(string repositoryHost, CancellationToken cancellation)
        {
            using (var ctx = DataProvider.CreateDataContext(cancellation))
            {
                return await ctx.ExecuteReaderAsync(LoadClientsByRepositorySql, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@Repository", DbType.String, 450, repositoryHost));
                },
                async (reader, cancel) => await GetClientsFromReader(reader, cancel)).ConfigureAwait(false);
            }
        }


        private static readonly string LoadClientsByAuthority = @"-- MsSqlClientStoreDataProviderExtension.LoadClientsByAuthority
SELECT * FROM ClientApps WHERE Authority = @Authority
SELECT S.* FROM ClientSecrets S JOIN ClientApps A ON S.ClientId = A.ClientId WHERE A.Authority = @Authority
";

        /// <inheritdoc/>
        public async Tasks.Task<Client[]> LoadClientsByAuthorityAsync(string authority, CancellationToken cancellation)
        {
            using (var ctx = DataProvider.CreateDataContext(cancellation))
            {
                return await ctx.ExecuteReaderAsync(LoadClientsByAuthority, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@Authority", DbType.String, 450, authority));
                },
                async (reader, cancel) => await GetClientsFromReader(reader, cancel)).ConfigureAwait(false);
            }
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

        private static readonly string UpsertClientSql = @"-- MsSqlClientStoreDataProviderExtension.SaveClient/UpsertClientApp
BEGIN TRY
    INSERT INTO [ClientApps] ([ClientId], [Name], [Repository], [UserName], [Authority], [Type])
    				  VALUES (@ClientId,  @Name,  @Repository,  @UserName,  @Authority,  @Type)
END TRY
BEGIN CATCH
    -- ignore duplicate key errors, throw the rest.
    -- 2601: Cannot insert duplicate key row in object '<Object Name>' with unique index '<Index Name>'.
    -- 2627: Violation of PRIMARY KEY constraint '%.*ls'. Cannot insert duplicate key in object '%.*ls'.
    IF ERROR_NUMBER() IN (2601, 2627) 
        UPDATE [dbo].[ClientApps]
            SET [Name] = @Name, [Repository] = @Repository, [UserName] = @UserName, [Authority] = @Authority, [Type] = @Type
        WHERE [ClientId] = @ClientId
END CATCH
";

        private static readonly string DeleteSecretSql = @"-- MsSqlClientStoreDataProviderExtension.SaveClient/DeleteSecrets
DELETE FROM [ClientSecrets] WHERE [ClientId] = @ClientId
";

        /// <inheritdoc/>
        public async Tasks.Task SaveClientAsync(Client client, CancellationToken cancellation)
        {
            using (var ctx = ((RelationalDataProviderBase)Providers.Instance.DataProvider).CreateDataContext(cancellation))
            {
                using (var transaction = ctx.BeginTransaction())
                {
                    // UPSERT CLIENT
                    await ctx.ExecuteNonQueryAsync(UpsertClientSql, cmd =>
                    {
                        cmd.Parameters.Add(ctx.CreateParameter("@ClientId", DbType.AnsiString, 50, client.ClientId));
                        cmd.Parameters.Add(ctx.CreateParameter("@Name", DbType.String, 450, client.Name ?? client.ClientId));
                        cmd.Parameters.Add(ctx.CreateParameter("@Repository", DbType.String, 450, client.Repository));
                        cmd.Parameters.Add(ctx.CreateParameter("@UserName", DbType.String, 450,
                            (object) client.UserName ?? DBNull.Value));
                        cmd.Parameters.Add(ctx.CreateParameter("@Authority", DbType.String, 450, client.Authority));
                        cmd.Parameters.Add(ctx.CreateParameter("@Type", DbType.Int32, (int)client.Type));
                    }).ConfigureAwait(false);

                    // DELETE ALL RELATED SECRETS
                    await ctx.ExecuteNonQueryAsync(DeleteSecretSql, cmd =>
                    {
                        cmd.Parameters.Add(ctx.CreateParameter("@ClientId", DbType.AnsiString, 50, client.ClientId));
                    }).ConfigureAwait(false);

                    // INSERT SECRETS
                    foreach (var secret in client.Secrets)
                        await SaveSecretAsync(client.ClientId, secret, false, (MsSqlDataContext)ctx);

                    transaction.Commit();
                }
            }
        }
        
        /// <inheritdoc/>
        public async Tasks.Task SaveSecretAsync(string clientId, ClientSecret secret, CancellationToken cancellation)
        {
            using (var ctx = DataProvider.CreateDataContext(cancellation))
                await SaveSecretAsync(clientId, secret, true, (MsSqlDataContext)ctx);
        }
        
        private async Tasks.Task SaveSecretAsync(string clientId, ClientSecret secret, bool deleteBefore,
            MsSqlDataContext ctx)
        {
            var sql = deleteBefore
                ? @"-- MsSqlClientStoreDataProviderExtension.SaveSecret (delete and insert)
DELETE FROM [ClientSecrets] WHERE Id = @Id
INSERT INTO [ClientSecrets] ([Id], [ClientId], [Value], [CreationDate], [ValidTill])
                      VALUES( @Id,  @ClientId,  @Value,  @CreationDate,  @ValidTill)
"
                : @"-- MsSqlClientStoreDataProviderExtension.SaveSecret (insert only)
INSERT INTO [ClientSecrets] ([Id], [ClientId], [Value], [CreationDate], [ValidTill])
                      VALUES( @Id,  @ClientId,  @Value,  @CreationDate,  @ValidTill)
";
            await ctx.ExecuteNonQueryAsync(sql, cmd =>
            {
                cmd.Parameters.Add(ctx.CreateParameter("@ClientId", DbType.AnsiString, 50, clientId));
                cmd.Parameters.Add(ctx.CreateParameter("@Id", DbType.String, 450, secret.Id));
                cmd.Parameters.Add(ctx.CreateParameter("@Value", DbType.String, 450, secret.Value));
                cmd.Parameters.Add(ctx.CreateParameter("@CreationDate", DbType.DateTime2, secret.CreationDate));
                cmd.Parameters.Add(ctx.CreateParameter("@ValidTill", DbType.DateTime2, secret.ValidTill));
            }).ConfigureAwait(false);
        }

        /* =============================================================================================== DELETE */

        private static readonly string DeleteClientSql = @"-- MsSqlClientStoreDataProviderExtension.DeleteClient
DELETE FROM [ClientApps] WHERE [ClientId] = @ClientId
";
        /// <inheritdoc/>
        public async Tasks.Task DeleteClientAsync(string clientId, CancellationToken cancellation)
        {
            using (var ctx = DataProvider.CreateDataContext(cancellation))
            {
                using (var transaction = ctx.BeginTransaction())
                {
                    // DELETE ALL RELATED SECRETS
                    await ctx.ExecuteNonQueryAsync(DeleteSecretSql,
                        cmd =>
                    {
                        cmd.Parameters.Add(ctx.CreateParameter("@ClientId", DbType.AnsiString, 50, clientId));
                    }).ConfigureAwait(false);

                    await ctx.ExecuteNonQueryAsync(DeleteClientSql,
                        cmd =>
                    {
                        cmd.Parameters.Add(ctx.CreateParameter("@ClientId", DbType.AnsiString, 50, clientId));
                    }).ConfigureAwait(false);
                    
                    transaction.Commit();
                }
            }
        }


        private static readonly string DeleteSecretByHostSql = @"-- MsSqlClientStoreDataProviderExtension.DeleteSecretByHost (secrets)
DELETE FROM ClientSecrets WHERE [ClientId] IN (SELECT [ClientId] FROM ClientApps WHERE [Repository] = @Repository)";
        private static readonly string DeleteClientByHostSql = @"-- MsSqlClientStoreDataProviderExtension.DeleteClientByHost (clientApp)
DELETE FROM [ClientApps] WHERE [Repository] = @Repository
";
        /// <inheritdoc/>
        public async Tasks.Task DeleteClientByRepositoryHostAsync(string repositoryHost, CancellationToken cancellation)
        {
            using (var ctx = DataProvider.CreateDataContext(cancellation))
            {
                using (var transaction = ctx.BeginTransaction())
                {
                    // DELETE ALL RELATED SECRETS
                    await ctx.ExecuteNonQueryAsync(DeleteSecretByHostSql,
                        cmd =>
                        {
                            cmd.Parameters.Add(ctx.CreateParameter("@Repository", DbType.String, 450, repositoryHost));
                        }).ConfigureAwait(false);

                    await ctx.ExecuteNonQueryAsync(DeleteClientByHostSql,
                        cmd =>
                        {
                            cmd.Parameters.Add(ctx.CreateParameter("@Repository", DbType.String, 450, repositoryHost));
                        }).ConfigureAwait(false);

                    transaction.Commit();
                }
            }
        }

        private static readonly string DeleteSecret = @"-- MsSqlClientStoreDataProviderExtension.DeleteSecret (secrets)
DELETE FROM ClientSecrets WHERE [ClientId] = @ClientId AND [Id] = @SecretId";

        /// <inheritdoc/>
        public async Tasks.Task DeleteSecretAsync(string clientId, string secretId, CancellationToken cancellation)
        {
            using (var ctx = DataProvider.CreateDataContext(cancellation))
            {
                await ctx.ExecuteNonQueryAsync(DeleteSecret, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@ClientId", DbType.AnsiString, 50, clientId));
                    cmd.Parameters.Add(ctx.CreateParameter("@SecretId", DbType.AnsiString, 50, secretId));
                }).ConfigureAwait(false);
            }
        }
    }
}
