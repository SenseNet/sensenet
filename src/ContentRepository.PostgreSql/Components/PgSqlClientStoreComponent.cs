using System;
using System.Threading;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.PgSqlClient;
using SenseNet.Diagnostics;
using SenseNet.Packaging;

namespace SenseNet.ContentRepository.Components
{
    public class PgSqlClientStoreComponent : SnComponent
    {
        public override string ComponentId { get; } = "SenseNet.ClientStore.PgSql";

        public override void AddPatches(PatchBuilder builder)
        {
            builder.Install("1.0.0", "2024-01-01", "PostgreSQL implementation of Client store.")
                .DependsOn("SenseNet.Services", "7.7.23")
                .ActionOnBefore(context =>
                {
                    if (!(Providers.Instance.DataProvider is RelationalDataProviderBase dataProvider))
                        throw new InvalidOperationException("Cannot install PgSqlClientStoreComponent because it is " +
                                                            $"incompatible with Data provider {Providers.Instance.DataProvider.GetType().FullName}.");

                    try
                    {
                        using var op = SnTrace.Database.StartOperation("PgSqlClientStoreComponent: " +
                            "Install PostgreSQL implementation of Client store (v1.0.0). " +
                            "Script name: PgSqlClientStoreDataProvider.DropAndCreateTablesSql.");
                        using var ctx = dataProvider.CreateDataContext(CancellationToken.None);
                        ctx.ExecuteNonQueryAsync(PgSqlClientStoreDataProvider.DropAndCreateTablesSql)
                            .GetAwaiter().GetResult();
                        op.Successful = true;
                    }
                    catch (Exception ex)
                    {
                        context.Log($"Error during installation of PgSqlClientStore: {ex.Message}");
                        throw;
                    }
                })
                .Action(context =>
                {
                    var clientStore = context.GetService<ClientStore>();
                    var clientOptions = context.GetService<IOptions<ClientStoreOptions>>().Value;
                    
                    clientStore.EnsureClientsAsync(clientOptions.Authority, clientOptions.RepositoryUrl.RemoveUrlSchema()).GetAwaiter().GetResult();
                });
        }
    }
}
