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
        public static IServiceCollection AddSenseNetCors(this IServiceCollection services)
        {
            services.AddCors();
            services.AddTransient<ICorsPolicyProvider, SnCorsPolicyProvider>();

            return services;
        }
        public static IServiceCollection AddSenseNetCors(this IServiceCollection services, Action<CorsOptions> setupAction)
        {
            services.AddCors(setupAction);
            services.AddTransient<ICorsPolicyProvider, SnCorsPolicyProvider>();

            return services;
        }

        public static IApplicationBuilder UseSenseNetCors(this IApplicationBuilder app)
        {
            app.UseCors("sensenet");
            return app;
        }
    }
}
