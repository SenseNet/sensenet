using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable once CheckNamespace
namespace SenseNet.WebHooks
{
    public class WebHookFilterData
    {
        public string Path { get; set; }
        public bool TriggersForAllEvents { get; set; }
        public ContentTypeFilterData[] ContentTypes { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum WebHookEventType
    {
        All,
        Create,
        Delete,
        Modify,
        Approve,
        Publish,
        Reject,
        CheckIn,
        CheckOut
    }

    public class ContentTypeFilterData
    {
        public string Name { get; set; }

        public WebHookEventType[] Events { get; set; }
    }
}
