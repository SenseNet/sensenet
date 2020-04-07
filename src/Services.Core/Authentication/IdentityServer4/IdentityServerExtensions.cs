using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace SenseNet.Services.Core.Authentication.IdentityServer4
{
    public static class IdentityServerExtensions
    {
        /// <summary>
        /// Adds custom IdentityServer client definitions for multi-repository clients.
        /// If not sure, use the <see cref="AddDefaultSenseNetIdentityServerClients"/> method instead.        
        /// </summary>
        /// <remarks>
        /// This method does not add the default client, developers need to add it
        /// manually if necessary.
        /// </remarks>
        /// <param name="appBuilder">The <see cref="AuthenticationBuilder"/> instance.</param>
        /// <param name="configureOptions">Configure method.</param>
        public static AuthenticationBuilder AddSenseNetIdentityServerClients(this AuthenticationBuilder appBuilder, 
            Action<SnClientRequestOptions> configureOptions)
        {
            var options = new SnClientRequestOptions();
            configureOptions(options);

            appBuilder.Services.AddSingleton(typeof(ISnClientRequestParametersProvider), 
                new DefaultSnClientRequestParametersProvider(options));

            return appBuilder;
        }

        /// <summary>
        /// Adds the default admin UI client definition for multi-repository 
        /// clients with a client id 'spa'.
        /// </summary>
        /// <remarks>
        /// This feature is required only if a client application needs to connect
        /// to multiple repositories. This module contains an OData action that
        /// returns the configured clients to the caller.
        /// </remarks>
        /// <param name="appBuilder">The <see cref="AuthenticationBuilder"/> instance.</param>
        /// <param name="authority">Url of the authority, usually an IdentityServer service.</param>
        public static AuthenticationBuilder AddDefaultSenseNetIdentityServerClients(this AuthenticationBuilder appBuilder,
            string authority)
        {
            if (string.IsNullOrEmpty(authority))
                throw new ArgumentNullException(nameof(authority));

            appBuilder.AddSenseNetIdentityServerClients(options =>
            {
                options.Authority = authority;
                options.Clients.Add(new SnIdentityServerClient
                {
                    ClientType = "adminui",
                    ClientId = "spa"
                });
            });

            return appBuilder;
        }
    }
}
