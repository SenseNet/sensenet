using System;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;
using SenseNet.Packaging;

namespace SenseNet.ContentRepository.Components
{
    public class MsSqlExclusiveLockComponent : SnComponent
    {
        public override string ComponentId { get; } = "SenseNet.ExclusiveLock.MsSql";

        public override void AddPatches(PatchBuilder builder)
        {
            builder.Install("1.0.0", "2020-10-15", "MS SQL data provider extension for the Exclusive lock feature.")
                .DependsOn("SenseNet.Services", "7.7.22")
                .ActionOnBefore(context =>
                {
                    var dataStore = Providers.Instance.DataStore;
                    if (!(dataStore.DataProvider is RelationalDataProviderBase dataProvider))
                        throw new InvalidOperationException("Cannot install MsSqlExclusiveLockComponent because it is " +
                                                            $"incompatible with Data provider {dataStore.DataProvider.GetType().FullName}.");


                    try
                    {
                        using var op = SnTrace.Database.StartOperation("MsSqlExclusiveLockComponent: " +
                            "Install MS SQL data provider extension for the Exclusive lock feature (v1.0.0). " +
                            "Script name: MsSqlExclusiveLockDataProvider.CreationScript.");
                        using var ctx = dataProvider.CreateDataContext(CancellationToken.None);
                        ctx.ExecuteNonQueryAsync(MsSqlExclusiveLockDataProvider.CreationScript).GetAwaiter().GetResult();
                        op.Successful = true;
                    }
                    catch (Exception ex)
                    {
                        context.Log($"Error during installation of MsSqlExclusiveLockComponent: {ex.Message}");
                        throw;
                    }
                });
        }
    }
}
