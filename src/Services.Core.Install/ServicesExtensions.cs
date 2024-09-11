using Microsoft.Extensions.DependencyInjection;
using SenseNet.Services.Core.Install;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class ServicesExtensions
    {
        /// <summary>
        /// Adds the sensenet installer to the service collection.
        /// This allows sensenet to install its default content repository database
        /// automatically when the application starts.
        /// </summary>
        public static IServiceCollection AddSenseNetInstallPackage(this IServiceCollection services)
        {
            return services.AddSenseNetInstallPackage(typeof(Installer).Assembly, Installer.InstallPackageName);
        }
    }
}
