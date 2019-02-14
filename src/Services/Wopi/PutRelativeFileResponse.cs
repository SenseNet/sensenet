using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SenseNet.Services.Wopi
{
    [JsonObject(MemberSerialization.OptOut)]
    internal class PutRelativeFileResponse : WopiResponse, IWopiObjectResponse
    {
        public string Name { get; internal set; }
        public string Url { get; internal set; } // http://server/<...>/wopi/files/(file_id)?access_token=(access token),
    }
}
