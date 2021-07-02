using System;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Packaging;
using SenseNet.Storage.Data.MsSqlClient;

namespace SenseNet.ContentRepository.Components
{
    public class MsSqlStatisticsComponent : SnComponent
    {
        public override string ComponentId { get; } = "SenseNet.Statistics.MsSql";

        public override void AddPatches(PatchBuilder builder)
        {
            builder.Install("1.0.0", "2020-06-22", "MS SQL data provider extension for the statistical data handling feature.")
                .ActionOnBefore(context =>
                {
                    var dataStore = Providers.Instance.DataStore;
                    if (!(dataStore.DataProvider is RelationalDataProviderBase dataProvider))
                        throw new InvalidOperationException("Cannot install MsSqlStatisticsComponent because it is " +
                                                            $"incompatible with Data provider {dataStore.DataProvider.GetType().FullName}.");


                    try
                    {
                        using var ctx = dataProvider.CreateDataContext(CancellationToken.None);
                        ctx.ExecuteNonQueryAsync(MsSqlStatisticalDataProvider.CreationScript).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        context.Log($"Error during installation of MsSqlStatisticsComponent: {ex.Message}");
                        throw;
                    }
                });
        }
    }
}
