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
        /// Registers a membership extender singleton instance.
        /// </summary>
        /// <typeparam name="T">An <see cref="IMembershipExtender"/> implementation.</typeparam>
        /// <param name="services">The IServiceCollection instance.</param>
        public static IServiceCollection AddSenseNetMembershipExtender<T>(this IServiceCollection services) where T : class, IMembershipExtender
        {
            services.AddSingleton<IMembershipExtender, T>();
            RepositoryBuilder.WriteLog("MembershipExtender", typeof(T).FullName);

            return services;
        }

        /// <summary>
        /// Registers one or more membership extenders used for extending user membership on-the-fly.
        /// </summary>
        /// <param name="services">The IServiceCollection instance.</param>
        /// <param name="membershipExtender">One or more <see cref="IMembershipExtender"/> instances.</param>
        public static IServiceCollection AddSenseNetMembershipExtenders(this IServiceCollection services, params IMembershipExtender[] membershipExtender)
        {
            if(membershipExtender == null)
                throw new ArgumentNullException(nameof(membershipExtender));
            if (membershipExtender.Length == 0)
                throw new ArgumentException($"The {nameof(membershipExtender)} cannot be empty.");

            // register instances with the interface
            foreach (var item in membershipExtender)
            {
                services.AddSingleton(item);
                RepositoryBuilder.WriteLog("MembershipExtender", item);
            }

            return services;
        }

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
                                return e.GetExtension(user, context).ExtensionIds;
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
