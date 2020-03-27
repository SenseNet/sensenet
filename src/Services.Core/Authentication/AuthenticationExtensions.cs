using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace SenseNet.Services.Core.Authentication
{
    public class SenseNetRegistrationBuilder
    {
        internal AuthenticationBuilder AuthenticationBuilder { get; }
        internal RegistrationProviderStore Store { get; }
        internal SenseNetRegistrationBuilder(AuthenticationBuilder builder, RegistrationProviderStore store) 
        {
            AuthenticationBuilder = builder;
            Store = store;
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
            var options = new RegistrationOptions();
            configure?.Invoke(options);

            var store = new RegistrationProviderStore(options);

            appBuilder.Services.AddSingleton(store);

            // return a feature-specific builder that contains the registered singleton provider store
            return new SenseNetRegistrationBuilder(appBuilder, store);
        }
        /// <summary>
        /// Adds a custom registration provider to the service collection.
        /// </summary>
        /// <param name="builder">The <see cref="SenseNetRegistrationBuilder"/> instance.</param>
        /// <param name="name">Name of the provider that this provider will be 
        /// responsible for (e.g. Google, GitHub).</param>
        /// <param name="provider">Registration provider instance.</param>
        /// <returns>A <see cref="SenseNetRegistrationBuilder"/> instance that lets developers
        /// add custom features to the registration process.</returns>
        public static SenseNetRegistrationBuilder AddProvider(this SenseNetRegistrationBuilder builder, 
            string name, IRegistrationProvider provider)
        {
            builder.Store.Add(name, provider);

            return builder;
        }
    }
}
