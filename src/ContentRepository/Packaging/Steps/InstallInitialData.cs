using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
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
            using (var ctx = new MsSqlDataContext(connectionInfo))
            {

            }
        }
    }
}
