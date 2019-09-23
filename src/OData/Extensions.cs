using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SenseNet.OData
{
    public static class Extensions
    {
        public static readonly string ODataRequestKey = "SnODataRequest";
        public static readonly string ODataResponseKey = "SnODataResponse";
        public static readonly string ODataFormatterKey = "SnODataFormatter";

        public static IApplicationBuilder UseSenseNetOdata(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ODataMiddleware>();
        }

        public static ODataRequest GetODataRequest(this HttpContext httpContext)
        {
            return httpContext.Items[ODataRequestKey] as ODataRequest;
        }
        public static void SetODataRequest(this HttpContext httpContext, ODataRequest value)
        {
            httpContext.Items[ODataRequestKey] = value;
        }

        public static ODataResponse GetODataResponse(this HttpContext httpContext)
        {
            return httpContext.Items[ODataResponseKey] as ODataResponse;
        }
        public static void SetODataResponse(this HttpContext httpContext, ODataResponse value)
        {
            httpContext.Items[ODataResponseKey] = value;
        }

        public static ODataFormatter GetODataFormatter(this HttpContext httpContext)
        {
            return httpContext.Items[ODataFormatterKey] as ODataFormatter;
        }
        public static void SetODataFormatter(this HttpContext httpContext, ODataFormatter value)
        {
            httpContext.Items[ODataFormatterKey] = value;
        }
    }
}
