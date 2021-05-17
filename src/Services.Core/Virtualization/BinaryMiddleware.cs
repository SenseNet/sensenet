using System;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ContentRepository.Storage;

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

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var statService = httpContext.RequestServices.GetService<IStatisticalDataCollector>();
            WebTransferStatInput statData = null;
            if (statService != null)
            {
                var request = httpContext.Request;
                var url = request.Path+ request.QueryString;
                statData = new WebTransferStatInput
                {
                    Url = url,
                    RequestTime = DateTime.UtcNow,
                    RequestLength = url.Length + (httpContext.Request.ContentLength ?? 0L)
                };
            }

            var bh = new BinaryHandler(httpContext);

            await bh.ProcessRequestCore().ConfigureAwait(false);

            if (statService != null)
            {
                statData.ResponseTime = DateTime.UtcNow;
                statData.ResponseStatusCode = httpContext.Response.StatusCode;
                statData.ResponseLength = httpContext.Response.ContentLength ?? 0;
#pragma warning disable 4014
                statService.RegisterWebTransfer(statData);
#pragma warning restore 4014
            }

            // Call next middleware in the chain if exists
            if (_next != null)
                await _next(httpContext).ConfigureAwait(false);
        }
    }
}
