using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
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

        private IDictionary<string, object> _postProperties;
        public IDictionary<string, object> PostProperties => _postProperties ??= GetPostProperties();

        public string EventName => PostProperties.TryGetValue("eventName", out var en) ? ((JsonElement)en).GetString() : null;

        private IDictionary<string, object> GetPostProperties()
        {
            if (PostData == null)
                return new Dictionary<string, object>();

            var postJson = JsonSerializer.Serialize(PostData);
            var postObject = JsonSerializer.Deserialize<ExpandoObject>(postJson) as IDictionary<string, object>;
            return postObject ?? new Dictionary<string, object>();
        }
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
