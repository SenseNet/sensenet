using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
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
        /// Register before authentication/authorization and UseSenseNetFiles so preflight
        /// requests are handled before entering the terminating binary branch.
        /// </summary>
        public static IApplicationBuilder UseSenseNetCors(this IApplicationBuilder app)
        {
            app.Use((context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    // The repository policy is built for the current origin. Caches must
                    // distinguish origins even for denied requests and requests without Origin.
                    var vary = context.Response.Headers.GetCommaSeparatedValues(HeaderNames.Vary);
                    if (!vary.Any(value => value == "*" ||
                        string.Equals(value, HeaderNames.Origin, StringComparison.OrdinalIgnoreCase)))
                        context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.Origin);

                    return Task.CompletedTask;
                });
                return next();
            });
            app.UseCors("sensenet");
            return app;
        }
    }
}
