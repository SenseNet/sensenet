using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace SenseNet.Services.Wopi
{
    [JsonObject(MemberSerialization.OptOut)]
    public class WopiResponse //UNDONE: internal.
    {
        [JsonIgnore]
        public HttpStatusCode StatusCode { get; internal set; }

        [JsonIgnore]
        public string ContentType
        {
            get { return Headers.TryGetValue("ContentType", out var mimeType) ? mimeType : null; }
            internal set { Headers["ContentType"] = value; }
        }

        [JsonIgnore]
        public IDictionary<string, string> Headers { get; internal set; } = new Dictionary<string, string>();
    }
}
