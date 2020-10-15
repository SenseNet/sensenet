using System;
using System.Threading;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Packaging;

namespace SenseNet.ContentRepository.Components
{
    public class MsSqlExclusiveLockComponent : SnComponent
    {
        public override string ComponentId { get; } = "SenseNet.ExclusiveLock.MsSql";

        public override void AddPatches(PatchBuilder builder)
        {
            builder.Install("1.0.0", "2020-10-15", "MS SQL data provider extension for the Exclusive lock feature.")
                .ActionOnBefore(context =>
                {
                    if (!(DataStore.DataProvider is RelationalDataProviderBase dataProvider))
                        throw new InvalidOperationException("Cannot install MsSqlExclusiveLockComponent because it is " +
                                                            $"incompatible with Data provider {DataStore.DataProvider.GetType().FullName}.");


                    try
                    {
                        using var ctx = dataProvider.CreateDataContext(CancellationToken.None);
                        ctx.ExecuteNonQueryAsync(MsSqlExclusiveLockDataProvider.CreationScript).GetAwaiter().GetResult();
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
