using Microsoft.AspNetCore.Http;

namespace SenseNet.OData
{
    public static class HttpExtensions
    {
        /// <summary>
        /// Adds the OData request object to the items of HttpContext.
        /// </summary>
        internal static void SetODataRequest(this HttpContext httpContext, ODataRequest odataRequest)
        {
            httpContext.Items[ODataMiddleware.ODataRequestHttpContextKey] = odataRequest;
        }
        /// <summary>
        /// Gets the OData request object from the Items collection.
        /// </summary>
        internal static ODataRequest GetODataRequest(this HttpContext httpContext)
        {
            return httpContext.Items[ODataMiddleware.ODataRequestHttpContextKey] as ODataRequest;
        }
    }
}
