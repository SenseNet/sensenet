using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SenseNet.OData.Formatters;

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

        public static ODataWriter GetODataFormatter(this HttpContext httpContext)
        {
            return httpContext.Items[ODataFormatterKey] as ODataWriter;
        }
        public static void SetODataFormatter(this HttpContext httpContext, ODataWriter value)
        {
            httpContext.Items[ODataFormatterKey] = value;
        }
    }
}
