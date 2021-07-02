using System;
using Microsoft.AspNetCore.Http;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics
{
    public class WebTransferRegistrator
    {
        private readonly IStatisticalDataCollector _dataCollector;

        public WebTransferRegistrator(IStatisticalDataCollector dataCollector)
        {
            _dataCollector = dataCollector;
        }

        public WebTransferStatInput RegisterWebRequest(HttpContext httpContext)
        {
            if (_dataCollector == null)
                return null;

            var request = httpContext.Request;
            string resourcePath = request.Path; // Do not store querystring but calculate it's length
            return new WebTransferStatInput
            {
                Url = resourcePath,
                HttpMethod = request.Method,
                RequestTime = DateTime.UtcNow,
                RequestLength = GetRequestLength(request)
            };
        }

        private long GetRequestLength(HttpRequest request)
        {
            return (request.Path.Value?.Length ?? 0L) +
                   request.Method.Length +
                   (request.QueryString.Value?.Length ?? 0L) +
                   (request.ContentLength ?? 0L) +
                   GetCookiesLength(request.Cookies) +
                   GetHeadersLength(request.Headers);
        }
        private long GetCookiesLength(IRequestCookieCollection requestCookies)
        {
            var sum = 0L;
            foreach (var cookie in requestCookies)
                sum += cookie.Key.Length + (cookie.Value?.Length ?? 0);
            return sum;
        }
        private long GetHeadersLength(IHeaderDictionary headers)
        {
            var sum = 0L;
            foreach (var header in headers)
            {
                sum += header.Key.Length;
                foreach (var stringValue in header.Value)
                    sum += stringValue.Length;
            }
            return sum;
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
            data.ResponseLength = responseLength +
                                  GetHeadersLength(httpContext.Response.Headers);

            _dataCollector.RegisterWebTransfer(data, httpContext.RequestAborted);
        }
    }
}
