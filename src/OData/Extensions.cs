using Microsoft.AspNetCore.Builder;

namespace SenseNet.OData
{
    public static class Extensions
    {
        /// <summary>
        /// Registers the sensenet OData middleware in the pipeline
        /// if the request contains the odata.svc prefix.
        /// </summary>
        public static IApplicationBuilder UseSenseNetOdata(this IApplicationBuilder builder)
        {
            // add OData middleware only if the request contains the appropriate prefix
            builder.MapWhen(httpContext => httpContext.Request.Path.StartsWithSegments("/odata.svc"),
                appBranch => { appBranch.UseMiddleware<ODataMiddleware>(); });

            return builder;
        }
    }
}
