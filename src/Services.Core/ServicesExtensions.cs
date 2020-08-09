using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Services.Core;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class ServicesExtensions
    {
        /// <summary>
        /// Registers a membership extender type as a scoped service. To execute extenders at runtime,
        /// please call the <see cref="UseSenseNetMembershipExtenders"/> method in Startup.Configure.
        /// </summary>
        /// <typeparam name="T">An <see cref="IMembershipExtender"/> implementation.</typeparam>
        /// <param name="services">The IServiceCollection instance.</param>
        public static IServiceCollection AddSenseNetMembershipExtender<T>(this IServiceCollection services) where T : class, IMembershipExtender
        {
            services.AddScoped<IMembershipExtender, T>();
            RepositoryBuilder.WriteLog("MembershipExtender", typeof(T).FullName);

            return services;
        }

        /// <summary>
        /// Registers a middleware in the pipeline to execute previously configured membership extenders.
        /// To register an extender, please call <see cref="AddSenseNetMembershipExtender{T}"/> in the
        /// ConfigureServices method of your Startup class.
        /// </summary>
        /// <param name="builder">The IApplicationBuilder instance.</param>
        public static IApplicationBuilder UseSenseNetMembershipExtenders(this IApplicationBuilder builder)
        {
            builder.Use(async (context, next) =>
            {
                var user = User.Current;

                // get all extenders registered with the interface
                var extenders = context.RequestServices.GetServices<IMembershipExtender>();
                if (extenders != null)
                {
                    var extensions = extenders
                        .SelectMany(e =>
                        {
                            try
                            {
                                return e.GetExtension(user).ExtensionIds;
                            }
                            catch (Exception ex)
                            {
                                SnTrace.System.WriteError($"Error executing membership extender {e.GetType().FullName} " +
                                                          $"for user {User.Current.Username}. {ex.Message}");
                            }

                            return Array.Empty<int>();
                        })
                        .Distinct()
                        .ToArray();

                    user.AddMembershipIdentities(extensions);
                }

                /* -------------- */
                if (next != null)
                    await next();
            });

            return builder; 
        }

        /// <summary>
        /// Switches the ResponseLengthLimiter feature on.
        /// </summary>
        /// <param name="builder">The IRepositoryBuilder instance.</param>
        /// <param name="maxResponseLengthInBytes">Response length limit value in bytes (optional).</param>
        /// <param name="maxFileLengthInBytes">File length limit value in bytes (optional).</param>
        /// <returns>The IRepositoryBuilder instance.</returns>
        public static IRepositoryBuilder UseResponseLimiter(this IRepositoryBuilder builder,
            long maxResponseLengthInBytes = 0, long maxFileLengthInBytes = 0)
        {
            Providers.Instance.SetProvider(typeof(IResponseLimiter),
                new SnResponseLimiter(maxResponseLengthInBytes, maxFileLengthInBytes));

            return builder;
        }
    }
}
