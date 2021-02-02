using Microsoft.AspNetCore.Builder;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Registers a sensenet middleware in the pipeline
        /// if the request contains the provided prefix.
        /// </summary>
        /// <param name="builder">IApplicationBuilder instance.</param>
        /// <param name="predicate">If this predicate returns true, the provided middleware will be added to the pipeline.</param>
        /// <param name="buildAppBranchBefore">Optional builder method. Use this when you want to add
        /// additional middleware in the pipeline before the sensenet middleware.</param>
        /// <param name="buildAppBranchAfter">Optional builder method. Use this when you want to add
        /// additional middleware in the pipeline after the sensenet middleware.</param>
        /// <param name="terminateAppBranch">Whether to add a terminating middleware after
        /// registering the provided middleware in case the condition is true. Default is true.</param>
        public static IApplicationBuilder MapMiddlewareWhen<T>(this IApplicationBuilder builder,
            Func<HttpContext, bool> predicate, 
            Action<IApplicationBuilder> buildAppBranchBefore = null,
            Action<IApplicationBuilder> buildAppBranchAfter = null,
            bool terminateAppBranch = false)
        {
            // add the middleware only if the request contains the appropriate prefix
            builder.MapWhen(predicate, appBranch =>
            {
                buildAppBranchBefore?.Invoke(appBranch);

                appBranch.UseMiddleware<T>();

                // Register a follow-up middleware defined by the caller or set a terminating, empty middleware.
                // If we do not do this, the system will try to set the status code which is not possible as
                // the request may have already been started by the middleware above.

                buildAppBranchAfter?.Invoke(appBranch);

                if (terminateAppBranch)
                    appBranch.Use((context, next) => Task.CompletedTask);
            });

            return builder;
        }

        /// <summary>
        /// Registers a sensenet middleware in the pipeline
        /// if the request contains the provided prefix.
        /// </summary>
        /// <param name="builder">IApplicationBuilder instance.</param>
        /// <param name="pathSegmentStart">The url segment to match for (e.g. /myprefix).</param>
        /// <param name="buildAppBranchBefore">Optional builder method. Use this when you want to add
        /// additional middleware in the pipeline before the sensenet middleware.</param>
        /// <param name="buildAppBranchAfter">Optional builder method. Use this when you want to add
        /// additional middleware in the pipeline after the sensenet middleware.</param>
        /// <param name="terminateAppBranch">Whether to add a terminating middleware after
        /// registering the provided middleware in case the condition is true. Default is true.</param>
        public static IApplicationBuilder MapMiddlewareWhen<T>(this IApplicationBuilder builder,
            string pathSegmentStart, 
            Action<IApplicationBuilder> buildAppBranchBefore = null, 
            Action<IApplicationBuilder> buildAppBranchAfter = null, 
            bool terminateAppBranch = false)
            where T : class
        {
            return MapMiddlewareWhen<T>(builder,
                httpContext => httpContext.Request.Path.StartsWithSegments(pathSegmentStart), 
                buildAppBranchBefore, buildAppBranchAfter, terminateAppBranch);
        }
    }
}
