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
        public static SenseNetRegistrationBuilder AddSenseNetRegistration(this AuthenticationBuilder appBuilder)
        {
            return AddSenseNetRegistration(appBuilder, null);
        }
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
        public static SenseNetRegistrationBuilder AddProvider(this SenseNetRegistrationBuilder builder, 
            string name, IRegistrationProvider provider)
        {
            builder.Store.Add(name, provider);

            return builder;
        }
    }
}
