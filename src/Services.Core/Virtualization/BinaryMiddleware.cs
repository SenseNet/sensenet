using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SenseNet.Services.Core.Virtualization
{
    /// <summary>
    /// ASP.NET Core middleware to process binary requests.
    /// </summary>
    public class BinaryMiddleware
    {
        private readonly RequestDelegate _next;
        
        public BinaryMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, WebTransferRegistrator statistics)
        {
            var statData = statistics?.RegisterWebRequest(httpContext);

            var bh = new BinaryHandler(httpContext);

            await bh.ProcessRequestCore().ConfigureAwait(false);

            statistics?.RegisterWebResponse(statData, httpContext);

            // Call next middleware in the chain if exists
            if (_next != null)
                await _next(httpContext).ConfigureAwait(false);
        }
    }
}
