using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.WebHooks.Common
{
    public class HttpWebHookClient : IWebHookClient
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<HttpWebHookClient> _logger;

        public HttpWebHookClient(IHttpClientFactory clientFactory, ILogger<HttpWebHookClient> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public Task CallServiceAsync(string url, CancellationToken cancel = default)
        {
            var client = _clientFactory.CreateClient();

            //client.BaseAddress = 
            //var result = await client.PostAsync()

            throw new NotImplementedException();
        }
    }
}
