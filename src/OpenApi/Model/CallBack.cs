﻿using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class CallBack
    {
        [JsonProperty("$ref")] public string Ref { get; set; }
        //TODO: CallBack or reference
    }
}