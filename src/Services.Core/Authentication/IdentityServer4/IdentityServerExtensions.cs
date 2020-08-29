using System;
using Microsoft.Extensions.DependencyInjection;

namespace SenseNet.Services.Core.Authentication.IdentityServer4
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
            Action<SnClientRequestOptions> configure = null)
        {
            services.Configure<SnClientRequestOptions>(options =>
            {
                // add default clients
                options.Clients.Add(new SnIdentityServerClient
                {
                    ClientType = "adminui",
                    ClientId = "spa"
                });
                options.Clients.Add(new SnIdentityServerClient
                {
                    ClientType = "client",
                    ClientId = "client"
                });

                // developers can extend or modify the list here
                configure?.Invoke(options);
            });

            services.AddSingleton<ISnClientRequestParametersProvider, DefaultSnClientRequestParametersProvider>();

            return services;
        }
    }
}
