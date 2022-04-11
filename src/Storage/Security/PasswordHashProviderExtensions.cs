using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ContentRepository.Storage.Security;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class PasswordHashProviderExtensions
    {
        public static IServiceCollection AddSenseNetPasswordHashProvider(this IServiceCollection services)
        {
            return services.AddPasswordHashProvider<SenseNetPasswordHashProvider>();
        }
        public static IServiceCollection AddPasswordHashProvider<T>(this IServiceCollection services) where T : class, IPasswordHashProvider
        {
            return services.AddSingleton<IPasswordHashProvider, T>();
        }
        public static IServiceCollection AddPasswordHashProviderForMigration<T>(this IServiceCollection services) where T : class, IPasswordHashProviderForMigration
        {
            return services.AddSingleton<IPasswordHashProviderForMigration, T>();
        }
    }
}
