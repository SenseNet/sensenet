using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Services.Core
{
    public class StatTools
    {
        private IStatisticalDataCollector _dataCollector;

        public StatTools(IStatisticalDataCollector dataCollector)
        {
            _dataCollector = dataCollector;
        }

        public WebTransferStatInput RegisterWebRequest(HttpContext httpContext)
        {
            if (_dataCollector == null)
                return null;

            var request = httpContext.Request;
            var url = request.Path + request.QueryString;
            return new WebTransferStatInput
            {
                Url = url,
                RequestTime = DateTime.UtcNow,
                RequestLength = url.Length + (httpContext.Request.ContentLength ?? 0L)
            };
        }

        public void RegisterWebResponse(WebTransferStatInput data, HttpContext httpContext)
        {
            RegisterWebResponse(data, httpContext, 0L);
        }
        public void RegisterWebResponse(WebTransferStatInput data, HttpContext httpContext, long currentLength)
        {
            if (_dataCollector == null)
                return;

            var length = ((long?)httpContext.Items["ResponseLength"] ?? 0L) + (httpContext.Response.ContentLength ?? 0L);

            data.ResponseTime = DateTime.UtcNow;
            data.ResponseStatusCode = httpContext.Response.StatusCode;
            data.ResponseLength = length;

            _dataCollector.RegisterWebTransfer(data);
        }
    }
}
