using Microsoft.Extensions.DependencyInjection;
using SenseNet.Services.Core.Install;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class ServicesExtensions
    {
        /// <summary>
        /// Adds the sensenet installer to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetInstallPackage(this IServiceCollection services)
        {
            return services.AddSenseNetInstallPackage(typeof(Installer).Assembly, Installer.InstallPackageName);
        }
    }
}
