using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace SenseNet.WebHooks
{
    /// <summary>
    /// WebHook client implementation for sending webhooks as HTTP requests.
    /// </summary>
    public class HttpWebHookClient : IWebHookClient
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<HttpWebHookClient> _logger;

        public HttpWebHookClient(IHttpClientFactory clientFactory, ILogger<HttpWebHookClient> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task SendAsync(string url, string httpMethod = null, object postData = null, 
            IDictionary<string, string> headers = null, CancellationToken cancel = default)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));

            if (string.IsNullOrEmpty(httpMethod))
                httpMethod = "POST";

            var client = _clientFactory.CreateClient();

            try
            {
                AddWellKnownHeaders(client, headers);

                HttpResponseMessage response;
                switch (httpMethod.ToUpper())
                {
                    case "GET":
                        response = await client.GetAsync(url, cancel).ConfigureAwait(false);
                        break;
                    case "POST":
                        response = await client.PostAsync(url, GetStringContent(postData, headers), cancel)
                            .ConfigureAwait(false);
                        break;
                    case "PUT":
                        response = await client.PutAsync(url, GetStringContent(postData, headers), cancel)
                            .ConfigureAwait(false);
                        break;
                    //case "DELETE":
                    // We cannot use the built-in Delete method because it does not allow sending a body.
                    default:
                        response = await client.SendAsync(GetMessage(url, httpMethod, postData, headers), cancel)
                            .ConfigureAwait(false);
                        break;
                }

                var msg = $"WebHook service request completed with {response.StatusCode}. Url: {url} Http method: {httpMethod}";

                if (response.IsSuccessStatusCode)
                    _logger.LogTrace(msg);
                else
                    _logger.LogWarning(msg);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error during webhook service call. Url: {url} Http method: {httpMethod}");
            }
        }

        private static void AddWellKnownHeaders(HttpClient client, IDictionary<string, string> headers)
        {
            if (headers == null)
                return;

            if (headers.TryGetValue("Authorization", out var authValue))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authValue);
            }
        }

        private static readonly string[] HeadersToSkip = new[] {"Authorization"};

        private static StringContent GetStringContent(object postData, IDictionary<string, string> headers)
        {
            var postText = postData == null ? string.Empty : JsonConvert.SerializeObject(postData);
            var content = new StringContent(postText, Encoding.UTF8, "application/json");

            if (headers?.Any() ?? false)
            {
                foreach (var header in headers.Where(h => !HeadersToSkip.Contains(h.Key)))
                {
                    content.Headers.Add(header.Key, header.Value);
                }
            }

            return content;
        }

        private static HttpRequestMessage GetMessage(string url, string httpMethod, object postData, IDictionary<string, string> headers)
        {
            var message = new HttpRequestMessage(new HttpMethod(httpMethod), url)
            {
                Content = GetStringContent(postData, headers)
            };

            return message;
        }
    }
}
