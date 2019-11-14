using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.Tools;

namespace SenseNet.Services.Core.Install
{
    public static class Installer
    {
        private const string InstallPackageName = "install-services-core.zip";

        /// <summary>
        /// Installs the sensenet Services main component.
        /// </summary>
        /// <param name="builder">Repository builder used by the install process to start the repository.
        /// It should be preconfigured with all the necessary sensenet providers like data provider
        /// and search engine.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        public static async Task<IRepositoryBuilder> InstallSenseNetAsync(this IRepositoryBuilder builder, 
            CancellationToken cancellationToken)
        {
            await Packaging.Installer.InstallSenseNetAsync(typeof(Installer).Assembly,
                InstallPackageName,
                builder as RepositoryBuilder, cancellationToken).ConfigureAwait(false);

            return builder;
        }

        /// <summary>
        /// Installs the sensenet Services main component.
        /// </summary>
        /// <param name="builder">Repository builder used by the install process to start the repository.
        /// It should be preconfigured with all the necessary sensenet providers like data provider
        /// and search engine.</param>
        public static IRepositoryBuilder InstallSenseNet(this IRepositoryBuilder builder)
        {
            Packaging.Installer.InstallSenseNetAsync(typeof(Installer).Assembly,
                InstallPackageName,
                builder as RepositoryBuilder, CancellationToken.None).GetAwaiter().GetResult();

            return builder;
        }
    }
}
