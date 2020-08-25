using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace SenseNet.Services.Core.Authentication
{
    public class SenseNetRegistrationBuilder
    {
        internal AuthenticationBuilder AuthenticationBuilder { get; }

        internal SenseNetRegistrationBuilder(AuthenticationBuilder builder) 
        {
            AuthenticationBuilder = builder;
        }
    }

    public static class AuthenticationExtensions
    {
        /// <summary>
        /// Adds the sensenet registration feature to the service collection.
        /// </summary>
        /// <param name="appBuilder">AuthenticationBuilder instance.</param>
        /// <returns>A <see cref="SenseNetRegistrationBuilder"/> instance that lets developers
        /// add custom features to the registration process.</returns>
        public static SenseNetRegistrationBuilder AddSenseNetRegistration(this AuthenticationBuilder appBuilder)
        {
            return AddSenseNetRegistration(appBuilder, null);
        }
        /// <summary>
        /// Adds the sensenet registration feature to the service collection.
        /// </summary>
        /// <param name="appBuilder">AuthenticationBuilder instance.</param>
        /// <param name="configure">Optional configuration method.</param>
        /// <returns>A <see cref="SenseNetRegistrationBuilder"/> instance that lets developers
        /// add custom features to the registration process.</returns>
        public static SenseNetRegistrationBuilder AddSenseNetRegistration(this AuthenticationBuilder appBuilder, 
            Action<RegistrationOptions> configure)
        {
            appBuilder.Services.Configure<RegistrationOptions>(options => { configure?.Invoke(options); });
            appBuilder.Services.AddSingleton<DefaultRegistrationProvider>();
            appBuilder.Services.AddSingleton<RegistrationProviderStore>();

            // return a feature-specific builder to simplify provider registration
            return new SenseNetRegistrationBuilder(appBuilder);
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
            builder.AuthenticationBuilder.Services.AddSingleton<IRegistrationProvider, T>();

            return builder;
        }
    }
}
