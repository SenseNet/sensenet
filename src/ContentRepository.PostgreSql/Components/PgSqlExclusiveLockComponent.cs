using System;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.PgSqlClient;
using SenseNet.Diagnostics;
using SenseNet.Packaging;

namespace SenseNet.ContentRepository.Components
{
    public class PgSqlExclusiveLockComponent : SnComponent
    {
        public override string ComponentId { get; } = "SenseNet.ExclusiveLock.PgSql";

        public override void AddPatches(PatchBuilder builder)
        {
            builder.Install("1.0.0", "2024-01-01", "PostgreSQL data provider extension for the Exclusive lock feature.")
                .DependsOn("SenseNet.Services", "7.7.22")
                .ActionOnBefore(context =>
                {
                    var dataStore = Providers.Instance.DataStore;
                    if (!(dataStore.DataProvider is RelationalDataProviderBase dataProvider))
                        throw new InvalidOperationException("Cannot install PgSqlExclusiveLockComponent because it is " +
                                                            $"incompatible with Data provider {dataStore.DataProvider.GetType().FullName}.");

                    try
                    {
                        using var op = SnTrace.Database.StartOperation("PgSqlExclusiveLockComponent: " +
                            "Install PostgreSQL data provider extension for the Exclusive lock feature (v1.0.0). " +
                            "Script name: PgSqlExclusiveLockDataProvider.CreationScript.");
                        using var ctx = dataProvider.CreateDataContext(CancellationToken.None);
                        ctx.ExecuteNonQueryAsync(PgSqlExclusiveLockDataProvider.CreationScript).GetAwaiter().GetResult();
                        op.Successful = true;
                    }
                    catch (Exception ex)
                    {
                        context.Log($"Error during installation of PgSqlExclusiveLockComponent: {ex.Message}");
                        throw;
                    }
                });
        }
    }
}
