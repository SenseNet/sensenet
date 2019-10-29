using System;
using Microsoft.AspNetCore.Builder;

namespace SenseNet.OData
{
    public static class Extensions
    {
        /// <summary>
        /// Registers the sensenet OData middleware in the pipeline
        /// if the request contains the odata.svc prefix.
        /// </summary>
        /// <param name="builder">IApplicationBuilder instance.</param>
        /// <param name="buildAppBranch">Optional builder method. Use this when you want to add
        /// additional middleware in the pipeline after the sensenet OData middleware.</param>
        public static IApplicationBuilder UseSenseNetOdata(this IApplicationBuilder builder, 
            Action<IApplicationBuilder> buildAppBranch = null)
        {
            // add OData middleware only if the request contains the appropriate prefix
            builder.MapWhen(httpContext => httpContext.Request.Path.StartsWithSegments("/odata.svc"),
                appBranch =>
                {
                    appBranch.UseMiddleware<ODataMiddleware>();

                    buildAppBranch?.Invoke(appBranch);
                });

            return builder;
        }
    }
}
