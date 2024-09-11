using Microsoft.Extensions.DependencyInjection;
using SenseNet.ContentRepository.Storage.Security;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class PasswordHashProviderExtensions
    {
        /// <summary>
        /// Adds the default password hash provider to the service collection. 
        /// </summary>
        public static IServiceCollection AddSenseNetPasswordHashProvider(this IServiceCollection services)
        {
            return services.AddPasswordHashProvider<SenseNetPasswordHashProvider>();
        }
        /// <summary>
        /// Adds a custom password hash provider to the service collection.
        /// Use this method when the default implementation
        /// (<c>SenseNet.ContentRepository.Storage.Security.SenseNetPasswordHashProvider</c>) needs to be replaced.
        /// </summary>
        public static IServiceCollection AddPasswordHashProvider<T>(this IServiceCollection services) where T : class, IPasswordHashProvider
        {
            return services.AddSingleton<IPasswordHashProvider, T>();
        }

        /// <summary>
        /// Adds a custom password hash provider for migration to the service collection.
        /// For internal use only.
        /// </summary>
        public static IServiceCollection AddPasswordHashProviderForMigration<T>(this IServiceCollection services) where T : class, IPasswordHashProviderForMigration
        {
            return services.AddSingleton<IPasswordHashProviderForMigration, T>();
        }
    }
}
