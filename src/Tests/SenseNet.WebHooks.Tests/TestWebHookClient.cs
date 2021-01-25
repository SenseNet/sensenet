using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.WebHooks.Tests
{
    internal class WebHookRequest
    {
        public string Url { get; set; }
        public string HttpMethod { get; set; }
        public object PostData { get; set; }
        public IDictionary<string, string> Headers { get; set; }
    }

    internal class TestWebHookClient : IWebHookClient
    {
        public IList<WebHookRequest> Requests { get; } = new List<WebHookRequest>();

        public Task SendAsync(string url, string httpMethod = "POST", object postData = null, 
            IDictionary<string, string> headers = null, CancellationToken cancel = default)
        {
            Requests.Add(new WebHookRequest
            {
                Url = url,
                HttpMethod = httpMethod,
                PostData = postData,
                Headers = headers == null ? null : new Dictionary<string, string>(headers)
            });

            return Task.CompletedTask;
        }
    }
}
