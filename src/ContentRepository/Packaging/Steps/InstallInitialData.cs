using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.DataModel;

// ReSharper disable CheckNamespace

namespace SenseNet.Packaging.Steps
{
    public class InstallInitialData : Step
    {
        public string ConnectionName { get; set; }
        public string DataSource { get; set; }
        public string InitialCatalogName { get; set; }
        public InitialCatalog InitialCatalog { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public override void Execute(ExecutionContext context)
        {
            var connectionInfo = new ConnectionInfo
            {
                ConnectionName = (string) context.ResolveVariable(ConnectionName),
                DataSource = (string) context.ResolveVariable(DataSource),
                InitialCatalog = InitialCatalog,
                InitialCatalogName = (string) context.ResolveVariable(InitialCatalogName),
                UserName = (string) context.ResolveVariable(UserName),
                Password = (string) context.ResolveVariable(Password)
            };
            var connectionString = MsSqlDataContext.GetConnectionString(connectionInfo);

            var initialData = InitialData.Load(new SenseNetServicesInitialData(), null);
            var dataOptions = Options.Create(DataOptions.GetLegacyConfiguration());
            var connOptions = Options.Create(new ConnectionStringOptions
            {
                ConnectionString = connectionString
            });
            
            var installer = new MsSqlDataInstaller(connOptions, NullLoggerFactory.Instance.CreateLogger<MsSqlDataInstaller>());
            var dataProvider = new MsSqlDataProvider(dataOptions, connOptions, installer);            

            installer.InstallInitialDataAsync(initialData, dataProvider, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
