using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

        public int NodeId => GetPostPropertyInt("nodeId");
        public string Path => GetPostPropertyString("path");
        public string EventName => GetPostPropertyString("eventName");

        public string GetPostPropertyString(string name)
        {
            return PostProperties.TryGetValue(name, out var en) ? (string)en : null;
        }
        public int GetPostPropertyInt(string name)
        {
            return PostProperties.TryGetValue(name, out var en) ? Convert.ToInt32(en) : 0;
        }

        private IDictionary<string, object> GetPostProperties()
        {
            if (PostData == null)
                return new Dictionary<string, object>();

            var postJson = JsonConvert.SerializeObject(PostData);
            var postObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(postJson);

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
