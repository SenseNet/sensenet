using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.WebHooks
{
    /// <summary>
    /// WebHook client implementation for sending webhooks as HTTP requests.
    /// </summary>
    public class HttpWebHookClient : IWebHookClient
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IStatisticalDataCollector _statCollector;
        private readonly ILogger<HttpWebHookClient> _logger;

        public HttpWebHookClient(IHttpClientFactory clientFactory, IStatisticalDataCollector statCollector, ILogger<HttpWebHookClient> logger)
        {
            _clientFactory = clientFactory;
            _statCollector = statCollector;
            _logger = logger;
        }

        public async Task SendAsync(string url, string eventName, int contentId, int subscriptionId, string httpMethod = null,
            object postData = null, IDictionary<string, string> headers = null, CancellationToken cancel = default)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));

            if (string.IsNullOrEmpty(httpMethod))
                httpMethod = "POST";

            var client = _clientFactory.CreateClient();

            var statData = postData == null
                ? new WebHookStatInput
                {
                    Url = url,
                    RequestTime = DateTime.UtcNow,
                }
                : new WebHookStatInput
                {
                    Url = url,
                    RequestTime = DateTime.UtcNow,
                    ContentId = contentId,
                    WebHookId = subscriptionId,
                    EventName = eventName,
                };
            

            try
            {
                AddWellKnownHeaders(client, headers);

                HttpResponseMessage response;
                StringContent stringContent;
                long length;
                switch (httpMethod.ToUpper())
                {
                    case "GET":
                        statData.RequestLength = url.Length;
                        response = await client.GetAsync(url, cancel).ConfigureAwait(false);
                        break;
                    case "POST":
                        stringContent = GetStringContent(postData, headers, out length);
                        statData.RequestLength = length + url.Length;
                        response = await client.PostAsync(url, stringContent, cancel)
                            .ConfigureAwait(false);
                        break;
                    case "PUT":
                        stringContent = GetStringContent(postData, headers, out length);
                        statData.RequestLength = length + url.Length;
                        response = await client.PutAsync(url, stringContent, cancel)
                            .ConfigureAwait(false);
                        break;
                    //case "DELETE":
                    // We cannot use the built-in Delete method because it does not allow sending a body.
                    default:
                        var message = GetMessage(url, httpMethod, postData, headers, out length);
                        statData.RequestLength = length + url.Length;
                        response = await client.SendAsync(message, cancel)
                            .ConfigureAwait(false);
                        break;
                }

                statData.ResponseStatusCode = (int)response.StatusCode;
                statData.ResponseTime = DateTime.UtcNow;
#pragma warning disable 4014
                _statCollector?.RegisterWebHook(statData);
#pragma warning restore 4014

                var msg = $"WebHook service request completed with {response.StatusCode}. Url: {url} Http method: {httpMethod}";

                if (response.IsSuccessStatusCode)
                    _logger.LogTrace(msg);
                else
                    _logger.LogWarning(msg);
            }
            catch (Exception ex)
            {
                statData.ResponseStatusCode = 500;
                statData.ErrorMessage = GetErrorMessage(ex);
                _logger.LogWarning(ex, $"Error during webhook service call. Url: {url} Http method: {httpMethod}");
            }
        }

        private string GetErrorMessage(Exception exception)
        {
            var messages = new List<string>();
            while (exception != null)
            {
                messages.Add(exception.Message);
                exception = exception.InnerException;
            }
            return string.Join(". Inner exception: ", messages);
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

        private static StringContent GetStringContent(object postData, IDictionary<string, string> headers, out long size)
        {
            var postText = postData == null ? string.Empty : JsonConvert.SerializeObject(postData);
            size = postText.Length;
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

        private static HttpRequestMessage GetMessage(string url, string httpMethod, object postData,
            IDictionary<string, string> headers, out long length)
        {
            var message = new HttpRequestMessage(new HttpMethod(httpMethod), url)
            {
                Content = GetStringContent(postData, headers, out length)
            };

            return message;
        }
    }
}
