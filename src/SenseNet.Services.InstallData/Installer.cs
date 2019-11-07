using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.Tools;

namespace SenseNet.Services.InstallData
{
    public static class Installer
    {
        private const string InstallPackageName = "install-services-core.zip";

        public static async Task<IRepositoryBuilder> InstallSenseNetAsync(this IRepositoryBuilder builder, 
            CancellationToken cancellationToken)
        {
            await Packaging.Installer.InstallSenseNetAsync(typeof(Installer).Assembly,
                InstallPackageName,
                builder as RepositoryBuilder, cancellationToken);

            return builder;
        }

        public static IRepositoryBuilder InstallSenseNet(this IRepositoryBuilder builder)
        {
            Packaging.Installer.InstallSenseNetAsync(typeof(Installer).Assembly,
                InstallPackageName,
                builder as RepositoryBuilder, CancellationToken.None).GetAwaiter().GetResult();

            return builder;
        }
    }
}
