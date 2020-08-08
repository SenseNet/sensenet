using System;
using System.Collections.Generic;
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
        /// Sets the membership extender used for extending user membership on-the-fly.
        /// </summary>
        /// <param name="services">The IServiceCollection instance.</param>
        /// <param name="membershipExtender">One or more <see cref="IMembershipExtender"/> instance.</param>
        public static IServiceCollection AddSenseNetMembershipExtender(this IServiceCollection services, params IMembershipExtender[] membershipExtender)
        {
            if(membershipExtender == null)
                throw new ArgumentNullException(nameof(membershipExtender));
            if (membershipExtender.Length == 0)
                throw new ArgumentException($"The {nameof(membershipExtender)} cannot be empty.");

            var service = services.FirstOrDefault(x => x.ServiceType == typeof(List<IMembershipExtender>));
            if (service == null)
            {
                services.AddSingleton(typeof(List<IMembershipExtender>), membershipExtender.ToList());
            }
            else
            {
                var extenders = (List<IMembershipExtender>)service.ImplementationInstance;
                extenders.AddRange(membershipExtender);
            }

            foreach (var item in membershipExtender)
                RepositoryBuilder.WriteLog("MembershipExtender", item);

            return services;
        }

        public static IApplicationBuilder UseSenseNetMembershipExtenders(this IApplicationBuilder builder)
        {
            builder.Use(async (context, next) =>
            {
                var user = User.Current;
                var extenders = context.RequestServices.GetService<List<IMembershipExtender>>();
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
