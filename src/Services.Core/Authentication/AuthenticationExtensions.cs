using Microsoft.Extensions.DependencyInjection;
using System;
using SenseNet.Services.Core.Authentication;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    /// <summary>
    /// Custom service builder API for simplifying the addition of registration services.
    /// </summary>
    public class SenseNetRegistrationBuilder
    {
        internal IServiceCollection Services { get; }

        internal SenseNetRegistrationBuilder(IServiceCollection services) 
        {
            Services = services;
        }
    }

    public static class AuthenticationExtensions
    {
        /// <summary>
        /// Adds the sensenet registration feature to the service collection.
        /// </summary>
        /// <param name="services">The IServiceCollection instance.</param>
        /// <param name="configure">Optional configuration method.</param>
        /// <returns>A <see cref="SenseNetRegistrationBuilder"/> instance that lets developers
        /// add custom features to the registration process.</returns>
        public static SenseNetRegistrationBuilder AddSenseNetRegistration(this IServiceCollection services, 
            Action<RegistrationOptions> configure = null)
        {
            services.Configure<RegistrationOptions>(options => { configure?.Invoke(options); });
            services.AddSingleton<DefaultRegistrationProvider>();
            services.AddSingleton<RegistrationProviderStore>();

            // return a feature-specific builder to simplify provider registration
            return new SenseNetRegistrationBuilder(services);
        }
        /// <summary>
        /// Adds a custom registration provider singleton to the service collection.
        /// </summary>
        /// <param name="builder">The <see cref="SenseNetRegistrationBuilder"/> instance.</param>
        /// <returns>A <see cref="SenseNetRegistrationBuilder"/> instance that lets developers
        /// add custom features to the registration process.</returns>
        public static SenseNetRegistrationBuilder AddProvider<T>(this SenseNetRegistrationBuilder builder) where T : class, IRegistrationProvider
        {
            // The RegistrationProviderStore singleton will later resolve these instances when it is requested.
            builder.Services.AddSingleton<IRegistrationProvider, T>();

            return builder;
        }
    }
}
