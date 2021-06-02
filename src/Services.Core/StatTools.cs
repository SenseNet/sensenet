using System;
using Microsoft.AspNetCore.Http;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core
{
    public class StatTools
    {
        private readonly IStatisticalDataCollector _dataCollector;

        public StatTools(IStatisticalDataCollector dataCollector)
        {
            _dataCollector = dataCollector;
        }

        public WebTransferStatInput RegisterWebRequest(HttpContext httpContext)
        {
            if (_dataCollector == null)
                return null;

            var request = httpContext.Request;
            string url = request.Path /*+ request.QueryString*/;
            return new WebTransferStatInput
            {
                Url = url,
                HttpMethod = request.Method,
                RequestTime = DateTime.UtcNow,
                RequestLength = url.Length + (httpContext.Request.ContentLength ?? 0L)
            };
        }

        public void RegisterWebResponse(WebTransferStatInput data, HttpContext httpContext)
        {
            RegisterWebResponse(data, httpContext, httpContext.Response.ContentLength ?? 0L);
        }
        public void RegisterWebResponse(WebTransferStatInput data, HttpContext httpContext, long responseLength)
        {
            if (_dataCollector == null)
                return;

            data.ResponseTime = DateTime.UtcNow;
            data.ResponseStatusCode = httpContext.Response.StatusCode;
            data.ResponseLength = responseLength;

            _dataCollector.RegisterWebTransfer(data);
        }
    }
}
