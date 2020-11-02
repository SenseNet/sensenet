using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Services.Core.Authentication.IdentityServer4;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class IdentityServerExtensions
    {
        /// <summary>
        /// Adds the default IdentityServer client definitions for multi-repository clients.
        /// Developers may modify the default list in the configure parameter method if required.
        /// </summary>
        /// <remarks>
        /// This feature is required only if a client application needs to connect
        /// to multiple repositories. This module contains an OData action that
        /// returns the configured clients to the caller.
        /// </remarks>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="configure">Configure method.</param>
        public static IServiceCollection AddSenseNetIdentityServerClients(this IServiceCollection services, 
            Action<ClientRequestOptions> configure = null)
        {
            services.Configure<ClientRequestOptions>(options =>
            {
                // add default clients
                if (options.Clients.All(c => c.ClientType != "adminui"))
                    options.Clients.Add(new SnIdentityServerClient
                    {
                        ClientType = "adminui",
                        ClientId = "adminui"
                    });
                if (options.Clients.All(c => c.ClientType != "client"))
                    options.Clients.Add(new SnIdentityServerClient
                    {
                        ClientType = "client",
                        ClientId = "client"
                    });

                // developers can extend or modify the list here
                configure?.Invoke(options);
            });

            services.AddClientRequestParametersProvider<DefaultSnClientRequestParametersProvider>();

            return services;
        }

        public static IServiceCollection AddClientRequestParametersProvider<T>(this IServiceCollection services) 
            where T : class, ISnClientRequestParametersProvider
        {
            return services.AddSingleton<ISnClientRequestParametersProvider, T>();
        }
    }
}
