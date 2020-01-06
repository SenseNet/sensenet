using SenseNet.ContentRepository;

namespace SenseNet.Services.Core.Install
{
    public static class Installer
    {
        private const string InstallPackageName = "install-services-core.zip";

        /// <summary>
        /// Installs the sensenet Services main component.
        /// </summary>
        /// <param name="installer">Installer instance that contains the <see cref="RepositoryBuilder"/>
        /// preconfigured with all the necessary sensenet providers like data provider and search engine.</param>
        public static Packaging.Installer InstallSenseNet(this Packaging.Installer installer)
        {
            installer.InstallSenseNet(typeof(Installer).Assembly, InstallPackageName);

            return installer;
        }
    }
}
