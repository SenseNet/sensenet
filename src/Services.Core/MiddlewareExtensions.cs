using Microsoft.AspNetCore.Builder;
using System;

namespace SenseNet.Services.Core
{
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Registers a sensenet middleware in the pipeline
        /// if the request contains the provided prefix.
        /// </summary>
        /// <param name="builder">IApplicationBuilder instance.</param>
        /// <param name="pathSegmentStart">The url segment to match for (e.g. /myprefix).</param>
        /// <param name="buildAppBranch">Optional builder method. Use this when you want to add
        /// additional middleware in the pipeline after the sensenet middleware.</param>
        public static IApplicationBuilder MapMiddlewareWhen<T>(this IApplicationBuilder builder,
            string pathSegmentStart, Action<IApplicationBuilder> buildAppBranch = null) where T: class
        {
            // add the middleware only if the request contains the appropriate prefix
            builder.MapWhen(httpContext => httpContext.Request.Path.StartsWithSegments(pathSegmentStart),
                appBranch =>
                {
                    appBranch.UseMiddleware<T>();

                    // Register a follow-up middleware defined by the caller or set a terminating, empty middleware.
                    // If we do not do this, the system will try to set the status code which is not possible as
                    // the request may have already been started by the middleware above.

                    if (buildAppBranch != null)
                        buildAppBranch.Invoke(appBranch);
                    else
                        appBranch.Use((context, next) => System.Threading.Tasks.Task.CompletedTask);
                });

            return builder;
        }
    }
}
