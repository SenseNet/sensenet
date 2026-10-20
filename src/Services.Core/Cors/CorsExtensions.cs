using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Services.Core.Cors;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class CorsExtensions
    {
        /// <summary>
        /// Adds cross-origin resource sharing services along with the default sensenet policy
        /// that is based on the allowed domains and other settings in PortalSettings in the repository.
        /// </summary>
        public static IServiceCollection AddSenseNetCors(this IServiceCollection services)
        {
            services.AddCors();
            services.AddTransient<ICorsPolicyProvider, SnCorsPolicyProvider>();

            return services;
        }
        /// <summary>
        /// Adds cross-origin resource sharing services along with the default sensenet policy
        /// that is based on the allowed domains and other settings in PortalSettings in the repository.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="setupAction"></param>
        public static IServiceCollection AddSenseNetCors(this IServiceCollection services, Action<CorsOptions> setupAction)
        {
            services.AddCors(setupAction);
            services.AddTransient<ICorsPolicyProvider, SnCorsPolicyProvider>();

            return services;
        }

        /// <summary>
        /// Adds the CORS middleware to the pipeline with the default sensenet policy.
        /// </summary>
        public static IApplicationBuilder UseSenseNetCors(this IApplicationBuilder app)
        {
            app.UseCors("sensenet");
            return app;
        }
    }
}
