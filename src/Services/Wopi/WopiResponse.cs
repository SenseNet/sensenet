using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SenseNet.Services.Wopi
{
    [JsonObject(MemberSerialization.OptOut)]
    public class WopiResponse
    {
        [JsonIgnore]
        public HttpStatusCode Status { get; internal set; }

        [JsonIgnore]
        public IDictionary<string, string> Headers { get; internal set; } = new Dictionary<string, string>();
    }
}
